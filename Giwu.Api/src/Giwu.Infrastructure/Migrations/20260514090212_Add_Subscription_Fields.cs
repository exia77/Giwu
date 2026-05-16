using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giwu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Subscription_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCustomerId",
                table: "tenants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CurrentPeriodEndsAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionStatus",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrialEndsAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingCustomerId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEndsAt",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "tenants");
        }
    }
}
