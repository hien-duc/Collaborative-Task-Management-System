using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collaborative_Task_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseInitialize3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProjectMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_UpdateProjectDelete
ON Projects
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if IsDeleted was updated to true
    IF UPDATE(IsDeleted)
    BEGIN
        -- Update IsActive to false in ProjectMembers for the affected project
        UPDATE ProjectMembers
        SET IsActive = 0
        FROM ProjectMembers pm
        INNER JOIN inserted i ON pm.ProjectId = i.Id
        WHERE i.IsDeleted = 1;

        -- Update IsDeleted to true in Tasks for the affected project
        UPDATE Tasks
        SET IsDeleted = 1
        FROM Tasks t
        INNER JOIN inserted i ON t.ProjectId = i.Id
        WHERE i.IsDeleted = 1;
    END
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProjectMembers");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_UpdateProjectDelete;");
        }
    }
}
