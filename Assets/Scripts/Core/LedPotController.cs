using UnityEngine;

public class LedPotController : MonoBehaviour
{
    public VirtualArduino arduino;

    [Header("Pins")]
    public int analogPin = 300; // A0 = 300
    public int pwmPin = 9;      // PWM pin: 3,5,6,9,10,11

    [Header("Options")]
    public bool run = true;

    void Start()
    {
        if (arduino == null)
            arduino = FindObjectOfType<VirtualArduino>();

        if (arduino == null)
        {
            Debug.LogError("LedPotController: Arduino not found!");
            enabled = false;
            return;
        }

        arduino.SetPinMode(pwmPin, PinMode.Output);
        Debug.Log($"LedPotController: using A{analogPin - 300} -> PWM D{pwmPin}");
    }

    void Update()
    {
        if (!run || arduino == null) return;

        // 0..1023
        int adc = arduino.AnalogRead(analogPin);

        // 0..255
        float pwm = Mathf.Clamp(adc / 1023f, 0f, 1f) * 255f;

        arduino.AnalogWrite(pwmPin, pwm);
    }
}
