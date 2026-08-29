using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Services.Interfaces;

public interface IPaymentService
{
    Task<List<Payment>> GetPaymentsAsync(PaymentStatus? status = null, DateTime? from = null, DateTime? to = null);
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<Payment> EnsurePaymentForOrderAsync(int orderId);
    Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status, string? actorUserId = null, string? note = null);
    Task<bool> UpdatePaymentMethodAsync(int paymentId, PaymentMethod method);
}
