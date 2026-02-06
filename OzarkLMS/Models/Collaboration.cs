using System.ComponentModel.DataAnnotations;

namespace OzarkLMS.Models
{
    public class ChatGroup
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public int CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // New properties for Group Chat System
        public bool IsDefault { get; set; } = false; // For "Main Organization Chat"
        public int OwnerId { get; set; } // Mutable owner
        public string? GroupPhotoUrl { get; set; } // Custom Group Photo

        // Navigation
        public User CreatedBy { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public List<ChatGroupMember> Members { get; set; } = new List<ChatGroupMember>();
    }

    public class ChatGroupMember
    {
        public int Id { get; set; }
        
        public int ChatGroupId { get; set; }
        public int UserId { get; set; }
        
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
        public string ViewMode { get; set; } = "List"; // "List" or "Grid"

        // Navigation
        public ChatGroup ChatGroup { get; set; }
        public User User { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        
        public string Message { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        
        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public ChatGroup Group { get; set; }
        public User Sender { get; set; }
    }
}
