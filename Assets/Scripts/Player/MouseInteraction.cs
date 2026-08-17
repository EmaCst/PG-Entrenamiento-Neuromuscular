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
            Debug.LogError("MouseInteraction debe estar agregado a una Camera.");
        }
    }

    void Update()
    {
        // Si no hay mouse conectado
        if (Mouse.current == null)
        {
            return;
        }

        // Detectar clic izquierdo
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Ray ray = cam.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Raycast golpeó: " + hit.collider.gameObject.name);

                TargetController target =
                    hit.collider.GetComponent<TargetController>();

                if (target != null)
                {
                    target.Touch();
                }
            }
            else
            {
                Debug.Log("El raycast no golpeó ningún objeto.");
            }
        }
    }
}