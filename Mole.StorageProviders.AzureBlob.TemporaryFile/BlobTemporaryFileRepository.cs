using System.Globalization;
using Azure;
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
    private readonly TemporaryFileSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTemporaryFileRepository"/> class.
    /// </summary>
    public BlobTemporaryFileRepository(
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings)
    {
        _clientFactory = clientFactory;
        _settings = fileSettings.Value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTemporaryFileRepository"/> class.
    /// </summary>
    [Obsolete("Use the constructor without IJsonSerializer. Scheduled for removal in V18.")]
    public BlobTemporaryFileRepository(
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings,
        IJsonSerializer jsonSerializer)
        : this(clientFactory, fileSettings)
    {
    }

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        // TODO: Can I pin this? Or do I have to recreate on every upload.
        BlobServiceClient serviceClient = _clientFactory.GetBlobServiceClient();

        BlobContainerClient container = serviceClient.GetBlobContainerClient(_settings.ContainerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }

    public async Task<TemporaryFileModel?> GetAsync(Guid key)
    {
        BlobContainerClient container = await GetContainerAsync();
        BlobClient blobClient = container.GetBlobClient(key.ToString());

        BlobProperties properties;
        Response<BlobDownloadInfo> fileResponse;
        try
        {
            properties = (await blobClient.GetPropertiesAsync()).Value;
            fileResponse = await blobClient.DownloadAsync();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }

        MetaDataFile metadata = MetaDataFile.FromDictionary(properties.Metadata);

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
                [Constants.Metadata.FileName] = model.FileName,
                [Constants.Metadata.Key] = model.Key.ToString(),
                [Constants.Metadata.AvailableUntil] = model.AvailableUntil.ToString("O")
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

        await foreach (BlobItem blob in container.GetBlobsAsync(BlobTraits.Metadata))
        {
            if (blob.Metadata.TryGetValue(Constants.Metadata.AvailableUntil, out string? availableUntilString)
                && DateTime.Parse(availableUntilString, null, DateTimeStyles.RoundtripKind) < now)
            {
                keysToDelete.Add(Guid.Parse(blob.Name));
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


