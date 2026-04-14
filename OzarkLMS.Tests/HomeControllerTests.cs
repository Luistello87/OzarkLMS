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
    public class HomeControllerTests
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

        private void MockUser(Controller controller, int userId, string role = "student", string username = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // ---- AddStickyNote ----

        [Fact]
        public async Task AddStickyNote_SavesNoteAndRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new HomeController(context);
            MockUser(controller, 5);

            var result = await controller.AddStickyNote("Remember to study", "yellow");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var note = await context.StickyNotes.FirstOrDefaultAsync();
            Assert.NotNull(note);
            Assert.Equal("Remember to study", note!.Content);
            Assert.Equal("yellow", note.Color);
            Assert.Equal(5, note.UserId);
        }

        // ---- DeleteStickyNote ----

        [Fact]
        public async Task DeleteStickyNote_OwnNote_RemovesAndRedirects()
        {
            var context = GetDatabaseContext();
            context.StickyNotes.Add(new StickyNote { Id = 1, Content = "My Note", UserId = 5, Color = "blue" });
            await context.SaveChangesAsync();

            var controller = new HomeController(context);
            MockUser(controller, 5);

            var result = await controller.DeleteStickyNote(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(0, await context.StickyNotes.CountAsync());
        }

        [Fact]
        public async Task DeleteStickyNote_OtherUsersNote_DoesNotRemove()
        {
            var context = GetDatabaseContext();
            context.StickyNotes.Add(new StickyNote { Id = 1, Content = "Not Mine", UserId = 99, Color = "blue" });
            await context.SaveChangesAsync();

            var controller = new HomeController(context);
            MockUser(controller, 5);

            await controller.DeleteStickyNote(1);

            Assert.Equal(1, await context.StickyNotes.CountAsync());
        }

        // ---- UpdateStickyNote ----

        [Fact]
        public async Task UpdateStickyNote_OwnNote_UpdatesContentAndRedirects()
        {
            var context = GetDatabaseContext();
            context.StickyNotes.Add(new StickyNote { Id = 1, Content = "Old Content", UserId = 5, Color = "green" });
            await context.SaveChangesAsync();

            var controller = new HomeController(context);
            MockUser(controller, 5);

            var result = await controller.UpdateStickyNote(1, "New Content");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var note = await context.StickyNotes.FindAsync(1);
            Assert.Equal("New Content", note!.Content);
        }

        [Fact]
        public async Task UpdateStickyNote_OtherUsersNote_DoesNotUpdate()
        {
            var context = GetDatabaseContext();
            context.StickyNotes.Add(new StickyNote { Id = 1, Content = "Original", UserId = 99, Color = "green" });
            await context.SaveChangesAsync();

            var controller = new HomeController(context);
            MockUser(controller, 5);

            await controller.UpdateStickyNote(1, "Tampered");

            var note = await context.StickyNotes.FindAsync(1);
            Assert.Equal("Original", note!.Content);
        }

        // ---- AddAnnouncement ----

        [Fact]
        public async Task AddAnnouncement_Admin_SavesAndRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new HomeController(context);
            MockUser(controller, 1, "admin");

            var date = DateTime.UtcNow;
            var result = await controller.AddAnnouncement("Welcome!", date);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var announcement = await context.DashboardAnnouncements.FirstOrDefaultAsync();
            Assert.NotNull(announcement);
            Assert.Equal("Welcome!", announcement!.Title);
        }

        // ---- DeleteAnnouncement ----

        [Fact]
        public async Task DeleteAnnouncement_Admin_RemovesAndRedirects()
        {
            var context = GetDatabaseContext();
            context.DashboardAnnouncements.Add(new DashboardAnnouncement
            {
                Id = 1,
                Title = "Old Announcement",
                Date = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = new HomeController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.DeleteAnnouncement(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(0, await context.DashboardAnnouncements.CountAsync());
        }

        [Fact]
        public async Task DeleteAnnouncement_NonExistent_StillRedirects()
        {
            var context = GetDatabaseContext();
            var controller = new HomeController(context);
            MockUser(controller, 1, "admin");

            var result = await controller.DeleteAnnouncement(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}
