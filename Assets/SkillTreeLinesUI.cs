using UnityEngine;
using UnityEngine.UI;

public class SkillTreeLinesUI : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        public RectTransform from;
        public RectTransform to;
        public Image line;
        public bool unlocked;
    }

    public RectTransform fixedCanvas;
    public Image linePrefab;

    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.green;

    public Connection[] connections;

    void Start()
    {
        foreach (var c in connections)
        {
            c.line = Instantiate(linePrefab, fixedCanvas);
            c.line.transform.SetAsFirstSibling();   // detrás de todo
            c.line.gameObject.SetActive(false);
            c.line.color = lockedColor;
            c.unlocked = false;
        }
    }


    void LateUpdate()
    {
        foreach (var c in connections)
        {
            if (!c.line.gameObject.activeSelf) continue;
            PositionLine(c.line.rectTransform, c.from, c.to);
        }
    }

    void PositionLine(RectTransform line, RectTransform a, RectTransform b)
    {
        Vector2 A = WorldToUI(a.position);
        Vector2 B = WorldToUI(b.position);

        Vector2 dir = B - A;
        float dist = dir.magnitude;

        line.sizeDelta = new Vector2(dist, 4f);
        line.anchoredPosition = A + dir * 0.5f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.rotation = Quaternion.Euler(0, 0, angle);
    }

    Vector2 WorldToUI(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fixedCanvas,
            screen,
            null,
            out Vector2 localPos
        );

        return localPos;
    }

    // Mostrar ramas posibles en gris
    public void ShowFrom(RectTransform fromNode)
    {
        foreach (var c in connections)
        {
            if (c.from == fromNode)
            {
                c.line.gameObject.SetActive(true);
                c.line.color = lockedColor;
            }
        }
    }

    // Marcar rama comprada en verde
    public void Unlock(RectTransform fromNode, RectTransform toNode)
    {
        foreach (var c in connections)
        {
            if (c.from == fromNode && c.to == toNode)
            {
                c.unlocked = true;
                c.line.gameObject.SetActive(true);
                c.line.color = unlockedColor;
                return;
            }
        }
    }
}
