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
        var connectionString = _temporaryFileSettings.ConnectionString;
        return new BlobServiceClient(connectionString);
    }
}
