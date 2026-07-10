using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeppelin.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientNumberAndIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientNumber",
                table: "Patients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rows with distinct sequential numbers (ordered
            // by creation date) before the unique index below is created -
            // the default 0 above would otherwise collide on row 2+.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"", ROW_NUMBER() OVER (ORDER BY ""CreatedAtUtc"") AS rn
                    FROM ""Patients""
                )
                UPDATE ""Patients"" p
                SET ""PatientNumber"" = numbered.rn
                FROM numbered
                WHERE p.""Id"" = numbered.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients",
                column: "PatientNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PatientNumber",
                table: "Patients");
        }
    }
}
