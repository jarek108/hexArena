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
            data.AddState(HexState.Selected);
            
            Assert.IsTrue(data.States.Contains(HexState.Selected));
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void RemoveState_UpdatesSet_AndFiresEvent()
        {
            data.AddState(HexState.Selected);
            eventFired = false; // Reset for removal check
            
            data.RemoveState(HexState.Selected);
            
            Assert.IsFalse(data.States.Contains(HexState.Selected));
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void UpdateStates_Batched_CorrectlyModifiesSet_AndFiresSingleEvent()
        {
            data.AddState(HexState.Hovered);
            int callCount = 0;
            data.OnStateChanged += () => callCount++; // Additional listener to count calls
            
            // Batch: Remove Hovered, Add Selected and Path
            data.UpdateStates(
                new[] { HexState.Selected, HexState.Path }, 
                new[] { HexState.Hovered }
            );

            Assert.IsFalse(data.States.Contains(HexState.Hovered), "Hovered should be removed");
            Assert.IsTrue(data.States.Contains(HexState.Selected), "Selected should be added");
            Assert.IsTrue(data.States.Contains(HexState.Path), "Path should be added");
            
            // Setup call + 1 batch call = 1 (we ignore the SetUp event because we added the listener after)
            Assert.AreEqual(1, callCount, "Event should only fire once for the batch operation");
        }

        [Test]
        public void UpdateStates_NoActualChange_DoesNotFireEvent()
        {
            data.AddState(HexState.Selected);
            int callCount = 0;
            data.OnStateChanged += () => callCount++;

            // Re-add existing, remove non-existent
            data.UpdateStates(new[] { HexState.Selected }, new[] { HexState.Hovered });

            Assert.AreEqual(0, callCount, "Event should not fire if the set remains logically identical");
        }
    }
}