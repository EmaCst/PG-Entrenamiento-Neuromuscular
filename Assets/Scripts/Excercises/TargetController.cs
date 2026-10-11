using UnityEngine;

public class TargetController : MonoBehaviour
{
    private AimLabManager manager;

    public bool IsActiveTarget => manager != null && manager.IsCurrentTarget(gameObject);

    public void SetManager(AimLabManager aimLabManager)
    {
        manager = aimLabManager;
    }

    public void Touch(RequiredHand usedHand)
    {
        if (manager == null)
        {
            Debug.LogError(
                "Target sin AimLabManager asignado."
            );

            return;
        }

        manager.TargetTouched(
            gameObject,
            usedHand
        );
    }
}
