using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service interface for Rent Payments and Proof-of-Payment Storage operations.
/// All calls carry the signed-in manager's JWT token for PostgreSQL Row Level Security.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Retrieves payments with joined tenant, room, and verifier profile information.
    /// Optionally filtered by status ('pending', 'approved', 'rejected') and search query.
    /// </summary>
    Task<List<Payment>> GetPaymentsAsync(string? status = null, string? search = null);

    /// <summary>
    /// Retrieves a single payment record by ID with joined tenant and verifier profiles.
    /// </summary>
    Task<Payment?> GetPaymentByIdAsync(int paymentId);

    /// <summary>
    /// Generates a short-lived signed URL for a private proof-of-payment file in Supabase Storage.
    /// </summary>
    Task<string?> GetProofSignedUrlAsync(string? proofUrlPath);

    /// <summary>
    /// Finds an active tenant by their unique payment reference string.
    /// </summary>
    Task<Tenant?> FindTenantByReferenceAsync(string paymentReference);

    /// <summary>
    /// Updates a payment status ('approved' or 'rejected') and records the verifying manager's ID and timestamp.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdatePaymentStatusAsync(int paymentId, string newStatus, Guid verifiedByUserId);

    /// <summary>
    /// Records a payment directly by management with status 'approved' and verifier details set.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> RecordPaymentAsync(Payment payment, Guid verifiedByUserId);

    /// <summary>
    /// Returns total counts for all, pending, approved, and rejected payments.
    /// </summary>
    Task<(int Total, int Pending, int Approved, int Rejected)> GetPaymentCountsAsync();
}
