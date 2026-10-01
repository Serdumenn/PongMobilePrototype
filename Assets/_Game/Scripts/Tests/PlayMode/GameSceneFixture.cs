using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public abstract class GameSceneFixture
{
    private const string SceneName = "Game";
    private const int TouchId = 1;

    private PrefsSnapshot prefs;
    private string saveRoot;
    private Touchscreen touchscreen;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode inputBehavior;
#endif

    protected SoloGameManager Game { get; private set; }

    [SetUp]
    public void SetUpScene()
    {
#if UNITY_EDITOR
        inputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        Application.runInBackground = true;
        touchscreen = InputSystem.AddDevice<Touchscreen>("PingiTestTouchscreen");

        prefs = PrefsSnapshot.Capture();
        PrefsSnapshot.UseCleanProfile();

        saveRoot = Path.Combine(Path.GetTempPath(), "PingiTests", "save_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(saveRoot);
        SaveLocation.Override(saveRoot);

        UnityEngine.Random.InitState(20261001);
    }

    [TearDown]
    public void TearDownScene()
    {
        Time.timeScale = 1f;
        DestroyPersistentObjects();
        SaveLocation.Override(null);
        prefs?.Restore();
        if (Directory.Exists(saveRoot)) Directory.Delete(saveRoot, true);
        if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = inputBehavior;
#endif
    }

    protected IEnumerator LoadGame()
    {
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;

        Game = Object.FindFirstObjectByType<SoloGameManager>();
        Assert.IsNotNull(Game, "SoloGameManager missing in scene");
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
    }

    protected void Touch(TouchPhase phase, Vector2 position)
    {
        Touch(TouchId, phase, position);
    }

    protected void Touch(int touchId, TouchPhase phase, Vector2 position)
    {
        touchscreen.MakeCurrent();
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = touchId, phase = phase, position = position, pressure = 1f });
    }

    protected static Vector2 ToScreen(Camera cam, Vector2 world)
    {
        var screen = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
        return new Vector2(Mathf.Clamp(screen.x, 0f, Screen.width - 1f), Mathf.Clamp(screen.y, 0f, Screen.height - 1f));
    }

    private static void DestroyPersistentObjects()
    {
        if (AdManager.Instance != null) Object.DestroyImmediate(AdManager.Instance.gameObject);
        var eventSystem = GameObject.Find("EditorEventSystem");
        if (eventSystem != null) Object.DestroyImmediate(eventSystem);
    }
}
