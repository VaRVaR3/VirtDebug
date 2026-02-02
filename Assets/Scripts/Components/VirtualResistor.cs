using UnityEngine;

public class VirtualResistor : CircuitComponent
{
    [Header("Resistor Settings")]
    public float[] standardValues = { 100, 220, 330, 470, 1000, 2200, 3300, 4700, 10000 };
    public Color[] bandColors = new Color[4];

    private Renderer[] bandRenderers;

    void Start()
    {
        maxVoltage = 50f;
        maxCurrent = 1.0f;
        requiresPolarity = false;

        if (resistance <= 0)
        {
            resistance = standardValues[Random.Range(0, standardValues.Length)];
        }

        InitializeBands();
    }

    void InitializeBands()
    {
        bandRenderers = new Renderer[4];

        for (int i = 0; i < 4; i++)
        {
            string bandName = $"Band_{i + 1}";
            Transform band = transform.Find(bandName);

            if (band == null)
            {
                GameObject bandObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bandObj.name = bandName;
                bandObj.transform.parent = transform;

                float position = -0.1f + (i * 0.066f);
                bandObj.transform.localPosition = new Vector3(position, 0, 0);
                bandObj.transform.localScale = new Vector3(0.03f, 0.12f, 0.12f);

                band = bandObj.transform;
            }

            bandRenderers[i] = band.GetComponent<Renderer>();
        }

        UpdateBandColors();
    }

    void UpdateBandColors()
    {
        if (bandRenderers == null || bandRenderers.Length < 4) return;

        int ohmValue = Mathf.RoundToInt(resistance);
        Color[] colors = new Color[4];

        if (ohmValue < 100)
        {
            colors[0] = GetColorForDigit(ohmValue / 10);
            colors[1] = GetColorForDigit(ohmValue % 10);
            colors[2] = Color.black;
            colors[3] = GetToleranceColor(5);
        }
        else if (ohmValue < 1000)
        {
            int firstDigit = ohmValue / 100;
            int secondDigit = (ohmValue % 100) / 10;

            colors[0] = GetColorForDigit(firstDigit);
            colors[1] = GetColorForDigit(secondDigit);
            colors[2] = Color.black;
            colors[3] = GetToleranceColor(5);
        }
        else
        {
            int kiloOhms = ohmValue / 1000;
            int remainder = (ohmValue % 1000) / 100;

            colors[0] = GetColorForDigit(kiloOhms);
            colors[1] = GetColorForDigit(remainder);
            colors[2] = Color.red;
            colors[3] = GetToleranceColor(5);
        }

        for (int i = 0; i < 4 && i < bandRenderers.Length; i++)
        {
            if (bandRenderers[i] != null)
            {
                bandRenderers[i].material.color = colors[i];
            }
        }
    }

    Color GetColorForDigit(int digit)
    {
        switch (digit)
        {
            case 0: return Color.black;
            case 1: return new Color(0.65f, 0.16f, 0.16f); // Коричневый
            case 2: return Color.red;
            case 3: return new Color(1f, 0.65f, 0f); // Оранжевый
            case 4: return Color.yellow;
            case 5: return Color.green;
            case 6: return Color.blue;
            case 7: return Color.magenta;
            case 8: return Color.gray;
            case 9: return Color.white;
            default: return Color.black;
        }
    }

    Color GetToleranceColor(int tolerancePercent)
    {
        switch (tolerancePercent)
        {
            case 1: return Color.green;
            case 2: return Color.red;
            case 5: return new Color(1f, 0.65f, 0f); // Оранжевый
            case 10: return Color.white;
            default: return Color.gray;
        }
    }

    public void SetResistance(float newResistance)
    {
        resistance = Mathf.Max(newResistance, 1f);
        UpdateBandColors();

        Debug.Log($"Resistor set to {resistance}Ω");
    }

    public override void OnVoltageChanged(float voltage)
    {
        base.OnVoltageChanged(voltage);

        // В CircuitComponent теперь есть поле currentCurrent
        if (currentCurrent > 0.5f)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                float heat = Mathf.Clamp01(currentCurrent / maxCurrent);
                Color heatedColor = Color.Lerp(new Color(0.65f, 0.16f, 0.16f), Color.red, heat * 0.3f);
                rend.material.color = heatedColor;
            }
        }
    }

    public override void UpdateComponent()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color targetColor = new Color(0.65f, 0.16f, 0.16f);
            if (rend.material.color != targetColor)
            {
                rend.material.color = Color.Lerp(rend.material.color, targetColor, Time.deltaTime);
            }
        }
    }
}