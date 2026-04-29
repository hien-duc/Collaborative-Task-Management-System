# Collaborative Task Management System

A robust, real-time web application built with ASP.NET Core for managing projects, tasks, and team collaboration. This system provides a centralized platform for teams to plan, track, and execute projects efficiently with real-time updates and detailed analytics.

## 🚀 Recent Updates: Supabase Migration
The system has been migrated to use **Supabase** for enhanced data handling and file storage:
- **CDN Integration**: Task attachments are now served via Supabase CDN.
- **Service Layer**: Updated `TaskServiceWithUoW` to interact with Supabase storage.
- **Schema Management**: Optimized PostgreSQL schema handling (Identity in `public`, application data in `internal`).

## Features

### 📋 Project & Task Management
- **Project Planning**: Create and manage projects with custom deadlines, priorities, and status tracking.
- **Kanban Task Board**: Visualize workflow with a dynamic task board featuring drag-and-drop status updates.
- **Task Details**: Assign tasks, set due dates, and attach files hosted on the Supabase CDN.

### 🤝 Collaboration
- **Real-time Notifications**: Instant updates via SignalR for task assignments and project changes.
- **Team Comments**: Real-time threaded discussions within tasks.
- **Role-Based Access**: Secure system access with Admin, Manager, and Team Member roles.

### 📊 Analytics & Administration
- **Interactive Dashboard**: View overall project progress and task status distributions.
- **Admin Dashboard**: System statistics, health monitoring, and audit logs.

## Tech Stack
- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Database**: PostgreSQL (Supabase)
- **Storage/CDN**: Supabase Storage
- **Real-time**: SignalR
- **Frontend**: Razor Pages, Bootstrap 5.1, jQuery
- **Architecture**: Repository Pattern & Unit of Work

## [Demo](https://www.codecademy.com/resources/docs) (Please try not to overflow my database please)( °̥̥̥̥̥̥̥̥◡͐°̥̥̥̥̥̥̥̥)
## Default Credentials (This is admin)
- **Email**: `admin@taskmanager.com`
- **Password**: `Admin@123456`

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Supabase Account](https://supabase.com/)
- Remember to manually create bucket in supabase
- Visual Studio 2022 or JetBrains Rider

### Installation & Setup

1.  **Clone the repository**
    ```bash
    git clone <repository-url>
    cd Collaborative-Task-Management-System
    ```

2.  **Environment Configuration**
    Create update `appsettings.json` with your credentials:
    ```env
    # Database Connection
    ConnectionStrings__DefaultConnection="Host=db.supabase.co;Database=postgres;Username=postgres;Password=your_password"
    ```
    Add these into `.env`
    ```env
    # Supabase CDN & Client Configuration
    SUPABASE_URL="https://your-project-id.supabase.co"
    SUPABASE_KEY="your-service-role-or-anon-key"
    ```

3.  **Apply Migrations**
    ```bash
    dotnet ef database update
    ```

4.  **Run the Application**
    ```bash
    dotnet run
    ```

## Architecture
This project utilizes a **Clean Architecture** approach:
- **Repository & Unit of Work**: Decouples the business logic from data access code, ensuring a single point of truth for database transactions.
- **Service Layer**: All business logic resides in services (e.g., `TaskServiceWithUoW`) which now integrate with the Supabase client.
- **Middleware**: Includes custom audit logging to track critical system actions.