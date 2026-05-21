#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Tek tıkla tüm MainCanvas UI hierarchy'sini yeniden inşa eder ve
/// SerializeField referanslarını SerializedObject API ile bağlar.
/// Menü: Game → Rebuild All UI
/// </summary>
public static class UIBuilder
{
    private const string UI = "Assets/Textures/UI/";

    // ════════════════════════════════════════════════════════════════════════
    //  Entry Point
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Game/Rebuild All UI")]
    static void RebuildAllUI()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[UIBuilder] Sahnede Canvas bulunamadı!"); return; }

        int   layer   = canvas.gameObject.layer;
        var   canvasT = canvas.transform;

        // ── Sprite'ları yükle ────────────────────────────────────────────────
        var sprBack     = Spr("back_button.png");
        var sprLike     = Spr("like_button.png");
        var sprPause    = Spr("pause_button.png");
        var sprPlay     = Spr("play_button.png");
        var sprSettings = Spr("settings_button.png");
        var sprCrown    = Spr("Crown.png");

        // ── Mevcut panelleri sil ─────────────────────────────────────────────
        foreach (var n in new[] { "GameUI", "MenuPanel", "SettingsPanel", "GameOverPanel", "PausePanel" })
            Kill(canvasT, n);

        // ════════════════════════════════════════════════════════════════════
        //  1. GameUI  (HUD)
        // ════════════════════════════════════════════════════════════════════
        var gameUI = MakeRT("GameUI", canvasT, layer);
        Stretch(gameUI);

        var hudScoreTMP = MakeTMP("ScoreText", gameUI, layer, "0", 96f,
                                  Color.white, HorizontalAlignmentOptions.Center);
        Pos(hudScoreTMP.rectTransform, 400, 120, 0, 700);

        var (pauseBtnImg, pauseBtn) = MakeImgBtn("PauseButton", gameUI, layer, sprPause);
        Pos(pauseBtnImg.rectTransform, 130, 130, 400, 700);

        // ════════════════════════════════════════════════════════════════════
        //  2. MenuPanel
        // ════════════════════════════════════════════════════════════════════
        var menuPanel = MakeRT("MenuPanel", canvasT, layer);
        Stretch(menuPanel);
        menuPanel.gameObject.AddComponent<PanelTransition>(); // RequireComponent → CanvasGroup

        // LogoArea
        var logoArea = MakeRT("LogoArea", menuPanel, layer);
        Pos(logoArea, 700, 350, 0, 600);

        var crownImg = MakeImg("CrownImage", logoArea, layer, sprCrown);
        Pos(crownImg.rectTransform, 180, 180, -180, 50);

        var titleTMP = MakeTMP("TitleText", logoArea, layer, "LEADERSHIP", 52f,
                                Color.black, HorizontalAlignmentOptions.Left);
        Pos(titleTMP.rectTransform, 400, 100, 80, -80);

        var (playImg, playBtn) = MakeImgBtn("PlayButton", menuPanel, layer, sprPlay);
        Pos(playImg.rectTransform, 180, 180, 0, 200);

        var (settingsImg, settingsBtn) = MakeImgBtn("SettingsButton", menuPanel, layer, sprSettings);
        Pos(settingsImg.rectTransform, 130, 130, -220, -650);

        var (likeImg, likeBtn) = MakeImgBtn("LikeButton", menuPanel, layer, sprLike);
        Pos(likeImg.rectTransform, 130, 130, 220, -650);

        var bestScoreTMP = MakeTMP("BestScoreText", menuPanel, layer, "", 48f,
                                   Color.black, HorizontalAlignmentOptions.Center);
        Pos(bestScoreTMP.rectTransform, 600, 80, 0, -500);
        bestScoreTMP.gameObject.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        //  3. SettingsPanel
        // ════════════════════════════════════════════════════════════════════
        var settingsPanel = MakeRT("SettingsPanel", canvasT, layer);
        Stretch(settingsPanel);
        // [RequireComponent(typeof(PanelTransition))] → PanelTransition → CanvasGroup
        settingsPanel.gameObject.AddComponent<SettingsPanelController>();

        var (backImg, backBtn) = MakeImgBtn("BackButton", settingsPanel, layer, sprBack);
        Pos(backImg.rectTransform, 130, 130, -400, 700);

        // Placeholder'lar — SettingsPanelController.Awake() bunları yeniden kurar
        Pos(MakeRT("VibrationRow",  settingsPanel, layer), 900, 120, 0, 200);
        Pos(MakeRT("ResetScoreRow", settingsPanel, layer), 900, 120, 0, 0);

