using Microsoft.AspNetCore.Hosting;
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
    public class ModulesControllerTests
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

        private void MockUser(Controller controller, int userId, string role = "instructor")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // ---- Create ----

        [Fact]
        public async Task Create_ValidModule_Instructor_RedirectsToCourseDetails()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "CS101", Code = "CS101", InstructorId = 10 });
            context.Enrollments.Add(new Enrollment { CourseId = 1, StudentId = 20 });
            context.Users.Add(new User { Id = 20, Username = "s1", Password = "p", Role = "student" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("/tmp/wwwroot");

            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 10, "instructor");

            var module = new Module { CourseId = 1, Title = "Week 1" };
            var result = await controller.Create(module, null, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Courses", redirect.ControllerName);

            var created = await context.Modules.FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Week 1", created!.Title);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsView()
        {
            var context = GetDatabaseContext();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 10, "instructor");
            controller.ModelState.AddModelError("Title", "Required");

            var module = new Module { CourseId = 1 };
            var result = await controller.Create(module, null, null);

            Assert.IsType<ViewResult>(result);
        }

        // ---- Delete (Module) ----

        [Fact]
        public async Task Delete_Module_Admin_RemovesAndRedirects()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101", InstructorId = 10 });
            context.Modules.Add(new Module { Id = 1, CourseId = 1, Title = "Module 1" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 1, "admin");

            var result = await controller.Delete(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(0, await context.Modules.CountAsync());
        }

        [Fact]
        public async Task Delete_Module_NonExistent_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 1, "admin");

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---- DeleteItem ----

        [Fact]
        public async Task DeleteItem_ValidItem_Admin_RemovesAndRedirects()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101", InstructorId = 10 });
            context.Modules.Add(new Module { Id = 1, CourseId = 1, Title = "Module 1" });
            context.ModuleItems.Add(new ModuleItem { Id = 1, ModuleId = 1, Title = "Item 1", Type = "file" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 1, "admin");

            var result = await controller.DeleteItem(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(0, await context.ModuleItems.CountAsync());
        }

        [Fact]
        public async Task DeleteItem_NonExistent_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 1, "admin");

            var result = await controller.DeleteItem(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---- AddItem (non-file) ----

        [Fact]
        public async Task AddItem_NonFileType_SavesItemAndRedirects()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101", InstructorId = 10 });
            context.Modules.Add(new Module { Id = 1, CourseId = 1, Title = "Module 1" });
            context.Enrollments.Add(new Enrollment { CourseId = 1, StudentId = 20 });
            context.Users.Add(new User { Id = 20, Username = "s1", Password = "p", Role = "student" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 10, "instructor");

            var result = await controller.AddItem(1, "Lecture Notes", "page", null, "link");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            var item = await context.ModuleItems.FirstOrDefaultAsync();
            Assert.NotNull(item);
            Assert.Equal("Lecture Notes", item!.Title);
            Assert.Equal("page", item.Type);
        }

        [Fact]
        public async Task AddItem_NonExistentModule_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ModulesController(context, mockEnv.Object);
            MockUser(controller, 10, "instructor");

            var result = await controller.AddItem(999, "Title", "page", null, null);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
