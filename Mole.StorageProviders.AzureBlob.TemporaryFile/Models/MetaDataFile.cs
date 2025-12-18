using Mole.StorageProviders.AzureBlob.TemporaryFile.Extensions;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Models;

public class MetaDataFile
{
    public required string FileName { get; init; }

    public required Guid Key { get; init; }

    public required DateTime AvailableUntil { get; init; }

    public static MetaDataFile FromDictionary(IDictionary<string, string> metadata) =>
        new()
        {
            FileName = metadata[Constants.Metadata.FileName],
            Key = Guid.Parse(metadata[Constants.Metadata.Key]),
            AvailableUntil = metadata[Constants.Metadata.AvailableUntil].ToRoundtripDateTime()
        };
}