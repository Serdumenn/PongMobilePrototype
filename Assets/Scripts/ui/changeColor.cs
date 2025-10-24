using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeColor : MonoBehaviour {
    public Text[] texts;
    public int currentIndex = 0;
    private Button button;
    public Color[] buttonColors;
    void Start()
    {
        if (button == null) button = GetComponent<Button>();
    }

    public void Change()
    {
        currentIndex = (currentIndex + 1) % buttonColors.Length;

        if (buttonColors.Length > 0)
        {   
            button.image.color = buttonColors[currentIndex];
        }

        foreach (Text text in texts)
        {
            text.gameObject.SetActive(false);
        }
        
        if (texts.Length > currentIndex)
        {
            texts[currentIndex].gameObject.SetActive(true);
        }
    }
}