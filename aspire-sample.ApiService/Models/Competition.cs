namespace aspire_sample.ApiService.Models;

public class Competition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public string RoundType { get; set; } = string.Empty;
    public CompetitionType Type { get; set; }
    public string? Description { get; set; }

    public ICollection<CompetitionResult> Results { get; set; } = [];
}
