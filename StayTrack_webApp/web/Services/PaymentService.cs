using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service managing rent payments, proof-of-payment signed URLs, and payment verification.
/// All calls carry the signed-in manager's JWT access token for Supabase RLS.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly ISupabaseAuthService _authService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        ISupabaseAuthService authService,
        ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _authService = authService;
        _logger = logger;
    }

    private SupabaseOptions Options => _optionsMonitor.CurrentValue;

    private string CleanBaseUrl => (Options.Url ?? string.Empty).Trim().TrimEnd('/');

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string pathAndQuery)
    {
        var token = await _authService.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
        }

        var url = $"{CleanBaseUrl}/rest/v1/{pathAndQuery.TrimStart('/')}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", Options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<List<Payment>> GetPaymentsAsync(string? status = null, string? search = null)
    {
        try
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                filter = $"&status=eq.{status.Trim().ToLowerInvariant()}";
            }

            var query = $"payment?select=*,tenant:tenant_id(*,profiles:user_id(*),room:room_id(*,property:property_id(*))),verified_by_profile:verified_by(*){filter}&order=payment_date.desc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback query if embedding names differ
                var fallback = $"payment?select=*,tenant(*,profiles(*),room(*,property(*))),profiles(*){filter}&order=payment_date.desc";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to query payments. Status: {StatusCode}", response.StatusCode);
                return new List<Payment>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var payments = JsonSerializer.Deserialize<List<Payment>>(content) ?? new List<Payment>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                payments = payments.Where(p =>
                    (p.Tenant?.Profile?.FullName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (p.Tenant?.StudentNumber?.ToLowerInvariant().Contains(q) ?? false) ||
                    p.Reference.ToLowerInvariant().Contains(q) ||
                    (p.Tenant?.PaymentReference?.ToLowerInvariant().Contains(q) ?? false) ||
                    (p.Tenant?.Room?.RoomNumber?.ToLowerInvariant().Contains(q) ?? false)
                ).ToList();
            }

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payments");
            return new List<Payment>();
        }
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        try
        {
            var query = $"payment?payment_id=eq.{paymentId}&select=*,tenant:tenant_id(*,profiles:user_id(*),room:room_id(*,property:property_id(*))),verified_by_profile:verified_by(*)";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = $"payment?payment_id=eq.{paymentId}&select=*,tenant(*,profiles(*),room(*,property(*))),profiles(*)";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Payment>>(content);
            return list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<string?> GetProofSignedUrlAsync(string? proofUrlPath)
    {
        if (string.IsNullOrWhiteSpace(proofUrlPath))
        {
            return null;
        }

        // If it's already an absolute URL
        if (proofUrlPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            proofUrlPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return proofUrlPath;
        }

        try
        {
            var token = await _authService.GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            var cleanPath = proofUrlPath.Trim().TrimStart('/');
            string bucket = "proof-of-payment";

            if (cleanPath.StartsWith("proof-of-payment/", StringComparison.OrdinalIgnoreCase))
            {
                bucket = "proof-of-payment";
                cleanPath = cleanPath.Substring("proof-of-payment/".Length);
            }
            else if (cleanPath.StartsWith("payment-proofs/", StringComparison.OrdinalIgnoreCase))
            {
                bucket = "payment-proofs";
                cleanPath = cleanPath.Substring("payment-proofs/".Length);
            }
            else if (cleanPath.Contains('/'))
            {
                var slashIdx = cleanPath.IndexOf('/');
                var possibleBucket = cleanPath.Substring(0, slashIdx);
                var remainder = cleanPath.Substring(slashIdx + 1);
                if (!string.IsNullOrWhiteSpace(possibleBucket) && !string.IsNullOrWhiteSpace(remainder))
                {
                    bucket = possibleBucket;
                    cleanPath = remainder;
                }
            }

            // Supabase Storage Sign Endpoint:
            // POST /storage/v1/object/sign/{bucket}/{path}
            var url = $"{CleanBaseUrl}/storage/v1/object/sign/{bucket}/{cleanPath}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", Options.AnonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new { expiresIn = 3600 }; // 1-hour validity
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            // Fallback bucket check if proof-of-payment failed and it wasn't payment-proofs
            if (!response.IsSuccessStatusCode && bucket == "proof-of-payment")
            {
                var fallbackUrl = $"{CleanBaseUrl}/storage/v1/object/sign/payment-proofs/{cleanPath}";
                using var fallbackReq = new HttpRequestMessage(HttpMethod.Post, fallbackUrl);
                fallbackReq.Headers.Add("apikey", Options.AnonKey);
                fallbackReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                fallbackReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var fallbackResp = await _httpClient.SendAsync(fallbackReq);
                if (fallbackResp.IsSuccessStatusCode)
                {
                    response = fallbackResp;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to generate signed URL for proof: {Path}. Status: {StatusCode}, Error: {Error}", cleanPath, response.StatusCode, err);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("signedURL", out var signedUrlElem))
            {
                var rel = signedUrlElem.GetString();
                if (string.IsNullOrEmpty(rel)) return null;

                if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return rel;
                }

                // Supabase Storage returns "/object/sign/...", which must be prefixed with "/storage/v1"
                var finalPath = rel.StartsWith("/") ? rel : "/" + rel;
                if (!finalPath.StartsWith("/storage/v1/", StringComparison.OrdinalIgnoreCase))
                {
                    finalPath = "/storage/v1" + finalPath;
                }

                return $"{CleanBaseUrl}{finalPath}";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception generating signed URL for proof: {Path}", proofUrlPath);
            return null;
        }
    }

    public async Task<Tenant?> FindTenantByReferenceAsync(string paymentReference)
    {
        if (string.IsNullOrWhiteSpace(paymentReference)) return null;

        try
        {
            var cleanRef = Uri.EscapeDataString(paymentReference.Trim());
            var query = $"tenant?payment_reference=eq.{cleanRef}&select=*,profiles:user_id(*),room:room_id(*,property:property_id(*))";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = $"tenant?payment_reference=eq.{cleanRef}&select=*,profiles(*),room(*,property(*))";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tenants = JsonSerializer.Deserialize<List<Tenant>>(content);
            return tenants?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding tenant by reference {Ref}", paymentReference);
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdatePaymentStatusAsync(int paymentId, string newStatus, Guid verifiedByUserId)
    {
        try
        {
            var payload = new
            {
                status = newStatus.Trim().ToLowerInvariant(),
                verified_by = verifiedByUserId,
                verified_at = DateTime.UtcNow.ToString("o")
            };

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"payment?payment_id=eq.{paymentId}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not update payment status: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId} status", paymentId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RecordPaymentAsync(Payment payment, Guid verifiedByUserId)
    {
        try
        {
            var payload = new
            {
                tenant_id = payment.TenantId,
                amount = payment.Amount,
                payment_date = payment.PaymentDate.ToString("yyyy-MM-dd"),
                period_month = payment.PeriodMonth.ToString("yyyy-MM-01"), // First day of the month
                reference = payment.Reference.Trim(),
                proof_url = (string?)null,
                status = "approved",
                verified_by = verifiedByUserId,
                verified_at = DateTime.UtcNow.ToString("o")
            };

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, "payment");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not record payment: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment");
            return (false, ex.Message);
        }
    }

    public async Task<(int Total, int Pending, int Approved, int Rejected)> GetPaymentCountsAsync()
    {
        try
        {
            var query = "payment?select=status";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return (0, 0, 0, 0);
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<PaymentStatusItem>>(content) ?? new List<PaymentStatusItem>();

            var total = items.Count;
            var pending = items.Count(p => p.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
            var approved = items.Count(p => p.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
            var rejected = items.Count(p => p.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase));

            return (total, pending, approved, rejected);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private class PaymentStatusItem
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}