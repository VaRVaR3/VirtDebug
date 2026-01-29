using UnityEngine;

public interface IConnectable
{
    // Метод GetName для получения имени
    string GetName();

    // Методы для работы с соединениями
    void OnConnected(int pin, IConnectable otherComponent, int otherPin);
    void OnDisconnected(int pin);
    Vector3 GetPinPosition(int pin);
    bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin);
}

// Класс-расширение для MonoBehaviour для реализации IConnectable
public static class IConnectableExtensions
{
    public static string GetName(this IConnectable connectable)
    {
        if (connectable is MonoBehaviour mono)
        {
            return mono.name;
        }
        return "Unknown";
    }
}