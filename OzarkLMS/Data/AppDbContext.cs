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
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<DashboardAnnouncement> DashboardAnnouncements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<StickyNote> StickyNotes { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
