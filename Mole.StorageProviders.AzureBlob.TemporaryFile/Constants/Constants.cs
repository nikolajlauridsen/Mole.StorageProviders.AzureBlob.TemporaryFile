namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Constants;

public static class Constants
{
    public const string SettingsSectionName = "Umbraco:Storage:AzureBlob:TemporaryFile";

    public const string PackageName = "Azure Blob Storage Provider for Umbraco Temporary Files";

    public static class Metadata
    {
        public const string FileName = "FileName";
        public const string Key = "Key";
        public const string AvailableUntil = "AvailableUntil";
    }
}