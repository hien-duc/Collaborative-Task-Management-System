using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collaborative_Task_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class SecondPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.EnsureSchema(
                name: "internal");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "Tasks",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "ProjectTeamMembers",
                newName: "ProjectTeamMembers",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Projects",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "ProjectMembers",
                newName: "ProjectMembers",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "FileAttachments",
                newName: "FileAttachments",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Comments",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "internal");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Tasks",
                schema: "internal",
                newName: "Tasks");

            migrationBuilder.RenameTable(
                name: "ProjectTeamMembers",
                schema: "internal",
                newName: "ProjectTeamMembers");

            migrationBuilder.RenameTable(
                name: "Projects",
                schema: "internal",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "ProjectMembers",
                schema: "internal",
                newName: "ProjectMembers");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "internal",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "FileAttachments",
                schema: "internal",
                newName: "FileAttachments");

            migrationBuilder.RenameTable(
                name: "Comments",
                schema: "internal",
                newName: "Comments");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "internal",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "public",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "public",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "public",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "public",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "public",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "public",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "public",
                newName: "AspNetRoleClaims");
        }
    }
}
