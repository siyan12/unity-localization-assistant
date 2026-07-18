using NUnit.Framework;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests
{
    public sealed class PackageSmokeTests
    {
        [Test]
        public void PackageIdentity_IsStable()
        {
            Assert.That(LocalizationAssistantPackage.PackageName,
                Is.EqualTo("com.siyan.unity-localization-assistant"));
            Assert.That(LocalizationAssistantPackage.DisplayName,
                Is.EqualTo("Unity Localization Assistant"));
        }
    }
}
