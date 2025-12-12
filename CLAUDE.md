# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Azure Blob Storage provider for Umbraco CMS temporary file uploads. Implements `ITemporaryFileRepository` to redirect temporary file storage from local disk to Azure Blob Storage, enabling stateless container deployments.

**Target Framework:** .NET 10
**Umbraco Version:** 17.x
**Package Version:** Aligns with Umbraco versioning (17.x = Umbraco 17)

## Build Commands

```bash
# Build the solution
dotnet build

# Build release
dotnet build -c Release

# Pack NuGet package
dotnet pack Mole.StorageProviders.AzureBlob.TemporaryFile/Mole.StorageProviders.AzureBlob.TemporaryFile.csproj -c Release -o Build.Out

# Run test site (requires Azure Blob Storage config in appsettings.json)
dotnet run --project TestSite
```

## Architecture

The library auto-registers via Umbraco's composer system (`TemporaryFileComposer`). No manual configuration needed beyond appsettings.

### Key Components

- **BlobTemporaryFileRepository** - Core implementation of `ITemporaryFileRepository`. Stores each upload as two blobs: the file content (`{guid}`) and a metadata sidecar (`{guid}.metadata`) containing filename, key, and expiration.

- **ITemporaryBlobClientFactory / TemporaryBlobClientFactory** - Factory for Azure `BlobServiceClient` creation from connection string.

- **TemporaryFileSettings** - Configuration POCO bound to `Umbraco:Storage:AzureBlob:TemporaryFile` section. Properties: `ConnectionString`, `ContainerName` (default: "tempfiles").

- **TemporaryFileComposer** - Umbraco IComposer that calls `AddBlobTemporaryFile()` extension method to wire up DI.

### Configuration Path

Settings are read from: `Umbraco:Storage:AzureBlob:TemporaryFile`

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "TemporaryFile": {
          "ConnectionString": "...",
          "ContainerName": "tempfiles"
        }
      }
    }
  }
}
```

## Project Structure

- `Mole.StorageProviders.AzureBlob.TemporaryFile/` - Main library (NuGet package)
- `TestSite/` - Umbraco test site for local development/debugging

## CI/CD

Releases triggered by pushing tags matching `v[0-9]+.[0-9]+.[0-9]+*`. Pipeline packs and publishes to NuGet.

## Code Style

**Namespace & File Organization**
- File-scoped namespaces (`namespace X;`)
- One class per file, filename matches class name

**Type Declarations**
- Primary constructors for classes with dependencies
- `required` modifier on properties that must be set
- Expression-bodied members for simple single-line methods (`=>`)
- Explicit return types on methods (not `var`-style inference)

**Naming**
- Private fields: `_camelCase` with underscore prefix
- Async methods: `*Async` suffix
- Interfaces: `I` prefix
- Constants: `PascalCase`
- No abbreviations in variable names (use `availableUntilString` not `availableUntilStr`)

**Nullability**
- Nullable enabled with warnings as errors
- Use nullable reference types (`string?`, `T?`)

**Variables & Types**
- `var` for local variables when type is obvious from context
- Explicit types when type isn't obvious from the right-hand side
- Explicit types for method return types

**Async/Await**
- Async/await throughout
- `await foreach` for async enumerables

**Other Patterns**
- Collection expressions (`[]` for empty collections)
- Pattern matching (`is false`, `is null`)
- No `this.` qualifier on instance members
