using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Complaint_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class SetExistingRecordsToActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Categories SET IsActive = 1");
            migrationBuilder.Sql("UPDATE Departments SET IsActive = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Categories SET IsActive = 0");
            migrationBuilder.Sql("UPDATE Departments SET IsActive = 0");
        }
    }
}
