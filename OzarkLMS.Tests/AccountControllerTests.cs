using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using OzarkLMS.Controllers;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.ViewModels;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;

namespace OzarkLMS.Tests
{
    public class AccountControllerTests
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

        private Mock<IAuthenticationService> MockAuth(HttpContext httpContext)
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            authServiceMock.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);
            serviceProviderMock.Setup(s => s.GetService(typeof(IUrlHelperFactory)))
                .Returns(new Mock<IUrlHelperFactory>().Object);

            httpContext.RequestServices = serviceProviderMock.Object;
            return authServiceMock;
        }



        [Fact]
        public async Task Login_ValidUser_RedirectsToHome()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Users.Add(new User { Username = "TestStudent1", Password = "Password123!", Role = "student" });
            await context.SaveChangesAsync();

            var controller = new AccountController(context, new Mock<IWebHostEnvironment>().Object, new Mock<IHttpClientFactory>().Object);

            // Mock HttpContext
            var httpContext = new DefaultHttpContext();
            MockAuth(httpContext);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var model = new LoginViewModel
            {
                Username = "TestStudent1",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_InvalidUser_ReturnsViewWithModelError()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AccountController(context, new Mock<IWebHostEnvironment>().Object, new Mock<IHttpClientFactory>().Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var model = new LoginViewModel
            {
                Username = "NonExistent",
                Password = "WrongPassword"
            };

            // Act
            var result = await controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }
    }
}
