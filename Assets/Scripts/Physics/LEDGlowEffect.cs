using UnityEngine;
using System.Collections;

public class LEDGlowEffect : MonoBehaviour
{
    public VirtualLED targetLED;
    public ParticleSystem glowParticles;
    public float maxParticleSize = 0.5f;
    public float maxEmissionRate = 50f;

    private Material particleMaterial;
    private Color originalColor;

    void Start()
    {
        if (targetLED == null)
            targetLED = GetComponentInParent<VirtualLED>();

        if (glowParticles == null)
        {
            // Создаем систему частиц если её нет
            CreateParticleSystem();
        }
        else
        {
            particleMaterial = glowParticles.GetComponent<ParticleSystemRenderer>().material;
            originalColor = targetLED.ledColor;
        }
    }

    void CreateParticleSystem()
    {
        GameObject particles = new GameObject("LED_Glow_Particles");
        particles.transform.SetParent(transform);
        particles.transform.localPosition = Vector3.zero;
        particles.transform.localScale = Vector3.one;

        glowParticles = particles.AddComponent<ParticleSystem>();

        // Настраиваем систему частиц
        var main = glowParticles.main;
        main.startSize = 0.1f;
        main.startSpeed = 0.5f;
        main.startLifetime = 1f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = glowParticles.emission;
        emission.rateOverTime = 0f;

        var shape = glowParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Создаем материал для частиц
        ParticleSystemRenderer psr = particles.GetComponent<ParticleSystemRenderer>();
        particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMaterial.color = targetLED.ledColor;
        psr.material = particleMaterial;

        glowParticles.Stop();
    }

    void Update()
    {
        if (targetLED == null || glowParticles == null) return;

        float intensity = targetLED.intensity;

        if (intensity > 0.1f)
        {
            // Обновляем цвет частиц
            Color particleColor = targetLED.ledColor * intensity;
            particleMaterial.color = particleColor;
            particleMaterial.SetColor("_EmissionColor", particleColor * 2f);

            // Обновляем параметры системы частиц
            var main = glowParticles.main;
            main.startSize = intensity * maxParticleSize;
            main.startColor = particleColor;

            var emission = glowParticles.emission;
            emission.rateOverTime = intensity * maxEmissionRate;

            if (!glowParticles.isPlaying)
                glowParticles.Play();
        }
        else
        {
            if (glowParticles.isPlaying)
                glowParticles.Stop();
        }
    }
}