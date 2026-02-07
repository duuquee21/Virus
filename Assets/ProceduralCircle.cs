using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralCircle : MonoBehaviour
{
    [Range(3, 1000)]
    public int segments = 128; // Recomendado para Jelly suave
    public float radius = 1f;

    void Start()
    {
        GenerateCircle();
    }

    // Usamos OnValidate para que veas los cambios en tiempo real en el Editor
    void OnValidate()
    {
        if (GetComponent<MeshFilter>().sharedMesh != null) GenerateCircle();
    }

    void GenerateCircle()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCircle";

        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1]; // NECESARIO PARA TU SHADER
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f); // Centro de la textura

        float angleStep = (2f * Mathf.PI) / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            vertices[i + 1] = new Vector3(x * radius, y * radius, 0f);

            // Mapeo de UVs de 0 a 1 basado en la posición del círculo
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);

            int startIdx = i * 3;
            triangles[startIdx] = 0;
            triangles[startIdx + 1] = i + 1;
            triangles[startIdx + 2] = (i == segments - 1) ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs; // Asignamos las coordenadas de textura
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}