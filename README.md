# OzarkLMS
A Modern Learning Management System built with ASP.NET Core MVC.

## Prerequisites
Before running the application, ensure you have the following installed:
- **.NET 9.0 SDK** (or compatible version)
- **PostgreSQL Database**

## Getting Started

### 1. Database Setup
The application uses PostgreSQL. You need to configure the connection string to match your local environment.

1.  Open `OzarkLMS/appsettings.json`.
2.  Update the `DefaultConnection` string with your PostgreSQL credentials:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Database=OZARK_DB;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
    }
    ```
    *Ensure the database `OZARK_DB` exists or that your user has permissions to create it.*

### 2. Run the Application
Open your terminal in the project root (`OzarkLMS/` folder) and run:

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The application will start effectively at `http://localhost:5099` (check terminal output for the exact port).

### 3. Login Credentials
The application is pre-seeded with the following demo accounts:

**Role: Instructor / Admin**
- **Email:** `admin@ozark.com`
- **Password:** `Admin123!`

**Role: Student**
- **Email:** `student@ozark.com`
- **Password:** `Student123!`

## Troubleshooting

-   **Database Errors**: Ensure PostgreSQL is running and the credentials in `appsettings.json` are correct.
-   **Port In Use**: If `5099` is occupied, change the port in `Properties/launchSettings.json` or run `dotnet run --urls "http://localhost:5001"`.
-   **Login Failed**: If the default credentials don't work, ensure the database was initialized correctly on the first run.

## Features
-   Course Management
-   Assignment & Quiz Creation (with Auto-Grading)
-   Student Gradebook & Dashboards
-   Role-Based Access Control (RBAC)