namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;

public class TemporaryFileSettings
{
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = "tempfiles";
}