using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RadiusLineRenderer : MonoBehaviour
{
    public int segments = 60;
    private LineRenderer line;

    void Awake()
    {
        SetupLine();
    }

    void SetupLine()
    {
        if (line != null) return;

        line = GetComponent<LineRenderer>();
        line.positionCount = segments + 1;

        // IMPORTANTE: Esto hace que el círculo sea relativo al Virus
        line.useWorldSpace = false;

        // Opcional: Para que los bordes sean suaves
        line.loop = true;
    }

    public void DrawCircle(float radius)
    {
        if (line == null) SetupLine();

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0));
            angle += (2 * Mathf.PI) / segments;
        }
    }
}