using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OzarkLMS.Data;
using OzarkLMS.Models;
using OzarkLMS.Services;
using Xunit;

namespace OzarkLMS.Tests
{
    public class SelfTestServiceTests
    {
        private AppDbContext GetDatabaseContext(bool withData = false)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            if (withData)
            {
                context.Users.Add(new User { Id = 1, Username = "admin", Password = "pass", Role = "admin" });
                context.Courses.Add(new Course { Id = 1, Name = "CS101", Code = "CS101" });
                context.SaveChanges();
            }

            return context;
        }

        private SelfTestService CreateService(AppDbContext context)
        {
            var logger = new Mock<ILogger<SelfTestService>>();
            return new SelfTestService(context, logger.Object);
        }

        [Fact]
        public async Task RunAllTestsAsync_ReturnsResults()
        {
            var context = GetDatabaseContext(withData: true);
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task RunAllTestsAsync_MathLogicTest_Passes()
        {
            var context = GetDatabaseContext();
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            var mathTest = results.FirstOrDefault(r => r.TestName == "Math Logic Unit Test");
            Assert.NotNull(mathTest);
            Assert.True(mathTest!.Passed);
        }

        [Fact]
        public async Task RunAllTestsAsync_DatabaseConnectivityTest_PassesWithInMemory()
        {
            var context = GetDatabaseContext();
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            var dbTest = results.FirstOrDefault(r => r.TestName == "Database Connectivity");
            Assert.NotNull(dbTest);
            Assert.True(dbTest!.Passed);
        }

        [Fact]
        public async Task RunAllTestsAsync_UserCountTest_PassesWhenUsersExist()
        {
            var context = GetDatabaseContext(withData: true);
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            var userTest = results.FirstOrDefault(r => r.TestName == "User Data Verification");
            Assert.NotNull(userTest);
            Assert.True(userTest!.Passed);
        }

        [Fact]
        public async Task RunAllTestsAsync_UserCountTest_FailsWhenNoUsers()
        {
            var context = GetDatabaseContext(withData: false);
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            var userTest = results.FirstOrDefault(r => r.TestName == "User Data Verification");
            Assert.NotNull(userTest);
            Assert.False(userTest!.Passed);
        }

        [Fact]
        public async Task RunAllTestsAsync_CourseDataTest_PassesWhenCoursesExist()
        {
            var context = GetDatabaseContext(withData: true);
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            var courseTest = results.FirstOrDefault(r => r.TestName == "Course Data Verification");
            Assert.NotNull(courseTest);
            Assert.True(courseTest!.Passed);
        }

        [Fact]
        public async Task RunAllTestsAsync_ReturnsExactlyFourTests()
        {
            var context = GetDatabaseContext(withData: true);
            var service = CreateService(context);

            var results = await service.RunAllTestsAsync();

            Assert.Equal(4, results.Count);
        }
    }
}
