using UnityEngine;

public class AimLabManager : MonoBehaviour
{
    public AimLabGenerator generator;

    private GameObject currentTarget;
    private int score = 0;

    void Start()
    {
        if (generator == null)
        {
            Debug.LogError("AimLabManager: Generator no está asignado.");
            return;
        }

        generator.GenerateGrid();

        foreach (GameObject target in generator.targets)
        {
            TargetController controller = target.GetComponent<TargetController>();

            if (controller != null)
            {
                controller.SetManager(this);
            }
        }

        ActivateRandomTarget();
    }

    void ActivateRandomTarget()
    {
        if (generator.targets.Count == 0)
        {
            Debug.LogError("AimLabManager: No hay targets generados.");
            return;
        }

        foreach (GameObject target in generator.targets)
        {
            Renderer renderer = target.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }
        }

        int index = Random.Range(0, generator.targets.Count);

        currentTarget = generator.targets[index];

        Renderer currentRenderer = currentTarget.GetComponent<Renderer>();

        if (currentRenderer != null)
        {
            currentRenderer.material.color = Color.green;
        }
    }

    public void TargetTouched(GameObject touchedTarget)
    {
        if (touchedTarget != currentTarget)
        {
            return;
        }

        score++;

        Debug.Log("Puntos: " + score);

        ActivateRandomTarget();
    }
}