namespace Giwu.HRMS.Hybrid.Models;

// â”€â”€â”€ Core entities â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public class PayRun
{
    public string Id { get; set; } = "";
    public string Period { get; set; } = "";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string Status { get; set; } = "";
    public string Frequency { get; set; } = "Monthly";
    public List<Payslip> Slips { get; set; } = new();
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? Notes { get; set; }
}

public class Payslip
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Init { get; set; } = "";
    public string Av { get; set; } = "";
    public string Role { get; set; } = "";
    public string Dept { get; set; } = "";

    public decimal BasicSalary { get; set; }
    public decimal Overtime { get; set; }
    public decimal Bonus { get; set; }
    public decimal Allowance { get; set; }

    // Deductions â€” employee share
    public decimal Sss { get; set; }
    public decimal PhilHealth { get; set; }
    public decimal PagIbig { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal LoanDeduction { get; set; }
    public decimal OtherDeduction { get; set; }

    public string Notes { get; set; } = "";
    public bool IsEdited { get; set; }

    public decimal Gross => BasicSalary + Overtime + Bonus + Allowance;
    public decimal TotalDeductions => Sss + PhilHealth + PagIbig + WithholdingTax + LoanDeduction + OtherDeduction;
    public decimal Net => Gross - TotalDeductions;
}

// â”€â”€â”€ Reference bracket records (used by the UI) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public record SssBracket(
    decimal RangeFrom, decimal RangeTo, decimal Msc,
    decimal RegularSsEmployee, decimal RegularSsEmployer,
    decimal MpfEmployee, decimal MpfEmployer,
    decimal Ec,
    decimal TotalEmployee, decimal TotalEmployer
);

public record PhilHealthBracket(
    decimal RangeFrom, decimal RangeTo, string RateDisplay,
    decimal MonthlyPremium, decimal EmployeeShare, decimal EmployerShare
);

public record PagIbigBracket(
    decimal RangeFrom, decimal RangeTo,
    string EmployeeRateDisplay, string EmployerRateDisplay,
    string EmployeeAmountDisplay, string EmployerAmountDisplay,
    string Notes
);

public record BirBracket(
    decimal AnnualFrom, decimal AnnualTo, decimal MonthlyFrom, decimal MonthlyTo,
    string RateDisplay, decimal FixedTax, decimal ExcessRate, decimal ExcessOver,
    string FormulaDisplay
);

// â”€â”€â”€ PH statutory calculator (2025/2026 rules) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
//
// Sources:
//  â€¢ SSS Circular 2024-006 effective Jan 2025 (15% total, 5% employee / 10% employer,
//    min MSC â‚±5,000, max MSC â‚±35,000). MSC follows the official bracket table.
//  â€¢ PhilHealth Circular 2019-0009 as amended (5% premium, floor â‚±10,000, ceiling
//    â‚±100,000, 50/50 employee/employer, so employee share = 2.5% of basic, capped
//    â‚±250â€“â‚±2,500).
//  â€¢ Pag-IBIG Circular 460 effective Feb 2024 (MFS capped at â‚±10,000; MFS â‰¤ â‚±1,500
//    employee 1% / employer 2%; MFS > â‚±1,500 both 2%, max â‚±200 each).
//  â€¢ BIR TRAIN Law, revised withholding tax table effective January 1, 2023 onward.
//
// Numbers below are suitable for internal use and payroll demos. Before production
// use, verify against the current official circulars â€” schedules change.

public static class PhPayrollCalculator
{
    public static readonly SssBracket[] SssBrackets = BuildSssBrackets();

