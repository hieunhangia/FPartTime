namespace Domain.Models.Orders;

public abstract class OrderBase
{
    public Guid Id { get; set; }
    public Guid PaymentTransactionId { get; set; }
    
    public PaymentTransaction? PaymentTransaction { get; set; }
}