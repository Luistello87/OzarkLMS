using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OzarkLMS.Controllers;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace OzarkLMS.Tests
{
    public class AssignmentsControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        }

        [Fact]
        public async Task Create_Assignment_Instructor_Success()
        {
            // Arrange
            var context = GetDatabaseContext();
            var instructorId = 1;
            context.Courses.Add(new Course { Id = 1, Name = "CS101", InstructorId = instructorId });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");
            var controller = new AssignmentsController(context, mockEnv.Object);
            MockUser(controller, instructorId, "instructor");

            var newAssignment = new Assignment
            {
                CourseId = 1,
                Title = "Midterm Project",
                Type = "assignment",
                Points = 100,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = await controller.Create(newAssignment, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Courses", redirectResult.ControllerName);

            var createdAssignment = await context.Assignments.FirstOrDefaultAsync(a => a.Title == "Midterm Project");
            Assert.NotNull(createdAssignment);
            Assert.Equal(100, createdAssignment.Points);
        }

        [Fact]
        public async Task Create_Assignment_MissingTitle_ReturnsViewWithModelError()
        {
            // Arrange
            var context = GetDatabaseContext();
            var instructorId = 1;
            context.Courses.Add(new Course { Id = 1, Name = "CS101", InstructorId = instructorId });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");
            var controller = new AssignmentsController(context, mockEnv.Object);
            MockUser(controller, instructorId, "instructor");

            var invalidAssignment = new Assignment
            {
                CourseId = 1,
                Title = "", // Missing Title
                Type = "assignment",
                Points = 100
            };

            // Manually add model error because binding doesn't run in unit tests automatically
            controller.ModelState.AddModelError("Title", "The Title field is required.");

            // Act
            var result = await controller.Create(invalidAssignment, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            // Ensure View is returned (either default view or specifically named)
        }

        [Fact]
        public async Task SubmitAssignment_Student_Success()
        {
            // Arrange
            var context = GetDatabaseContext();
            var studentId = 2;
            var assignmentId = 1;

            context.Assignments.Add(new Assignment { Id = assignmentId, CourseId = 1, Title = "Test Assignment" });
            context.Users.Add(new User { Id = studentId, Username = "Student1", Role = "student" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");
            var controller = new AssignmentsController(context, mockEnv.Object);
            MockUser(controller, studentId, "student");

            // Mock File Upload
            var formFiles = new List<IFormFile>();
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.pdf");
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            formFiles.Add(fileMock.Object);

            // Act
            var result = await controller.SubmitAssignment(assignmentId, "My submission", formFiles);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Take", redirectResult.ActionName); // Redirects back to Take page to show banner

            var submission = await context.Submissions.FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
            Assert.NotNull(submission);
            // Note: AttachmentUrl logic mocks file system interactions which might fail in unit test if not careful, 
            // but controller wraps it in Directory checks. 
            // Ideally we mock environment or ensure wwwroot/uploads exists.
            // For this test, we verify submission record is created.
        }

        [Fact]
        public async Task GradeSubmission_Instructor_Success()
        {
            // Arrange
            var context = GetDatabaseContext();
            var instructorId = 1;
            var submissionId = 1;

            context.Submissions.Add(new Submission { Id = submissionId, AssignmentId = 1, StudentId = 2, Score = 0 });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");
            var controller = new AssignmentsController(context, mockEnv.Object);
            MockUser(controller, instructorId, "instructor");

            // Act
            var result = await controller.GradeSubmission(submissionId, 95, "Great job!");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Submissions", redirectResult.ActionName);

            var updatedSubmission = await context.Submissions.FindAsync(submissionId);
            Assert.NotNull(updatedSubmission);
            Assert.Equal(95, updatedSubmission.Score);
            Assert.Equal("Great job!", updatedSubmission.Feedback);
        }

    }
}
