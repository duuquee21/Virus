using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SkillTreeLinesUI : MonoBehaviour
{
    public enum LineState { Hidden, Discovering, Locked, Unlocking, Unlocked }

    [System.Serializable]
    public class Connection
    {
        public RectTransform from;
        public RectTransform to;

        [HideInInspector] public Image lineBackground;
        [HideInInspector] public Image lineForeground;
        [HideInInspector] public LineState state = LineState.Hidden;
    }

    [Header("Configuración de Líneas")]
    public RectTransform fixedCanvas;
    public Image linePrefab;
    public float lineThickness = 6f;

    [Header("Animación")]
    public float discoveryDuration = 0.5f;
    public float unlockDuration = 0.8f;

    [Header("Configuración de Color")]
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color unlockedColor = Color.white;

    [Header("Offsets Globales")]
    public float globalOffsetSalida = 20f;
    public float globalOffsetLlegada = 20f;
    public Vector2 globalOffsetPosicion;

    public Connection[] connections;
    private Dictionary<RectTransform, List<Connection>> connectionsByFrom = new Dictionary<RectTransform, List<Connection>>();
    private Dictionary<RectTransform, List<Connection>> connectionsByTo = new Dictionary<RectTransform, List<Connection>>();

    void Start()
    {
        foreach (var c in connections)
        {
            if (c.from == null || c.to == null) continue;

            c.lineBackground = CreateLineImage($"BG_{c.from.name}_{c.to.name}", lockedColor);
            c.lineForeground = CreateLineImage($"FG_{c.from.name}_{c.to.name}", unlockedColor);

            c.lineBackground.transform.SetAsFirstSibling();
            c.lineForeground.transform.SetSiblingIndex(c.lineBackground.transform.GetSiblingIndex() + 1);

            if (!connectionsByFrom.ContainsKey(c.from)) connectionsByFrom[c.from] = new List<Connection>();
            connectionsByFrom[c.from].Add(c);

            if (!connectionsByTo.ContainsKey(c.to)) connectionsByTo[c.to] = new List<Connection>();
            connectionsByTo[c.to].Add(c);

            // IMPORTANTE: Si el nodo de origen ya está desbloqueado por el guardado, 
            // mostramos la línea gris inmediatamente sin esperar.
            SkillNode fromNode = c.from.GetComponent<SkillNode>();
            if (fromNode != null && fromNode.IsUnlocked)
            {
                // Si el de destino también está desbloqueado, la línea nace blanca.
                SkillNode toNode = c.to.GetComponent<SkillNode>();
                if (toNode != null && toNode.IsUnlocked)
                {
                    c.lineBackground.gameObject.SetActive(true);
                    c.lineBackground.fillAmount = 1;
                    c.lineForeground.gameObject.SetActive(true);
                    c.lineForeground.fillAmount = 1;
                    c.state = LineState.Unlocked;
                }
                else
                {
                    // Si solo el origen está desbloqueado, la línea nace gris.
                    c.lineBackground.gameObject.SetActive(true);
                    c.lineBackground.fillAmount = 1;
                    c.state = LineState.Locked;
                }
            }
        }
    }

    Image CreateLineImage(string name, Color color)
    {
        Image img = Instantiate(linePrefab, fixedCanvas);
        img.name = name;
        img.color = color;
        img.fillAmount = 0;
        img.gameObject.SetActive(false);
        img.rectTransform.pivot = new Vector2(0, 0.5f);
        return img;
    }

    void LateUpdate()
    {
        foreach (var c in connections)
        {
            if (c == null || c.lineBackground == null) continue;

            // CAMBIO CRÍTICO: Una línea debe ser visible si su origen está activo 
            // Y si no está en estado Hidden.
            bool shouldBeVisible = c.from.gameObject.activeInHierarchy && c.state != LineState.Hidden;

            if (shouldBeVisible)
            {
                c.lineBackground.gameObject.SetActive(true);

                // El foreground solo si está en proceso de desbloqueo o ya desbloqueado
                bool showForeground = (c.state == LineState.Unlocking || c.state == LineState.Unlocked);
                c.lineForeground.gameObject.SetActive(showForeground);

                // Actualizar posición siempre que sea visible
                PositionLine(c.lineBackground.rectTransform, c.from, c.to);
                PositionLine(c.lineForeground.rectTransform, c.from, c.to);
            }
            else
            {
                c.lineBackground.gameObject.SetActive(false);
                c.lineForeground.gameObject.SetActive(false);
            }

            // Lógica de disparo de desbloqueo
            SkillNode targetNode = c.to.GetComponent<SkillNode>();
            if (targetNode != null && targetNode.IsUnlocked && c.state == LineState.Locked)
            {
                StartCoroutine(AnimateUnlock(c));
            }
        }
    }
    public void ShowFrom(RectTransform fromNode)
    {
        if (connectionsByFrom.TryGetValue(fromNode, out List<Connection> outgoingLines))
        {
            foreach (var line in outgoingLines)
            {
                if (line.state == LineState.Hidden)
                {
                    StartCoroutine(WaitAndDiscover(line));
                }
            }
        }
    }

    private IEnumerator WaitAndDiscover(Connection currentLine)
    {
        currentLine.state = LineState.Discovering;

        if (connectionsByTo.TryGetValue(currentLine.from, out List<Connection> parentLines))
        {
            bool waiting = true;
            while (waiting)
            {
                waiting = false;
                foreach (var pL in parentLines)
                {
                    // Solo esperamos si la línea padre está en proceso de llenarse.
                    // Si ya está Unlocked, pasamos.
                    if (pL.state != LineState.Unlocked)
                    {
                        waiting = true;
                        break;
                    }
                }
                if (waiting) yield return null;
            }
        }

        currentLine.lineBackground.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < discoveryDuration)
        {
            elapsed += Time.deltaTime;
            currentLine.lineBackground.fillAmount = Mathf.Lerp(0, 1, elapsed / discoveryDuration);
            yield return null;
        }
        currentLine.lineBackground.fillAmount = 1;
        currentLine.state = LineState.Locked;
    }

    private IEnumerator AnimateUnlock(Connection c)
    {
        c.state = LineState.Unlocking;
        c.lineForeground.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < unlockDuration)
        {
            elapsed += Time.deltaTime;
            c.lineForeground.fillAmount = Mathf.Lerp(0, 1, elapsed / unlockDuration);
            yield return null;
        }

        c.lineForeground.fillAmount = 1;
        c.state = LineState.Unlocked;

        // Al terminar, avisamos a los hijos de que esta línea ya terminó
        ShowFrom(c.to);
    }

    void PositionLine(RectTransform lineRT, RectTransform a, RectTransform b)
    {
        Vector2 A = WorldToUI(a.position);
        Vector2 B = WorldToUI(b.position);
        Vector2 dir = B - A;
        float dist = dir.magnitude;
        Vector2 startPos = A + (dir.normalized * globalOffsetSalida) + globalOffsetPosicion;
        float width = Mathf.Max(0, dist - globalOffsetSalida - globalOffsetLlegada);

        lineRT.anchoredPosition = startPos;
        lineRT.sizeDelta = new Vector2(width, lineThickness);
        lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    Vector2 WorldToUI(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fixedCanvas, screen, null, out Vector2 localPos);
        return localPos;
    }
}