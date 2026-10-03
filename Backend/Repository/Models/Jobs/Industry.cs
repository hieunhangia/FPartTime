namespace Repository.Models.Jobs;

public class Industry
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Job>? Jobs { get; set; }
}