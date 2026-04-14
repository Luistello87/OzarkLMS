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
    public class CoursesControllerTests
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

        private void MockUser(Controller controller, int userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // ---- Index ----

        [Fact]
        public async Task Index_AdminRole_ReturnsAllCourses()
        {
            var context = GetDatabaseContext();
            context.Courses.AddRange(
                new Course { Id = 1, Name = "Course A", Code = "CA101" },
                new Course { Id = 2, Name = "Course B", Code = "CB101" }
            );
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var courses = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Equal(2, courses.Count());
        }

        [Fact]
        public async Task Index_InstructorRole_ReturnsOwnCourses()
        {
            var context = GetDatabaseContext();
            context.Courses.AddRange(
                new Course { Id = 1, Name = "My Course", Code = "MC101", InstructorId = 10 },
                new Course { Id = 2, Name = "Other Course", Code = "OC101", InstructorId = 99 }
            );
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 10, "instructor");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var courses = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Single(courses);
        }

        [Fact]
        public async Task Index_StudentRole_ReturnsEnrolledCourses()
        {
            var context = GetDatabaseContext();
            context.Courses.AddRange(
                new Course { Id = 1, Name = "Enrolled Course", Code = "EC101" },
                new Course { Id = 2, Name = "Not Enrolled", Code = "NE101" }
            );
            context.Enrollments.Add(new Enrollment { CourseId = 1, StudentId = 5 });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 5, "student");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var courses = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Single(courses);
        }

        // ---- Details ----

        [Fact]
        public async Task Details_NullId_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "student");

            var result = await controller.Details(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ValidId_ReturnsCourse()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Test Course", Code = "TC101" });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "student");

            var result = await controller.Details(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Course>(viewResult.Model);
        }

        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "student");

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---- Create ----

        [Fact]
        public async Task Create_ValidCourse_Admin_RedirectsToIndex()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var course = new Course { Name = "New Course", Code = "NC101" };
            var result = await controller.Create(course);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            Assert.Equal(1, await context.Courses.CountAsync());
        }

        [Fact]
        public async Task Create_ValidCourse_Instructor_AssignsSelfAsInstructor()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 42, "instructor");

            var course = new Course { Name = "My Course", Code = "MC101" };
            var result = await controller.Create(course);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var created = await context.Courses.FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(42, created.InstructorId);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsView()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");
            controller.ModelState.AddModelError("Name", "Required");

            var course = new Course { Code = "NC101" };
            var result = await controller.Create(course);

            Assert.IsType<ViewResult>(result);
        }

        // ---- Edit ----

        [Fact]
        public async Task Edit_Get_NullId_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.Edit(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidCourse_Admin_RedirectsToIndex()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Old Name", Code = "OC101" });
            await context.SaveChangesAsync();

            // Detach so the controller can track the updated entity without conflict
            context.ChangeTracker.Clear();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var updated = new Course { Id = 1, Name = "Updated Name", Code = "OC101" };
            var result = await controller.Edit(1, updated);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var course = new Course { Id = 99, Name = "Test", Code = "T101" };
            var result = await controller.Edit(1, course);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---- Delete ----

        [Fact]
        public async Task Delete_ValidId_Admin_RedirectsToIndex()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "To Delete", Code = "TD101" });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.Delete(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(0, await context.Courses.CountAsync());
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            var context = GetDatabaseContext();
            var controller = new CoursesController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---- AddStudent / RemoveStudent ----

        [Fact]
        public async Task AddStudent_CreatesEnrollment_RedirectsToManageStudents()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101" });
            context.Users.Add(new User { Id = 5, Username = "student1", Password = "pass", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "instructor");

            var result = await controller.AddStudent(1, 5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageStudents", redirect.ActionName);
            Assert.Equal(1, await context.Enrollments.CountAsync());
        }

        [Fact]
        public async Task RemoveStudent_RemovesEnrollment_RedirectsToManageStudents()
        {
            var context = GetDatabaseContext();
            context.Enrollments.Add(new Enrollment { CourseId = 1, StudentId = 5 });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "instructor");

            var result = await controller.RemoveStudent(1, 5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageStudents", redirect.ActionName);
            Assert.Equal(0, await context.Enrollments.CountAsync());
        }

        // ---- AddMeeting ----

        [Fact]
        public async Task AddMeeting_ValidData_RedirectsToDetails()
        {
            var context = GetDatabaseContext();
            context.Courses.Add(new Course { Id = 1, Name = "Course", Code = "C101" });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "instructor");

            var start = DateTime.UtcNow;
            var end = start.AddHours(1);
            var result = await controller.AddMeeting(1, "Lecture 1", start, end, "https://zoom.us/meeting");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(1, await context.Meetings.CountAsync());
        }

        // ---- DeleteMeeting ----

        [Fact]
        public async Task DeleteMeeting_ValidId_RemovesMeeting()
        {
            var context = GetDatabaseContext();
            context.Meetings.Add(new Meeting
            {
                Id = 1,
                CourseId = 1,
                Name = "Lecture 1",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            });
            await context.SaveChangesAsync();

            var controller = new CoursesController(context);
            MockUser(controller, 1, "instructor");

            var result = await controller.DeleteMeeting(1, 1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(0, await context.Meetings.CountAsync());
        }
    }
}
