using Mole.StorageProviders.AzureBlob.TemporaryFile.Extensions;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Models;

public class MetaDataFile
{
    public required string FileName { get; set; }

    public required Guid Key { get; set; }

    public required DateTime AvailableUntil { get; set; }

    public static MetaDataFile FromDictionary(IDictionary<string, string> metadata) =>
        new()
        {
            FileName = metadata[Constants.Constants.Metadata.FileName],
            Key = Guid.Parse(metadata[Constants.Constants.Metadata.Key]),
            AvailableUntil = metadata[Constants.Constants.Metadata.AvailableUntil].ToRoundtripDateTime()
        };
}