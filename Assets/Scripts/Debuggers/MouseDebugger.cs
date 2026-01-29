using UnityEngine;
using UnityEngine.EventSystems;

public class MouseDebugger : MonoBehaviour
{
    void OnMouseEnter()
    {
        Debug.Log($"Mouse ENTER: {name}");
    }

    void OnMouseExit()
    {
        Debug.Log($"Mouse EXIT: {name}");
    }

    void OnMouseDown()
    {
        Debug.Log($"Mouse DOWN: {name}");
    }

    void OnMouseUp()
    {
        Debug.Log($"Mouse UP: {name}");
    }

    void OnMouseDrag()
    {
        Debug.Log($"Mouse DRAG: {name}");
    }
}