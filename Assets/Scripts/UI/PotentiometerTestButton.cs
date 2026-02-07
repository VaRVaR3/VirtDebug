using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PotentiometerTestButton : MonoBehaviour
{
    [Header("UI")]
    public Button btnStartStop;

    [Header("Which analog pin to display")]
    public int analogPin = 300; // A0 = 300, A1 = 301 ... A5 = 305

    private Coroutine routine;

    void Start()
    {
        if (btnStartStop != null)
            btnStartStop.onClick.AddListener(ToggleTest);
    }

    public void ToggleTest()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
            ArduinoSimulator.Instance?.SerialPrint("[STOP] Pot test stopped"); 
            return;
        }
        

        routine = StartCoroutine(TestLoop());
    }

    private IEnumerator TestLoop()
    {

        ArduinoSimulator.Instance?.SerialPrint("[OK] Pot test started (prints A0 voltage/value)");

        while (true)
        {
            var sim = ArduinoSimulator.Instance;
            var arduino = sim != null ? sim.currentArduino : null;

            if (arduino == null)
            {
                sim?.SerialPrint("❌ No Arduino placed");
            }
            else
            {
                // Voltage:
                float v = arduino.GetAnalogVoltage(analogPin);
                int adc = Mathf.RoundToInt(v * 1023f / 5f);
                sim?.SerialPrint($"A{analogPin - 300}: {v:0.00}V  (ADC {adc})");
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}