        // ════════════════════════════════════════════════════════════════════
        //  4. GameOverPanel
        // ════════════════════════════════════════════════════════════════════
        var gameOverPanel = MakeRT("GameOverPanel", canvasT, layer);
        Stretch(gameOverPanel);
        gameOverPanel.gameObject.AddComponent<PanelTransition>();

        var goScoreTMP = MakeTMP("ScoreText", gameOverPanel, layer, "0", 128f,
                                  Color.white, HorizontalAlignmentOptions.Center);
        Pos(goScoreTMP.rectTransform, 400, 120, 0, 200);

        var (retryImg, retryBtn) = MakeImgBtn("RetryButton", gameOverPanel, layer, sprPlay);
        Pos(retryImg.rectTransform, 200, 200, -150, -100);

        var (goBtnImg, goBtn) = MakeImgBtn("MenuButton", gameOverPanel, layer, sprBack);
        Pos(goBtnImg.rectTransform, 200, 200, 150, -100);

        // ════════════════════════════════════════════════════════════════════
        //  5. PausePanel  (YENİ)
        // ════════════════════════════════════════════════════════════════════
        var pausePanel = MakeRT("PausePanel", canvasT, layer);
        Stretch(pausePanel);
        pausePanel.gameObject.AddComponent<PanelTransition>();

        // Yarı saydam siyah overlay
        var pauseBgRT = MakeRT("Background", pausePanel, layer);
        Stretch(pauseBgRT);
        pauseBgRT.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var (resumeImg, resumeBtn) = MakeImgBtn("ResumeButton", pausePanel, layer, sprPlay);
        Pos(resumeImg.rectTransform, 200, 200, -150, 0);

        var (pauseMenuImg, pauseMenuBtn) = MakeImgBtn("MenuButton", pausePanel, layer, sprBack);
        Pos(pauseMenuImg.rectTransform, 200, 200, 150, 0);

        // ════════════════════════════════════════════════════════════════════
        //  SerializeField Wiring
        // ════════════════════════════════════════════════════════════════════
        Wire_MenuUIController(menuPanel, settingsPanel, playBtn, settingsBtn, backBtn);
        Wire_SoloGameManager(gameOverPanel, retryBtn, goBtn, menuPanel, gameUI.gameObject);
        Wire_SoloScoreManager(hudScoreTMP, goScoreTMP, bestScoreTMP);
        Wire_PauseToggleButtonController(pauseBtn, pauseBtnImg, sprPause, sprPlay,
                                         pausePanel, resumeBtn, pauseMenuBtn);
        Wire_GameOverAdGate(retryBtn, goBtn);
        Wire_SettingsPanelController(settingsPanel);

        // ── Bitir ────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[UIBuilder] UI Rebuild tamamlandı ✓");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Wire Helpers
    // ════════════════════════════════════════════════════════════════════════

    static void Wire_MenuUIController(
        RectTransform menuPanel, RectTransform settingsPanel,
        Button playBtn, Button settingsBtn, Button backBtn)
    {
        var ctrl = Object.FindFirstObjectByType<MenuUIController>();
        if (ctrl == null) { MissingWarn("MenuUIController"); return; }
        var so = new SerializedObject(ctrl);
        so.FindProperty("MenuPanel").objectReferenceValue      = menuPanel.GetComponent<PanelTransition>();
        so.FindProperty("SettingsPanel").objectReferenceValue  = settingsPanel.GetComponent<PanelTransition>();
        so.FindProperty("PlayButton").objectReferenceValue     = playBtn;
        so.FindProperty("SettingsButton").objectReferenceValue = settingsBtn;
        so.FindProperty("BackButton").objectReferenceValue     = backBtn;
        so.ApplyModifiedProperties();
    }

    static void Wire_SoloGameManager(
        RectTransform gameOverPanel, Button retryBtn, Button menuBtn,
        RectTransform menuPanel, GameObject hudPanel)
    {
        var gm = Object.FindFirstObjectByType<SoloGameManager>();
        if (gm == null) { MissingWarn("SoloGameManager"); return; }
        var so = new SerializedObject(gm);
        so.FindProperty("GameOverPanel").objectReferenceValue = gameOverPanel.GetComponent<PanelTransition>();
        so.FindProperty("RetryButton").objectReferenceValue   = retryBtn;
        so.FindProperty("MenuButton").objectReferenceValue    = menuBtn;
        so.FindProperty("MenuPanel").objectReferenceValue     = menuPanel.GetComponent<PanelTransition>();
        so.FindProperty("HudPanel").objectReferenceValue      = hudPanel;
        so.ApplyModifiedProperties();
    }

