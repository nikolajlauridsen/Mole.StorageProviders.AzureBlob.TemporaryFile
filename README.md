# Mole.StorageProviders.AzureBlob.TemporaryFile

Azure Blob Storage provider for temporary file uploads in Umbraco CMS. This package enables Umbraco deployments to store temporary uploaded files in Azure Blob Storage instead of local disk, eliminating filesystem dependencies for uploaded content.

## Why Use This Package?

When running Umbraco in containers (Docker, Kubernetes, Azure Container Instances, etc.), storing files on the local filesystem creates challenges, especially when load balancing:

- Files are lost when containers restart or scale
- Shared storage across multiple container instances is complex
- Ephemeral container filesystems aren't designed for file persistence

This package solves these problems by redirecting temporary file uploads to Azure Blob Storage, making your Umbraco deployment truly stateless and container-friendly.

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Mole.StorageProviders.AzureBlob.TemporaryFile
```

## Configuration

### Basic Configuration

The package includes a default composer (`TemporaryFileComposer`) that automatically registers the provider. You only need to configure your connection settings in `appsettings.json`:

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "TemporaryFile": {
          "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your-account;AccountKey=your-key;EndpointSuffix=core.windows.net",
          "ContainerName": "umbraco-temp-uploads"
        }
      }
    }
  }
}
```

Or by environment variables:

```sh
UMBRACO__STORAGE__AZUREBLOB__TEMPORARYFILE__CONNECTIONSTRING=DefaultEndpointsProtocol=https;AccountName=...
UMBRACO__STORAGE__AZUREBLOB__TEMPORARYFILE__CONTAINERNAME=umbraco-temp-uploads
```

The `ContainerName` is optional and defaults to `tempfiles`.

### Configuration in Code

If you need to configure the provider in code, create a custom composer and disable the default one:

```csharp
using Azure.Storage.Blobs;
using Umbraco.Cms.Core.Composing;
using Mole.StorageProviders.AzureBlob.TemporaryFile.DependencyInjection;

[assembly: DisableComposer(typeof(TemporaryFileComposer))]

namespace YourProject;

internal sealed class CustomTemporaryFileComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddBlobTemporaryFile(options =>
        {
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "umbraco-temp-uploads";
        });
}
```

### Advanced Configuration - Azure AD Authentication

For Azure AD authentication (managed identities), use the extension methods on `TemporaryFileSettings`:

```csharp
using Azure.Identity;
using Azure.Storage.Blobs;
using Umbraco.Cms.Core.Composing;
using Mole.StorageProviders.AzureBlob.TemporaryFile.DependencyInjection;

[assembly: DisableComposer(typeof(TemporaryFileComposer))]
namespace YourProject;

internal sealed class AzureAdTemporaryFileComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddBlobTemporaryFile(options =>
        {
            options.ConnectionString = "https://[storage-account].blob.core.windows.net";
            options.ContainerName = "umbraco-temp-uploads";
            options.TryCreateBlobServiceClientUsingUri(uri => new BlobServiceClient(uri, new DefaultAzureCredential()));
        });
}
```

> **Note**
> When implementing a custom composer, disable the default `TemporaryFileComposer` using the `DisableComposer` attribute at assembly level.

Available configuration methods:

- `CreateBlobServiceClientUsingDefault()` - Uses the connection string with default constructor (default behavior)
- `CreateBlobServiceClientUsing(Func<BlobServiceClient>)` - Provides a custom factory function to create the client
- `TryCreateBlobServiceClientUsingUri(Func<Uri, BlobServiceClient>)` - If the connection string is a URI, uses the delegate to create a client

**Security Note:** For production environments, Azure Managed Identity is the recommended approach as it eliminates the need to store connection strings in configuration files.

## Versioning
This package starts at version 17 to align with Umbraco's versioning scheme and other storage provider packages. This makes it easier to identify which version to install based on your Umbraco version. For example, version 17.x is compatible with Umbraco 17.


## How It Works

This package implements Umbraco's `ITemporaryFileRepository` interface, redirecting all temporary file operations to Azure Blob Storage. When files are uploaded through the Umbraco backoffice, they're stored in your configured Azure Blob container instead of the local `~/umbraco/Data/TEMP` folder.

## Requirements

- Umbraco 17.0 or higher
- Azure Storage Account

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and create pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or feature requests, please open an issue on the [GitHub repository](https://github.com/nikolajlauridsen/Mole.StorageProviders.AzureBlob.TemporaryFile/issues).