namespace Giwu.Domain.Common;

public class Address
{
    public string Line1 { get; set; } = "";
    public string Line2 { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "PH";
}

public class EmergencyContact
{
    public string Name { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string Phone { get; set; } = "";
}
