using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace OzarkLMS.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var students = await _context.Users.Where(u => u.Role == "student").ToListAsync();
            var instructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
            var courses = await _context.Courses.ToListAsync();

            ViewBag.StudentCount = students.Count;
            ViewBag.InstructorCount = instructors.Count;
            ViewBag.CourseCount = courses.Count;

            var viewModel = new AdminDashboardViewModel
            {
                Students = students,
                Instructors = instructors
            };

            return View(viewModel);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("Username,Password,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                // In a real app, check for duplicates and hash password
                if (_context.Users.Any(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Dashboard));
            }
            return View(user);
        }

        // POST: Admin/CreateInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstructor(string username, string password)
        {
             if (await _context.Users.AnyAsync(u => u.Username == username))
             {
                 // In a real app we'd pass an error back, for now just redirect
                 return RedirectToAction(nameof(Dashboard));
             }

             var user = new User
             {
                 Username = username,
                 Password = password, // Note: Should be hashed
                 Role = "instructor"
             };
             _context.Users.Add(user);
             await _context.SaveChangesAsync();
             return RedirectToAction(nameof(Dashboard));
        }
        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return RedirectToAction(nameof(Dashboard));

            // 1. Remove Enrollments (Cascade Delete)
            var enrollments = _context.Enrollments.Where(e => e.StudentId == id);
            _context.Enrollments.RemoveRange(enrollments);

            // 2. Remove Submissions (Cascade Delete)
            var submissions = _context.Submissions.Where(s => s.StudentId == id);
            _context.Submissions.RemoveRange(submissions);

            // 3. Unassign Courses (if Instructor)
            var courses = _context.Courses.Where(c => c.InstructorId == id);
            foreach (var course in courses)
            {
                course.InstructorId = null;
            }

            // 4. Remove User
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
