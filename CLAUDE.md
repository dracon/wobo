# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **woboapi**, an ASP.NET Core 10.0 Web API project using PostgreSQL with Entity Framework Core. The project uses minimal API architecture with controllers, services, and EF Core for data access.

## Development Environment

This project uses **mise** for tool management (see `mise.toml`). The environment is configured with:
- .NET 10.0 SDK
- GitVersion.Tool 5.12.0
- ASPNETCORE_ENVIRONMENT set to "Development"

Install mise and run `mise install` to set up the development environment.

## Common Commands

### Build and Run
```bash
dotnet build                    # Build the project
dotnet run                      # Run the application (starts on https://localhost:<port>)
dotnet watch                    # Run with hot reload
```

### Database Operations
```bash
# Add a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Drop database
dotnet ef database drop
```

### Package Management
```bash
dotnet restore                  # Restore NuGet packages
dotnet add package <PackageName> # Add new package
dotnet list package              # List installed packages
```

### Testing
```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~UserServiceTests"

# Run integration tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Run tests with coverage
dotnet-coverage collect "dotnet test" -f xml -o coverage.xml
```

## Testing & CI/CD

### Test Suite
The project has comprehensive test coverage with **39 tests** (17 unit + 22 integration):
- **Unit Tests**: `woboapi.Tests/UserServiceTests.cs` - Tests UserService business logic with mocked dependencies
- **Integration Tests**:
  - `woboapi.Tests/AuthControllerIntegrationTests.cs` - Tests login endpoint
  - `woboapi.Tests/UserControllerIntegrationTests.cs` - Tests CRUD endpoints

Tests use xUnit, Moq for mocking, and in-memory database for isolation.

### GitHub Actions Workflows

#### Main CI/CD Pipeline (`.github/workflows/ci.yml`)
Runs on: push to `main`/`develop`, pull requests, manual trigger

Jobs:
- **build-and-test**: Builds project and runs all tests (unit + integration)
- **code-quality**: Checks code formatting with `dotnet format` and runs security scans
- **build-docker**: Builds Docker image (only on main branch pushes)

#### Code Coverage (`.github/workflows/code-coverage.yml`)
Runs on: push to `main`, pull requests to `main`

- Generates code coverage reports using `dotnet-coverage`
- Uploads coverage to Codecov
- Creates HTML coverage report with ReportGenerator
- Uploads report as artifact

#### PR Validation (`.github/workflows/pr-validation.yml`)
Runs on: pull request events (opened, synchronize, reopened)

- Validates build and tests
- Checks for breaking API changes
- Validates commit messages with commitlint
- Posts automated comment with validation results

### Dependabot Configuration
Located in `.github/dependabot.yml`:
- **NuGet packages**: Weekly updates on Mondays, max 10 PRs
- **GitHub Actions**: Weekly updates on Mondays, max 5 PRs
- Auto-assigns reviewer "wolfgang"
- Tags PRs with appropriate labels

### Viewing Workflow Runs
```bash
# View workflow status (requires gh CLI)
gh workflow list
gh run list
gh run view <run-id>
```

## Architecture

### Database Configuration
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10.0
- **Connection String**: Configured in `appsettings.json` with environment variable override support
  - Default: `appsettings.json` → `ConnectionStrings:DefaultConnection`
  - Override: Environment variable `ConnectionStrings__DefaultConnection` takes precedence (see Program.cs:10-13)
- **DbContext**: `ApplicationDbContext` in `Data/ApplicationDbContext.cs`

### Project Structure
```
Controllers/     # API Controllers (e.g., UserController, WeatherForecastController)
Services/        # Business logic services implementing interfaces
Interfaces/      # Service interface definitions
Models/          # Domain models and DTOs
Data/            # EF Core DbContext
Migrations/      # EF Core database migrations
Exceptions/      # Custom exception classes
```

### Key Patterns

**Service Layer Pattern**: Business logic is separated into services that implement interfaces:
- Interfaces defined in `Interfaces/` (e.g., `IUserService`)
- Implementations in `Services/` (e.g., `UserService`)
- Services are registered in DI container in Program.cs (though UserService registration is currently missing)

**Database Context**: `ApplicationDbContext` manages all database operations:
- Contains `DbSet<UserModel> Users`
- Configures unique index on `UserModel.Email` (Data/ApplicationDbContext.cs:18-19)

**Models**: Entity models in `Models/` directory:
- `UserModel`: Main user entity with Id, Name, Email, Password, Gender, timestamps
- `Gender`: Enum with values: female, male, neutral

### API Documentation

In development mode, the API exposes:
- **OpenAPI spec**: `/openapi/v1.json`
- **Scalar API UI**: `/scalar/v1` (interactive API documentation using Scalar.AspNetCore)

### Important Notes

- The project targets **.NET 10.0** (cutting edge version)
- The Dockerfile references .NET 9.0 images but the project uses .NET 10.0 - this mismatch should be corrected
- `UserService` currently throws `NotImplementedException` for most methods - these are stubs awaiting implementation
- `UserController` namespace is incorrect (`MyApp.Namespace` instead of `woboapi`) and doesn't integrate with `UserService`
- Connection string in `appsettings.json` contains hardcoded credentials (postgres/postgres) - use environment variables in production
