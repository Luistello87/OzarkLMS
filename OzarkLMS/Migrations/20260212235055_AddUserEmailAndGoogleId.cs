using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzarkLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailAndGoogleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='Email') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""Email"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='GoogleId') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""GoogleId"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'Users' AND indexname = 'IX_Users_Email') THEN
                        CREATE UNIQUE INDEX ""IX_Users_Email"" ON ""Users"" (""Email"");
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");
        }
    }
}
