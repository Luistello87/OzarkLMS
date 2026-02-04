using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using Microsoft.AspNetCore.Authorization;

namespace OzarkLMS.Controllers
{
    [Authorize(Roles = "admin, instructor")]
    public class ModulesController : Controller
    {
        private readonly AppDbContext _context;

        public ModulesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Modules/Create?courseId=5
        public IActionResult Create(int? courseId)
        {
            if (courseId == null) return NotFound();
            
            // Security Check
            if (User.IsInRole("instructor"))
            {
                var course = _context.Courses.Find(courseId);
                if (course == null) return NotFound();
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (course.InstructorId != userId) return Forbid();
            }

            ViewBag.CourseId = courseId;
            return View();
        }

        // POST: Modules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,Title")] Module module)
        {
            if (ModelState.IsValid)
            {
                if (User.IsInRole("instructor"))
                {
                    var course = await _context.Courses.FindAsync(module.CourseId);
                    if (course == null) return NotFound();
                    var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                    if (course.InstructorId != userId) return Forbid();
                }

                _context.Add(module);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Courses", new { id = module.CourseId });
            }
            ViewBag.CourseId = module.CourseId;
            return View(module);
        }

        // GET: Modules/AddItem?moduleId=5
        public async Task<IActionResult> AddItem(int? moduleId)
        {
            if (moduleId == null) return NotFound();
            var module = await _context.Modules.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == moduleId);
            if (module == null) return NotFound();
            
            if (User.IsInRole("instructor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (module.Course.InstructorId != userId) return Forbid();
            }
            
            ViewBag.Module = module;
            return View();
        }

        // POST: Modules/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int moduleId, string title, string type, IFormFile? file)
        {
            var module = await _context.Modules.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == moduleId);
            if (module == null) return NotFound();

            if (User.IsInRole("instructor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (module.Course.InstructorId != userId) return Forbid();
            }

            string? contentUrl = null;

            if (type == "file" && file != null && file.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if(!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                contentUrl = "/uploads/" + fileName;
            }

            var item = new ModuleItem
            {
                ModuleId = moduleId,
                Title = title,
                Type = type,
                ContentUrl = contentUrl
            };

            _context.ModuleItems.Add(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Courses", new { id = module.CourseId });
        }
    }
}
