using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class RoundControllerTests
    {
        // GDD §12–13 numbers, injected (rules are config-injected, not SO-coupled).
        private static RoundController NewStarted(
            ScoreService score = null,
            ComboTracker combo = null,
            HeatService heat = null,
            StabilizationProgress stab = null,
            RoundTimer timer = null)
        {
            var rc = new RoundController(
                score ?? new ScoreService(10, 50, 2, 10),
                combo ?? new ComboTracker(5),
                heat ?? new HeatService(8, 1),
                stab ?? new StabilizationProgress(20),
                timer ?? new RoundTimer(90f));
            rc.Start();
            return rc;
        }

        private static int StarsAfter(int corrects)
        {
            var rc = NewStarted();
            for (int i = 0; i < corrects; i++)
            {
                rc.ApplyCorrect();
            }

            return rc.Stars;
        }

        // ---- wiring the rules (no re-implementation, no double-apply) ----

        [Test]
        public void Correct_Updates_All_Meters_Once()
        {
            var rc = NewStarted();
            rc.ApplyCorrect();
            Assert.AreEqual(10, rc.Score);
            Assert.AreEqual(1, rc.Combo);
            Assert.AreEqual(1, rc.Stabilization);
            Assert.AreEqual(0, rc.Heat);
        }

        [Test]
        public void Fifth_Correct_Awards_Milestone_Bonus()
        {
            var rc = NewStarted();
            for (int i = 0; i < 5; i++)
            {
                rc.ApplyCorrect();
            }

            Assert.AreEqual(5, rc.Combo);
            Assert.AreEqual(100, rc.Score); // 5×10 + 50 milestone bonus
        }

        [Test]
        public void Milestone_Relieves_Heat()
        {
            var rc = NewStarted();
            rc.ApplyWrong(); // heat 1, combo 0
            rc.ApplyWrong(); // heat 2, combo 0
            for (int i = 0; i < 5; i++)
            {
                rc.ApplyCorrect(); // combo 1..5; at 5 → milestone → heat −1
            }

            Assert.AreEqual(5, rc.Combo);
            Assert.AreEqual(1, rc.Heat); // 2 − 1 relief
        }

        [Test]
        public void Wrong_Adds_Heat_And_Resets_Combo()
        {
            var rc = NewStarted();
            rc.ApplyCorrect();
            rc.ApplyCorrect(); // combo 2
            rc.ApplyWrong();
            Assert.AreEqual(1, rc.Heat);
            Assert.AreEqual(0, rc.Combo);
        }

        [Test]
        public void Expired_Held_Resets_Combo()
        {
            var rc = NewStarted();
            rc.ApplyCorrect();
            rc.ApplyExpired(wasHeld: true);
            Assert.AreEqual(1, rc.Heat);
            Assert.AreEqual(0, rc.Combo);
        }

        [Test]
        public void Expired_Not_Held_Keeps_Combo()
        {
            var rc = NewStarted();
            rc.ApplyCorrect();
            rc.ApplyCorrect();
            rc.ApplyExpired(wasHeld: false);
            Assert.AreEqual(1, rc.Heat);
            Assert.AreEqual(2, rc.Combo);
        }

        // ---- end states in isolation ----

        [Test]
        public void Victory_When_Stabilized()
        {
            var rc = NewStarted();
            for (int i = 0; i < 20; i++)
            {
                rc.ApplyCorrect();
            }

            Assert.AreEqual(RoundPhase.Won, rc.Phase);
            Assert.AreEqual(3, rc.Stars);
            Assert.IsTrue(rc.IsOver);
        }

        [Test]
        public void Overload_When_Heat_Caps()
        {
            var rc = NewStarted();
            for (int i = 0; i < 8; i++)
            {
                rc.ApplyWrong();
            }

            Assert.AreEqual(RoundPhase.Overloaded, rc.Phase);
        }

        [Test]
        public void TimeOut_When_Clock_Runs_Out()
        {
            var rc = NewStarted();
            rc.Tick(90f);
            Assert.AreEqual(RoundPhase.TimedOut, rc.Phase);
        }

        // ---- concurrent end-state priority: victory > overload > time-out (guardrails §16) ----

        [Test]
        public void Victory_Beats_Overload_And_TimeOut_On_Same_Tick()
        {
            var heat = new HeatService(8, 1);
            for (int i = 0; i < 8; i++)
            {
                heat.RegisterWrongInsert(); // overloaded
            }

            var stab = new StabilizationProgress(20);
            for (int i = 0; i < 20; i++)
            {
                stab.RegisterCorrect(); // complete
            }

            var timer = new RoundTimer(90f);
            timer.Tick(90f); // timed out

            var rc = NewStarted(heat: heat, stab: stab, timer: timer);
            rc.Tick(0f); // resolve all three at once

            Assert.AreEqual(RoundPhase.Won, rc.Phase);
        }

        [Test]
        public void Overload_Beats_TimeOut_On_Same_Tick()
        {
            var heat = new HeatService(8, 1);
            for (int i = 0; i < 8; i++)
            {
                heat.RegisterWrongInsert(); // overloaded
            }

            var timer = new RoundTimer(90f);
            timer.Tick(90f); // timed out (stabilization stays 0)

            var rc = NewStarted(heat: heat, timer: timer);
            rc.Tick(0f);

            Assert.AreEqual(RoundPhase.Overloaded, rc.Phase);
        }

        // ---- star rating (GDD §13) ----

        [Test]
        public void Star_Rating_Boundaries()
        {
            Assert.AreEqual(0, StarsAfter(5));
            Assert.AreEqual(1, StarsAfter(6));
            Assert.AreEqual(1, StarsAfter(11));
            Assert.AreEqual(2, StarsAfter(12));
            Assert.AreEqual(2, StarsAfter(19));
            Assert.AreEqual(3, StarsAfter(20));
        }

        [Test]
        public void Overload_Before_Six_Correct_Is_Zero_Stars()
        {
            var rc = NewStarted();
            for (int i = 0; i < 4; i++)
            {
                rc.ApplyCorrect(); // 4 correct
            }

            for (int i = 0; i < 8; i++)
            {
                rc.ApplyWrong(); // overload at 4 correct
            }

            Assert.AreEqual(RoundPhase.Overloaded, rc.Phase);
            Assert.AreEqual(0, rc.Stars);
        }

        // ---- lifecycle guards + events ----

        [Test]
        public void No_Action_After_Round_Ends()
        {
            var rc = NewStarted();
            for (int i = 0; i < 8; i++)
            {
                rc.ApplyWrong(); // overload → round over
            }

            int heatAtEnd = rc.Heat;
            rc.ApplyCorrect(); // ignored
            rc.ApplyWrong();   // ignored
            rc.Tick(100f);     // ignored

            Assert.AreEqual(RoundPhase.Overloaded, rc.Phase);
            Assert.AreEqual(0, rc.Stabilization, "ApplyCorrect after end must be a no-op");
            Assert.AreEqual(heatAtEnd, rc.Heat, "ApplyWrong after end must be a no-op");
        }

        [Test]
        public void Ended_Event_Fires_Once_With_Result_And_Stars()
        {
            var rc = NewStarted();
            int fired = 0;
            RoundEndedEvent captured = default;
            rc.Ended += e =>
            {
                fired++;
                captured = e;
            };

            for (int i = 0; i < 20; i++)
            {
                rc.ApplyCorrect();
            }

            Assert.AreEqual(1, fired, "Ended must fire exactly once");
            Assert.AreEqual(RoundPhase.Won, captured.Result);
            Assert.AreEqual(3, captured.Stars);
        }

        [Test]
        public void CorrectInserted_Event_Flags_Milestone()
        {
            var rc = NewStarted();
            bool lastMilestone = false;
            rc.CorrectInserted += e => lastMilestone = e.IsMilestone;

            for (int i = 0; i < 4; i++)
            {
                rc.ApplyCorrect();
            }

            Assert.IsFalse(lastMilestone, "combo 4 is not a milestone");

            rc.ApplyCorrect(); // 5th → milestone
            Assert.IsTrue(lastMilestone, "combo 5 is a milestone");
        }
    }
}
