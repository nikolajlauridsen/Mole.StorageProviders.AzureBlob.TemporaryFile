using Mole.StorageProviders.AzureBlob.TemporaryFile.Migration.Migrations._17._1;
using Umbraco.Cms.Core.Packaging;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Migration;

public class TemporaryFilePackageMigrationPlan : PackageMigrationPlan
{
    public TemporaryFilePackageMigrationPlan() : base(Constants.Constants.PackageName)
    {
    }

    protected override void DefinePlan()
    {
        // TO 17.1
        To<MigrateSideCarToMetadata>(new Guid("6B75CF8F-AAEC-4BCD-9E9E-97D9D17A92B5"));
    }
}