using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;
using StayTrack.Models.ViewModels;

namespace StayTrack.Services;

/// <summary>
/// Service generating live on-demand monthly rental reports and CSV exports.
/// Calculates status per tenant directly from payment records without persistent report tables.
/// All queries carry the signed-in manager's JWT token for Supabase RLS.
/// </summary>
public class ReportService : IReportService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly ISupabaseAuthService _authService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        ISupabaseAuthService authService,
        ILogger<ReportService> logger)
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

    public async Task<MonthlyReportViewModel> GenerateMonthlyReportAsync(DateTime month)
    {
        var periodMonth = new DateTime(month.Year, month.Month, 1);
        var periodMonthStr = periodMonth.ToString("yyyy-MM-01");

        var report = new MonthlyReportViewModel
        {
            SelectedMonth = periodMonth
        };

        try
        {
            // 1. Fetch active tenants with profile and room/property
            var tenantQuery = "tenant?status=eq.active&select=*,profiles:user_id(*),room:room_id(*,property:property_id(*))&order=room:room_id(room_number).asc";
            using var tenantReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, tenantQuery);
            var tenantRes = await _httpClient.SendAsync(tenantReq);

            if (!tenantRes.IsSuccessStatusCode)
            {
                var fallback = "tenant?status=eq.active&select=*,profiles(*),room(*,property(*))";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                tenantRes = await _httpClient.SendAsync(fallbackReq);
            }

            var tenants = new List<Tenant>();
            if (tenantRes.IsSuccessStatusCode)
            {
                var content = await tenantRes.Content.ReadAsStringAsync();
                tenants = JsonSerializer.Deserialize<List<Tenant>>(content) ?? new List<Tenant>();
            }

            // 2. Fetch payments for this period month
            var paymentQuery = $"payment?period_month=eq.{periodMonthStr}&select=*";
            using var payReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, paymentQuery);
            var payRes = await _httpClient.SendAsync(payReq);

            var payments = new List<Payment>();
            if (payRes.IsSuccessStatusCode)
            {
                var content = await payRes.Content.ReadAsStringAsync();
                payments = JsonSerializer.Deserialize<List<Payment>>(content) ?? new List<Payment>();
            }

            // Group payments by tenant_id
            var paymentsByTenant = payments.GroupBy(p => p.TenantId).ToDictionary(g => g.Key, g => g.ToList());

            // 3. Calculate status for every active tenant
            foreach (var t in tenants)
            {
                paymentsByTenant.TryGetValue(t.TenantId, out var tenantPayments);
                tenantPayments ??= new List<Payment>();

                var approvedSum = tenantPayments.Where(p => p.Status.Equals("approved", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);
                var pendingSum = tenantPayments.Where(p => p.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);

                string status;
                if (approvedSum >= t.RentalAmount)
                {
                    status = "Paid";
                }
                else if (approvedSum == 0 && pendingSum > 0)
                {
                    status = "Pending";
                }
                else
                {
                    status = "Unpaid";
                }

                report.Items.Add(new MonthlyTenantReportItem
                {
                    TenantId = t.TenantId,
                    StudentName = t.Profile?.FullName ?? "Tenant #" + t.TenantId,
                    StudentNumber = t.StudentNumber,
                    PropertyName = t.Room?.Property?.Name ?? "Property",
                    RoomNumber = t.Room?.RoomNumber ?? t.RoomId.ToString(),
                    PaymentReference = t.PaymentReference,
                    RentalAmount = t.RentalAmount,
                    ApprovedAmount = approvedSum,
                    PendingAmount = pendingSum,
                    Status = status
                });
            }

            // Sort by Room / Student name
            report.Items = report.Items.OrderBy(i => i.PropertyName).ThenBy(i => i.RoomNumber).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly report for {Month}", periodMonthStr);
        }

        return report;
    }

    public byte[] GenerateCsv(MonthlyReportViewModel report)
    {
        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Student Name,Student Number,Property,Room,Payment Reference,Monthly Rent (R),Approved Paid (R),Status");

        foreach (var item in report.Items)
        {
            sb.AppendLine($"\"{EscapeCsv(item.StudentName)}\",\"{EscapeCsv(item.StudentNumber)}\",\"{EscapeCsv(item.PropertyName)}\",\"{EscapeCsv(item.RoomNumber)}\",\"{EscapeCsv(item.PaymentReference)}\",{item.RentalAmount:F2},{item.ApprovedAmount:F2},\"{item.Status}\"");
        }

        // Summary Rows
        sb.AppendLine();
        sb.AppendLine($",,,,TOTALS,{report.TotalExpected:F2},{report.TotalCollected:F2},");
        sb.AppendLine($",,,,Paid Tenants: {report.PaidCount},Unpaid Tenants: {report.UnpaidCount},Pending Verification: {report.PendingCount},Outstanding: R {report.TotalOutstanding:F2}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}