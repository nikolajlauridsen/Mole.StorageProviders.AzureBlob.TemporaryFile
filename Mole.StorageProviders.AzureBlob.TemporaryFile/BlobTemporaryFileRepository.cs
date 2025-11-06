using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    private readonly TemporaryFileSettings _settings;

    public BlobTemporaryFileRepository(
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings,
        IJsonSerializer jsonSerializer)
    {
        _clientFactory = clientFactory;
        _jsonSerializer = jsonSerializer;
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

    public async Task<TemporaryFileModel?> GetAsync(Guid key)
    {
        var container = await GetContainerAsync();
        
        // First we need to get metadata
        var metaDataClient = container.GetBlobClient(GetMetaDataFileName(key));
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
        var metadata = _jsonSerializer.Deserialize<MetaDataFile>(await streamReader.ReadToEndAsync());

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
            OpenReadStream = () => fileResponse.Value.Content
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

    public Task<IEnumerable<Guid>> CleanUpOldTempFiles(DateTime dateTime)
    {
        return Task.FromResult(Enumerable.Empty<Guid>());
    }
}


