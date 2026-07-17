namespace aspire.ApiService.Models;

public class TrainingSession
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }

    public ICollection<TrainingAttendance> Attendances { get; set; } = [];
}
