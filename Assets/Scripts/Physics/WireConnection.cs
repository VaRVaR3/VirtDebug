using UnityEngine;

[System.Serializable]
public class WireConnection
{
    public int sourcePin;
    public CircuitComponent sourceComponent;
    public int targetPin;
    public CircuitComponent targetComponent;
    public GameObject wireVisual;
}