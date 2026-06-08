using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllMarketAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "PreRefundStatus",
                table: "Orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefundId",
                table: "Orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_PreRefundStatus",
                table: "Orders",
                sql: "\"PreRefundStatus\" IS NULL OR \"PreRefundStatus\" IN ('Paid', 'Preparing')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders",
                sql: "\"Status\" IN ('Awaiting for payment', 'Paid', 'Preparing', 'Shipped', 'Delivered', 'Cancelled', 'Expired', 'Refunding', 'Refunded')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_PreRefundStatus",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreRefundStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeRefundId",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders",
                sql: "\"Status\" IN ('Awaiting for payment', 'Paid', 'Preparing', 'Shipped', 'Delivered', 'Cancelled', 'Expired')");
        }
    }
}
