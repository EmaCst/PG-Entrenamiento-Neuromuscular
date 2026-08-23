using UnityEngine;

public enum RequiredHand
{
    Left,
    Right
}

public class AimLabHandController : MonoBehaviour
{
    public RequiredHand CurrentHand { get; private set; }

    public RequiredHand GenerateRandomHand()
    {
        CurrentHand = Random.value < 0.5f
            ? RequiredHand.Left
            : RequiredHand.Right;

        return CurrentHand;
    }

    public Color GetCurrentColor()
    {
        if (CurrentHand == RequiredHand.Left)
        {
            return Color.blue;
        }

        return Color.red;
    }

    public bool IsCorrectHand(RequiredHand usedHand)
    {
        return usedHand == CurrentHand;
    }
}