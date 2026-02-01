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
            c.line.transform.SetAsFirstSibling();
            c.line.gameObject.SetActive(false);
            c.line.color = lockedColor;
            c.unlocked = false;
        }
    }

    void LateUpdate()
    {
        foreach (var c in connections)
        {
            if (c.line == null || !c.line.gameObject.activeSelf) continue;
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fixedCanvas, screen, null, out Vector2 localPos);
        return localPos;
    }

    // Mostrar líneas en gris (cuando un padre se desbloquea)
    public void ShowFrom(RectTransform fromNode)
    {
        foreach (var c in connections)
        {
            if (c.from == fromNode)
            {
                c.line.gameObject.SetActive(true);
                // Si el nodo de destino ya está desbloqueado por otra rama, la ponemos verde
                SkillNode targetNode = c.to.GetComponent<SkillNode>();
                if (targetNode != null && targetNode.IsUnlocked)
                    c.line.color = unlockedColor;
                else
                    c.line.color = lockedColor;
            }
        }
    }

    // Activar el color verde (solo cuando el nodo destino se compra)
    public void Unlock(RectTransform fromNode, RectTransform toNode)
    {
        foreach (var c in connections)
        {
            // Buscamos todas las conexiones que lleguen a ese nodo recien comprado
            // porque si tiene dos padres, AMBAS líneas deben ponerse verdes
            if (c.to == toNode)
            {
                c.unlocked = true;
                c.line.gameObject.SetActive(true);
                c.line.color = unlockedColor;
            }
        }
    }
}