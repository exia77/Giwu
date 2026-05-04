using Giwu.Domain.Common;

namespace Giwu.Domain.Organization;


public class Employee : AuditableEntity
{
    // Identity
    public string EmployeeNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Suffix { get; set; } = "";
    public string PreferredName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PersonalEmail { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public Gender Gender { get; set; }
    public CivilStatus CivilStatus { get; set; }

    // Employment
    public Guid DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public string JobTitle { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public DateOnly? RegularizationDate { get; set; }
    public DateOnly? SeparationDate { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Regular;
    public WorkArrangement WorkArrangement { get; set; } = WorkArrangement.Onsite;

    // Compensation (encrypt at rest in production)
    [AuditRedact] public decimal MonthlyBaseSalary { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.SemiMonthly;
    public string BankName { get; set; } = "";
    [AuditRedact] public string BankAccountNumber { get; set; } = "";

    // PH government IDs (encrypt at rest in production)
    [AuditRedact] public string Tin { get; set; } = "";
    [AuditRedact] public string SssNumber { get; set; } = "";
    [AuditRedact] public string PhilHealthNumber { get; set; } = "";
    [AuditRedact] public string PagibigNumber { get; set; } = "";

    // Owned value objects (mapped as JSON columns)
    public Address PermanentAddress { get; set; } = new();
    public Address CurrentAddress { get; set; } = new();
    public EmergencyContact Emergency { get; set; } = new();

    public string FullName => string.Join(" ",
        new[] { FirstName, MiddleName, LastName, Suffix }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
