using System;

public enum PinMode
{
    Input,
    Output,
    Input_Pullup,
    Analog,
    PWM,
    Ground,
    Power
}

public enum ComponentType
{
    LED,
    Resistor,
    Button,
    Potentiometer,
    LCD,
    Arduino
}

public enum ConnectionType
{
    Digital,
    Analog,
    Power,
    Ground
}

public enum LEDStatus
{
    NotConnected,
    PartialConnection,
    ReversePolarity,
    NoResistor,
    ShortCircuit,
    OK,
    Success,
    Warning,
    Error,
    Info
}

// ”ƒјЋ»“№ этот enum - он уже определен в VirtualButton.cs
// public enum ButtonStatus
// {
//     NotConnected,
//     Ready,
//     Pressed,
//     Error
// }