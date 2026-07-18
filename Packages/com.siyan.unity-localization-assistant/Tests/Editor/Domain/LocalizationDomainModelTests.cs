using NUnit.Framework;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests
{
    public sealed class LocalizationDomainModelTests
    {
        [Test]
        public void DraftEntry_DefaultsToNonDestructiveState()
        {
            var draft = new LocalizationDraftEntry();

            Assert.That(draft.Enabled, Is.False);
            Assert.That(draft.ChangeKind, Is.EqualTo(ChangeKind.None));
            Assert.That(draft.LocaleValues, Is.Empty);
            Assert.That(draft.Diagnostics, Is.Empty);
        }

        [Test]
        public void ChangeKind_CanRepresentCombinedChanges()
        {
            var changes = ChangeKind.CreateKey |
                          ChangeKind.AddLocaleValue |
                          ChangeKind.AssignReference;

            Assert.That(changes.HasFlag(ChangeKind.CreateKey), Is.True);
            Assert.That(changes.HasFlag(ChangeKind.AddLocaleValue), Is.True);
            Assert.That(changes.HasFlag(ChangeKind.AssignReference), Is.True);
            Assert.That(changes.HasFlag(ChangeKind.RenameKey), Is.False);
        }
    }
}
