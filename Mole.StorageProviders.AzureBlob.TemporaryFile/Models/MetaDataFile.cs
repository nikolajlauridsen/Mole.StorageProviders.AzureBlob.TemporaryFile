using Mole.StorageProviders.AzureBlob.TemporaryFile.Extensions;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Models;

public class MetaDataFile
{
    public required string FileName { get; set; }

    public required Guid Key { get; set; }

    public required DateTime AvailableUntil { get; set; }

    public static MetaDataFile FromDictionary(IDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue(Constants.Constants.Metadata.FileName, out var fileName) is false ||
            metadata.TryGetValue(Constants.Constants.Metadata.Key, out var key) is false ||
            metadata.TryGetValue(Constants.Constants.Metadata.AvailableUntil, out var availableUntil) is false)
        {
            throw new InvalidOperationException("Blob is missing required metadata");
        }

        return new MetaDataFile
        {
            FileName = fileName,
            Key = Guid.Parse(key),
            AvailableUntil = availableUntil.ToRoundtripDateTime()
        };
    }
}