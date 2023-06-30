using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    public partial class ManualCheckingIndexesUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId_UserId",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_UserId",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualExerciseCheckings_CourseId_SlideId",
                table: "ManualExerciseCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualExerciseCheckings_CourseId_UserId",
                table: "ManualExerciseCheckings");

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_IsChecked_SlideId",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "IsChecked", "SlideId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_IsChecked_UserId_SlideId_Times~",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "IsChecked", "UserId", "SlideId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId_UserId_Timestamp",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "SlideId", "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualExerciseCheckings_CourseId_IsChecked_SlideId",
                table: "ManualExerciseCheckings",
                columns: new[] { "CourseId", "IsChecked", "SlideId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualExerciseCheckings_CourseId_IsChecked_UserId_SlideId_T~",
                table: "ManualExerciseCheckings",
                columns: new[] { "CourseId", "IsChecked", "UserId", "SlideId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualExerciseCheckings_CourseId_SlideId_UserId_Timestamp",
                table: "ManualExerciseCheckings",
                columns: new[] { "CourseId", "SlideId", "UserId", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_IsChecked_SlideId",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_IsChecked_UserId_SlideId_Times~",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId_UserId_Timestamp",
                table: "ManualQuizCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualExerciseCheckings_CourseId_IsChecked_SlideId",
                table: "ManualExerciseCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualExerciseCheckings_CourseId_IsChecked_UserId_SlideId_T~",
                table: "ManualExerciseCheckings");

            migrationBuilder.DropIndex(
                name: "IX_ManualExerciseCheckings_CourseId_SlideId_UserId_Timestamp",
                table: "ManualExerciseCheckings");

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "SlideId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_SlideId_UserId",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "SlideId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualQuizCheckings_CourseId_UserId",
                table: "ManualQuizCheckings",
                columns: new[] { "CourseId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualExerciseCheckings_CourseId_SlideId",
                table: "ManualExerciseCheckings",
                columns: new[] { "CourseId", "SlideId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualExerciseCheckings_CourseId_UserId",
                table: "ManualExerciseCheckings",
                columns: new[] { "CourseId", "UserId" });
        }
    }
}
