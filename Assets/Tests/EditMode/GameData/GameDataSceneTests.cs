using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Randint.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameDataSceneTests
{
    private static Type GameType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);
    private static MonoBehaviour[] Components(string name) => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
        .Where(component => component.GetType().Name == name).ToArray();
    private static object Property(object owner, string name) => owner.GetType().GetProperty(name).GetValue(owner);
    private static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    [Test]
    public void SkillInstancesUseJsonAndKeepSpIndependent()
    {
        Type type = GameType("OperatorSkill");
        object first = Activator.CreateInstance(type, new object[] { "dorothy_skill_1" });
        object second = Activator.CreateInstance(type, new object[] { "dorothy_skill_1" });
        type.GetMethod("OnNaturalRecovery").Invoke(first, new object[] { 2 });
        Assert.That(Property(first, "MaxSP"), Is.EqualTo(2));
        Assert.That(Property(first, "IsReady"), Is.True);
        Assert.That(Property(second, "CurrentSP"), Is.Zero);
        Assert.That(GameData.Catalog.Skill("dorothy_skill_1").maxSP, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator ScenesResolveJsonAndNewBattleRestoresStats()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Map_1F");
        yield return null;
        Assert.That(Components("MapNode").Length, Is.EqualTo(8));
        foreach (MonoBehaviour node in Components("MapNode"))
            Assert.That(Property(node, "Description"), Is.EqualTo(GameData.Text($"map.map_1f.node_{Property(node, "NodeID")}.description")));

        yield return SceneManager.LoadSceneAsync("Playing_Scene");
        yield return null;
        MonoBehaviour[] operators = Components("Operator");
        Assert.That(operators.Length, Is.EqualTo(3));
        foreach (MonoBehaviour op in operators)
        {
            Assert.That(Property(op, "CurrentHP"), Is.EqualTo(Property(op, "MaxHP")));
            op.GetType().GetMethod("DebugDamage").Invoke(op, new object[] { 1 });
            Assert.That(Property(op, "CurrentHP"), Is.EqualTo((int)Property(op, "MaxHP") - 1));
        }
        Assert.That(Components("Enemy").Length, Is.EqualTo(4));
        foreach (MonoBehaviour enemy in Components("Enemy"))
            Assert.That(Property(enemy, "MaxHP"), Is.EqualTo(1000));
        Assert.That(GameData.Catalog.Operator("saria").maxHP, Is.EqualTo(3150));

        yield return SceneManager.LoadSceneAsync("Playing_Scene");
        yield return null;
        foreach (MonoBehaviour op in Components("Operator"))
            Assert.That(Property(op, "CurrentHP"), Is.EqualTo(Property(op, "MaxHP")));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator SuccessAndFailurePresentersReadJsonText()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Playing_Scene");
        // 기존 진입 Fade와 결과 Fade의 시간 제어가 겹치지 않게 기다립니다.
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!(bool)Property(Components("BattleSceneFadeIn").Single(), "IsFadeComplete"))
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        MonoBehaviour presenter = Components("BattleResultPresenter").Single();
        presenter.GetType().GetField("displayDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, 0.2f);
        foreach (string result in new[] { "Success", "Failure" })
        {
            object resultType = Enum.Parse(GameType("BattleResultType"), result);
            var routine = (IEnumerator)presenter.GetType().GetMethod("ShowResult").Invoke(presenter, new[] { resultType });
            presenter.StartCoroutine(routine);
            yield return null;
            string key = result.ToLowerInvariant();
            Assert.That(Property(Field(presenter, "resultTitleText"), "text"), Is.EqualTo(GameData.Text($"battle.result.{key}.title")));
            Assert.That(Property(Field(presenter, "resultMessageText"), "text"), Is.EqualTo(GameData.Text($"battle.result.{key}.message")));
            Assert.That(Property(Field(presenter, "characterDialogueText"), "text"), Is.EqualTo(GameData.Text($"dialogue.battle_result.{key}")));
            Assert.That(Time.timeScale, Is.Zero);
            deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)Field(presenter, "isShowing"))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return null;
            }
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
        yield return new ExitPlayMode();
    }
}
