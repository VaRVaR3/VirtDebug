using UnityEngine;
using System.Collections;

public class LedPotController : MonoBehaviour
{
    [Header("Auto (если Arduino спавнится кнопкой)")]
    public bool autoFindArduino = true;

    public VirtualArduino arduino;

    [Header("Input (pot wiper)")]
    public int analogPin = 300;   // A0=300, A1=301 ... A5=305

    [Header("Output (LED anode on PWM pin)")]
    public int pwmPin = 9;        // 3/5/6/9/10/11 (на Uno)

    [Header("Debug")]
    public bool logToConsole = false;

    private bool ready;

    private IEnumerator Start()
    {
        if (autoFindArduino && arduino == null)
        {
            // ждём пока Arduino реально появится в сцене (если спавнится UI-кнопкой)
            while (arduino == null)
            {
                // если у тебя есть ArduinoSimulator.currentArduino — лучше так:
                if (ArduinoSimulator.Instance != null && ArduinoSimulator.Instance.currentArduino != null)
                    arduino = ArduinoSimulator.Instance.currentArduino;
                else
                    arduino = FindObjectOfType<VirtualArduino>();

                yield return null;
            }
        }

        if (arduino == null)
        {
            Debug.LogError("LedPotController: Arduino not found");
            yield break;
        }

        // Подготовим PWM pin
        arduino.SetPinMode(pwmPin, PinMode.Output);
        ready = true;

        Debug.Log($"LedPotController READY: analogPin={analogPin}, pwmPin={pwmPin}");
    }

    private void Update()
    {
        if (!ready || arduino == null) return;

        // 1) читаем аналог (0..1023)
        int adc = arduino.AnalogRead(analogPin);

        // 2) переводим в PWM (0..255)
        int pwm = Mathf.Clamp(Mathf.RoundToInt(adc / 4f), 0, 255);

        // 3) пишем PWM на выход
        arduino.AnalogWrite(pwmPin, pwm);

        if (logToConsole && Time.frameCount % 15 == 0)
        {
            float v = adc * 5f / 1023f;
            Debug.Log($"Pot A{analogPin - 300}: {v:0.00}V (ADC {adc}) -> PWM {pwm} on D{pwmPin}");
        }
    }
}
