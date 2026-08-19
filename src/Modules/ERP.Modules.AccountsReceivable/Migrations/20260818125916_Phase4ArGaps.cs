using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Modules.AccountsReceivable.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ArGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionNotes",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FollowUpDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RelatedDocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PromiseToPayDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoubtfulAccountAllowances",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsOfDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReserveAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PostedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubtfulAccountAllowances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DunningTemplates",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Bucket = table.Column<int>(type: "int", nullable: false),
                    MinDaysOverdue = table.Column<int>(type: "int", nullable: false),
                    MaxDaysOverdue = table.Column<int>(type: "int", nullable: false),
                    SendEmail = table.Column<bool>(type: "bit", nullable: false),
                    SendPdf = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DunningTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResaleCertificates",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuedState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IssueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DocumentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResaleCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionNoteActivities",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    ActivityDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionNoteActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionNoteActivities_CollectionNotes_CollectionNoteId",
                        column: x => x.CollectionNoteId,
                        principalSchema: "ar",
                        principalTable: "CollectionNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllowanceByBuckets",
                schema: "ar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<int>(type: "int", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReserveRate = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceByBuckets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowanceByBuckets_DoubtfulAccountAllowances_AllowanceRunId",
                        column: x => x.AllowanceRunId,
                        principalSchema: "ar",
                        principalTable: "DoubtfulAccountAllowances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceByBuckets_AllowanceRunId",
                schema: "ar",
                table: "AllowanceByBuckets",
                column: "AllowanceRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionNoteActivities_CollectionNoteId",
                schema: "ar",
                table: "CollectionNoteActivities",
                column: "CollectionNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionNotes_AssignedTo",
                schema: "ar",
                table: "CollectionNotes",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionNotes_CustomerId",
                schema: "ar",
                table: "CollectionNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionNotes_Status",
                schema: "ar",
                table: "CollectionNotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulAccountAllowances_CompanyId",
                schema: "ar",
                table: "DoubtfulAccountAllowances",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtfulAccountAllowances_Status",
                schema: "ar",
                table: "DoubtfulAccountAllowances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DunningTemplates_Bucket",
                schema: "ar",
                table: "DunningTemplates",
                column: "Bucket");

            migrationBuilder.CreateIndex(
                name: "IX_DunningTemplates_CompanyId",
                schema: "ar",
                table: "DunningTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResaleCertificates_CustomerId",
                schema: "ar",
                table: "ResaleCertificates",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResaleCertificates_CustomerId_CertificateNumber",
                schema: "ar",
                table: "ResaleCertificates",
                columns: new[] { "CustomerId", "CertificateNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResaleCertificates_ExpiryDate",
                schema: "ar",
                table: "ResaleCertificates",
                column: "ExpiryDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowanceByBuckets",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "CollectionNoteActivities",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "DunningTemplates",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "ResaleCertificates",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "DoubtfulAccountAllowances",
                schema: "ar");

            migrationBuilder.DropTable(
                name: "CollectionNotes",
                schema: "ar");
        }
    }
}
