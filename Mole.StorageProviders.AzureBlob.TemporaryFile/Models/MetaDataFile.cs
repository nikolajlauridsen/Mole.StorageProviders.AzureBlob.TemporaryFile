namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Models;

public class MetaDataFile
{
    public required string FileName { get; set; }
    
    public required Guid Key { get; set; }
    
    public required DateTime AvailableUntil { get; set; }
}