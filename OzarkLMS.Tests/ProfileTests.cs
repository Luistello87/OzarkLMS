using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Controllers;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;

namespace OzarkLMS.Tests
{
    public class ProfileTests
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

        private void MockUser(Controller controller, int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("UserId", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task UpdateBio_ValidBio_UpdatesUser()
        {
            // Arrange
            var context = GetDatabaseContext();
            var userId = 1;
            context.Users.Add(new User { Id = userId, Username = "TestUser", Bio = "Old Bio" });
            await context.SaveChangesAsync();

            var controller = new AccountController(context, new Mock<IWebHostEnvironment>().Object, new Mock<IHttpClientFactory>().Object);
            MockUser(controller, userId);

            // Act
            var result = await controller.UpdateBio("I love coding");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);

            var updatedUser = await context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal("I love coding", updatedUser.Bio);
        }
    }
}
