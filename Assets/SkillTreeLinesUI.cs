using UnityEngine;
using UnityEngine.UI;

public class SkillTreeLinesUI : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        public RectTransform from;
        public RectTransform to;
        public Image lineInstance;
    }

    public RectTransform canvasRoot;
    public Image linePrefab;

    [Header("Connections")]
    public Connection[] connections;

    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.green;

    public void DrawConnections()
    {
        foreach (var c in connections)
        {
            if (!c.from || !c.to) continue;

            if (c.lineInstance == null)
            {
                c.lineInstance = Instantiate(linePrefab, canvasRoot);
                c.lineInstance.name = "Line";
                c.lineInstance.transform.SetAsFirstSibling();
            }

            PositionLine(c.lineInstance.rectTransform, c.from, c.to);

            bool active = c.from.gameObject.activeSelf && c.to.gameObject.activeSelf;
            c.lineInstance.gameObject.SetActive(active);

            if (active)
                c.lineInstance.color = lockedColor;
        }
    }

    public void UnlockLine(RectTransform unlockedNode)
    {
        foreach (var c in connections)
        {
            if (c.to == unlockedNode && c.lineInstance)
                c.lineInstance.color = unlockedColor;
        }
    }

    void PositionLine(RectTransform line, RectTransform a, RectTransform b)
    {
        Vector2 posA = GetLocalPosition(a);
        Vector2 posB = GetLocalPosition(b);

        Vector2 dir = posB - posA;
        float distance = dir.magnitude;

        line.sizeDelta = new Vector2(distance, 4);
        line.localPosition = posA + dir / 2f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    Vector2 GetLocalPosition(RectTransform target)
    {
        Vector3 worldPos = target.position;
        Vector3 localPos = canvasRoot.InverseTransformPoint(worldPos);
        return localPos;
    }
}