    private static SssBracket[] BuildSssBrackets()
    {
        // Standard SSS schedule: MSC in â‚±500 increments from 5,000 to 35,000.
        // Compensation range for MSC X = approximately [X - 250, X + 249.99].
        var brackets = new List<SssBracket>();
        for (decimal msc = 5000m; msc <= 35000m; msc += 500m)
        {
            var rangeFrom = msc == 5000m ? 0m : msc - 250m;
            var rangeTo = msc + 249.99m;

            // Regular SS is capped at MSC â‚±20,000
            var regularMsc = Math.Min(msc, 20000m);
            var regEe = Math.Round(regularMsc * 0.05m, 2);
            var regEr = Math.Round(regularMsc * 0.10m, 2);

            // MPF (Mandatory Provident Fund) on the excess MSC above â‚±20,000.
            // Same 5%/10% employee/employer split as Regular SS â€” total still 15% of MSC.
            // (MPF is just where the contribution is credited; the rate split is identical.)
            var mpfMsc = Math.Max(0m, msc - 20000m);
            var mpfEe = Math.Round(mpfMsc * 0.05m, 2);
            var mpfEr = Math.Round(mpfMsc * 0.10m, 2);

            // EC (Employees' Compensation): â‚±10 if MSC â‰¤ â‚±14,500, else â‚±30 (employer-paid)
            var ec = msc <= 14500m ? 10m : 30m;

            brackets.Add(new SssBracket(
                rangeFrom, rangeTo, msc,
                regEe, regEr, mpfEe, mpfEr, ec,
                TotalEmployee: regEe + mpfEe,
                TotalEmployer: regEr + mpfEr + ec
            ));
        }
        return brackets.ToArray();
    }

    public static SssBracket GetSssBracket(decimal monthlyBasic)
    {
        if (monthlyBasic >= 35000m) return SssBrackets[^1];
        if (monthlyBasic < 5000m)   return SssBrackets[0];
        foreach (var b in SssBrackets)
        {
            if (monthlyBasic >= b.RangeFrom && monthlyBasic <= b.RangeTo) return b;
        }
        return SssBrackets[^1];
    }

    public static decimal CalculateSss(decimal monthlyBasic) =>
        GetSssBracket(monthlyBasic).TotalEmployee;

    // â”€â”€ PhilHealth â€” three reference rows, computed on the fly for the middle band
    public static readonly PhilHealthBracket[] PhilHealthBrackets = new[]
    {
        new PhilHealthBracket(0m,        10000m,    "5% (floor applies)",   500m,    250m,   250m),
        // Middle band stays as a parameterized row in the UI; values are computed for any salary.
        new PhilHealthBracket(10000.01m, 99999.99m, "5% Ã— salary",          0m,      0m,     0m),
        new PhilHealthBracket(100000m,   decimal.MaxValue, "5% (ceiling capped)", 5000m, 2500m, 2500m),
    };

    public static decimal CalculatePhilHealth(decimal monthlyBasic)
    {
        var salary = Math.Clamp(monthlyBasic, 10000m, 100000m);
        return Math.Round(salary * 0.025m, 2);
    }

    public static int GetPhilHealthBracketIndex(decimal monthlyBasic)
    {
        if (monthlyBasic <= 10000m) return 0;
        if (monthlyBasic >= 100000m) return 2;
        return 1;
    }

    // â”€â”€ Pag-IBIG (HDMF Circular 460, effective Feb 2024)
    public static readonly PagIbigBracket[] PagIbigBrackets = new[]
    {
        new PagIbigBracket(0m,        1500m,            "1%",            "2%", "1% of MFS",   "2% of MFS",
            "For MFS â‰¤ â‚±1,500: employee pays 1%, employer pays 2%."),
        new PagIbigBracket(1500.01m,  10000m,           "2%",            "2%", "2% of MFS",   "2% of MFS",
            "For MFS > â‚±1,500 up to â‚±10,000: both parties pay 2% of MFS."),
        new PagIbigBracket(10000.01m, decimal.MaxValue, "2% (capped)",   "2% (capped)", "â‚±200 (max)", "â‚±200 (max)",
            "MFS is capped at â‚±10,000 â€” maximum contribution is â‚±200 each side."),
    };

    public static int GetPagIbigBracketIndex(decimal monthlyBasic)
    {
        if (monthlyBasic <= 1500m) return 0;
        if (monthlyBasic <= 10000m) return 1;
        return 2;
    }

    public static decimal CalculatePagIbig(decimal monthlyBasic)
    {
        var mfs = Math.Min(monthlyBasic, 10000m);
        var rate = monthlyBasic <= 1500m ? 0.01m : 0.02m;
        return Math.Round(mfs * rate, 2);
    }

