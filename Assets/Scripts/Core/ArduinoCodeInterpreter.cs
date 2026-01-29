using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ArduinoCodeInterpreter
{
    private VirtualArduino arduino;
    private Dictionary<string, int> variables = new Dictionary<string, int>();
    private List<string> loopCommands = new List<string>();

    public void ExecuteCode(string code, VirtualArduino targetArduino)
    {
        this.arduino = targetArduino;
        variables.Clear();
        arduino.variables.Clear();
        loopCommands.Clear();

        string[] lines = PreprocessCode(code);
        ParseSections(lines);
        ExecuteSetup();
        StartLoopExecution();
    }

    private string[] PreprocessCode(string code)
    {
        // Удаление многострочных комментариев
        code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);

        string[] lines = code.Split('\n');
        List<string> cleanedLines = new List<string>();

        foreach (string line in lines)
        {
            string cleaned = line;
            int commentIndex = cleaned.IndexOf("//");
            if (commentIndex >= 0)
                cleaned = cleaned.Substring(0, commentIndex);

            cleaned = cleaned.Trim();

            if (!string.IsNullOrEmpty(cleaned))
                cleanedLines.Add(cleaned);
        }

        return cleanedLines.ToArray();
    }

    private void ParseSections(string[] lines)
    {
        bool inSetupSection = false;
        bool inLoopSection = false;

        foreach (string line in lines)
        {
            if (line.Contains("void setup()"))
            {
                inSetupSection = true;
                inLoopSection = false;
                continue;
            }
            else if (line.Contains("void loop()"))
            {
                inSetupSection = false;
                inLoopSection = true;
                continue;
            }
            else if (line.Contains("{"))
            {
                continue;
            }
            else if (line.Contains("}"))
            {
                inSetupSection = false;
                inLoopSection = false;
                continue;
            }

            if (inSetupSection)
            {
                ExecuteLine(line);
            }
            else if (inLoopSection)
            {
                if (!string.IsNullOrEmpty(line))
                    loopCommands.Add(line);
            }
            else
            {
                ExecuteLine(line);
            }
        }
    }

    private void ExecuteSetup()
    {
        Debug.Log("Executing setup...");
        ArduinoSimulator.Instance.SerialPrint("Setup executed");
    }

    private void StartLoopExecution()
    {
        ArduinoSimulator.Instance.StartCoroutine(ExecuteLoopCoroutine());
    }

    private System.Collections.IEnumerator ExecuteLoopCoroutine()
    {
        while (true)
        {
            foreach (string command in loopCommands)
            {
                ExecuteLine(command);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ExecuteLine(string line)
    {
        try
        {
            if (line.Contains("pinMode"))
            {
                HandlePinMode(line);
                return;
            }

            if (line.Contains("digitalWrite"))
            {
                HandleDigitalWrite(line);
                return;
            }

            if (line.Contains("digitalRead"))
            {
                HandleDigitalRead(line);
                return;
            }

            if (line.Contains("analogWrite"))
            {
                HandleAnalogWrite(line);
                return;
            }

            if (line.Contains("Serial.println") || line.Contains("Serial.print"))
            {
                HandleSerialPrint(line);
                return;
            }

            if (line.Contains("delay"))
            {
                HandleDelay(line);
                return;
            }

            if (line.StartsWith("if"))
            {
                HandleIfStatement(line);
                return;
            }

            if (line.Contains("int ") || line.Contains("float ") ||
                line.Contains("bool ") || line.Contains("byte "))
            {
                HandleVariableDeclaration(line);
                return;
            }

            if (line.Contains("=") && !line.Contains("=="))
            {
                HandleAssignment(line);
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing line: {line}\n{e.Message}");
            ArduinoSimulator.Instance.SerialPrint($"ERROR: {e.Message} in line: {line}");
        }
    }

    private void HandlePinMode(string line)
    {
        var match = Regex.Match(line, @"pinMode\s*\(\s*(\d+)\s*,\s*(INPUT|OUTPUT|INPUT_PULLUP)\s*\)");
        if (match.Success)
        {
            int pin = int.Parse(match.Groups[1].Value);
            string modeStr = match.Groups[2].Value;

            PinMode mode;
            if (modeStr == "OUTPUT")
                mode = PinMode.Output;
            else if (modeStr == "INPUT_PULLUP")
                mode = PinMode.Input_Pullup;
            else
                mode = PinMode.Input;

            arduino.SetPinMode(pin, mode);
            ArduinoSimulator.Instance.SerialPrint($"pinMode({pin}, {modeStr})");
        }
    }

    private void HandleDigitalWrite(string line)
    {
        var match = Regex.Match(line, @"digitalWrite\s*\(\s*(\d+)\s*,\s*(HIGH|LOW)\s*\)");
        if (match.Success)
        {
            int pin = int.Parse(match.Groups[1].Value);
            string valueStr = match.Groups[2].Value;
            int value = (valueStr == "HIGH") ? 1 : 0;

            if (arduino.DigitalWrite(pin, value))
                ArduinoSimulator.Instance.SerialPrint($"digitalWrite({pin}, {valueStr})");
            else
                ArduinoSimulator.Instance.SerialPrint($"ERROR: Failed to write to pin {pin}");
        }
    }

    private void HandleSerialPrint(string line)
    {
        var match = Regex.Match(line, @"Serial\.(?:print|println)\s*\(\s*""([^""]+)""\s*\)");
        if (match.Success)
        {
            string content = match.Groups[1].Value;
            ArduinoSimulator.Instance.SerialPrint(content);
        }
        else
        {
            match = Regex.Match(line, @"Serial\.(?:print|println)\s*\(\s*(\w+)\s*\)");
            if (match.Success)
            {
                string varName = match.Groups[1].Value;
                if (variables.ContainsKey(varName))
                    ArduinoSimulator.Instance.SerialPrint(variables[varName].ToString());
            }
        }
    }

    private void HandleDelay(string line)
    {
        var match = Regex.Match(line, @"delay\s*\(\s*(\d+)\s*\)");
        if (match.Success)
        {
            int milliseconds = int.Parse(match.Groups[1].Value);
            ArduinoSimulator.Instance.StartCoroutine(DelayCoroutine(milliseconds / 1000f));
        }
    }

    private System.Collections.IEnumerator DelayCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private void HandleVariableDeclaration(string line)
    {
        var match = Regex.Match(line, @"(int|float|bool|byte)\s+(\w+)\s*(?:=\s*([^;]+))?;");
        if (match.Success)
        {
            string varName = match.Groups[2].Value;
            if (match.Groups[3].Success)
            {
                string valueStr = match.Groups[3].Value.Trim();

                if (int.TryParse(valueStr, out int intValue))
                    variables[varName] = intValue;
                else if (valueStr == "HIGH")
                    variables[varName] = 1;
                else if (valueStr == "LOW")
                    variables[varName] = 0;
                else
                    variables[varName] = 0;

                ArduinoSimulator.Instance.SerialPrint($"Variable declared: {varName} = {variables[varName]}");
            }
            else
            {
                variables[varName] = 0;
            }
        }
    }

    private void HandleIfStatement(string line)
    {
        var match = Regex.Match(line, @"if\s*\(\s*digitalRead\s*\(\s*(\d+)\s*\)\s*==\s*(HIGH|LOW)\s*\)");
        if (match.Success)
        {
            int pin = int.Parse(match.Groups[1].Value);
            string expectedState = match.Groups[2].Value;
            int expectedValue = (expectedState == "HIGH") ? 1 : 0;

            int actualValue = arduino.DigitalRead(pin);

            if (actualValue != expectedValue)
            {
                // В упрощенной версии просто пропускаем
            }
        }
    }

    private void HandleAssignment(string line)
    {
        var match = Regex.Match(line, @"(\w+)\s*=\s*([^;]+);");
        if (match.Success)
        {
            string varName = match.Groups[1].Value;
            string valueStr = match.Groups[2].Value.Trim();

            if (valueStr == "HIGH")
                variables[varName] = 1;
            else if (valueStr == "LOW")
                variables[varName] = 0;
            else if (valueStr == "!digitalRead")
            {
                var pinMatch = Regex.Match(line, @"digitalRead\s*\(\s*(\d+)\s*\)");
                if (pinMatch.Success)
                {
                    int pin = int.Parse(pinMatch.Groups[1].Value);
                    variables[varName] = (arduino.DigitalRead(pin) == 1) ? 0 : 1;
                }
            }
            else if (int.TryParse(valueStr, out int intValue))
                variables[varName] = intValue;
        }
    }

    private void HandleDigitalRead(string line)
    {
        var match = Regex.Match(line, @"digitalRead\s*\(\s*(\d+)\s*\)");
        if (match.Success)
        {
            int pin = int.Parse(match.Groups[1].Value);
            int value = arduino.DigitalRead(pin);
            ArduinoSimulator.Instance.SerialPrint($"digitalRead({pin}) = {value}");
        }
    }

    private void HandleAnalogWrite(string line)
    {
        var match = Regex.Match(line, @"analogWrite\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)");
        if (match.Success)
        {
            int pin = int.Parse(match.Groups[1].Value);
            int value = int.Parse(match.Groups[2].Value);

            if (arduino.AnalogWrite(pin, value))
                ArduinoSimulator.Instance.SerialPrint($"analogWrite({pin}, {value})");
        }
    }
}

// УДАЛИТЕ эти строки - enum уже определен в отдельном файле
// public enum PinMode { Input, Output, Input_Pullup }
// public enum ComponentType { LED, Resistor, Button, Potentiometer, LCD, Arduino }
// public enum ConnectionType { Digital, Analog, Power, Ground }