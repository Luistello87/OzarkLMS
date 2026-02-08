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

            var currentUserId = int.Parse(userIdClaim.Value);
            
            // If id is provided, view that user's profile. Otherwise view own.
            // But the current route is just /Account/Profile. 
            // We need to support viewing others. Let's stick to "Own Profile" for now per existing code, 
            // OR if a query param ?userId=5 is passed.
            // The signature is just Profile(). Let's check query string manually or assume this is "My Profile".
            // Requirement: "The profile owner sees all their posts. Other users see only posts they are allowed to see."
            // This implies we CAN view other profiles.
            
            // Let's check if 'id' is in Query
            int targetUserId = currentUserId;
            if (Request.Query.ContainsKey("userId"))
            {
                int.TryParse(Request.Query["userId"], out targetUserId);
            }

            var user = await _context.Users
                .AsSplitQuery()
                .Include(u => u.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Votes)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(c => c.User)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(c => c.Votes)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(c => c.Replies)
                            .ThenInclude(r => r.User)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(c => c.Replies)
                            .ThenInclude(r => r.Votes)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null) return NotFound();

            var viewModel = new UserProfileViewModel
            {
                User = user,
                EnrolledCourses = user.Enrollments.Select(e => e.Course).ToList(),
                TaughtCourses = await _context.Courses.Where(c => c.InstructorId == targetUserId).ToListAsync(),
                ChatGroups = await _context.ChatGroupMembers
                    .Where(m => m.UserId == targetUserId)
                    .Select(m => m.ChatGroup)
                    .OrderByDescending(g => g.IsDefault)
                    .ToListAsync(),
                
                // Social Hub
                Posts = user.Posts.OrderByDescending(p => p.CreatedAt).ToList(),
                FollowersCount = user.Followers.Count,
                FollowingCount = user.Following.Count,
                IsFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId)
            };

            // If Admin, load lists and clear course lists (per requirement to replace them)
            if (user.Role == "admin" && targetUserId == currentUserId)
            {
                viewModel.AllInstructors = await _context.Users.Where(u => u.Role == "instructor").ToListAsync();
                viewModel.AllStudents = await _context.Users.Where(u => u.Role == "student").ToListAsync();
                viewModel.EnrolledCourses.Clear(); 
            }

            ViewBag.CurrentUserId = currentUserId; // To check if Owner
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBio(string bio)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login");
            var userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Bio = bio;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(int targetUserId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login");
            var currentUserId = int.Parse(userIdClaim.Value);

            if (currentUserId == targetUserId) return RedirectToAction(nameof(Profile));

            var existingFollow = await _context.Follows.FindAsync(currentUserId, targetUserId);
            if (existingFollow != null)
            {
                _context.Follows.Remove(existingFollow);
            }
            else
            {
                _context.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = targetUserId });
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Profile), new { userId = targetUserId });
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
