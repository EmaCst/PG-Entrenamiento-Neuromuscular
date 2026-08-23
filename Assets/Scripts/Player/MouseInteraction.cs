using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInteraction : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError(
                "MouseInteraction debe estar agregado a una Camera."
            );
        }
    }

    void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        // CLICK IZQUIERDO = MANO IZQUIERDA
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryInteract(RequiredHand.Left);
        }

        // CLICK DERECHO = MANO DERECHA
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryInteract(RequiredHand.Right);
        }
    }

    void TryInteract(RequiredHand usedHand)
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            cam.ScreenPointToRay(mousePosition);

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit
            )
        )
        {
            TargetController target =
                hit.collider.GetComponent<TargetController>();

            if (target != null)
            {
                Debug.Log(
                    "Target tocado con: " +
                    usedHand
                );

                target.Touch(usedHand);
            }
        }
    }
}