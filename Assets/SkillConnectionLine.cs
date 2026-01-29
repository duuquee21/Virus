using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SkillConnectionLine : MonoBehaviour
{
    public RectTransform fromNode;
    public RectTransform toNode;

    LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = false;   // CLAVE para UI
    }

    void Update()
    {
        if (fromNode == null || toNode == null) return;

        Vector3 fromPos = fromNode.anchoredPosition;
        Vector3 toPos = toNode.anchoredPosition;

        line.SetPosition(0, fromPos);
        line.SetPosition(1, toPos);
    }
}
