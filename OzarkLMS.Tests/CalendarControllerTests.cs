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
    public class CalendarControllerTests
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

        private void MockUser(Controller controller, int userId, string role = "student")
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

        [Fact]
        public async Task Create_Event_SavesAndRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new CalendarController(context);
            MockUser(controller, 7);

            var start = DateTime.UtcNow;
            var result = await controller.Create("Study Session", start, start.AddHours(2));

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var evt = await context.CalendarEvents.FirstOrDefaultAsync();
            Assert.NotNull(evt);
            Assert.Equal("Study Session", evt!.Title);
            Assert.Equal(7, evt.UserId);
        }

        [Fact]
        public async Task Create_Event_WithoutEnd_SavesWithNullEnd()
        {
            var context = GetDatabaseContext();
            var controller = new CalendarController(context);
            MockUser(controller, 7);

            var start = DateTime.UtcNow;
            await controller.Create("All Day Event", start, null);

            var evt = await context.CalendarEvents.FirstOrDefaultAsync();
            Assert.NotNull(evt);
            Assert.Null(evt!.End);
        }

        [Fact]
        public async Task Delete_OwnEvent_RemovesAndRedirects()
        {
            var context = GetDatabaseContext();
            context.CalendarEvents.Add(new CalendarEvent
            {
                Id = 1,
                Title = "My Event",
                Start = DateTime.UtcNow,
                UserId = 7
            });
            await context.SaveChangesAsync();

            var controller = new CalendarController(context);
            MockUser(controller, 7);

            var result = await controller.Delete(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(0, await context.CalendarEvents.CountAsync());
        }

        [Fact]
        public async Task Delete_OtherUsersEvent_DoesNotRemove()
        {
            var context = GetDatabaseContext();
            context.CalendarEvents.Add(new CalendarEvent
            {
                Id = 1,
                Title = "Not Mine",
                Start = DateTime.UtcNow,
                UserId = 99 // belongs to another user
            });
            await context.SaveChangesAsync();

            var controller = new CalendarController(context);
            MockUser(controller, 7);

            await controller.Delete(1);

            Assert.Equal(1, await context.CalendarEvents.CountAsync()); // still exists
        }

        [Fact]
        public async Task Delete_NonExistentEvent_StillRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new CalendarController(context);
            MockUser(controller, 7);

            var result = await controller.Delete(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}
