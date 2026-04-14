using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OzarkLMS.Controllers;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;
using Xunit;

namespace OzarkLMS.Tests
{
    public class AdminControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void MockAdminUser(Controller controller)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("UserId", "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // ---- Dashboard ----

        [Fact]
        public async Task Dashboard_ReturnsViewWithCounts()
        {
            var context = GetDatabaseContext();
            context.Users.AddRange(
                new User { Id = 1, Username = "s1", Password = "p", Role = "student" },
                new User { Id = 2, Username = "s2", Password = "p", Role = "student" },
                new User { Id = 3, Username = "i1", Password = "p", Role = "instructor" }
            );
            context.Courses.Add(new Course { Id = 1, Name = "Course1", Code = "C101" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.Dashboard();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(2, viewResult.ViewData["StudentCount"]);
            Assert.Equal(1, viewResult.ViewData["InstructorCount"]);
            Assert.Equal(1, viewResult.ViewData["CourseCount"]);
        }

        // ---- CreateUser ----

        [Fact]
        public async Task CreateUser_ValidUser_RedirectsToDashboard()
        {
            var context = GetDatabaseContext();
            var controller = new AdminController(context);
            MockAdminUser(controller);

            var user = new User { Username = "newstudent", Password = "pass123", Role = "student" };
            var result = await controller.CreateUser(user);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task CreateUser_DuplicateUsername_ReturnsViewWithError()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 1, Username = "existinguser", Password = "pass", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var user = new User { Username = "existinguser", Password = "newpass", Role = "student" };
            var result = await controller.CreateUser(user);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task CreateUser_InvalidModel_ReturnsView()
        {
            var context = GetDatabaseContext();
            var controller = new AdminController(context);
            MockAdminUser(controller);
            controller.ModelState.AddModelError("Username", "Required");

            var user = new User { Password = "pass", Role = "student" };
            var result = await controller.CreateUser(user);

            Assert.IsType<ViewResult>(result);
        }

        // ---- CreateInstructor ----

        [Fact]
        public async Task CreateInstructor_NewUser_RedirectsToDashboard()
        {
            var context = GetDatabaseContext();
            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.CreateInstructor("instructor1", "pass123");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);

            var created = await context.Users.FirstOrDefaultAsync(u => u.Username == "instructor1");
            Assert.NotNull(created);
            Assert.Equal("instructor", created.Role);
        }

        [Fact]
        public async Task CreateInstructor_DuplicateUsername_RedirectsWithoutCreating()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 1, Username = "instructor1", Password = "pass", Role = "instructor" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.CreateInstructor("instructor1", "newpass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
            Assert.Equal(1, await context.Users.CountAsync()); // No new user added
        }

        // ---- DeleteUser ----

        [Fact]
        public async Task DeleteUser_ExistingUser_SoftDeletesUser()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 5, Username = "todelete", Password = "pass", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.DeleteUser(5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);

            var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == 5);
            Assert.NotNull(user);
            Assert.True(user!.IsDeleted);
        }

        [Fact]
        public async Task DeleteUser_InstructorWithCourses_UnassignsCourses()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 10, Username = "instructor", Password = "pass", Role = "instructor" });
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101", InstructorId = 10 });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            await controller.DeleteUser(10);

            var course = await context.Courses.FindAsync(1);
            Assert.NotNull(course);
            Assert.Null(course!.InstructorId);
        }

        [Fact]
        public async Task DeleteUser_NonExistentUser_RedirectsToDashboard()
        {
            var context = GetDatabaseContext();
            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.DeleteUser(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
        }

        // ---- EditUser ----

        [Fact]
        public async Task EditUser_ValidData_UpdatesUserAndRedirects()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 3, Username = "oldname", Password = "oldpass", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.EditUser(3, "newname", "newpass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);

            var user = await context.Users.FindAsync(3);
            Assert.Equal("newname", user!.Username);
            Assert.Equal("newpass", user.Password);
        }

        [Fact]
        public async Task EditUser_EmptyPassword_DoesNotUpdatePassword()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { Id = 3, Username = "user", Password = "originalpass", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            await controller.EditUser(3, "user", "");

            var user = await context.Users.FindAsync(3);
            Assert.Equal("originalpass", user!.Password);
        }

        [Fact]
        public async Task EditUser_DuplicateUsername_RedirectsWithoutSaving()
        {
            var context = GetDatabaseContext();
            context.Users.AddRange(
                new User { Id = 1, Username = "existing", Password = "p", Role = "student" },
                new User { Id = 2, Username = "other", Password = "p", Role = "student" }
            );
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.EditUser(2, "existing", "newpass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);

            var user = await context.Users.FindAsync(2);
            Assert.Equal("other", user!.Username); // unchanged
        }

        [Fact]
        public async Task EditUser_NonExistentUser_RedirectsToDashboard()
        {
            var context = GetDatabaseContext();
            var controller = new AdminController(context);
            MockAdminUser(controller);

            var result = await controller.EditUser(999, "name", "pass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
        }
    }
}
