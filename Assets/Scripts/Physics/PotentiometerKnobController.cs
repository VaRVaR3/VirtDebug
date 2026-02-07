using UnityEngine;

public class PotentiometerKnobController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ручка/цилиндр, которую вращаем. Если пусто — попробует найти дочерний объект с именем 'Cylinder' или 'Knob'.")]
    public Transform knob;

    [Tooltip("Если не задано — возьмёт VirtualPotentiometer с этого же объекта.")]
    public VirtualPotentiometer pot;

    [Header("Rotation")]
    public float maxRotation = 270f;
    public bool invert = false;

    void Awake()
    {
        if (pot == null)
            pot = GetComponent<VirtualPotentiometer>();

        if (knob == null)
        {
            // авто-поиск ручки
            var t = transform.Find("Cylinder");
            if (t == null) t = transform.Find("Knob");
            if (t != null) knob = t;
        }
    }

    void LateUpdate()
    {
        if (pot == null || knob == null) return;

        float v = Mathf.Clamp01(pot.value01);
        if (invert) v = 1f - v;

        float angle = Mathf.Lerp(0f, maxRotation, v);
        knob.localRotation = Quaternion.Euler(0f, angle, 0f);
    }
}
