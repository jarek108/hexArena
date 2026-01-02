using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;
using HexGame.Units;
using System.Linq;

namespace HexGame.Tests
{
    public class TurnFlowTests
    {
        private GameObject gmGO;
        private GameMaster gm;
        private BattleBrothersRuleset ruleset;
        private UnitManager unitManager;
        private GameObject unitManagerGO;
        private UnitSet unitSet;
        private List<GameObject> createdObjects = new List<GameObject>();

        [SetUp]
        public void Setup()
        {
            createdObjects.Clear();
            // Reset Singletons
            typeof(GameMaster).GetProperty("Instance").SetValue(null, null);
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);

            gmGO = new GameObject("GameMaster");
            createdObjects.Add(gmGO);
            gm = gmGO.AddComponent<GameMaster>();
            typeof(GameMaster).GetProperty("Instance").SetValue(null, gm);

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            gm.ruleset = ruleset;

            unitManagerGO = new GameObject("UnitManager");
            createdObjects.Add(unitManagerGO);
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            
            // Critical: Erase any existing units from previous tests
            unitManager.EraseAllUnits();

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();

            Object.DestroyImmediate(ruleset);
            typeof(GameMaster).GetProperty("Instance").SetValue(null, null);
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
        }

        private Unit CreateUnit(string name, int initiative, int team = 0)
        {
            var type = new UnitType { id = name.ToLower(), Name = name };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "INI", value = initiative },
                new UnitStatValue { id = "AP", value = 9 },
                new UnitStatValue { id = "FAT", value = 100 }
            };
            unitSet.units.Add(type);

            var go = new GameObject(name);
            go.transform.SetParent(unitManagerGO.transform);
            createdObjects.Add(go);
            var unit = go.AddComponent<Unit>();
            unit.unitSet = unitSet;
            unit.Initialize(type.id, team);
            return unit;
        }

        [Test]
        public void RoundStart_SortsUnitsByInitiative()
        {
            var u1 = CreateUnit("Slow", 50);
            var u2 = CreateUnit("Fast", 100);
            var u3 = CreateUnit("Medium", 75);

            gm.StartNewRound();

            Assert.AreEqual("Fast", gm.activeUnit.UnitName, "Fastest unit should be active first.");
            Assert.AreEqual(2, gm.TurnQueue.Count);
            Assert.AreEqual("Medium", gm.TurnQueue[0].UnitName, "Medium unit should be next.");
            Assert.AreEqual("Slow", gm.TurnQueue[1].UnitName, "Slowest unit should be last.");
        }

        [Test]
        public void TurnStart_RestoresAP()
        {
            var u = CreateUnit("Unit", 100);
            u.SetStat("AP", 0);

            gm.StartNewRound(); // Starts turn for 'u'

            Assert.AreEqual(9, u.GetStat("AP"), "AP should be restored to base value on turn start.");
        }

        [Test]
        public void RoundStart_RecoversFatigue()
        {
            var u = CreateUnit("Unit", 100);
            u.SetStat("FAT", 50);

            gm.StartNewRound();

            Assert.AreEqual(35, u.GetStat("FAT"), "Fatigue should recover by 15 on round start.");
        }

        [Test]
        public void AdvanceTurn_CyclesToNextUnit()
        {
            var u1 = CreateUnit("Fast", 100);
            var u2 = CreateUnit("Slow", 50);

            gm.StartNewRound(); // Active: u1
            Assert.AreEqual("Fast", gm.activeUnit.UnitName);

            gm.AdvanceTurn(); // Active: u2
            Assert.AreEqual("Slow", gm.activeUnit.UnitName);
            Assert.AreEqual(0, gm.TurnQueue.Count);
        }

                [Test]

                public void AdvanceTurn_StartsNewRound_WhenQueueEmpty()

                {

                    var u = CreateUnit("Unit", 100);

        

                    gm.roundNumber = 0; // Reset just in case

                    gm.StartNewRound(); // Round 1. Active: u, Queue: empty

                    Assert.AreEqual(1, gm.roundNumber);

        

                    gm.AdvanceTurn(); // Queue was empty, should start Round 2. roundNumber becomes 2

                    Assert.AreEqual(2, gm.roundNumber);

                    Assert.AreEqual("Unit", gm.activeUnit.UnitName);

                }

        

                [Test]

                public void Wait_MovesUnitToEndOfRound()

                {

                    var u1 = CreateUnit("Fast", 100);

                    var u2 = CreateUnit("Slow", 50);

        

                    gm.StartNewRound(); // Active: u1, Queue: u2

                    Assert.AreEqual("Fast", gm.activeUnit.UnitName);

        

                    gm.WaitCurrentTurn(); // Active: u2, Waiting: u1

                    Assert.AreEqual("Slow", gm.activeUnit.UnitName);

                    Assert.AreEqual(1, gm.WaitingQueue.Count);

                    Assert.AreEqual(u1, gm.WaitingQueue[0]);

        

                    gm.AdvanceTurn(); // Main queue empty, processes Waiting queue. Active: u1

                    Assert.AreEqual("Fast", gm.activeUnit.UnitName);

                    Assert.AreEqual(0, gm.WaitingQueue.Count);

                }

            }

        }

        