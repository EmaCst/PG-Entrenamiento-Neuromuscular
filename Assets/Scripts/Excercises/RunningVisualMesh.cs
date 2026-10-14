using UnityEngine;

// Visual-only meshes: no CreatePrimitive, physics components or collider stripping dependencies.
public static class RunningVisualMesh
{
    private static GameObject Render(string name, Material material, Mesh mesh)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }
    public static GameObject Disc(string name, float radius, Material material, out Mesh mesh)
    {
        const int sides = 32;
        var vertices = new Vector3[sides + 1]; var triangles = new int[sides * 3];
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % sides + 1; triangles[i * 3 + 2] = i + 1;
        }
        mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return Render(name, material, mesh);
    }
    public static GameObject Quad(string name, Material material, out Mesh mesh)
    {
        mesh = new Mesh { name = name + " mesh", vertices = new[] {
            new Vector3(-0.5f,-0.5f,0), new Vector3(-0.5f,0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(0.5f,-0.5f,0) },
            triangles = new[] { 0,1,2,0,2,3 } };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return Render(name, material, mesh);
    }
}
