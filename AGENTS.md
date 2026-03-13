# StartSCH Agent Guidelines

## Overview
This repository contains the StartSCH news site built with ASP.NET Core, Blazor, Entity Framework, and Lit. The application consists of:
- StartSch: Main ASP.NET Core web application
- StartSch.Tests: Unit tests using MSTest
- StartSch.Wasm: Blazor WebAssembly client (currently unused)

## Development Setup

### Prerequisites
- .NET 10 SDK
- bun (for CSS/TypeScript bundling)

### Environment Setup
1. Clone repository
2. Set up AuthSCH credentials:
   ```
   dotnet user-secrets set AuthSch:ClientId YOUR_CLIENT_ID
   dotnet user-secrets set AuthSch:ClientSecret YOUR_CLIENT_SECRET
   ```
3. Install dependencies: `dotnet restore` and `bun install`

## Commands

### Building
```bash
# Standard build
dotnet build

# Build with hot reload (recommended for development)
dotnet watch

# Production build
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter FullyQualifiedName~StartSch.Tests.DateFormatterTests

# Run specific test method
dotnet test --filter FullyQualifiedName~StartSch.Tests.DateFormatterTests.Test
```

### Database Migrations
```bash
# Install EF tools (if needed)
dotnet tool restore
dotnet tool install dotnet-ef

# Add migration
dotnet ef migrations add <MigrationName> --context SqliteDb --output-dir Data/Migrations/Sqlite
dotnet ef migrations add <MigrationName> --context PostgresDb --output-dir Data/Migrations/Postgres

# Apply migrations (happens automatically on startup)
dotnet ef database update --context SqliteDb
dotnet ef database update --context PostgresDb

# Remove last migration
dotnet ef migrations remove --context SqliteDb
dotnet ef migrations remove --context PostgresDb
```

### Asset Bundling
Handled automatically via MSBuild targets:
- Development: `bun run build`
- Production: `bun run build:prod`

## Code Style Guidelines

### Formatting
- Charset: UTF-8
- Line endings: LF (Unix-style)
- Indentation: Spaces (4 spaces per indent)
- YAML files: 2 spaces indent
- Final newline: Always insert
- Trimming whitespace: Not enforced

### C# Conventions
#### Naming
- Classes, interfaces, methods, properties: PascalCase
- Private fields: _camelCase (underscore prefix)
- Parameters, local variables: camelCase
- Constants: PascalCase
- Namespaces: PascalCase matching folder structure

#### Imports
- System namespaces first, alphabetized
- Third-party libraries next, alphabetized
- Project imports last, alphabetized
- Within each group: alphabetical by namespace
- Usings.cs file contains global usings for common imports

#### Types
- Prefer `var` for obvious types
- Explicit types when clarity is needed
- Use `string?` for nullable reference types (nullable enabled)
- Prefer interfaces over concrete types for dependencies
- Use records for immutable data transfer objects

#### Error Handling
- Prefer throwing exceptions for unexpected conditions
- Use `ArgumentNullException`, `ArgumentException` for parameter validation
- Catch specific exceptions rather than general `Exception`
- Log errors using ILogger
- Don't swallow exceptions without good reason
- Use `try/finally` or `using` for resource cleanup

#### Async/Await
- Use `async`/`await` for I/O-bound operations
- Avoid `.Result` or `.GetAwaiter().GetResult()` except in Main
- Configure await with `.ConfigureAwait(false)` when appropriate
- Return `Task` or `Task<T>`, never `void` (except event handlers)

#### Dependency Injection
- Register services with appropriate lifetimes:
  - Singleton: stateless, thread-safe
  - Scoped: per-request (DbContext, services with EF)
  - Transient: lightweight, stateless
- Prefer constructor injection over property injection
- Use `IOptions<TOptions>` for configuration access

#### Blazor Components
- Partial classes for code-behind (.razor.cs)
- Use `@attribute [Authorize]` for authorization
- Event callbacks: use `EventCallback<T>`
- Parameter naming: match C# conventions
- Avoid direct DOM manipulation; use JS interop when necessary

#### Testing (MSTest)
- Test classes: `[TestClass]`
- Test methods: `[TestMethod]`
- Data-driven tests: `[DataRow]` attributes
- Arrange-Act-Assert pattern
- Descriptive test names: MethodUnderTest_Scenario_ExpectedResult
- Use `Assert.AreEqual` for value comparisons
- Use `Assert.IsTrue`/`Assert.IsFalse` for boolean conditions
- Use `Assert.ThrowsException<T>` for exception testing

### Specific Patterns in This Codebase
#### NodaTime Usage
- Configure JSON serialization with `.ConfigureForNodaTime()`
- Use `LocalDateTime`, `Instant`, `ZoneId` appropriately
- Convert between time zones using `InZoneStrictly()`

#### Background Tasks
- Implement `IBackgroundTaskHandler<T>`
- Register with `AddScopedBackgroundTaskHandler`
- Use appropriate retry counts and delays
- Handle cancellation tokens properly

#### Authentication
- OpenID Connect with AuthSCH
- Claims transformation in `UserInformationReceived` event
- Use `Constants` class for scheme names
- Authorization via `[Authorize]` attributes or `AuthorizationService`

#### Entity Framework
- Use `IDbContextFactory<Db>` for DI
- Prefer scoped DbContext in services
- Use `PooledDbContextFactory` for performance
- Migrations separated by provider (Sqlite/Postgres)
- Sensitive data logging only in development

### File Organization
- Features grouped by module (Cmsch, SchBody, etc.)
- Services in Services folder
- Data models and DbContext in Data folder
- Background tasks in BackgroundTasks folder
- Modules follow IModule pattern
- Controllers in Controllers folder
- Components in Components folder
- Tests mirror production structure

## Git Practices
- Branch naming: `feature/description` or `bugfix/description`
- Commit messages: Conventional Commits style
  - feat: new feature
  - fix: bug fix
  - docs: documentation changes
  - style: formatting, missing semicolons
  - refactor: code restructuring
  - test: adding/modifying tests
  - chore: build process changes
- Squash merging for pull requests
- Keep PRs focused on single feature/fix

## Editor Configuration
See `.editorconfig` for detailed settings:
- indent_style = space
- indent_size = 4
- charset = utf-8
- end_of_line = lf
- insert_final_newline = true
- [{*.yml,*.yaml}] indent_size = 2

## Additional Resources
- README.md: Detailed setup and development instructions
- StartSch/Usings.cs: Global using directives
- StartSch/Constants.cs: Application-wide constants
- StartSch.Tests/: Test examples and patterns