using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Units;

public class CombatTests
{
    private GameObject attackerObj;
    private GameObject targetObj;
    private Unit attacker;
    private Unit target;
    private BattleBrothersRuleset ruleset;

    [SetUp]
    public void Setup()
    {
        attackerObj = new GameObject("Attacker");
        attacker = attackerObj.AddComponent<Unit>();
        attacker.Stats["HP"] = 100;
        attacker.Stats["MSKL"] = 50;

        targetObj = new GameObject("Target");
        target = targetObj.AddComponent<Unit>();
        target.Stats["HP"] = 100;
        target.Stats["MDEF"] = 0;

        ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
    }

    [TearDown]
    public void Teardown()
    {
        if (attackerObj != null) Object.DestroyImmediate(attackerObj);
        if (targetObj != null) Object.DestroyImmediate(targetObj);
        if (ruleset != null) Object.DestroyImmediate(ruleset);
    }

    [Test]
    public void OnHit_ReducesHP()
    {
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
        LogAssert.Expect(LogType.Log, $"[Ruleset] {attacker.UnitName} attacks {target.UnitName}");
    }
}
