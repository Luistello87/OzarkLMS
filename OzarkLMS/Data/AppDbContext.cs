using Microsoft.EntityFrameworkCore;
using OzarkLMS.Models;

namespace OzarkLMS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleItem> ModuleItems { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionAttachment> SubmissionAttachments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<DashboardAnnouncement> DashboardAnnouncements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        
        public DbSet<PrivateChat> PrivateChats { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }
        
        public DbSet<StickyNote> StickyNotes { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Follow> Follows { get; set; }

        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<PostCommentVote> PostCommentVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Social Hub: Follow Relationship
            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Social Hub: Votes
            modelBuilder.Entity<PostVote>()
                .HasKey(pv => new { pv.PostId, pv.UserId });

            modelBuilder.Entity<PostVote>()
                .HasOne(pv => pv.Post)
                .WithMany(p => p.Votes)
                .HasForeignKey(pv => pv.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostComment>()
                .HasOne(pc => pc.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostComment>()
                .HasOne(pc => pc.ParentComment)
                .WithMany(pc => pc.Replies)
                .HasForeignKey(pc => pc.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a parent comment deletes replies.

            // Comment Votes
            modelBuilder.Entity<PostCommentVote>()
                .HasKey(cv => new { cv.CommentId, cv.UserId });

            modelBuilder.Entity<PostCommentVote>()
                .HasOne(cv => cv.Comment)
                .WithMany(c => c.Votes)
                .HasForeignKey(cv => cv.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter: Soft Delete for Users
            modelBuilder.Entity<User>()
                .HasQueryFilter(u => !u.IsDeleted);

            // Seed generic data if needed here, or just basic config
            base.OnModelCreating(modelBuilder);
            
            // Configure Enrollment relationships explicitly if needed, 
            // but EF Core convention usually handles this well with the Nav properties.
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId);
        }
    }
}
