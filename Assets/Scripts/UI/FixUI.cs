using UnityEngine;
using UnityEngine.UI;

public class FixUI : MonoBehaviour
{
    void Start()
    {
        FixComponentPanel();
        FixButtons();
    }

    void FixComponentPanel()
    {
        GameObject panel = GameObject.Find("ComponentPanel");
        if (panel == null) return;

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) return;

        // Устанавливаем Anchor в Top-Left
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        // Устанавливаем позицию и размер
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(250, 400);

        // Удаляем все Layout Groups
        var layout = panel.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layout != null) Destroy(layout);

        var fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter != null) Destroy(fitter);
    }

    void FixButtons()
    {
        string[] buttonNames = {
            "Btn_Arduino", "Btn_LED", "Btn_Resistor",
            "Btn_Button", "Btn_Potentiometer", "Btn_LCD",
            "Btn_Compile", "Btn_Clear"
        };

        float startY = -10f;
        float buttonHeight = 40f;
        float spacing = 10f;

        for (int i = 0; i < buttonNames.Length; i++)
        {
            GameObject btn = GameObject.Find(buttonNames[i]);
            if (btn == null) continue;

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt == null) continue;

            // Устанавливаем Anchor в Top-Left
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            // Вычисляем позицию
            float posY = startY - (i * (buttonHeight + spacing));

            // Устанавливаем позицию и размер
            rt.anchoredPosition = new Vector2(10, posY);
            rt.sizeDelta = new Vector2(230, buttonHeight);

            // Настраиваем цвета
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.18f, 0.18f, 0.18f); // #2D2D2D
                colors.highlightedColor = new Color(0.24f, 0.24f, 0.24f); // #3D3D3D
                colors.pressedColor = new Color(0.11f, 0.11f, 0.11f); // #1D1D1D
                button.colors = colors;
            }
        }
    }
}