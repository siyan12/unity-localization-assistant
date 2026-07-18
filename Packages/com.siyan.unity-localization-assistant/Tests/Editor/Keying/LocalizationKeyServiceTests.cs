using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests.Keying
{
    public sealed class LocalizationKeyServiceTests
    {
        [Test]
        public void Expand_ReplacesTokensAndNormalizesUnsafeRuns()
        {
            var service = new LocalizationKeyService();

            var key = service.Expand(
                "{sourceId}.{targetId}.{elementId}",
                "  Café / Boss  ",
                "Display Name",
                "Primary + Bonus");

            Assert.That(key, Is.EqualTo("Café-Boss.Display-Name.Primary-Bonus"));
        }

        [Test]
        public void Normalize_UsesUnicodeFormKcAndPreservesCase()
        {
            var service = new LocalizationKeyService();

            var key = service.Normalize("ＡｂＣ_１２３.CamelCase");

            Assert.That(key, Is.EqualTo("AbC_123.CamelCase"));
        }

        [Test]
        public void Normalize_PreservesSupplementaryPlaneLetters()
        {
            var key = new LocalizationKeyService().Normalize("Item \U00010400 Name");

            Assert.That(key, Is.EqualTo("Item-\U00010400-Name"));
        }

        [Test]
        public void Expand_IsIndependentOfCurrentCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var service = new LocalizationKeyService();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var turkish = service.Expand("{sourceId}.{targetId}", "ITEM İ", "Title I", string.Empty);

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var english = service.Expand("{sourceId}.{targetId}", "ITEM İ", "Title I", string.Empty);

                Assert.That(turkish, Is.EqualTo(english));
                Assert.That(english, Is.EqualTo("ITEM-İ.Title-I"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void BuildOwnershipIndex_SortsKeysAndOwnersOrdinally()
        {
            var entries = new[]
            {
                Draft("Assets/Z.asset", "zeta", "b.key", "title"),
                Draft("Assets/B.asset", "beta", "a.key", "description"),
                Draft("Assets/A.asset", "alpha", "a.key", "title")
            };

            var index = new LocalizationKeyService().BuildOwnershipIndex(entries);

            Assert.That(index.Keys, Is.EqualTo(new[] { "a.key", "b.key" }));
            Assert.That(
                index.GetOwners("a.key").Select(owner => owner.SourceAssetPath),
                Is.EqualTo(new[] { "Assets/A.asset", "Assets/B.asset" }));
            Assert.That(index.GetOwners("missing"), Is.Empty);
        }

        [Test]
        public void BuildOwnershipIndex_PreservesNormalizationCollisionAsMultipleOwners()
        {
            var service = new LocalizationKeyService();
            var firstKey = service.Expand("{sourceId}.{targetId}", "item / one", "title", string.Empty);
            var secondKey = service.Expand("{sourceId}.{targetId}", "item + one", "title", string.Empty);

            var index = service.BuildOwnershipIndex(new[]
            {
                Draft("Assets/One.asset", "item / one", firstKey, "title"),
                Draft("Assets/Two.asset", "item + one", secondKey, "title")
            });

            Assert.That(firstKey, Is.EqualTo("item-one.title"));
            Assert.That(secondKey, Is.EqualTo(firstKey));
            Assert.That(index.GetOwners(firstKey), Has.Count.EqualTo(2));
        }

        private static LocalizationDraftEntry Draft(
            string assetPath,
            string sourceIdentity,
            string key,
            string targetId)
        {
            return new LocalizationDraftEntry
            {
                SourceAssetPath = assetPath,
                SourceIdentity = sourceIdentity,
                PropertyPath = targetId,
                TargetId = targetId,
                SuggestedKey = key
            };
        }
    }
}
