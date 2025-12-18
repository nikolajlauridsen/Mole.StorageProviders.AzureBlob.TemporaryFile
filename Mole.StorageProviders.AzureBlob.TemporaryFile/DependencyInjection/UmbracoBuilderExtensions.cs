using Microsoft.Extensions.DependencyInjection;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Factories;
using Mole.StorageProviders.AzureBlob.TemporaryFile.Settings;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Persistence.Repositories;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBlobTemporaryFile(this IUmbracoBuilder builder)
    {
        builder.AddConfiguration();
        builder.Services.AddSingleton<ITemporaryBlobClientFactory, TemporaryBlobClientFactory>();
        builder.Services.AddSingleton<ITemporaryFileRepository, BlobTemporaryFileRepository>();
        return builder;
    }
    
    private static IUmbracoBuilder AddConfiguration(this IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<TemporaryFileSettings>().Bind(builder.Config.GetSection(Constants.Constants.SettingsSectionName));
        return builder;
    }
}

