using OzarkLMS.Models;

namespace OzarkLMS.ViewModels
{
    public class DashboardViewModel
    {
        public User User { get; set; } = null!;
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Assignment> UpcomingAssignments { get; set; } = new List<Assignment>();
        public List<StickyNote> StickyNotes { get; set; } = new List<StickyNote>();
        public List<DashboardAnnouncement> Announcements { get; set; } = new List<DashboardAnnouncement>();
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    }
}
