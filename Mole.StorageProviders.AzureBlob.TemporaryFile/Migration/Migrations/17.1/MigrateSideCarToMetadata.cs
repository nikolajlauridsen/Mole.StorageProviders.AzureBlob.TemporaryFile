using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Migration.Migrations._17._1;

public class MigrateSideCarToMetadata : AsyncPackageMigrationBase
{
    private const string MetadataExtension = ".metadata";

    private readonly ITemporaryBlobClientFactory _clientFactory;
    private readonly TemporaryFileSettings _settings;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger<MigrateSideCarToMetadata> _logger;

    public MigrateSideCarToMetadata(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings,
        ITemporaryBlobClientFactory clientFactory,
        IOptions<TemporaryFileSettings> fileSettings,
        IJsonSerializer jsonSerializer,
        ILogger<MigrateSideCarToMetadata> logger)
        : base(packagingService, mediaService, mediaFileManager, mediaUrlGenerators, shortStringHelper, contentTypeBaseServiceProvider, context, packageMigrationsSettings)
    {
        _clientFactory = clientFactory;
        _settings = fileSettings.Value;
        _jsonSerializer = jsonSerializer;
        _logger = logger;
    }

    protected override async Task MigrateAsync()
    {
        BlobContainerClient container = await GetContainerAsync();

        await foreach (BlobItem blob in container.GetBlobsAsync())
        {
            if (blob.Name.EndsWith(MetadataExtension) is false)
            {
                continue;
            }

            await MigrateSidecarFileAsync(container, blob.Name);
        }
    }

    private async Task MigrateSidecarFileAsync(BlobContainerClient container, string sidecarBlobName)
    {
        // Extract the key from the sidecar filename (e.g., "guid.metadata" -> "guid")
        var contentBlobName = sidecarBlobName[..^MetadataExtension.Length];

        _logger.LogInformation("Migrating sidecar file {SidecarBlobName} to metadata on {ContentBlobName}", sidecarBlobName, contentBlobName);

        // Download and deserialize the sidecar metadata
        BlobClient sidecarClient = container.GetBlobClient(sidecarBlobName);
        var sidecarResponse = await sidecarClient.DownloadAsync();

        using StreamReader reader = new(sidecarResponse.Value.Content);
        var json = await reader.ReadToEndAsync();
        var metaData = _jsonSerializer.Deserialize<SidecarMetaData>(json);

        if (metaData is null)
        {
            _logger.LogWarning("Failed to deserialize sidecar metadata from {SidecarBlobName}", sidecarBlobName);
            return;
        }

        // Get the content blob and set the metadata
        BlobClient contentClient = container.GetBlobClient(contentBlobName);
        if ((await contentClient.ExistsAsync()).Value is false)
        {
            _logger.LogWarning("Content blob {ContentBlobName} does not exist for sidecar {SidecarBlobName}, deleting orphaned sidecar", contentBlobName, sidecarBlobName);
            await sidecarClient.DeleteIfExistsAsync();
            return;
        }

        var blobMetadata = new Dictionary<string, string>
        {
            [Constants.Metadata.FileName] = metaData.FileName,
            [Constants.Metadata.Key] = metaData.Key.ToString(),
            [Constants.Metadata.AvailableUntil] = metaData.AvailableUntil.ToString("O")
        };

        await contentClient.SetMetadataAsync(blobMetadata);

        // Delete the sidecar file
        await sidecarClient.DeleteIfExistsAsync();

        _logger.LogInformation("Successfully migrated sidecar file {SidecarBlobName}", sidecarBlobName);
    }

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        BlobServiceClient serviceClient = _clientFactory.GetBlobServiceClient();
        BlobContainerClient container = serviceClient.GetBlobContainerClient(_settings.ContainerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }

    /// <summary>
    /// Represents the old sidecar metadata file structure.
    /// </summary>
    private sealed class SidecarMetaData
    {
        public required string FileName { get; set; }
        public required Guid Key { get; set; }
        public required DateTime AvailableUntil { get; set; }
    }
}