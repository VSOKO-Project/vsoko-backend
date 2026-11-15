using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.DataManager.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackAnalyses",
                columns: table => new
                {
                    FeedbackId = table.Column<string>(type: "text", nullable: false),
                    SourceComment = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FindingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryAfterUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackAnalyses", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_FeedbackAnalyses_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    RequiresAttention = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackSignalReviews",
                columns: table => new
                {
                    FeedbackId = table.Column<string>(type: "text", nullable: false),
                    SignalKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSignalReviews", x => new { x.FeedbackId, x.SignalKey });
                    table.ForeignKey(
                        name: "FK_FeedbackSignalReviews_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FeedbackCategories",
                columns: new[] { "Id", "Description", "Name", "NormalizedName", "RequiresAttention" },
                values: new object[,]
                {
                    { "assessment", "Понятность, стабильность и справедливость критериев и выставления оценок.", "Прозрачность оценивания", "ПРОЗРАЧНОСТЬ ОЦЕНИВАНИЯ", false },
                    { "communication", "Доступность преподавателя, обратная связь и уважительное общение.", "Общение и поддержка", "ОБЩЕНИЕ И ПОДДЕРЖКА", false },
                    { "corruption", "Требования денег, подарков или услуг в обмен на оценки.", "Возможные неправомерные требования", "ВОЗМОЖНЫЕ НЕПРАВОМЕРНЫЕ ТРЕБОВАНИЯ", true },
                    { "discrimination", "Неравное обращение по личным или групповым признакам.", "Возможная дискриминация", "ВОЗМОЖНАЯ ДИСКРИМИНАЦИЯ", true },
                    { "explanation", "Ясность изложения, ответы на вопросы и объяснение сложных тем.", "Понятность объяснений", "ПОНЯТНОСТЬ ОБЪЯСНЕНИЙ", false },
                    { "harassment", "Нежелательные сексуальные комментарии, предложения или действия.", "Возможные домогательства", "ВОЗМОЖНЫЕ ДОМОГАТЕЛЬСТВА", true },
                    { "humiliation", "Сообщения об оскорблениях, публичных унижениях, систематическом преследовании.", "Возможные унижения и травля", "ВОЗМОЖНЫЕ УНИЖЕНИЯ И ТРАВЛЯ", true },
                    { "materials", "Качество, актуальность и доступность материалов и методичек.", "Учебные материалы", "УЧЕБНЫЕ МАТЕРИАЛЫ", false },
                    { "other-risk", "Иные сообщения о возможном вреде или серьёзном нарушении, не подходящие к остальным категориям.", "Другая ситуация, требующая проверки", "ДРУГАЯ СИТУАЦИЯ, ТРЕБУЮЩАЯ ПРОВЕРКИ", true },
                    { "practice", "Практические задания, содержание курса, соотношение теории и практики.", "Практика и содержание", "ПРАКТИКА И СОДЕРЖАНИЕ", false },
                    { "threats", "Угрозы, запугивание, месть за жалобы, принуждение.", "Возможные угрозы и давление", "ВОЗМОЖНЫЕ УГРОЗЫ И ДАВЛЕНИЕ", true },
                    { "workload", "Объём заданий, сроки, расписание и организация занятий.", "Нагрузка и организация", "НАГРУЗКА И ОРГАНИЗАЦИЯ", false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackAnalyses_RetryAfterUtc",
                table: "FeedbackAnalyses",
                column: "RetryAfterUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCategories_NormalizedName",
                table: "FeedbackCategories",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackAnalyses");

            migrationBuilder.DropTable(
                name: "FeedbackCategories");

            migrationBuilder.DropTable(
                name: "FeedbackSignalReviews");
        }
    }
}
