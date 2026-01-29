using UnityEngine;
using TMPro;

public class VirtualLCD : CircuitComponent
{
    [Header("LCD Settings")]
    public int columns = 16;
    public int rows = 2;
    public TextMeshPro displayText;
    public Color textColor = Color.green;
    public Color backlightColor = Color.blue;
    public float backlightIntensity = 0.5f;

    private string[] lines;
    private int currentCursorColumn = 0;
    private int currentCursorRow = 0;
    private bool backlightEnabled = true;
    private Renderer lcdRenderer;
    private Light backlightLight;

    void Start()
    {
        // Инициализация линий дисплея
        lines = new string[rows];
        for (int i = 0; i < rows; i++)
        {
            lines[i] = new string(' ', columns);
        }

        // Настройка компонентов
        lcdRenderer = GetComponent<Renderer>();
        backlightLight = GetComponentInChildren<Light>();

        // Настройка LCD
        resistance = 50f;
        maxVoltage = 5.0f;
        maxCurrent = 0.05f;
        requiresPolarity = true;

        // Настройка отображения
        if (displayText != null)
        {
            displayText.color = textColor;
            displayText.fontSize = 0.1f;
            UpdateDisplay();
        }

        // Настройка подсветки
        if (backlightLight != null)
        {
            backlightLight.color = backlightColor;
            backlightLight.intensity = backlightEnabled ? backlightIntensity : 0f;
        }

        // Начальное сообщение
        Print("LCD Ready");
        SetCursor(0, 1);
        Print("16x2 Display");
    }

    public void Print(string text)
    {
        if (string.IsNullOrEmpty(text) || !isActive) return;

        foreach (char c in text)
        {
            if (currentCursorColumn >= columns)
            {
                currentCursorColumn = 0;
                currentCursorRow++;

                if (currentCursorRow >= rows)
                {
                    ScrollUp();
                    currentCursorRow = rows - 1;
                }
            }

            // Заменяем символ в текущей позиции
            char[] lineChars = lines[currentCursorRow].ToCharArray();
            lineChars[currentCursorColumn] = c;
            lines[currentCursorRow] = new string(lineChars);

            currentCursorColumn++;
        }

        UpdateDisplay();
    }

    public void Println(string text)
    {
        Print(text);
        currentCursorColumn = 0;
        currentCursorRow++;

        if (currentCursorRow >= rows)
        {
            ScrollUp();
            currentCursorRow = rows - 1;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < rows; i++)
        {
            lines[i] = new string(' ', columns);
        }
        currentCursorColumn = 0;
        currentCursorRow = 0;
        UpdateDisplay();
    }

    public void SetCursor(int column, int row)
    {
        currentCursorColumn = Mathf.Clamp(column, 0, columns - 1);
        currentCursorRow = Mathf.Clamp(row, 0, rows - 1);
    }

    public void SetBacklight(bool enabled)
    {
        backlightEnabled = enabled;
        if (backlightLight != null)
        {
            backlightLight.intensity = enabled ? backlightIntensity : 0f;
        }
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            string displayContent = "";
            for (int i = 0; i < rows; i++)
            {
                displayContent += lines[i] + "\n";
            }
            displayText.text = displayContent.TrimEnd('\n');
        }
    }

    private void ScrollUp()
    {
        for (int i = 0; i < rows - 1; i++)
        {
            lines[i] = lines[i + 1];
        }
        lines[rows - 1] = new string(' ', columns);
    }

    public override void OnVoltageChanged(float voltage)
    {
        base.OnVoltageChanged(voltage);

        // LCD может реагировать на напряжение (например, включать/выключать подсветку)
        if (voltage > 3.0f && !backlightEnabled)
        {
            SetBacklight(true);
        }
        else if (voltage < 1.0f && backlightEnabled)
        {
            SetBacklight(false);
        }
    }

    public override void UpdateComponent()
    {
        // Можно добавить анимации или эффекты
    }

    public string GetLine(int row)
    {
        if (row >= 0 && row < rows)
            return lines[row];
        return "";
    }
}