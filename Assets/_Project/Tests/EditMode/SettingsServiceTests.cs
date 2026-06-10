using NUnit.Framework;
using StarforgeRelay.Persistence;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class SettingsServiceTests
    {
        [Test]
        public void Default_Enables_Sound_And_Haptics()
        {
            Assert.IsTrue(GameSettings.Default.SoundEnabled);
            Assert.IsTrue(GameSettings.Default.HapticsEnabled);
        }

        [Test]
        public void Current_Reflects_The_Loaded_Settings()
        {
            var store = new FakeSettingsStore(new GameSettings(soundEnabled: false, hapticsEnabled: true));
            var service = new SettingsService(store);
            Assert.IsFalse(service.Current.SoundEnabled);
            Assert.IsTrue(service.Current.HapticsEnabled);
        }

        [Test]
        public void SetSoundEnabled_Change_Updates_Persists_And_Raises_Once()
        {
            var store = new FakeSettingsStore(GameSettings.Default); // sound on
            var service = new SettingsService(store);
            int raised = 0;
            GameSettings payload = default;
            service.Changed += s =>
            {
                raised++;
                payload = s;
            };

            service.SetSoundEnabled(false);

            Assert.IsFalse(service.Current.SoundEnabled);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(1, raised);
            Assert.IsFalse(payload.SoundEnabled);
            Assert.IsFalse(store.Load().SoundEnabled, "change should be persisted");
        }

        [Test]
        public void SetSoundEnabled_SameValue_Is_NoOp()
        {
            var store = new FakeSettingsStore(GameSettings.Default); // sound on
            var service = new SettingsService(store);
            int raised = 0;
            service.Changed += _ => raised++;

            service.SetSoundEnabled(true); // already on

            Assert.AreEqual(0, store.SaveCount);
            Assert.AreEqual(0, raised);
            Assert.IsTrue(service.Current.SoundEnabled);
        }

        [Test]
        public void SetHapticsEnabled_Change_Updates_Persists_And_Raises_Once()
        {
            var store = new FakeSettingsStore(GameSettings.Default); // haptics on
            var service = new SettingsService(store);
            int raised = 0;
            service.Changed += _ => raised++;

            service.SetHapticsEnabled(false);

            Assert.IsFalse(service.Current.HapticsEnabled);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(1, raised);
            Assert.IsFalse(store.Load().HapticsEnabled, "change should be persisted");
        }

        [Test]
        public void SetHapticsEnabled_SameValue_Is_NoOp()
        {
            var store = new FakeSettingsStore(GameSettings.Default); // haptics on
            var service = new SettingsService(store);
            int raised = 0;
            service.Changed += _ => raised++;

            service.SetHapticsEnabled(true); // already on

            Assert.AreEqual(0, store.SaveCount);
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Set_One_Setting_Leaves_The_Other_Untouched()
        {
            var store = new FakeSettingsStore(GameSettings.Default); // both on
            var service = new SettingsService(store);

            service.SetSoundEnabled(false);

            Assert.IsTrue(service.Current.HapticsEnabled, "haptics must not change when only sound is toggled");
        }
    }
}
