using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RadiusLineRenderer : MonoBehaviour
{
    public int segments = 60;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        DrawCircle(1f);
    }

    public void DrawCircle(float radius)
    {
        float angle = 0f;

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0));
            angle += 2 * Mathf.PI / segments;
        }
    }
}
