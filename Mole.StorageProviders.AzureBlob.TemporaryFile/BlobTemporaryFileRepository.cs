using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Models;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;
using Umbraco.Cms.Core.Models.TemporaryFile;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Serialization;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile;

public class BlobTemporaryFileRepository : ITemporaryFileRepository
{
    private readonly ITemporaryBlobClientFactory _clientFactory;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger<BlobTemporaryFileRepository> _logger;
    private readonly TemporaryFileSettings _settings;

    public BlobTemporaryFileRepository(
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings,
        IJsonSerializer jsonSerializer,
        ILogger<BlobTemporaryFileRepository> logger)
    {
        _clientFactory = clientFactory;
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _settings = fileSettings.Value;
    }

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        // TODO: Can I pin this? Or do I have to recreate on every upload.
        var serviceClient = _clientFactory.GetBlobServiceClient();

        var container = serviceClient.GetBlobContainerClient(_settings.ContainerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }

    private MetaDataFile CreateMetaDataFile(TemporaryFileModel model) =>
        new()
        {
            FileName = model.FileName,
            AvailableUntil = model.AvailableUntil,
            Key = model.Key,
        };

    private string GetMetaDataFileName(Guid key)
        => $"{key}{Constants.Constants.MetadaExtension}";

    private async Task<MetaDataFile?> DownloadMetaDataFileAsync(BlobContainerClient container, string blobName)
    {
        var metaDataClient = container.GetBlobClient(blobName);
        if ((await metaDataClient.ExistsAsync())?.Value is false)
        {
            return null;
        }

        var metaDataResponse = await metaDataClient.DownloadAsync();

        if (metaDataResponse is null)
        {
            return null;
        }

        using var streamReader = new StreamReader(metaDataResponse.Value.Content);
        return _jsonSerializer.Deserialize<MetaDataFile>(await streamReader.ReadToEndAsync());
    }

    public async Task<TemporaryFileModel?> GetAsync(Guid key)
    {
        BlobContainerClient container = await GetContainerAsync();
        
        // First we need to get metadata
        var metadata = await DownloadMetaDataFileAsync(container, GetMetaDataFileName(key));
        if (metadata is null)
        {
            return null;
        }
        
        // Now the actual file
        var fileClient = container.GetBlobClient(key.ToString());
        var fileResponse = await fileClient.DownloadAsync();

        if (fileResponse is null)
        {
            return null;
        }

        
        return new TemporaryFileModel
        {
            AvailableUntil = metadata.AvailableUntil,
            FileName = metadata.FileName,
            Key = key,
            OpenReadStream = () =>
            {
                // The CMS uses methods not supported by the Stream returned by azure, so we copy to a memory stream.
                var stream = new MemoryStream();
                fileResponse.Value.Content.CopyTo(stream);
                return stream;
            }
        };
    }

    public async Task SaveAsync(TemporaryFileModel model)
    {
        var container = await GetContainerAsync();
        // Create and upload metadata file so we have a chance to find our temp file again
        var temporaryFileModel = CreateMetaDataFile(model);
        var metaData = new BinaryData(_jsonSerializer.Serialize(temporaryFileModel));
        var filename = GetMetaDataFileName(model.Key);
        await container.UploadBlobAsync(filename, metaData);
        
        // Now upload the actual file content
        await using var readStream = model.OpenReadStream();
        await container.UploadBlobAsync(model.Key.ToString(),  readStream);
    }

    public async Task DeleteAsync(Guid key)
    {
        var container = await GetContainerAsync();

        await container.DeleteBlobIfExistsAsync(key.ToString(), DeleteSnapshotsOption.IncludeSnapshots);
        await container.DeleteBlobIfExistsAsync(GetMetaDataFileName(key), DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task<IEnumerable<Guid>> CleanUpOldTempFiles(DateTime now)
    {
        _logger.LogInformation("Running cleanup.");
        var container = await GetContainerAsync();
        List<Guid> keysToDelete = new();
        
        // Find all metadata files and check for expired files
        await foreach (BlobItem blob in container.GetBlobsAsync().Where(x => x.Name.EndsWith(Constants.Constants.MetadaExtension)))
        {
            var metaData = await DownloadMetaDataFileAsync(container, blob.Name);
            if (metaData is null)
            {
                continue;
            }

            if (metaData.AvailableUntil < now)
            {
                keysToDelete.Add(metaData.Key);
            }
        }

        _logger.LogInformation("Found {0} keys to delete.", keysToDelete.Count);
        if (keysToDelete.Count == 0)
        {
            return [];
        }

        // Might as well do it actually async
        var deleteTasks = new List<Task>();
        foreach (var key in keysToDelete)
        {
           deleteTasks.Add(container.DeleteBlobIfExistsAsync(key.ToString()));
           deleteTasks.Add(container.DeleteBlobIfExistsAsync(GetMetaDataFileName(key)));
        }
        Task.WaitAll(deleteTasks);
        
        _logger.LogInformation("Cleanup complete.");
        return keysToDelete;
    }
}


