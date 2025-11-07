using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.DependencyInjection;

public class TemporaryFileComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddBlobTemporaryFile();
    }
}