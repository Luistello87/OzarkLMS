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

        // Navigation
        public User CreatedBy { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
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
