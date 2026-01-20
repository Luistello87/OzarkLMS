using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace OzarkLMS.Controllers
{
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.Include(c => c.Instructor).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Items)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }
        
        // GET: Courses/Create
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Instructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Create([Bind("Id,Name,Code,Term,Color,Icon,InstructorId")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Instructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
            return View(course);
        }
        // GET: Courses/Edit/5
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewBag.Instructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Term,Color,Icon,InstructorId")] Course course)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Courses.Any(e => e.Id == course.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Instructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
            return View(course);
        }

        // GET: Courses/ManageStudents/5
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> ManageStudents(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            var enrolledStudentIds = course.Enrollments.Select(e => e.StudentId).ToList();
            
            // Get all students NOT enrolled in this course
            var availableStudents = await _context.Users
                .Where(u => u.Role == "student" && !enrolledStudentIds.Contains(u.Id))
                .ToListAsync();

            var viewModel = new CourseStudentsViewModel
            {
                Course = course,
                EnrolledStudents = course.Enrollments.Select(e => e.Student).ToList(),
                AvailableStudents = availableStudents
            };

            return View(viewModel);
        }

        // POST: Courses/AddStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(int courseId, int studentId)
        {
            var enrollment = new Enrollment { CourseId = courseId, StudentId = studentId };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageStudents), new { id = courseId });
        }

        // POST: Courses/RemoveStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int courseId, int studentId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId);
            
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageStudents), new { id = courseId });
        }
    }
}