    static void Wire_SoloScoreManager(
        TextMeshProUGUI hudScore, TextMeshProUGUI goScore, TextMeshProUGUI bestScore)
    {
        var sm = Object.FindFirstObjectByType<SoloScoreManager>();
        if (sm == null) { MissingWarn("SoloScoreManager"); return; }
        var so = new SerializedObject(sm);
        so.FindProperty("HudScoreText").objectReferenceValue      = hudScore;
        so.FindProperty("GameOverScoreText").objectReferenceValue = goScore;
        so.FindProperty("MenuBestScoreText").objectReferenceValue = bestScore;
        so.ApplyModifiedProperties();
    }

    static void Wire_PauseToggleButtonController(
        Button pauseToggleBtn, Image pauseIcon,
        Sprite pauseSprite, Sprite playSprite,
        RectTransform pausePanel, Button resumeBtn, Button pauseMenuBtn)
    {
        var ctrl = Object.FindFirstObjectByType<PauseToggleButtonController>();
        if (ctrl == null) { MissingWarn("PauseToggleButtonController"); return; }
        var so = new SerializedObject(ctrl);
        so.FindProperty("PauseToggleButton").objectReferenceValue    = pauseToggleBtn;
        so.FindProperty("PauseToggleIcon").objectReferenceValue      = pauseIcon;
        so.FindProperty("PauseSprite").objectReferenceValue          = pauseSprite;
        so.FindProperty("PlaySprite").objectReferenceValue           = playSprite;
        so.FindProperty("PausePanelTransition").objectReferenceValue = pausePanel.GetComponent<PanelTransition>();
        so.FindProperty("ResumeButton").objectReferenceValue         = resumeBtn;
        so.FindProperty("MenuButton").objectReferenceValue           = pauseMenuBtn;
        so.ApplyModifiedProperties();
    }

    static void Wire_GameOverAdGate(Button retryBtn, Button menuBtn)
    {
        var gate = Object.FindFirstObjectByType<GameOverAdGate>();
        if (gate == null) { MissingWarn("GameOverAdGate"); return; }
        var so = new SerializedObject(gate);
        so.FindProperty("RetryButton").objectReferenceValue = retryBtn;
        so.FindProperty("MenuButton").objectReferenceValue  = menuBtn;
        so.ApplyModifiedProperties();
    }

    static void Wire_SettingsPanelController(RectTransform settingsPanel)
    {
        var ctrl = settingsPanel.GetComponent<SettingsPanelController>();
        if (ctrl == null) { MissingWarn("SettingsPanelController on SettingsPanel"); return; }
        var sm = Object.FindFirstObjectByType<SoloScoreManager>();
        if (sm == null) return;
        var so = new SerializedObject(ctrl);
        so.FindProperty("Score").objectReferenceValue = sm;
        so.ApplyModifiedProperties();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Factory Helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Sadece RectTransform olan boş konteyner.</summary>
    static RectTransform MakeRT(string name, Transform parent, int layer)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Image bileşeni olan obje (Button yok).</summary>
    static Image MakeImg(string name, Transform parent, int layer, Sprite sprite)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    /// <summary>Image + Button bileşeni olan obje.</summary>
    static (Image img, Button btn) MakeImgBtn(string name, Transform parent, int layer, Sprite sprite)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return (img, btn);
    }

    /// <summary>TextMeshProUGUI bileşeni olan obje.</summary>
    static TextMeshProUGUI MakeTMP(string name, Transform parent, int layer,
        string text, float size, Color color, HorizontalAlignmentOptions hAlign)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.horizontalAlignment = hAlign;
        tmp.verticalAlignment   = VerticalAlignmentOptions.Middle;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Layout Helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Merkez anchor, verilen boyut ve konum.</summary>
    static void Pos(RectTransform rt, float w, float h, float x, float y)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>Tam doldur: 0,0 → 1,1 anchor, sıfır sizeDelta.</summary>
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Utility
    // ════════════════════════════════════════════════════════════════════════

    static Sprite Spr(string filename)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(UI + filename);
        if (s == null)
            Debug.LogWarning($"[UIBuilder] Sprite yüklenemedi: {UI}{filename}");
        return s;
    }

    static void Kill(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static void MissingWarn(string component) =>
        Debug.LogWarning($"[UIBuilder] '{component}' sahnede bulunamadı — wiring atlandı.");
}
#endif
