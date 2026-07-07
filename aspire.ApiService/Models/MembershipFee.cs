namespace aspire.ApiService.Models;

public class MembershipFee
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public FeeStatus Status { get; set; }
}
