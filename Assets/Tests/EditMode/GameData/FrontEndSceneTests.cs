using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Randint.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FrontEndSceneTests
{
    private static Type GameType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static MonoBehaviour Controller(string name) => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
        .Single(c => c.GetType().Name == name);
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name).Invoke(target, null);
    private static void Click(object controller, string buttonName)
    {
        // 실제 Button.onClick 경로를 사용합니다. Controller 메서드만 호출하는 테스트와 구분합니다.
        object click = Property(Field(controller, buttonName), "onClick");
        Invoke(click, "Invoke");
    }

    [UnityTest]
    public IEnumerator TitleButtonEntersLobbyAndLoadsJson()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1));
        MonoBehaviour controller = Controller("TitleSceneController");
        CheckLocalizedText(controller);
        AssertButtonRaycast(controller, "enterButton", true);
        Click(controller, "enterButton");
        Click(controller, "enterButton"); // 중복 입력으로 이중 로드를 시작하지 않습니다.
        Assert.That(Property(Field(controller, "sceneLoader"), "IsLoading"), Is.True);
        yield return WaitForScene("Lobby");
        Assert.That(Controller("LobbySceneController"), Is.Not.Null);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LobbyModalsBlockNewGameAndPreserveExistingMemory()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Lobby");
        yield return null;
        Type state = GameType("MapRunState");
        state.GetMethod("SaveCurrentNode").Invoke(null, new object[] { 1234, "test" });
        MonoBehaviour lobby = Controller("LobbySceneController");
        CheckLocalizedText(lobby);
        AssertButtonRaycast(lobby, "newGameButton", true);
        foreach (string kind in new[] { "settings", "continue" })
        {
            Click(lobby, kind + "Button");
            Assert.That((bool)Property(lobby, "IsModalOpen"), Is.True);
            Assert.That(((GameObject)Field(lobby, kind + "Window")).activeInHierarchy, Is.True);
            Assert.That(((CanvasGroup)Field(lobby, "mainWindow")).interactable, Is.False);
            yield return null;
            CheckLocalizedText(lobby);
            AssertButtonRaycast(lobby, "newGameButton", false);
            AssertButtonRaycast(lobby, kind + "CloseButton", true);
            Invoke(lobby, "StartNewGame");
            Assert.That(Property(Field(lobby, "sceneLoader"), "IsLoading"), Is.False);
            Assert.That(state.GetProperty("CurrentNodeID").GetValue(null), Is.EqualTo(1234));
            Click(lobby, kind + "CloseButton");
            Assert.That((bool)Property(lobby, "IsModalOpen"), Is.False);
            Assert.That(((CanvasGroup)Field(lobby, "mainWindow")).interactable, Is.True);
        }
        state.GetMethod("Reset").Invoke(null, null);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator NewGameResetsMapPositionAndRestoresTime()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Lobby");
        yield return null;
        Type state = GameType("MapRunState");
        state.GetMethod("SaveCurrentNode").Invoke(null, new object[] { 1234, "old run" });
        state.GetMethod("BeginMapEntry").Invoke(null, null);
        Time.timeScale = 0f;
        Click(Controller("LobbySceneController"), "newGameButton");
        yield return WaitForScene("Map_1F");
        yield return null;
        MonoBehaviour map = Controller("MapPlayerPositionFeedback");
        Assert.That(Property(map, "CurrentNode"), Is.EqualTo(Field(map, "startNode")));
        Assert.That(state.GetProperty("IsEnteringMapFromBattle").GetValue(null), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(Property(Controller("MapSceneFadeIn"), "IsFadeComplete"), Is.True);
        yield return new ExitPlayMode();
    }

    private static IEnumerator WaitForScene(string scene)
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (SceneManager.GetActiveScene().name != scene)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Scene 전환 시간 초과");
            yield return null;
        }
        yield return null;
    }

    private static void CheckLocalizedText(MonoBehaviour controller)
    {
        Canvas.ForceUpdateCanvases();
        MonoBehaviour localizer = controller.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "LocalizedSceneTexts");
        foreach (object binding in (IEnumerable)Field(localizer, "bindings"))
        {
            object text = binding.GetType().GetField("target").GetValue(binding);
            string key = (string)binding.GetType().GetField("key").GetValue(binding);
            Assert.That(Property(text, "text"), Is.EqualTo(GameData.Text(key)));
            Assert.That(Property(text, "font"), Is.Not.Null);
            if (((Component)text).gameObject.activeInHierarchy)
            {
                text.GetType().GetMethod("ForceMeshUpdate").Invoke(text, new object[] { false, false });
                Assert.That(Property(text, "isTextOverflowing"), Is.False, key + ": TMP 영역 넘침");
            }
        }
    }

    private static void AssertButtonRaycast(MonoBehaviour controller, string buttonName, bool shouldReceive)
    {
        Canvas.ForceUpdateCanvases();
        var button = (Component)Field(controller, buttonName);
        var rect = (RectTransform)button.transform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        Type eventSystemType = GameType("UnityEngine.EventSystems.EventSystem");
        object eventSystem = eventSystemType.GetProperty("current").GetValue(null);
        Type pointerType = GameType("UnityEngine.EventSystems.PointerEventData");
        object pointer = Activator.CreateInstance(pointerType, new[] { eventSystem });
        pointerType.GetProperty("position").SetValue(pointer, screenPoint);
        Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(GameType("UnityEngine.EventSystems.RaycastResult"));
        var results = (IList)Activator.CreateInstance(listType);
        eventSystemType.GetMethod("RaycastAll").Invoke(eventSystem, new object[] { pointer, results });
        Assert.That(results.Count, Is.GreaterThan(0));
        var topObject = (GameObject)Property(results[0], "gameObject");
        bool reachesButton = topObject.transform == button.transform || topObject.transform.IsChildOf(button.transform);
        Assert.That(reachesButton, Is.EqualTo(shouldReceive), buttonName + ": 화면 입력 차단/클릭 영역");
    }
}
