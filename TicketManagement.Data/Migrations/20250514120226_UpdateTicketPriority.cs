using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make sure the Priority column is correctly typed as int
            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 1); // Default to Medium
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert change if needed
            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }
    }
}
