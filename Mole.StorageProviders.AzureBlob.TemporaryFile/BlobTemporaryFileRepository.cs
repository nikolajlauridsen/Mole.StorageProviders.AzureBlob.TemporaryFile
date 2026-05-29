using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Extensions;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Models;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;
using Umbraco.Cms.Core.Models.TemporaryFile;
using Umbraco.Cms.Core.Persistence.Repositories;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile;

public class BlobTemporaryFileRepository : ITemporaryFileRepository
{
    private readonly ILogger<BlobTemporaryFileRepository> _logger;
    private readonly Lazy<Task<BlobContainerClient>> _containerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTemporaryFileRepository"/> class.
    /// </summary>
    public BlobTemporaryFileRepository(
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings,
        ILogger<BlobTemporaryFileRepository> logger)
    {
        _logger = logger;
        _containerClient = new Lazy<Task<BlobContainerClient>>(() => InitializeContainerAsync(clientFactory, fileSettings.Value));
    }

    private static async Task<BlobContainerClient> InitializeContainerAsync(ITemporaryBlobClientFactory clientFactory, TemporaryFileSettings settings)
    {
        BlobServiceClient serviceClient = clientFactory.GetBlobServiceClient();
        BlobContainerClient container = serviceClient.GetBlobContainerClient(settings.ContainerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }

    private Task<BlobContainerClient> GetContainerAsync() => _containerClient.Value;

    public async Task<TemporaryFileModel?> GetAsync(Guid key)
    {
        BlobContainerClient container = await GetContainerAsync();
        BlobClient blobClient = container.GetBlobClient(key.ToString());

        Response<BlobDownloadInfo> fileResponse;
        try
        {
            fileResponse = await blobClient.DownloadAsync();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }

        IDictionary<string, string>? metadataDictionary = fileResponse.Value.Details.Metadata;
        if (metadataDictionary == null || metadataDictionary.Count == 0)
        {
            // If metadata is missing or empty, treat the blob as unusable (e.g., older or corrupted blob).
            return null;
        }

        MetaDataFile metadata = MetaDataFile.FromDictionary(metadataDictionary);
        return new TemporaryFileModel
        {
            AvailableUntil = metadata.AvailableUntil,
            FileName = metadata.FileName,
            Key = key,
            OpenReadStream = () =>
            {
                // The CMS uses methods not supported by the Stream returned by azure, so we copy to a memory stream.
                MemoryStream stream = new MemoryStream();
                fileResponse.Value.Content.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        };
    }

    public async Task SaveAsync(TemporaryFileModel model)
    {
        BlobContainerClient container = await GetContainerAsync();
        BlobClient blobClient = container.GetBlobClient(model.Key.ToString());

        var options = new BlobUploadOptions
        {
            Metadata = new Dictionary<string, string>
            {
                [Constants.Constants.Metadata.FileName] = model.FileName,
                [Constants.Constants.Metadata.Key] = model.Key.ToString(),
                [Constants.Constants.Metadata.AvailableUntil] = model.AvailableUntil.ToRoundtripString()
            }
        };

        await using Stream readStream = model.OpenReadStream();
        await blobClient.UploadAsync(readStream, options);
    }

    public async Task DeleteAsync(Guid key)
    {
        BlobContainerClient container = await GetContainerAsync();
        await container.DeleteBlobIfExistsAsync(key.ToString(), DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task<IEnumerable<Guid>> CleanUpOldTempFiles(DateTime now)
    {
        BlobContainerClient container = await GetContainerAsync();
        List<Guid> keysToDelete = [];

        await foreach (BlobItem blob in container.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }))
        {
            MetaDataFile metadata;
            try
            {
                metadata = MetaDataFile.FromDictionary(blob.Metadata);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(exception, "Blob {BlobName} is missing required metadata, skipping cleanup...", blob.Name);
                continue;
            }

            if (metadata.AvailableUntil < now)
            {
                keysToDelete.Add(metadata.Key);
            }
        }

        if (keysToDelete.Count == 0)
        {
            return [];
        }
        
        List<Task> deleteTasks = [];
        foreach (Guid key in keysToDelete)
        {
            deleteTasks.Add(container.DeleteBlobIfExistsAsync(key.ToString(), DeleteSnapshotsOption.IncludeSnapshots));
        }

        await Task.WhenAll(deleteTasks);

        return keysToDelete;
    }
}


