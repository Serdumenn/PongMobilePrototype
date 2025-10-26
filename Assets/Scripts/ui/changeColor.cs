using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeColor : MonoBehaviour
{
    public Text[] texts;
    public int currentIndex = 0;
    private Button button;
    public Color[] buttonColors;

    void Start()
    {
        if (button == null) button = GetComponent<Button>();
        ApplyState();
    }

    public void Change()
    {
        if (buttonColors == null || buttonColors.Length == 0)
        {
            Debug.LogWarning("changeColor: buttonColors boş.");
            return;
        }

        currentIndex = (currentIndex + 1) % buttonColors.Length;
        ApplyState();
    }

    private void ApplyState()
    {
        if (button != null && buttonColors != null && buttonColors.Length > 0)
        {
            int safeIndex = Mathf.Clamp(currentIndex, 0, buttonColors.Length - 1);
            if (button.image != null) button.image.color = buttonColors[safeIndex];
        }

        if (texts != null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null) texts[i].gameObject.SetActive(i == currentIndex);
            }
        }
    }
}