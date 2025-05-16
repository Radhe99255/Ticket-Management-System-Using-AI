# Ticket Management System Using AI

A full-stack ticket management system built with ASP.NET Core and Entity Framework Core.

## Features

- User authentication and authorization
- Ticket creation and management
- Real-time messaging system
- Multi-level logging (Info, Warning, Error)
- Role-based access control (Admin and Regular users)
- RESTful API endpoints

## Technical Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Logging**: Serilog
- **Real-time Communication**: SignalR

## Project Structure

- **TicketManagement.Api**: REST API and backend logic
- **TicketManagement.Data**: Data access layer and models
- **TicketManagement.Web**: Web interface (MVC)

## Getting Started

1. Clone the repository
2. Update the connection string in `appsettings.json`
3. Run Entity Framework migrations:
   ```
   dotnet ef database update
   ```
4. Run the application:
   ```
   dotnet run
   ```

## Logging

The application uses Serilog for structured logging with three separate log files:
- Info level logs: `Logs/info-{date}.txt`
- Warning level logs: `Logs/warning-{date}.txt`
- Error level logs: `Logs/error-{date}.txt`