    // â”€â”€ BIR Withholding Tax (TRAIN Law, monthly table effective Jan 2023 onward)
    public static readonly BirBracket[] BirBrackets = new[]
    {
        new BirBracket(    0m,    250000m,       0m,    20833m, "0%",     0m,        0.00m,      0m,
            "No tax due"),
        new BirBracket(250000m,   400000m,   20833m,    33332m, "15% of excess",    0m,        0.15m,  20833m,
            "15% Ã— (taxable âˆ’ â‚±20,833)"),
        new BirBracket(400000m,   800000m,   33333m,    66666m, "â‚±1,875 + 20% of excess",     1875m,     0.20m,  33333m,
            "â‚±1,875 + 20% Ã— (taxable âˆ’ â‚±33,333)"),
        new BirBracket(800000m,  2000000m,   66667m,   166666m, "â‚±8,541.80 + 25% of excess",  8541.80m,  0.25m,  66667m,
            "â‚±8,541.80 + 25% Ã— (taxable âˆ’ â‚±66,667)"),
        new BirBracket(2000000m, 8000000m,  166667m,   666666m, "â‚±33,541.80 + 30% of excess", 33541.80m, 0.30m, 166667m,
            "â‚±33,541.80 + 30% Ã— (taxable âˆ’ â‚±166,667)"),
        new BirBracket(8000000m, decimal.MaxValue, 666667m, decimal.MaxValue, "â‚±183,541.80 + 35% of excess", 183541.80m, 0.35m, 666667m,
            "â‚±183,541.80 + 35% Ã— (taxable âˆ’ â‚±666,667)"),
    };

    public static BirBracket GetBirBracket(decimal monthlyTaxable)
    {
        foreach (var b in BirBrackets)
        {
            if (monthlyTaxable >= b.MonthlyFrom && monthlyTaxable <= b.MonthlyTo) return b;
        }
        return BirBrackets[^1];
    }

    public static decimal CalculateWithholdingTax(decimal taxableIncome)
    {
        var b = GetBirBracket(taxableIncome);
        if (taxableIncome <= 20833m) return 0m;
        return Math.Round(b.FixedTax + (taxableIncome - b.ExcessOver) * b.ExcessRate, 2);
    }

    /// <summary>Recomputes all statutory deductions + tax based on current gross inputs.</summary>
    public static void RecalculateStatutory(Payslip p)
    {
        p.Sss = CalculateSss(p.BasicSalary);
        p.PhilHealth = CalculatePhilHealth(p.BasicSalary);
        p.PagIbig = CalculatePagIbig(p.BasicSalary);
        var taxable = p.Gross - (p.Sss + p.PhilHealth + p.PagIbig);
        p.WithholdingTax = CalculateWithholdingTax(taxable);
    }
}

// â”€â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public static class PayrollFormatting
{
    public static readonly string[] PayRunStages =
        new[] { "Draft", "Calculated", "Approved", "Released" };

    public static string StatusBadgeClass(string s) => s switch
    {
        "Draft"      => "pay-badge-draft",
        "Calculated" => "pay-badge-calculated",
        "Approved"   => "pay-badge-approved",
        "Released"   => "pay-badge-released",
        _            => ""
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--pay-green-light);color:var(--pay-green-text)",
        "blue"    => "background:var(--pay-blue-light);color:var(--pay-blue-text)",
        "amber"   => "background:var(--pay-amber-light);color:var(--pay-amber-text)",
        "red"     => "background:var(--pay-red-light);color:var(--pay-red-text)",
        "purple"  => "background:var(--pay-purple-light);color:var(--pay-purple-text)",
        "primary" => "background:var(--pay-green-light);color:var(--pay-green-text)",
        _         => ""
    };

    public static string Money(decimal v) =>
        "â‚±" + v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

    public static string MoneyCompact(decimal v)
    {
        if (Math.Abs(v) >= 1_000_000m) return $"â‚±{v/1_000_000m:F2}M";
        if (Math.Abs(v) >= 1_000m)     return $"â‚±{v/1_000m:F0}k";
        return $"â‚±{v:F0}";
    }

    public static string MoneyRange(decimal from, decimal to)
    {
        if (to == decimal.MaxValue) return $"Over {Money(from)}";
        if (from == 0m) return $"Up to {Money(to)}";
        return $"{Money(from)} â€“ {Money(to)}";
    }

    public static string FmtDate(DateTime? d) => d.HasValue ? d.Value.ToString("MMM d, yyyy") : "â€”";
    public static string FmtDateShort(DateTime d) => d.ToString("MMM d");
    public static string FmtPeriodRange(DateTime s, DateTime e) => $"{s:MMM d} â€“ {e:MMM d, yyyy}";
}
