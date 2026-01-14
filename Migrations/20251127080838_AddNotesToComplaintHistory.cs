using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Complaint_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToComplaintHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ComplaintHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ComplaintHistories");
        }
    }
}
