using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.ViewModels;
using System.Security.Claims;

namespace OzarkLMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("UserId", user.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password, // Note: Should be hashed in production
                    Role = model.Role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Auto-login after register
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("UserId", user.Id.ToString())
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login");

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.Enrollments)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            var viewModel = new UserProfileViewModel
            {
                User = user,
                EnrolledCourses = user.Enrollments.Select(e => e.Course).ToList(),
                TaughtCourses = await _context.Courses.Where(c => c.InstructorId == userId).ToListAsync()
            };

            // If Admin, load lists and clear course lists (per requirement to replace them)
            if (user.Role == "admin")
            {
                viewModel.AllInstructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
                viewModel.AllStudents = await _context.Users.Where(u => u.Role == "student").ToListAsync();
                
                // Requirement: "Instead of showing Enrolled Courses display..."
                viewModel.EnrolledCourses.Clear(); 
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile? file, string? imageUrl)
        {
             var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
             if (userIdClaim == null) return RedirectToAction("Login");
             var userId = int.Parse(userIdClaim.Value);

             var user = await _context.Users.FindAsync(userId);
             if (user == null) return NotFound();

             if (file != null && file.Length > 0)
             {
                  var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                  if(!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                  var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                  var filePath = Path.Combine(uploads, fileName);
                  using (var stream = new FileStream(filePath, FileMode.Create))
                  {
                      await file.CopyToAsync(stream);
                  }
                  user.ProfilePictureUrl = "/uploads/" + fileName;
             }
             else if (!string.IsNullOrEmpty(imageUrl))
             {
                 user.ProfilePictureUrl = imageUrl;
             }

             await _context.SaveChangesAsync();
             return RedirectToAction(nameof(Profile));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult LoginRedirect()
        {
             return RedirectToAction("Login", "Account");
        }
    }
}
