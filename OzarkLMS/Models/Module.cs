using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OzarkLMS.Models
{
    public class Module
    {
        public int Id { get; set; }
        
        public int CourseId { get; set; } // FK

        [Required]
        public string Title { get; set; } = string.Empty;

        public List<ModuleItem> Items { get; set; } = new List<ModuleItem>();
    }

    public class ModuleItem
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "page"; // page, file, quiz, assignment
    }
}
