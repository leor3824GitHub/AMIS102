using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.AssetManagement
{
    /// <inheritdoc />
    public partial class UnifyPropertyItemCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SemiExpendableProperties_SemiExpendableItems_SemiExpendable~",
                schema: "am",
                table: "SemiExpendableProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_SMRRItems_SemiExpendableItems_SemiExpendableItemId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropTable(
                name: "SemiExpendableItems",
                schema: "am");

            migrationBuilder.RenameColumn(
                name: "SemiExpendableItemId",
                schema: "am",
                table: "SMRRItems",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SMRRItems_SemiExpendableItemId",
                schema: "am",
                table: "SMRRItems",
                newName: "IX_SMRRItems_ItemId");

            migrationBuilder.RenameColumn(
                name: "SemiExpendableItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SemiExpendableProperties_SemiExpendableItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                newName: "IX_SemiExpendableProperties_ItemId");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                schema: "am",
                table: "PPERRItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                schema: "am",
                table: "PPEItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PropertyItemCatalog",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UACSObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyItemCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PPERRItems_ItemId",
                schema: "am",
                table: "PPERRItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PPEItems_ItemId",
                schema: "am",
                table: "PPEItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_Code",
                schema: "am",
                table: "PropertyItemCatalog",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyItemCatalog_Name",
                schema: "am",
                table: "PropertyItemCatalog",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_PPEItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "PPEItems",
                column: "ItemId",
                principalSchema: "am",
                principalTable: "PropertyItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PPERRItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "PPERRItems",
                column: "ItemId",
                principalSchema: "am",
                principalTable: "PropertyItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SemiExpendableProperties_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "ItemId",
                principalSchema: "am",
                principalTable: "PropertyItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SMRRItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "SMRRItems",
                column: "ItemId",
                principalSchema: "am",
                principalTable: "PropertyItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PPEItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PPERRItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "PPERRItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SemiExpendableProperties_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "SemiExpendableProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_SMRRItems_PropertyItemCatalog_ItemId",
                schema: "am",
                table: "SMRRItems");

            migrationBuilder.DropTable(
                name: "PropertyItemCatalog",
                schema: "am");

            migrationBuilder.DropIndex(
                name: "IX_PPERRItems_ItemId",
                schema: "am",
                table: "PPERRItems");

            migrationBuilder.DropIndex(
                name: "IX_PPEItems_ItemId",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "am",
                table: "PPERRItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "am",
                table: "PPEItems");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                schema: "am",
                table: "SMRRItems",
                newName: "SemiExpendableItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SMRRItems_ItemId",
                schema: "am",
                table: "SMRRItems",
                newName: "IX_SMRRItems_SemiExpendableItemId");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                newName: "SemiExpendableItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SemiExpendableProperties_ItemId",
                schema: "am",
                table: "SemiExpendableProperties",
                newName: "IX_SemiExpendableProperties_SemiExpendableItemId");

            migrationBuilder.CreateTable(
                name: "SemiExpendableItems",
                schema: "am",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedUsefulLifeYears = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UACSObjectCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemiExpendableItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableItems_Code",
                schema: "am",
                table: "SemiExpendableItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemiExpendableItems_Name",
                schema: "am",
                table: "SemiExpendableItems",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_SemiExpendableProperties_SemiExpendableItems_SemiExpendable~",
                schema: "am",
                table: "SemiExpendableProperties",
                column: "SemiExpendableItemId",
                principalSchema: "am",
                principalTable: "SemiExpendableItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SMRRItems_SemiExpendableItems_SemiExpendableItemId",
                schema: "am",
                table: "SMRRItems",
                column: "SemiExpendableItemId",
                principalSchema: "am",
                principalTable: "SemiExpendableItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
