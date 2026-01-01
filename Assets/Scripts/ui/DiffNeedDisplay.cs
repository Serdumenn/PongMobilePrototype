using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DiffNeedDisplay : MonoBehaviour
{
    [Header("PlayerPrefs key")]
    public string prefsKey = "diffNeed";

    [Header("Fallback if key doesn't exist yet")]
    public int defaultValue = 3;

    [Header("Clamp range")]
    public int minValue = 1;
    public int maxValue = 9;

    [Header("Optional explicit targets (leave empty to auto-find)")]
    public Text uguiText;
    public Component tmpText;

    int _lastValue = int.MinValue;

    void Awake()
    {
        AutoBindIfNeeded();
        Refresh(true);
    }

    void OnEnable()
    {
        AutoBindIfNeeded();
        Refresh(true);
    }

    void Update()
    {
        Refresh(false);
    }

    void AutoBindIfNeeded()
    {
        if (uguiText == null)
            uguiText = GetComponentInChildren<Text>(true);

        if (tmpText == null)
        {
            var comps = GetComponentsInChildren<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null) continue;
                var t = comps[i].GetType();
                if (t.Name == "TextMeshProUGUI" || t.Name == "TMP_Text")
                {
                    tmpText = comps[i];
                    break;
                }
            }
        }
    }

    void Refresh(bool force)
    {
        int v = PlayerPrefs.GetInt(prefsKey, defaultValue);
        v = Mathf.Clamp(v, minValue, maxValue);

        if (!force && v == _lastValue) return;
        _lastValue = v;

        bool wrote = false;

        if (uguiText != null)
        {
            uguiText.text = v.ToString();
            wrote = true;
        }

        if (tmpText != null)
        {
            var prop = tmpText.GetType().GetProperty("text");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(tmpText, v.ToString(), null);
                wrote = true;
            }
        }

        if (!wrote)
        {
            Debug.LogWarning(
                $"[DiffNeedDisplay] No Text/TMP found under '{gameObject.name}'. " +
                $"Your number is probably an Image sprite. Add a Text/TMP component for the value, or assign uguiText/tmpText manually.");
        }
    }
}