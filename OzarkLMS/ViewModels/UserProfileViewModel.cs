using OzarkLMS.Models;

namespace OzarkLMS.ViewModels
{
    public class UserProfileViewModel
    {
        public User? User { get; set; }
        public List<Course> EnrolledCourses { get; set; } = new List<Course>();
        public List<Course> TaughtCourses { get; set; } = new List<Course>();
    }
}
