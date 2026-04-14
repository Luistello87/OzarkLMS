using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OzarkLMS.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReplySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Users_UserId",
                table: "CalendarEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroupMembers_Users_UserId",
                table: "ChatGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroups_Users_CreatedById",
                table: "ChatGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_StudentId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Users_UserId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Users_UserId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateChats_Users_User1Id",
                table: "PrivateChats");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateChats_Users_User2Id",
                table: "PrivateChats");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_Users_SenderId",
                table: "PrivateMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_StickyNotes_Users_UserId",
                table: "StickyNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Submissions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "StickyNotes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "PrivateMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "PrivateMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PrivateMessages' AND column_name='ParentMessageId') THEN
                        ALTER TABLE ""PrivateMessages"" ADD COLUMN ""ParentMessageId"" integer;
                    END IF;
                END $$;");

            migrationBuilder.AlterColumn<int>(
                name: "User2Id",
                table: "PrivateChats",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "User1Id",
                table: "PrivateChats",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Posts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "PostComments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Enrollments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "ChatMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ChatMessages' AND column_name='ParentMessageId') THEN
                        ALTER TABLE ""ChatMessages"" ADD COLUMN ""ParentMessageId"" integer;
                    END IF;
                END $$;");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "ChatGroups",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "ChatGroups",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatGroupMembers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CalendarEvents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Meetings') THEN
                        CREATE TABLE ""Meetings"" (
                            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                            ""CourseId"" integer NOT NULL,
                            ""Name"" character varying(200) NOT NULL,
                            ""StartTime"" timestamp with time zone NOT NULL,
                            ""EndTime"" timestamp with time zone NOT NULL,
                            ""Url"" text NOT NULL,
                            CONSTRAINT ""PK_Meetings"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_Meetings_Courses_CourseId"" FOREIGN KEY (""CourseId"") REFERENCES ""Courses"" (""Id"") ON DELETE CASCADE
                        );
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SharedPosts') THEN
                        CREATE TABLE ""SharedPosts"" (
                            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                            ""UserId"" integer NULL,
                            ""PostId"" integer NOT NULL,
                            ""SharedAt"" timestamp with time zone NOT NULL,
                            CONSTRAINT ""PK_SharedPosts"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_SharedPosts_Posts_PostId"" FOREIGN KEY (""PostId"") REFERENCES ""Posts"" (""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_SharedPosts_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"")
                        );
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'PrivateMessages' AND indexname = 'IX_PrivateMessages_ParentMessageId') THEN
                        CREATE INDEX ""IX_PrivateMessages_ParentMessageId"" ON ""PrivateMessages"" (""ParentMessageId"");
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'ChatMessages' AND indexname = 'IX_ChatMessages_ParentMessageId') THEN
                        CREATE INDEX ""IX_ChatMessages_ParentMessageId"" ON ""ChatMessages"" (""ParentMessageId"");
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'Meetings' AND indexname = 'IX_Meetings_CourseId') THEN
                        CREATE INDEX ""IX_Meetings_CourseId"" ON ""Meetings"" (""CourseId"");
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'SharedPosts' AND indexname = 'IX_SharedPosts_PostId') THEN
                        CREATE INDEX ""IX_SharedPosts_PostId"" ON ""SharedPosts"" (""PostId"");
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'SharedPosts' AND indexname = 'IX_SharedPosts_UserId') THEN
                        CREATE INDEX ""IX_SharedPosts_UserId"" ON ""SharedPosts"" (""UserId"");
                    END IF;
                END $$;");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Users_UserId",
                table: "CalendarEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroupMembers_Users_UserId",
                table: "ChatGroupMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroups_Users_CreatedById",
                table: "ChatGroups",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatMessages_ParentMessageId",
                table: "ChatMessages",
                column: "ParentMessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_StudentId",
                table: "Enrollments",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Users_UserId",
                table: "PostComments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Users_UserId",
                table: "Posts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateChats_Users_User1Id",
                table: "PrivateChats",
                column: "User1Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateChats_Users_User2Id",
                table: "PrivateChats",
                column: "User2Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_PrivateMessages_ParentMessageId",
                table: "PrivateMessages",
                column: "ParentMessageId",
                principalTable: "PrivateMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_Users_SenderId",
                table: "PrivateMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StickyNotes_Users_UserId",
                table: "StickyNotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_StudentId",
                table: "Submissions",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Users_UserId",
                table: "CalendarEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroupMembers_Users_UserId",
                table: "ChatGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroups_Users_CreatedById",
                table: "ChatGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatMessages_ParentMessageId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_StudentId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Users_UserId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Users_UserId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateChats_Users_User1Id",
                table: "PrivateChats");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateChats_Users_User2Id",
                table: "PrivateChats");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_PrivateMessages_ParentMessageId",
                table: "PrivateMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_Users_SenderId",
                table: "PrivateMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_StickyNotes_Users_UserId",
                table: "StickyNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_StudentId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropTable(
                name: "SharedPosts");

            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_ParentMessageId",
                table: "PrivateMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ParentMessageId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ParentMessageId",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "ParentMessageId",
                table: "ChatMessages");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "StickyNotes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "PrivateMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "PrivateMessages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "User2Id",
                table: "PrivateChats",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "User1Id",
                table: "PrivateChats",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "PostComments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Enrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "ChatGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "ChatGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatGroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CalendarEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Users_UserId",
                table: "CalendarEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroupMembers_Users_UserId",
                table: "ChatGroupMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroups_Users_CreatedById",
                table: "ChatGroups",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_StudentId",
                table: "Enrollments",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Users_UserId",
                table: "PostComments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Users_UserId",
                table: "Posts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateChats_Users_User1Id",
                table: "PrivateChats",
                column: "User1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateChats_Users_User2Id",
                table: "PrivateChats",
                column: "User2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_Users_SenderId",
                table: "PrivateMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StickyNotes_Users_UserId",
                table: "StickyNotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_StudentId",
                table: "Submissions",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
