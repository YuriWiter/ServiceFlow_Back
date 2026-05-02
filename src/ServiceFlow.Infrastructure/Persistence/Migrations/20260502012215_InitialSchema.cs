using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "serviceflow");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_orders",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    opening_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    make = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    vin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "serviceflow",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_requests",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    price_estimate_cents = table.Column<long>(type: "bigint", nullable: false),
                    urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repair_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_repair_requests_service_orders_service_order_id",
                        column: x => x.service_order_id,
                        principalSchema: "serviceflow",
                        principalTable: "service_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_status_history",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    to_stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_status_history_service_orders_service_order_id",
                        column: x => x.service_order_id,
                        principalSchema: "serviceflow",
                        principalTable: "service_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repair_media",
                schema: "serviceflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repair_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repair_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_repair_media_repair_requests_repair_request_id",
                        column: x => x.repair_request_id,
                        principalSchema: "serviceflow",
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone_number",
                schema: "serviceflow",
                table: "customers",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_customers_user_id",
                schema: "serviceflow",
                table: "customers",
                column: "user_id",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_repair_media_repair_request_id",
                schema: "serviceflow",
                table: "repair_media",
                column: "repair_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_repair_requests_customer_decision",
                schema: "serviceflow",
                table: "repair_requests",
                column: "customer_decision");

            migrationBuilder.CreateIndex(
                name: "ix_repair_requests_service_order_id",
                schema: "serviceflow",
                table: "repair_requests",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_assigned_staff_id",
                schema: "serviceflow",
                table: "service_orders",
                column: "assigned_staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_created_at",
                schema: "serviceflow",
                table: "service_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_customer_id",
                schema: "serviceflow",
                table: "service_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_stage",
                schema: "serviceflow",
                table: "service_orders",
                column: "stage");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_vehicle_id",
                schema: "serviceflow",
                table: "service_orders",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_status_history_service_order_id_changed_at",
                schema: "serviceflow",
                table: "service_status_history",
                columns: new[] { "service_order_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "serviceflow",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_customer_id",
                schema: "serviceflow",
                table: "vehicles",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_license_plate",
                schema: "serviceflow",
                table: "vehicles",
                column: "license_plate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "repair_media",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "service_status_history",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "users",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "repair_requests",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "serviceflow");

            migrationBuilder.DropTable(
                name: "service_orders",
                schema: "serviceflow");
        }
    }
}
