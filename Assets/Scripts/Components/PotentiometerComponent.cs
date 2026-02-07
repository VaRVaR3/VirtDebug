using UnityEngine;

public class PotentiometerComponent : CircuitComponent
{
    public VirtualPotentiometer pot;

    protected override void Awake()
    {
        base.Awake(); // важно!

        if (pot == null)
            pot = GetComponent<VirtualPotentiometer>();
    }

    public override void Initialize(VirtualArduino arduino)
    {
        base.Initialize(arduino);
    }
}
