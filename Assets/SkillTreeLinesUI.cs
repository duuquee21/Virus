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
        public Image lineBackground;
        public Image lineForeground;
        public LineState state = LineState.Hidden;
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

    private List<Connection> connections = new List<Connection>();
    private Dictionary<RectTransform, List<Connection>> connectionsByFrom = new Dictionary<RectTransform, List<Connection>>();
    private Dictionary<RectTransform, List<Connection>> connectionsByTo = new Dictionary<RectTransform, List<Connection>>();

    void Awake()
    {
        GenerateConnections();
    }

    void Start()
    {
        InitializeConnectionsVisuals();
    }

    void GenerateConnections()
    {
        connections.Clear();
        connectionsByFrom.Clear();
        connectionsByTo.Clear();

        SkillNode[] allNodes = FindObjectsOfType<SkillNode>(true);

        foreach (SkillNode childNode in allNodes)
        {
            if (childNode.requiredParentNodes == null) continue;

            foreach (SkillNode parentNode in childNode.requiredParentNodes)
            {
                if (parentNode == null) continue;

                Connection newConn = new Connection();
                newConn.from = parentNode.GetComponent<RectTransform>();
                newConn.to = childNode.GetComponent<RectTransform>();

                connections.Add(newConn);

                if (!connectionsByFrom.ContainsKey(newConn.from))
                    connectionsByFrom[newConn.from] = new List<Connection>();
                connectionsByFrom[newConn.from].Add(newConn);

                if (!connectionsByTo.ContainsKey(newConn.to))
                    connectionsByTo[newConn.to] = new List<Connection>();
                connectionsByTo[newConn.to].Add(newConn);
            }
        }
    }

    void InitializeConnectionsVisuals()
    {
        foreach (var c in connections)
        {
            c.lineBackground = CreateLineImage($"BG_{c.from.name}_{c.to.name}", lockedColor);
            c.lineForeground = CreateLineImage($"FG_{c.from.name}_{c.to.name}", unlockedColor);

            c.lineBackground.transform.SetAsFirstSibling();
            c.lineForeground.transform.SetSiblingIndex(c.lineBackground.transform.GetSiblingIndex() + 1);

            SkillNode fromNode = c.from.GetComponent<SkillNode>();
            SkillNode toNode = c.to.GetComponent<SkillNode>();

            if (fromNode != null && fromNode.IsUnlocked && toNode != null && toNode.IsUnlocked)
            {
                SetLineInstant(c, 1, 1, LineState.Unlocked);
            }
            else if (fromNode != null && fromNode.IsUnlocked)
            {
                SetLineInstant(c, 1, 0, LineState.Locked);
            }
        }
    }

    void SetLineInstant(Connection c, float bgFill, float fgFill, LineState state)
    {
        c.lineBackground.gameObject.SetActive(bgFill > 0);
        c.lineBackground.fillAmount = bgFill;
        c.lineForeground.gameObject.SetActive(fgFill > 0);
        c.lineForeground.fillAmount = fgFill;
        c.state = state;
    }

    Image CreateLineImage(string name, Color color)
    {
        Image img = Instantiate(linePrefab, fixedCanvas);
        img.name = name;
        img.color = color;
        img.fillAmount = 0;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.gameObject.SetActive(false);
        img.rectTransform.pivot = new Vector2(0, 0.5f);
        return img;
    }

    void LateUpdate()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var c in connections)
        {
            if (c.state == LineState.Hidden) continue;

            PositionLine(c.lineBackground, c.from, c.to);
            PositionLine(c.lineForeground, c.from, c.to);

            SkillNode fromNode = c.from.GetComponent<SkillNode>();
            SkillNode toNode = c.to.GetComponent<SkillNode>();
            if (toNode != null && toNode.IsUnlocked
                && fromNode != null && fromNode.IsUnlocked
                && c.state == LineState.Locked)
            {
                StartCoroutine(AnimateUnlock(c));
            }
        }
    }

    // DISPONIBILIDAD DE LÍNEAS GRISES
    public void ShowFrom(RectTransform fromNode)
    {
        if (connectionsByFrom.TryGetValue(fromNode, out List<Connection> outgoingLines))
        {
            foreach (var line in outgoingLines)
            {
                // Solo si la línea no ha empezado nada, iniciamos el dibujo de la gris
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

    // RECOLOR BLANCO (ENERGÍA)
    private IEnumerator AnimateUnlock(Connection c)
    {
        c.state = LineState.Unlocking; // Cambiamos el estado INMEDIATAMENTE para que LateUpdate no lance más corrutinas
        c.lineForeground.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < unlockDuration)
        {
            elapsed += Time.deltaTime;
            c.lineForeground.fillAmount = Mathf.Lerp(0, 1, elapsed / unlockDuration);
            yield return null;
        }

        c.lineForeground.fillAmount = 1;

        // --- ÚNICO PUNTO DE ACTIVACIÓN ---
        // Ahora que la línea blanca ha terminado de "tocar" el nodo destino,
        // este nodo lanza sus propias líneas grises hacia sus hijos.
        ShowFrom(c.to);
    }

    void PositionLine(Image lineImg, RectTransform a, RectTransform b)
    {
        RectTransform lineRT = lineImg.rectTransform;

        Vector2 A = WorldToUI(a.position);
        Vector2 B = WorldToUI(b.position);

        Vector2 dir = B - A;
        float dist = dir.magnitude;
        if (dist < 0.01f) return;

        Vector2 n = dir / dist;

        // Anchura recortada por offsets
        float width = Mathf.Max(0f, dist - globalOffsetSalida - globalOffsetLlegada);

        // Si el destino está "a la izquierda" del origen en UI, invertimos el origen del fill
        bool flip = (dir.x < 0f);

        if (!flip)
        {
            // Relleno de izquierda a derecha (origen en A)
            lineImg.fillOrigin = 0;                 // Left
            lineRT.pivot = new Vector2(0f, 0.5f);

            Vector2 startPos = A + (n * globalOffsetSalida) + globalOffsetPosicion;

            lineRT.anchoredPosition = startPos;
            lineRT.sizeDelta = new Vector2(width, lineThickness);
            lineRT.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
        else
        {
            // Relleno de derecha a izquierda local, pero visualmente va de A -> B
            // (porque anclamos en el extremo cercano a B)
            lineImg.fillOrigin = 1;                 // Right
            lineRT.pivot = new Vector2(1f, 0.5f);

            Vector2 endPos = B - (n * globalOffsetLlegada) + globalOffsetPosicion;

            lineRT.anchoredPosition = endPos;
            lineRT.sizeDelta = new Vector2(width, lineThickness);
            lineRT.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
    }

    Vector2 WorldToUI(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fixedCanvas, screen, null, out Vector2 localPos);
        return localPos;
    }
}