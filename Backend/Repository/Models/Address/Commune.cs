namespace Repository.Models.Address;

public class Commune
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string ProvinceCode { get; set; } = null!;

    public Province? Province { get; set; }
}