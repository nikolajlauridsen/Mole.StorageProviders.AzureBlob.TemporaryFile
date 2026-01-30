using Azure.Storage.Blobs;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;

public class TemporaryFileSettings
{
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = "tempfiles";

    internal Func<BlobServiceClient>? CreateBlobServiceClient { get; set; }

    internal Func<Uri, BlobServiceClient?>? TryCreateBlobServiceClientFromUri { get; set; }

    /// <summary>
    /// Creates the BlobServiceClient using the default constructor (connection string).
    /// </summary>
    public TemporaryFileSettings CreateBlobServiceClientUsingDefault()
    {
        CreateBlobServiceClient = null;
        TryCreateBlobServiceClientFromUri = null;
        return this;
    }

    /// <summary>
    /// Creates the BlobServiceClient using the provided factory function.
    /// </summary>
    public TemporaryFileSettings CreateBlobServiceClientUsing(Func<BlobServiceClient> factory)
    {
        CreateBlobServiceClient = factory;
        TryCreateBlobServiceClientFromUri = null;
        return this;
    }

    /// <summary>
    /// If the connection string is parsed to a URI, uses the delegate to create a BlobServiceClient.
    /// This is useful for Azure AD authentication with managed identities.
    /// </summary>
    public TemporaryFileSettings TryCreateBlobServiceClientUsingUri(Func<Uri, BlobServiceClient> factory)
    {
        CreateBlobServiceClient = null;
        TryCreateBlobServiceClientFromUri = factory;
        return this;
    }
}