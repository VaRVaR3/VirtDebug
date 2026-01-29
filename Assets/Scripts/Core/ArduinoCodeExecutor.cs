using System.Collections;
using UnityEngine;

public class ArduinoCodeExecutor : MonoBehaviour
{
    [Header("Code Execution")]
    public string arduinoCode = @"
void setup() {
  pinMode(13, OUTPUT);
}

void loop() {
  digitalWrite(13, HIGH);
  delay(1000);
  digitalWrite(13, LOW);
  delay(1000);
}";

    public bool autoRun = false;
    public float delayBetweenCommands = 0.5f;

    private VirtualArduino arduino;

    void Start()
    {
        arduino = FindObjectOfType<VirtualArduino>();

        if (autoRun && arduino != null)
        {
            StartCoroutine(ExecuteCode());
        }
    }

    IEnumerator ExecuteCode()
    {
        Debug.Log("Starting Arduino code execution...");

        // Пример простой программы
        // pinMode
        arduino.SetPinMode(13, PinMode.Output);
        yield return new WaitForSeconds(delayBetweenCommands);

        // Бесконечный цикл
        while (true)
        {
            // digitalWrite HIGH
            arduino.DigitalWrite(13, 1);
            ArduinoSimulator.Instance.SerialPrint("LED ON");
            yield return new WaitForSeconds(1f);

            // digitalWrite LOW
            arduino.DigitalWrite(13, 0);
            ArduinoSimulator.Instance.SerialPrint("LED OFF");
            yield return new WaitForSeconds(1f);
        }
    }

    public void RunCustomCode(string code)
    {
        if (arduino == null)
        {
            Debug.LogError("Arduino not found!");
            return;
        }

        StartCoroutine(ParseAndExecute(code));
    }

    IEnumerator ParseAndExecute(string code)
    {
        string[] lines = code.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("pinMode"))
            {
                // Извлекаем параметры: pinMode(13, OUTPUT)
                int start = trimmedLine.IndexOf('(') + 1;
                int end = trimmedLine.IndexOf(')');
                string paramsStr = trimmedLine.Substring(start, end - start);
                string[] parameters = paramsStr.Split(',');

                if (parameters.Length == 2)
                {
                    int pin = int.Parse(parameters[0].Trim());
                    string modeStr = parameters[1].Trim().ToUpper();

                    PinMode mode = modeStr == "OUTPUT" ? PinMode.Output : PinMode.Input;
                    arduino.SetPinMode(pin, mode);

                    Debug.Log($"pinMode({pin}, {modeStr})");
                }
            }
            else if (trimmedLine.StartsWith("digitalWrite"))
            {
                // Извлекаем параметры: digitalWrite(13, HIGH)
                int start = trimmedLine.IndexOf('(') + 1;
                int end = trimmedLine.IndexOf(')');
                string paramsStr = trimmedLine.Substring(start, end - start);
                string[] parameters = paramsStr.Split(',');

                if (parameters.Length == 2)
                {
                    int pin = int.Parse(parameters[0].Trim());
                    string valueStr = parameters[1].Trim().ToUpper();
                    int value = valueStr == "HIGH" ? 1 : 0;

                    arduino.DigitalWrite(pin, value);
                    Debug.Log($"digitalWrite({pin}, {valueStr})");
                }
            }
            else if (trimmedLine.StartsWith("delay"))
            {
                // Извлекаем параметры: delay(1000)
                int start = trimmedLine.IndexOf('(') + 1;
                int end = trimmedLine.IndexOf(')');
                string param = trimmedLine.Substring(start, end - start);
                int milliseconds = int.Parse(param.Trim());

                yield return new WaitForSeconds(milliseconds / 1000f);
                Debug.Log($"delay({milliseconds})");
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}