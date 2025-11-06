using Azure.Storage.Blobs;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;

public interface ITemporaryBlobClientFactory
{
    BlobServiceClient GetBlobServiceClient();
}