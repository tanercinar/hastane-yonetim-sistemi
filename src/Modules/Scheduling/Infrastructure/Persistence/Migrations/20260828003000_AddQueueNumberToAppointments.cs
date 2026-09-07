using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Migrations;

public partial class AddQueueNumberToAppointments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "queue_number",
            schema: "scheduling",
            table: "appointments",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "queue_number",
            schema: "scheduling",
            table: "appointments");
    }
}
