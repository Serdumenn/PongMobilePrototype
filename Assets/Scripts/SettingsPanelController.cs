using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PanelTransition))]
public sealed class SettingsPanelController : MonoBehaviour
{
    // HapticManager.cs bu sabiti okuyor — public ve const kalmalı
    public const string HapticsKey = "HapticsEnabled";

    private const float  ResetConfirmTimeout = 2.5f;
    private const string ResetDefaultText    = "Reset";
    private const string ResetConfirmText    = "Confirm?";

    // Tek SerializeField — Inspector'da zaten bağlı
    [SerializeField] private SoloScoreManager Score;

    // Awake'de atanır, başka SerializeField yok
    private TMP_Text  ButtonLabel;
    private bool      PendingResetConfirm;
    private Coroutine ResetTimeoutRoutine;

    // ── Awake ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();

        // İdempotent: önceki Awake'den veya eski YAML artıklarından kalan
        // child'ları temizle, sonra taze kur
        DestroyChild("VibrationRow");
        DestroyChild("ResetScoreRow");

        BuildUI();
    }

    private void OnDisable()
    {
        CancelResetConfirm();
    }

    // ── UI Kurulumu ──────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // VibrationRow — panel ortasından 200 birim yukarı
        RectTransform vibRT = CreateRow("VibrationRow", new Vector2(0f, 200f));
        CreateLabel(vibRT, "VibrationLabel", "Vibration");
        BuildToggle(vibRT);

        // ResetScoreRow — panel tam ortasında
        RectTransform resetRT = CreateRow("ResetScoreRow", new Vector2(0f, 0f));
        CreateLabel(resetRT, "ResetLabel", "Reset Score");
        BuildResetButton(resetRT);
    }

    // ── Satır Konteyneri ─────────────────────────────────────────────────────

    private RectTransform CreateRow(string name, Vector2 anchoredPos)
    {
        RectTransform rt = MakeRT(name, this.transform);
        SetCenter(rt, new Vector2(900f, 120f));
        rt.anchoredPosition = anchoredPos;
        return rt;
    }

    // ── Etiket ───────────────────────────────────────────────────────────────

    private void CreateLabel(RectTransform parent, string name, string text)
    {
        var go = new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);

        // AddComponent<TextMeshProUGUI> → RequireComponent zinciri RectTransform'u ekler
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text                = text;
        tmp.fontSize            = 48f;
        tmp.color               = Color.white;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Left;
        tmp.verticalAlignment   = VerticalAlignmentOptions.Middle;

        RectTransform rt = go.GetComponent<RectTransform>();
        SetCenter(rt, new Vector2(500f, 100f));
        rt.anchoredPosition = new Vector2(-200f, 0f);
    }

    // ── Toggle ───────────────────────────────────────────────────────────────

    private void BuildToggle(RectTransform parent)
    {
        // Toggle konteyneri — içinde Image yok, bu yüzden MakeRT kullanılıyor
        RectTransform toggleRT = MakeRT("VibrationToggle", parent);
        SetCenter(toggleRT, new Vector2(160f, 80f));
        toggleRT.anchoredPosition = new Vector2(250f, 0f);

        // Background — Toggle'ın dolu gri arka planı (spec: #555555, 160×80)
        var bgGO = new GameObject("Background");
        bgGO.layer = gameObject.layer;
        bgGO.transform.SetParent(toggleRT, false);
        Image bgImg = bgGO.AddComponent<Image>(); // RectTransform auto-eklenir
        bgImg.color = new Color(0x55 / 255f, 0x55 / 255f, 0x55 / 255f, 1f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        SetCenter(bgRT, new Vector2(160f, 80f));
        bgRT.anchoredPosition = Vector2.zero;

        // Checkmark — Background'ın ortasında küçük beyaz kare (spec: beyaz, 80×80)
        var ckGO = new GameObject("Checkmark");
        ckGO.layer = gameObject.layer;
        ckGO.transform.SetParent(bgRT, false);
        Image ckImg = ckGO.AddComponent<Image>(); // RectTransform auto-eklenir
        ckImg.color = Color.white;
        RectTransform ckRT = ckGO.GetComponent<RectTransform>();
        SetCenter(ckRT, new Vector2(80f, 80f));
        ckRT.anchoredPosition = Vector2.zero;

        // Toggle — Image'lardan SONRA ekleniyor (spec gereği)
        Toggle toggle          = toggleRT.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic   = bgImg;  // targetGraphic → Background Image
        toggle.graphic         = ckImg;  // graphic       → Checkmark Image
        toggle.isOn            = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        toggle.onValueChanged.AddListener(OnVibrationToggled);
    }

    // ── Reset Butonu ─────────────────────────────────────────────────────────

    private void BuildResetButton(RectTransform parent)
    {
        // Button konteyneri — Image auto-adds RectTransform
        var btnGO = new GameObject("ResetButton");
        btnGO.layer = gameObject.layer;
        btnGO.transform.SetParent(parent, false);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0x8B / 255f, 0f, 0f, 1f); // #8B0000

        Button btn          = btnGO.AddComponent<Button>();
        btn.targetGraphic   = btnImg;
        btn.onClick.AddListener(OnResetPressed);

        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        SetCenter(btnRT, new Vector2(160f, 80f));
        btnRT.anchoredPosition = new Vector2(250f, 0f);

        // ButtonLabel — butonun tam içini kaplayan metin
        var labelGO = new GameObject("ButtonLabel");
        labelGO.layer = gameObject.layer;
        labelGO.transform.SetParent(btnRT, false);

        ButtonLabel                     = labelGO.AddComponent<TextMeshProUGUI>();
        ButtonLabel.text                = ResetDefaultText;
        ButtonLabel.fontSize            = 40f;
        ButtonLabel.color               = Color.white;
        ButtonLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;
        ButtonLabel.verticalAlignment   = VerticalAlignmentOptions.Middle;

        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRT); // Buton yüzeyini tamamen kapla
    }

    // ── Vibration Mantığı ─────────────────────────────────────────────────────

    private void OnVibrationToggled(bool IsOn)
    {
        PlayerPrefs.SetInt(HapticsKey, IsOn ? 1 : 0);
        PlayerPrefs.Save();
        if (IsOn) HapticManager.Soft(); // Yalnızca açılırken titreşim ver
    }

    // ── Reset Mantığı (double-tap) ────────────────────────────────────────────

    private void OnResetPressed()
    {
        HapticManager.Soft();

        if (!PendingResetConfirm)
        {
            // İlk tap — onay bekleniyor
            PendingResetConfirm = true;
            if (ButtonLabel != null) ButtonLabel.text = ResetConfirmText;
            if (ResetTimeoutRoutine != null) StopCoroutine(ResetTimeoutRoutine);
            ResetTimeoutRoutine = StartCoroutine(ResetConfirmTimeoutCo());
            return;
        }

        // İkinci tap — işlemi gerçekleştir
        CancelResetConfirm();

        if (Score != null)
            Score.ResetBestScore();
        else
            Debug.LogWarning("[SettingsPanelController] Score referansı null — ResetBestScore çağrılamadı.");

        HapticManager.Medium();
    }

    private IEnumerator ResetConfirmTimeoutCo()
    {
        yield return new WaitForSecondsRealtime(ResetConfirmTimeout);
        CancelResetConfirm();
    }

    private void CancelResetConfirm()
    {
        PendingResetConfirm = false;
        if (ResetTimeoutRoutine != null)
        {
            StopCoroutine(ResetTimeoutRoutine);
            ResetTimeoutRoutine = null;
        }
        if (ButtonLabel != null)
            ButtonLabel.text = ResetDefaultText;
    }

    // ── RectTransform Yardımcıları ────────────────────────────────────────────

    // Sadece RectTransform gereken konteynerleri oluşturur (Image/TMP yok)
    private RectTransform MakeRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // Merkez anchor, merkez pivot, istenilen boyut
    private static void SetCenter(RectTransform rt, Vector2 sizeDelta)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
    }

    // Tam doldur (0,0 → 1,1 anchor), zero sizeDelta
    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // Verilen isimde bir child varsa yok eder (idempotent çalışma için)
    private void DestroyChild(string childName)
    {
        Transform t = this.transform.Find(childName);
        if (t != null) Destroy(t.gameObject);
    }
}
