using UnityEngine;
using System.Collections.Generic;

public class AimLabGenerator : MonoBehaviour
{
    public GameObject targetPrefab;

    public int rows = 3;
    public int columns = 3;
    public float spacing = 1.5f;

  
    public List<GameObject> targets { get; private set; }
        = new List<GameObject>();

    public void GenerateGrid()
    {

        targets.Clear();

        float offsetX = (columns - 1) * spacing / 2f;
        float offsetY = (rows - 1) * spacing / 2f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPosition = new Vector3(
                    col * spacing - offsetX,
                    row * spacing - offsetY,
                    0f
                );

                GameObject target = Instantiate(
                    targetPrefab,
                    transform
                );

                target.transform.localPosition = localPosition;
                target.transform.localRotation = Quaternion.identity;

                targets.Add(target);
            }
        }

        Debug.Log("Targets generados: " + targets.Count);
    }
}