using OzarkLMS.Models;

namespace OzarkLMS.ViewModels
{
    public class AdminDashboardViewModel
    {
        public List<User> Students { get; set; } = new List<User>();
        public List<User> Instructors { get; set; } = new List<User>();
    }
}
