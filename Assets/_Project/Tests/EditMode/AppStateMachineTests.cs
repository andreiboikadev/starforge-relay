using System;
using NUnit.Framework;
using StarforgeRelay.App;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public class AppStateMachineTests
    {
        // Per-result RoundComplete display delays (GDD §12), mirrored by the production RoundConfig defaults.
        private const float WonDelay = 2f;
        private const float OverloadDelay = 1.5f;
        private const float TimeoutDelay = 1f;

        // Records lifecycle calls so the tests can assert what the machine asked for (and what it did NOT).
        private sealed class FakeRoundLifecycle : IRoundLifecycle
        {
            public int StartCount { get; private set; }
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }
            public int StopCount { get; private set; }

            public event Action<RoundEndedEvent> RoundEnded;

            public void StartRound() => StartCount++;
            public void PauseRound() => PauseCount++;
            public void ResumeRound() => ResumeCount++;
            public void StopRound() => StopCount++;

            public void RaiseEnded(RoundEndedEvent e) => RoundEnded?.Invoke(e);
        }

        private static AppStateMachine NewMachine(FakeRoundLifecycle fake) =>
            new AppStateMachine(fake, new RoundCompleteDelays(WonDelay, OverloadDelay, TimeoutDelay));

        private static AppStateMachine InPlaying(FakeRoundLifecycle fake)
        {
            AppStateMachine machine = NewMachine(fake);
            machine.Begin();              // Boot
            machine.Tick(0f);             // Boot -> MainMenu
            machine.RequestCalibration(); // -> Calibration
            machine.RequestStartRound();  // -> Playing
            return machine;
        }

        [Test]
        public void Begin_EntersBoot()
        {
            AppStateMachine machine = NewMachine(new FakeRoundLifecycle());
            machine.Begin();
            Assert.AreEqual(AppPhase.Boot, machine.Phase);
        }

        [Test]
        public void Boot_AdvancesToMainMenu_OnFirstTick()
        {
            AppStateMachine machine = NewMachine(new FakeRoundLifecycle());
            machine.Begin();
            machine.Tick(0f);
            Assert.AreEqual(AppPhase.MainMenu, machine.Phase);
        }

        [Test]
        public void Calibration_StartRound_EntersPlaying_AndStartsOnce()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            Assert.AreEqual(AppPhase.Playing, machine.Phase);
            Assert.AreEqual(1, fake.StartCount);
        }

        [Test]
        public void PauseThenResume_DoesNotRestartTheRound()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);

            machine.RequestPause();
            Assert.AreEqual(AppPhase.Paused, machine.Phase);
            Assert.AreEqual(1, fake.PauseCount);

            machine.RequestResume();
            Assert.AreEqual(AppPhase.Playing, machine.Phase);
            Assert.AreEqual(1, fake.ResumeCount);
            Assert.AreEqual(1, fake.StartCount, "resume must not start a new round");
        }

        [Test]
        public void RoundEnded_EntersRoundComplete_AndStoresSnapshot()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            var ended = new RoundEndedEvent(RoundPhase.Won, 3, 250, 20, 2);

            fake.RaiseEnded(ended);

            Assert.AreEqual(AppPhase.RoundComplete, machine.Phase);
            Assert.AreEqual(RoundPhase.Won, machine.LastResult.Result);
            Assert.AreEqual(250, machine.LastResult.Score);
            Assert.AreEqual(3, machine.LastResult.Stars);
        }

        [Test]
        public void RoundComplete_AdvancesToResults_AfterDelay()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            fake.RaiseEnded(new RoundEndedEvent(RoundPhase.TimedOut, 2, 120, 14, 1));

            machine.Tick(TimeoutDelay * 0.5f);
            Assert.AreEqual(AppPhase.RoundComplete, machine.Phase, "not before the delay elapses");

            machine.Tick(TimeoutDelay);
            Assert.AreEqual(AppPhase.Results, machine.Phase);
        }

        [Test]
        public void RoundComplete_Victory_HoldsForVictoryDelay()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            fake.RaiseEnded(new RoundEndedEvent(RoundPhase.Won, 3, 250, 20, 0));

            // Past the time-out delay (1 s) but before the victory delay (2 s): still showing the victory beat.
            machine.Tick(WonDelay - 0.1f);
            Assert.AreEqual(AppPhase.RoundComplete, machine.Phase, "victory holds for its full 2 s beat");

            machine.Tick(0.2f);
            Assert.AreEqual(AppPhase.Results, machine.Phase);
        }

        [Test]
        public void RoundComplete_Overload_HoldsForOverloadDelay()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            fake.RaiseEnded(new RoundEndedEvent(RoundPhase.Overloaded, 0, 0, 5, 8));

            machine.Tick(OverloadDelay - 0.1f);
            Assert.AreEqual(AppPhase.RoundComplete, machine.Phase, "overload holds for its full 1.5 s beat");

            machine.Tick(0.2f);
            Assert.AreEqual(AppPhase.Results, machine.Phase);
        }

        [Test]
        public void Results_PlayAgain_StartsAFreshRound()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            fake.RaiseEnded(new RoundEndedEvent(RoundPhase.Won, 3, 250, 20, 0));
            machine.Tick(WonDelay); // RoundComplete -> Results

            machine.RequestStartRound();

            Assert.AreEqual(AppPhase.Playing, machine.Phase);
            Assert.AreEqual(2, fake.StartCount);
        }

        [Test]
        public void Results_MainMenu_StopsRound()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            fake.RaiseEnded(new RoundEndedEvent(RoundPhase.Overloaded, 0, 0, 5, 8));
            machine.Tick(OverloadDelay); // -> Results

            machine.RequestMainMenu();

            Assert.AreEqual(AppPhase.MainMenu, machine.Phase);
            Assert.AreEqual(1, fake.StopCount);
        }

        [Test]
        public void Paused_MainMenu_StopsRound()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = InPlaying(fake);
            machine.RequestPause();

            machine.RequestMainMenu();

            Assert.AreEqual(AppPhase.MainMenu, machine.Phase);
            Assert.AreEqual(1, fake.StopCount);
        }

        [Test]
        public void IllegalTriggers_AreNoOps()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = NewMachine(fake);
            machine.Begin();
            machine.Tick(0f); // MainMenu

            machine.RequestPause();  // invalid in MainMenu
            machine.RequestResume(); // invalid in MainMenu
            Assert.AreEqual(AppPhase.MainMenu, machine.Phase);
            Assert.AreEqual(0, fake.PauseCount);
            Assert.AreEqual(0, fake.ResumeCount);

            machine.RequestCalibration();
            machine.RequestStartRound(); // -> Playing
            machine.RequestStartRound(); // invalid in Playing
            machine.RequestResume();     // invalid in Playing
            Assert.AreEqual(AppPhase.Playing, machine.Phase);
            Assert.AreEqual(1, fake.StartCount);
            Assert.AreEqual(0, fake.ResumeCount);
        }

        [Test]
        public void PhaseChanged_FiresOnEachTransition()
        {
            var fake = new FakeRoundLifecycle();
            AppStateMachine machine = NewMachine(fake);
            int count = 0;
            machine.PhaseChanged += _ => count++;

            machine.Begin();              // initial enter (Boot) -> 1
            machine.Tick(0f);             // -> MainMenu -> 2
            machine.RequestCalibration(); // -> Calibration -> 3
            machine.RequestStartRound();  // -> Playing -> 4

            Assert.AreEqual(4, count);
        }
    }
}
