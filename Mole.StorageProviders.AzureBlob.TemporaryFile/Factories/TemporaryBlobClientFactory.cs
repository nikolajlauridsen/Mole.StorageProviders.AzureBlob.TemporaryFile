using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;

public class TemporaryBlobClientFactory : ITemporaryBlobClientFactory
{
    private readonly TemporaryFileSettings _temporaryFileSettings;

    public TemporaryBlobClientFactory(IOptions<TemporaryFileSettings> temporaryFileSettings)
    {
        _temporaryFileSettings = temporaryFileSettings.Value;
    }
    
    public BlobServiceClient GetBlobServiceClient()
    {
        // If a custom factory function is provided, use it
        if (_temporaryFileSettings.CreateBlobServiceClient is not null)
        {
            return _temporaryFileSettings.CreateBlobServiceClient();
        }

        var connectionString = _temporaryFileSettings.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionString is not configured for TemporaryFileSettings.");
        }

        // Try to create using URI factory if connection string is a URI
        if (_temporaryFileSettings.TryCreateBlobServiceClientFromUri is not null &&
            Uri.TryCreate(connectionString, UriKind.Absolute, out Uri? uri))
        {
            BlobServiceClient? client = _temporaryFileSettings.TryCreateBlobServiceClientFromUri(uri);
            if (client is not null)
            {
                return client;
            }
        }

        // Fall back to default connection string constructor
        return new BlobServiceClient(connectionString);
    }
}
