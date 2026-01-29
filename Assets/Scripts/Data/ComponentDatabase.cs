using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComponentDatabase", menuName = "Arduino Simulator/Component Database")]
public class ComponentDatabase : ScriptableObject
{
    [System.Serializable]
    public class ComponentInfo
    {
        public string componentName;
        public GameObject prefab;
        public float defaultResistance;
        public float maxVoltage;
        public float maxCurrent;
        public bool requiresPolarity;
    }

    public List<ComponentInfo> components = new List<ComponentInfo>();

    public ComponentInfo GetComponentInfo(string componentName)
    {
        return components.Find(c => c.componentName == componentName);
    }

    public GameObject GetPrefab(string componentName)
    {
        ComponentInfo info = GetComponentInfo(componentName);
        return info?.prefab;
    }
}