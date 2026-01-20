using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;

namespace OzarkLMS.Controllers
{
    [Authorize]
    public class CollaborationController : Controller
    {
        private readonly AppDbContext _context;

        public CollaborationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Collaboration
        public async Task<IActionResult> Index()
        {
            var groups = await _context.ChatGroups
                .Include(g => g.CreatedBy)
                .Include(g => g.Messages)
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();

            return View(groups);
        }

        // POST: /Collaboration/CreateGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            var group = new ChatGroup
            {
                Name = name,
                Description = description,
                CreatedById = userId
            };

            _context.ChatGroups.Add(group);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index)); // Or go straight to Details/ID
        }

        // GET: /Collaboration/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var group = await _context.ChatGroups
                .Include(g => g.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            return View(group);
        }

        // POST: /Collaboration/PostMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int groupId, string message, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(message) && file == null) return RedirectToAction(nameof(Details), new { id = groupId });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            string? attachmentUrl = null;

            if (file != null && file.Length > 0)
            {
                // Simple file upload simulation - saving to wwwroot/uploads
                // In real app, use Blob Storage or secure folder
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                attachmentUrl = "/uploads/" + fileName;
            }

            var chatMessage = new ChatMessage
            {
                GroupId = groupId,
                SenderId = userId,
                Message = message ?? (file != null ? "Sent a file" : ""),
                AttachmentUrl = attachmentUrl
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(int groupId, string username)
        {
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (targetUser == null)
            {
                // Better to use TempData for error, skipping for brevity
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var group = await _context.ChatGroups.FindAsync(groupId);
            var senderName = User.Identity!.Name;

            var notification = new Notification
            {
                RecipientId = targetUser.Id,
                Title = "Chat Invitation",
                Message = $"{senderName} invited you to join the group '{group!.Name}'. Go to Student Hub to join!",
                SenderId = int.Parse(User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }
    }
}
