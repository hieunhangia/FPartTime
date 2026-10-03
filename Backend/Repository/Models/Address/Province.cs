namespace Repository.Models.Address;

public class Province
{
    public required string Code { get; set; }
    public required string Name { get; set; }

    public ICollection<Commune>? Communes { get; set; }
}