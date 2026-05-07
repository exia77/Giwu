using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giwu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Tenant_Settings_Owned_Types : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Loc_CurrencyCode",
                table: "tenants",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Loc_CurrencySymbol",
                table: "tenants",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Loc_DateFormat",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Loc_FiscalYearStartMonth",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Loc_Timezone",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Loc_WeekStart",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_BenefitsRenewal",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_BirthdayReminder",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_ComplianceDeadline",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_ContractExpiring",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_LeaveApproved",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_LeaveRejected",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_NewHireOnboarding",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_NewLeaveRequest",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_PayrollGenerated",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Notif_PayslipReleased",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pay_FirstCutoffDay",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pay_Frequency",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Pay_HolidayOvertimeRate",
                table: "tenants",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Pay_IncludeAllowanceIn13thMonth",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Pay_IncludeOtIn13thMonth",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Pay_NightDifferentialRate",
                table: "tenants",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Pay_RegularOvertimeRate",
                table: "tenants",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Pay_RestDayOvertimeRate",
                table: "tenants",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Pay_RoundStatutoryDeductions",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Pay_SecondCutoffDay",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Sec_IpWhitelist",
                table: "tenants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Sec_IpWhitelistEnabled",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Sec_MaxFailedLoginAttempts",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sec_MinPasswordLength",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sec_PasswordExpiryDays",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Sec_RequireLowercase",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sec_RequireMfa",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sec_RequireNumber",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sec_RequireSpecial",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sec_RequireUppercase",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Sec_SessionTimeout",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Loc_CurrencyCode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Loc_CurrencySymbol",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Loc_DateFormat",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Loc_FiscalYearStartMonth",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Loc_Timezone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Loc_WeekStart",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_BenefitsRenewal",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_BirthdayReminder",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_ComplianceDeadline",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_ContractExpiring",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_LeaveApproved",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_LeaveRejected",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_NewHireOnboarding",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_NewLeaveRequest",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_PayrollGenerated",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Notif_PayslipReleased",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_FirstCutoffDay",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_Frequency",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_HolidayOvertimeRate",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_IncludeAllowanceIn13thMonth",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_IncludeOtIn13thMonth",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_NightDifferentialRate",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_RegularOvertimeRate",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_RestDayOvertimeRate",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_RoundStatutoryDeductions",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Pay_SecondCutoffDay",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_IpWhitelist",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_IpWhitelistEnabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_MaxFailedLoginAttempts",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_MinPasswordLength",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_PasswordExpiryDays",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_RequireLowercase",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_RequireMfa",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_RequireNumber",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_RequireSpecial",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_RequireUppercase",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Sec_SessionTimeout",
                table: "tenants");
        }
    }
}
