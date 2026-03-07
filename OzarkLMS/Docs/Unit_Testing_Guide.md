# OzarkLMS Unit Testing Guide

## Introduction
This document serves as a guide for the OzarkLMS development team to understand how unit testing is implemented within our project. It covers the tools we use, the design patterns we follow, and provides practical examples of how our tests are structured.

## Core Testing Tools
Our unit testing environment relies on three main libraries:

1. **xUnit**: The core testing framework for our application. Tests are denoted using the `[Fact]` attribute, and xUnit is responsible for running these methods and reporting the results.
2. **Entity Framework Core InMemory (`Microsoft.EntityFrameworkCore.InMemory`)**: Used to create a temporary, volatile database in RAM. This ensures that our tests run quickly and do not interfere with or rely on our actual PostgreSQL development/production databases.
3. **Moq**: A mocking framework used to simulate the behavior of complex dependencies (such as `IWebHostEnvironment` or `IHttpClientFactory`). This limits the scope of our tests to just the controller logic.

## The AAA Pattern (Arrange, Act, Assert)
All unit tests in OzarkLMS follow the industry-standard AAA pattern. This ensures our tests are readable, consistent, and easy to maintain.

### 1. Arrange
The setup phase. In this step, we initialize objects, configure mock dependencies, set up the in-memory database, and establish the context required for the test.

**Example Setup (Assignments Controller):**
```csharp
// 1. Initialize the In-Memory Database
var context = GetDatabaseContext(); 
var instructorId = 1;
context.Courses.Add(new Course { Id = 1, Name = "CS101", InstructorId = instructorId });
await context.SaveChangesAsync();

// 2. Mock required dependencies (e.g., Web Host Environment for file paths)
var mockEnv = new Mock<IWebHostEnvironment>();
mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

// 3. Instantiate the Controller being tested
var controller = new AssignmentsController(context, mockEnv.Object);

// 4. Mock the User Context (simulate a logged-in user)
MockUser(controller, instructorId, "instructor");

// 5. Create test data to submit
var newAssignment = new Assignment { Title = "Midterm Project", Points = 100 };
```

### 2. Act
The execution phase. This step is usually a single line of code that invokes the specific method or endpoint being tested using the data prepared in the Arrange phase.

```csharp
// Execute the Create action on the controller
var result = await controller.Create(newAssignment, null);
```

### 3. Assert
The verification phase. Here, we validate that the action produced the expected result. If any assertion fails, the test fails.

```csharp
// 1. Verify the HTTP Response type (e.g., Redirect to a detailed view)
var redirectResult = Assert.IsType<RedirectToActionResult>(result);
Assert.Equal("Details", redirectResult.ActionName);

// 2. Verify Database State (check if the assignment was actually saved)
var createdAssignment = await context.Assignments.FirstOrDefaultAsync(a => a.Title == "Midterm Project");
Assert.NotNull(createdAssignment);
Assert.Equal(100, createdAssignment.Points);
```

## Summary
By rigorously applying the AAA pattern and isolating our controllers using Moq and In-Memory databases. This provides confidence that existing features remain functional as we continue to scale the OzarkLMS platform.

