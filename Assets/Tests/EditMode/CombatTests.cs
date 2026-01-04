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
    private GameMaster gameMaster;
    private BattleBrothersRuleset ruleset;

    [SetUp]
    public void Setup()
    {
        unitManagerGO = new GameObject("UnitManager");
        unitManager = unitManagerGO.AddComponent<UnitManager>();
        var unitSet = new UnitSet();
        unitSet.units.Add(new UnitType { id = "attacker", Name = "Attacker" });
        unitSet.units.Add(new UnitType { id = "target", Name = "Target" });
        unitManager.ActiveUnitSet = unitSet;
        typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

        var go1 = new GameObject("Attacker");
        attacker = go1.AddComponent<Unit>();
        attacker.Initialize("attacker", 1);
        attacker.SetStat("HP", 100);
        attacker.SetStat("MAT", 50);
        attacker.SetStat("ABY", 20);
        attacker.SetStat("ADM", 100);

        var go2 = new GameObject("Target");
        target = go2.AddComponent<Unit>();
        target.Initialize("target", 2);
        target.SetStat("HP", 100);
        target.SetStat("MDF", 0);

        var gmGO = new GameObject("GameMaster");
        gameMaster = gmGO.AddComponent<GameMaster>();
        ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
        gameMaster.ruleset = ruleset;
        
        // Inject mock instance since GameMaster is a singleton
        typeof(GameMaster).GetProperty("Instance").SetValue(null, gameMaster);
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
        attacker.SetStat("HP", 100);
        target.SetStat("HP", 100);
        attacker.SetStat("ABY", 100);
        attacker.SetStat("ADM", 100);

        // Act
        ruleset.OnHit(attacker, target, 20);

        // Assert
        Assert.AreEqual(80, target.GetStat("HP"));
    }

    [Test]
    public void OnHit_KillsUnit_WhenHPDropsToZero()
    {
        // Arrange
        target.SetStat("HP", 10);
        attacker.SetStat("ABY", 100);

        // Act
        ruleset.OnHit(attacker, target, 20);

        // Assert
        // In EditMode, DestroyImmediate works. The object should appear null.
        Assert.IsTrue(target == null || target.gameObject == null, "Target should be destroyed");
    }
}
