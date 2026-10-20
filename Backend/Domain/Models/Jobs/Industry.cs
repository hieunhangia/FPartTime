namespace Domain.Models.Jobs;

public class Industry
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Job>? Jobs { get; set; }
}