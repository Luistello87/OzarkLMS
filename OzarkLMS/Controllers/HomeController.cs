using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OzarkLMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var stickyNotes = await _context.StickyNotes.Where(n => n.UserId == user.Id).ToListAsync();
            var announcements = await _context.DashboardAnnouncements.OrderByDescending(a => a.Date).ToListAsync();

            var courses = await _context.Courses.Include(c => c.Instructor).ToListAsync();
            // Fetch all assignments from all courses for the todo list
            var upcomingAssignments = await _context.Assignments.ToListAsync(); // Needs filtering if we had filtering logic

            var viewModel = new DashboardViewModel
            {
                User = user,
                Courses = courses,
                UpcomingAssignments = upcomingAssignments,
                StickyNotes = stickyNotes,
                Announcements = announcements
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStickyNote(string content, string color)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            var note = new StickyNote
            {
                Content = content,
                Color = color,
                UserId = userId
            };
            _context.StickyNotes.Add(note);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStickyNote(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            var note = await _context.StickyNotes.FindAsync(id);
            if (note != null && note.UserId == userId)
            {
                _context.StickyNotes.Remove(note);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStickyNote(int id, string content)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            var note = await _context.StickyNotes.FindAsync(id);
            if (note != null && note.UserId == userId)
            {
                note.Content = content;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnnouncement(string title, DateTime date)
        {
            var announcement = new DashboardAnnouncement
            {
                Title = title,
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc)
            };
            _context.DashboardAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.DashboardAnnouncements.FindAsync(id);
            if (announcement != null)
            {
                _context.DashboardAnnouncements.Remove(announcement);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


    }
}
