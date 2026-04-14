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
    public class NotificationControllerTests
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

        // ---- Create (Direct) ----

        [Fact]
        public async Task Create_DirectNotification_SavesAndRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new NotificationController(context);
            MockUser(controller, 1, "instructor");

            var result = await controller.Create("Assignment Due", "Your assignment is due tomorrow", 5, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var notification = await context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal("Assignment Due", notification!.Title);
            Assert.Equal(5, notification.RecipientId);
            Assert.Equal(1, notification.SenderId);
        }

        // ---- Create (Broadcast) ----

        [Fact]
        public async Task Create_BroadcastNotification_NotifiesAllStudents()
        {
            var context = GetDatabaseContext();
            context.Users.AddRange(
                new User { Id = 2, Username = "s1", Password = "p", Role = "student" },
                new User { Id = 3, Username = "s2", Password = "p", Role = "student" },
                new User { Id = 4, Username = "inst", Password = "p", Role = "instructor" }
            );
            await context.SaveChangesAsync();

            var controller = new NotificationController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.Create("Broadcast", "System-wide message", null, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var count = await context.Notifications.CountAsync();
            Assert.Equal(2, count); // only the 2 students
        }

        // ---- MarkRead ----

        [Fact]
        public async Task MarkRead_ExistingNotification_MarksAsRead()
        {
            var context = GetDatabaseContext();
            context.Notifications.Add(new Notification
            {
                Id = 1,
                Title = "Test",
                Message = "msg",
                IsRead = false
            });
            await context.SaveChangesAsync();

            var controller = new NotificationController(context);
            MockUser(controller, 1);

            var result = await controller.MarkRead(1);

            Assert.IsType<OkResult>(result);
            var notification = await context.Notifications.FindAsync(1);
            Assert.True(notification!.IsRead);
        }

        [Fact]
        public async Task MarkRead_NonExistentNotification_ReturnsOk()
        {
            var context = GetDatabaseContext();
            var controller = new NotificationController(context);
            MockUser(controller, 1);

            var result = await controller.MarkRead(999);

            Assert.IsType<OkResult>(result);
        }

        // ---- Delete ----

        [Fact]
        public async Task Delete_OwnNotification_RemovesAndReturnsOk()
        {
            var context = GetDatabaseContext();
            context.Notifications.Add(new Notification
            {
                Id = 1,
                Title = "My Notification",
                Message = "msg",
                RecipientId = 5
            });
            await context.SaveChangesAsync();

            var controller = new NotificationController(context);
            MockUser(controller, 5);

            var result = await controller.Delete(1);

            Assert.IsType<OkResult>(result);
            Assert.Equal(0, await context.Notifications.CountAsync());
        }

        [Fact]
        public async Task Delete_OtherUsersNotification_DoesNotRemove()
        {
            var context = GetDatabaseContext();
            context.Notifications.Add(new Notification
            {
                Id = 1,
                Title = "Other's Notification",
                Message = "msg",
                RecipientId = 99
            });
            await context.SaveChangesAsync();

            var controller = new NotificationController(context);
            MockUser(controller, 5);

            await controller.Delete(1);

            Assert.Equal(1, await context.Notifications.CountAsync());
        }

        // ---- ClearAll ----

        [Fact]
        public async Task ClearAll_RemovesUserNotificationsAndReturnsOk()
        {
            var context = GetDatabaseContext();
            context.Notifications.AddRange(
                new Notification { Id = 1, Title = "N1", Message = "m", RecipientId = 5 },
                new Notification { Id = 2, Title = "N2", Message = "m", RecipientId = 5 },
                new Notification { Id = 3, Title = "N3", Message = "m", RecipientId = 99 } // other user
            );
            await context.SaveChangesAsync();

            var controller = new NotificationController(context);
            MockUser(controller, 5);

            var result = await controller.ClearAll();

            Assert.IsType<OkResult>(result);

            var remaining = await context.Notifications.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(99, remaining[0].RecipientId);
        }
    }
}
