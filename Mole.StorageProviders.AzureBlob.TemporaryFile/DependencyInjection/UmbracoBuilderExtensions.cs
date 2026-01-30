using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        builder.Services.AddSingleton<ITemporaryFileRepository, BlobTemporaryFileRepository>(factory => new BlobTemporaryFileRepository(
            factory.GetRequiredService<ITemporaryBlobClientFactory>(),
            factory.GetRequiredService<IOptions<TemporaryFileSettings>>(),
            factory.GetRequiredService<ILogger<BlobTemporaryFileRepository>>()
        ));
        return builder;
    }

    public static IUmbracoBuilder AddBlobTemporaryFile(this IUmbracoBuilder builder, Action<TemporaryFileSettings> configure)
    {
        builder.AddConfiguration();
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<ITemporaryBlobClientFactory, TemporaryBlobClientFactory>();
        builder.Services.AddSingleton<ITemporaryFileRepository, BlobTemporaryFileRepository>(factory => new BlobTemporaryFileRepository(
            factory.GetRequiredService<ITemporaryBlobClientFactory>(),
            factory.GetRequiredService<IOptions<TemporaryFileSettings>>(),
            factory.GetRequiredService<ILogger<BlobTemporaryFileRepository>>()
        ));
        return builder;
    }
    
    private static IUmbracoBuilder AddConfiguration(this IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<TemporaryFileSettings>().Bind(builder.Config.GetSection(Constants.Constants.SettingsSectionName));
        return builder;
    }
}

