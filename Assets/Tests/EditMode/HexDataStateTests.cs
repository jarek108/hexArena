using NUnit.Framework;
using HexGame;
using System.Collections.Generic;

namespace HexGame.Tests
{
    [TestFixture]
    public class HexDataStateTests
    {
        private HexData data;
        private bool eventFired;

        [SetUp]
        public void SetUp()
        {
            data = new HexData(0, 0);
            eventFired = false;
            data.OnStateChanged += () => eventFired = true;
        }

        [Test]
        public void AddState_UpdatesSet_AndFiresEvent()
        {
            data.AddState("Selected");
            
            Assert.IsTrue(data.States.Contains("Selected"));
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void RemoveState_UpdatesSet_AndFiresEvent()
        {
            data.AddState("Selected");
            eventFired = false; // Reset for removal check
            
            data.RemoveState("Selected");
            
            Assert.IsFalse(data.States.Contains("Selected"));
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void UpdateStates_Batched_CorrectlyModifiesSet_AndFiresSingleEvent()
        {
            data.AddState("Hovered");
            int callCount = 0;
            data.OnStateChanged += () => callCount++; // Additional listener to count calls
            
            // Batch: Remove Hovered, Add Selected and Path
            data.UpdateStates(
                new[] { "Selected", "Path" }, 
                new[] { "Hovered" }
            );

            Assert.IsFalse(data.States.Contains("Hovered"), "Hovered should be removed");
            Assert.IsTrue(data.States.Contains("Selected"), "Selected should be added");
            Assert.IsTrue(data.States.Contains("Path"), "Path should be added");
            
            // Setup call + 1 batch call = 1 (we ignore the SetUp event because we added the listener after)
            Assert.AreEqual(1, callCount, "Event should only fire once for the batch operation");
        }

        [Test]
        public void UpdateStates_NoActualChange_DoesNotFireEvent()
        {
            data.AddState("Selected");
            int callCount = 0;
            data.OnStateChanged += () => callCount++;

            // Re-add existing, remove non-existent
            data.UpdateStates(new[] { "Selected" }, new[] { "Hovered" });

            Assert.AreEqual(0, callCount, "Event should not fire if the set remains logically identical");
        }
    }
}