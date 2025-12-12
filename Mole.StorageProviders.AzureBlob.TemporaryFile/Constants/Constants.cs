using System.ComponentModel;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile;

public static class Constants
{
    public const string SettingsSectionName = "Umbraco:Storage:AzureBlob:TemporaryFile";

    /// <summary>
    /// No longer used. Kept for binary compatibility.
    /// </summary>
    [Obsolete("No longer used. Metadata is now stored as blob metadata. Scheduled for removal in V18.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string MetadaExtension = ".metadata";

    public static class Metadata
    {
        public const string FileName = "FileName";
        public const string Key = "Key";
        public const string AvailableUntil = "AvailableUntil";
    }
}