namespace Giwu.Domain.Benefits;

public enum BenefitCategory
{
    Health,
    Insurance,
    Allowance,
    Retirement,
    Loan,
    Wellness,
    Education,
    Leave,
    Other,
}

public enum EnrollmentStatus
{
    Active,
    Pending,
    Terminated,
    Suspended,
}

public enum BenefitRequestType
{
    Claim,
    Loan,
    Reimbursement,
}

public enum BenefitRequestStatus
{
    Pending,
    Approved,
    Released,
    Rejected,
    Repaying,
    Closed,
}
