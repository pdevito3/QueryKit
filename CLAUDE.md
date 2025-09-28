# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QueryKit is a .NET library for parsing and applying filtering and sorting operations to IQueryable and IEnumerable collections. It provides a fluent, string-based syntax for complex queries that translates to efficient LINQ expressions.

## Architecture

- **Core Library**: `/QueryKit/` - Main library with parsing and expression building logic
  - `FilterParser.cs` - Main parser for filter syntax (e.g., `Name == "John" && Age > 25`)  
  - `SortParser.cs` - Parser for sort syntax (e.g., `Name asc, Age desc`)
  - `QueryKitExtensions.cs` - Extension methods (`ApplyQueryKitFilter`, `ApplyQueryKitSort`, `ApplyQueryKit`)
  - `QueryKitPropertyMappings.cs` - Configuration for property aliases and custom behaviors
  - `/Configuration/` - Configuration classes for customizing parsing behavior
  - `/Operators/` - Implementation of comparison operators (equals, contains, greater than, etc.)
  - `/Expressions/` - Expression tree building logic
  - `/Exceptions/` - Custom exception types for parsing errors

- **Test Projects**: 
  - `/QueryKit.UnitTests/` - Unit tests using xUnit
  - `/QueryKit.IntegrationTests/` - Integration tests with Entity Framework and PostgreSQL via Testcontainers
  - `/QueryKit.WebApiTestProject/` - Test web API for integration scenarios
  - `/SharedTestingHelper/` - Shared test utilities and data builders

## Development Commands

### Building
```bash
dotnet build
dotnet build --configuration Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test QueryKit.UnitTests/
dotnet test QueryKit.IntegrationTests/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Packaging
```bash
# Pack NuGet package (already configured for multi-targeting: net6.0, net7.0, net8.0, net9.0)
dotnet pack --configuration Release
```

## Key Dependencies

- **Sprache** (2.3.1) - Parser combinator library for building the filter/sort syntax parser
- **Ardalis.SmartEnum** (8.2.0) - For type-safe enumerations in operators and configuration

## Development Notes

- The library supports multiple .NET versions (net6.0 through net9.0)
- Integration tests use PostgreSQL via Testcontainers for realistic database scenarios  
- Filter syntax supports complex expressions with parentheses, logical operators (&&, ||), and extensive comparison operators
- Property mappings allow aliasing entity properties to different query names
- Custom operators and derived properties can be configured via QueryKitConfiguration
- Error handling provides specific exception types for different parsing failures