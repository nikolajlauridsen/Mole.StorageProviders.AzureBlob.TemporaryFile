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

Add your Azure Blob Storage connection string to `appsettings.json`:

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

The `ContainerName` is optional and defaults to `tempfiles`

**Security Note:** For production environments, use Azure Key Vault, Managed Identity, or environment variables instead of storing connection strings in configuration files.

## Versioning
This package starts at version 17 to align with Umbraco's versioning scheme and other storage provider packages. This makes it easier to identify which version to install based on your Umbraco version. For example, version 18.x is compatible with Umbraco 18.


## How It Works

This package implements Umbraco's `ITemporaryFileRepository` interface, redirecting all temporary file operations to Azure Blob Storage. When files are uploaded through the Umbraco backoffice, they're stored in your configured Azure Blob container instead of the local `~/umbraco/Data/TEMP` folder.

## Requirements

- Umbraco 18.0 or higher
- Azure Storage Account

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and create pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or feature requests, please open an issue on the [GitHub repository](https://github.com/nikolajlauridsen/Mole.StorageProviders.AzureBlob.TemporaryFile/issues).