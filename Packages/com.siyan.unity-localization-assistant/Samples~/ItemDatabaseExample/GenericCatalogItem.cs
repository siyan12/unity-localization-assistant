using UnityEngine;
using UnityEngine.Localization;

namespace Siyan.UnityLocalizationAssistant.Samples.GenericItemCatalog
{
    [CreateAssetMenu(
        fileName = "GenericCatalogItem",
        menuName = "Localization Assistant Samples/Generic Catalog Item")]
    public sealed class GenericCatalogItem : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private LocalizedString displayName = new LocalizedString();
        [SerializeField] private LocalizedString description = new LocalizedString();

        public string StableId => stableId;
        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;
    }
}
