using UnityEngine;

public class SimpleLED : CircuitComponent
{
    [Header("Simple LED Settings")]
    public Color ledColor = Color.red;
    public float intensity = 0f;

    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                rend.material = mat;
            }
        }

        canBurnOut = false;
        maxVoltage = 5f;
        maxCurrent = 0.02f;
        resistance = 220f;
    }

    public override void OnVoltageChanged(float voltage)
    {
        base.OnVoltageChanged(voltage);

        intensity = voltage > 2.0f ? 1f : 0f;

        if (mat != null)
        {
            mat.color = ledColor * (0.3f + intensity * 0.7f);
            mat.SetColor("_EmissionColor", ledColor * intensity);
        }
    }

    public override void UpdateComponent()
    {
        // Простая реализация - обновляем материал
        if (mat != null && rend != null)
        {
            rend.UpdateGIMaterials();
        }
    }
}