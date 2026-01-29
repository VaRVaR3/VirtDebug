using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConnectionUI : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public Image statusIndicator;

    void Update()
    {
        if (ConnectionManager.Instance != null)
        {
            if (ConnectionManager.Instance.isConnecting)
            {
                statusText.text = "CONNECTING... Click on target pin";
                statusIndicator.color = Color.yellow;
            }
            else
            {
                statusText.text = "Ready to connect";
                statusIndicator.color = Color.green;
            }
        }
    }

    public void ShowMessage(string message, Color color)
    {
        statusText.text = message;
        statusIndicator.color = color;

        Invoke("ResetStatus", 2f);
    }

    void ResetStatus()
    {
        statusText.text = "Ready to connect";
        statusIndicator.color = Color.green;
    }
}