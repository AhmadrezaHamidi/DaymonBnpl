using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnplService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBnpl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bnpl");

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeePercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    FixedAmount = table.Column<long>(type: "bigint", nullable: true),
                    MaxAmount = table.Column<long>(type: "bigint", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    FeePercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    UpfrontFeePercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    LatePenaltyDailyPercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MinAmount = table.Column<long>(type: "bigint", nullable: false),
                    MaxAmount = table.Column<long>(type: "bigint", nullable: false),
                    AmountStep = table.Column<long>(type: "bigint", nullable: false),
                    CompletionHours = table.Column<int>(type: "int", nullable: false),
                    ActivationMinutes = table.Column<int>(type: "int", nullable: false),
                    GatewayPaymentMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxActiveContractsPerCustomer = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    MobileNumber = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    PlanTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    FeePercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    UpfrontFeePercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    LatePenaltyDailyPercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    UpfrontFee = table.Column<long>(type: "bigint", nullable: false),
                    InstallmentAmount = table.Column<long>(type: "bigint", nullable: false),
                    TotalFee = table.Column<long>(type: "bigint", nullable: false),
                    TotalRepayment = table.Column<long>(type: "bigint", nullable: false),
                    DiscountCodeId = table.Column<int>(type: "int", nullable: true),
                    DiscountCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    DiscountAmount = table.Column<long>(type: "bigint", nullable: false),
                    TokenPrice = table.Column<long>(type: "bigint", nullable: false),
                    TokenAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TermsAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TokensLocked = table.Column<bool>(type: "bit", nullable: false),
                    TokensReleased = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requests_DiscountCodes_DiscountCodeId",
                        column: x => x.DiscountCodeId,
                        principalSchema: "bnpl",
                        principalTable: "DiscountCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requests_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "bnpl",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    PenaltyPaid = table.Column<long>(type: "bigint", nullable: false),
                    PenaltyWaived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "bnpl",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    ContractNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Penalty = table.Column<long>(type: "bigint", nullable: false),
                    GatewayId = table.Column<int>(type: "int", nullable: true),
                    GatewayTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderReference = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PayUrl = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    ReturnUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    TrackingNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TokenAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TokenPrice = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    By = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RefundTrackingNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedBy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "bnpl",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestHistory",
                schema: "bnpl",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    ToStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    By = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestHistory_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "bnpl",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_Code",
                schema: "bnpl",
                table: "DiscountCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installments_RequestId_Number",
                schema: "bnpl",
                table: "Installments",
                columns: new[] { "RequestId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installments_Status_DueDate",
                schema: "bnpl",
                table: "Installments",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_Id",
                schema: "bnpl",
                table: "Payments",
                columns: new[] { "CustomerId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderReference",
                schema: "bnpl",
                table: "Payments",
                column: "ProviderReference");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestId_Status",
                schema: "bnpl",
                table: "Payments",
                columns: new[] { "RequestId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_ExpiresAt",
                schema: "bnpl",
                table: "Payments",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_RequestId",
                schema: "bnpl",
                table: "RequestHistory",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ContractNumber",
                schema: "bnpl",
                table: "Requests",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CustomerId_Status",
                schema: "bnpl",
                table: "Requests",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_DiscountCodeId",
                schema: "bnpl",
                table: "Requests",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_MobileNumber",
                schema: "bnpl",
                table: "Requests",
                column: "MobileNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_PlanId",
                schema: "bnpl",
                table: "Requests",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status_ExpiresAt",
                schema: "bnpl",
                table: "Requests",
                columns: new[] { "Status", "ExpiresAt" });

            // دادهٔ اولیه (همان BnplSeed): ردیف تنظیمات با پیش‌فرض‌های فیگما و طرح «۱۲ قسط» — کارمزد ۲۲٪ (۴٪ با قسط اول)، جریمهٔ روزانه ۱٪.
            migrationBuilder.InsertData(
                schema: "bnpl",
                table: "Settings",
                columns: new[] { "Id", "IsEnabled", "MinAmount", "MaxAmount", "AmountStep", "CompletionHours", "ActivationMinutes",
                    "GatewayPaymentMinutes", "MaxActiveContractsPerCustomer", "Version" },
                values: new object[] { 1, true, 50_000_000L, 5_000_000_000L, 10_000_000L, 24, 30, 20, 3, 1 });

            migrationBuilder.InsertData(
                schema: "bnpl",
                table: "Plans",
                columns: new[] { "Title", "InstallmentCount", "FeePercent", "UpfrontFeePercent", "LatePenaltyDailyPercent", "IsActive", "SortOrder", "Version" },
                values: new object[] { "۱۲ قسط", 12, 22m, 4m, 1m, true, 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Installments",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "RequestHistory",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "Requests",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "DiscountCodes",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "bnpl");
        }
    }
}
