using OzarkLMS.Models;
using System.Collections.Generic;

namespace OzarkLMS.ViewModels
{
    public class CourseStudentsViewModel
    {
        public List<User> EnrolledStudents { get; set; } = new List<User>();
        public List<User> AvailableStudents { get; set; } = new List<User>();
    }
}
