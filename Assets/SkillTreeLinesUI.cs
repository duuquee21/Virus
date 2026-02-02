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
        [HideInInspector] public bool unlocked;
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
            // Verificamos que los nodos existan antes de crear la línea
            if (c.from == null || c.to == null) continue;

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
            // --- PROTECCIÓN ANTIERRORES ---
            // Si la línea o los nodos han sido destruidos, saltamos esta conexión
            if (c == null || c.from == null || c.to == null || c.line == null)
                continue;

            // Solo dibujamos si ambos nodos están activos en la jerarquía
            if (!c.from.gameObject.activeInHierarchy || !c.to.gameObject.activeInHierarchy)
            {
                c.line.gameObject.SetActive(false);
                continue;
            }

            // Si llegamos aquí, todo es seguro
            c.line.gameObject.SetActive(true);
            PositionLine(c.line.rectTransform, c.from, c.to);

            // Color dinámico según el estado del nodo destino
            SkillNode targetNode = c.to.GetComponent<SkillNode>();
            if (targetNode != null && targetNode.IsUnlocked)
                c.line.color = unlockedColor;
            else
                c.line.color = lockedColor;
        }
    }

    void PositionLine(RectTransform line, RectTransform a, RectTransform b)
    {
        // Doble verificación de seguridad
        if (a == null || b == null || line == null) return;

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
        if (fixedCanvas == null) return Vector2.zero;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fixedCanvas, screen, null, out Vector2 localPos);
        return localPos;
    }

    public void ShowFrom(RectTransform fromNode) { /* El LateUpdate se encarga ahora */ }

    public void Unlock(RectTransform fromNode, RectTransform toNode)
    {
        foreach (var c in connections)
        {
            if (c != null && c.to == toNode && c.line != null)
            {
                c.unlocked = true;
                c.line.color = unlockedColor;
            }
        }
    }
}