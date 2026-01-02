using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Units;

public class CombatTests
{
    private GameObject unitManagerGO;
    private UnitManager unitManager;
    private GameObject attackerObj;
    private GameObject targetObj;
    private Unit attacker;
    private Unit target;
    private BattleBrothersRuleset ruleset;

    [SetUp]
    public void Setup()
    {
        unitManagerGO = new GameObject("UnitManager");
        unitManager = unitManagerGO.AddComponent<UnitManager>();
        unitManager.activeUnitSetPath = "";
        typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

        attackerObj = new GameObject("Attacker");
        attacker = attackerObj.AddComponent<Unit>();
        attacker.Stats["HP"] = 100;
        attacker.Stats["MAT"] = 50;
        attacker.Stats["ABY"] = 20;
        attacker.Stats["ADM"] = 100;

        targetObj = new GameObject("Target");
        target = targetObj.AddComponent<Unit>();
        target.Stats["HP"] = 100;
        target.Stats["MDF"] = 0;

        // Setup Names via Set
        var unitSet = new UnitSet();
        unitSet.units = new List<UnitType> { 
            new UnitType { id = "attacker", Name = "Attacker" },
            new UnitType { id = "target", Name = "Target" }
        };
        unitManager.ActiveUnitSet = unitSet;
        attacker.Initialize("attacker", 1);
        target.Initialize("target", 2);

        ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
        ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
        ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
        ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
    }

    [TearDown]
    public void Teardown()
    {
        typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
        if (unitManagerGO != null) Object.DestroyImmediate(unitManagerGO);
        if (attackerObj != null) Object.DestroyImmediate(attackerObj);
        if (targetObj != null) Object.DestroyImmediate(targetObj);
        if (ruleset != null) Object.DestroyImmediate(ruleset);
    }

    [Test]
    public void OnHit_ReducesHP()
    {
        // Arrange
        attacker.Stats["HP"] = 100;
        target.Stats["HP"] = 100;
        attacker.Stats["ABY"] = 100;
        attacker.Stats["ADM"] = 100;

        // Act
        ruleset.OnHit(attacker, target, 20);

        // Assert
        Assert.AreEqual(80, target.Stats["HP"]);
    }

    [Test]
    public void OnHit_KillsUnit_WhenHPDropsToZero()
    {
        // Arrange
        target.Stats["HP"] = 10;
        attacker.Stats["ABY"] = 100;

        // Act
        ruleset.OnHit(attacker, target, 20);

        // Assert
        // In EditMode, DestroyImmediate works. The object should appear null.
        Assert.IsTrue(targetObj == null || target == null, "Target should be destroyed");
    }

    [Test]
    public void OnAttacked_LogsMessage()
    {
        // Just verify no crash
        ruleset.OnAttacked(attacker, target);
        LogAssert.Expect(LogType.Log, "[Ruleset] Attacker attacks Target");
    }
}