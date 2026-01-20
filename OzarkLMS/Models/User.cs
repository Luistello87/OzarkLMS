using System.ComponentModel.DataAnnotations;

namespace OzarkLMS.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // In a real app, hash this!

        [Required]
        public string Role { get; set; } = "student"; // student, instructor, admin

        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
