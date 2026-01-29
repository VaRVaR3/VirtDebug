using UnityEngine;

public class WireTest : MonoBehaviour
{
    public WireVisual wireVisual;

    [ContextMenu("“естировать обновление провода")]
    public void TestWireUpdate()
    {
        if (wireVisual != null)
        {
            Debug.Log("ѕринудительное обновление провода...");
            wireVisual.ForceUpdateWire();
        }
        else
        {
            Debug.LogError("WireVisual не назначен!");
        }
    }
}