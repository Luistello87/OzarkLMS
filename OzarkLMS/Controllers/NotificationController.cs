using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;

namespace OzarkLMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Notification
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            var userId = int.Parse(userIdClaim.Value);
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId || n.RecipientId == null) // Broadcast + Direct
                .OrderByDescending(n => n.SentDate)
                .Include(n => n.Sender)
                .ToListAsync();

            return View(notifications);
        }

        // GET: /Notification/Send (Admin/Instructor Only)
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Send()
        {
            ViewBag.Students = await _context.Users.Where(u => u.Role == "student").ToListAsync();
            return View();
        }

        // POST: /Notification/Create
        [HttpPost]
        [Authorize(Roles = "admin, instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, string message, int? recipientId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var senderId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var notification = new Notification
            {
                Title = title,
                Message = message,
                SenderId = senderId,
                RecipientId = recipientId // If null, logic for broadcast could be improved, but basic support here
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}
