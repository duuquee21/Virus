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
        public RectTransform nodeA;
        public RectTransform nodeB;
        public RectTransform activeFrom;
        public RectTransform activeTo;
        public Image lineBackground;
        public Image lineForeground;
        public LineState state = LineState.Hidden;

        [Header("Offsets Individuales (Opcional)")]
        public bool overrideGlobalOffsets = false;
        public float offsetSalida;
        public float offsetLlegada;
        public Vector2 offsetPosicion;

        public bool IsBetween(RectTransform a, RectTransform b)
        {
            return (nodeA == a && nodeB == b) || (nodeA == b && nodeB == a);
        }
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

    [Header("Offsets Globales (Por defecto)")]
    public float globalOffsetSalida = 20f;
    public float globalOffsetLlegada = 20f;
    public Vector2 globalOffsetPosicion;

    private List<Connection> connections = new List<Connection>();

    void Awake()
    {
        GenerateConnections();
        InitializeConnectionsVisuals();
    }

    void GenerateConnections()
    {
        connections.Clear();
        SkillNode[] allNodes = FindObjectsOfType<SkillNode>(true);

        foreach (SkillNode childNode in allNodes)
        {
            if (childNode.requiredParentNodes == null) continue;

            foreach (SkillNode parentNode in childNode.requiredParentNodes)
            {
                if (parentNode == null) continue;

                RectTransform rA = parentNode.GetComponent<RectTransform>();
                RectTransform rB = childNode.GetComponent<RectTransform>();

                if (connections.Exists(c => c.IsBetween(rA, rB))) continue;

                Connection newConn = new Connection
                {
                    nodeA = rA,
                    nodeB = rB,
                    activeFrom = rA,
                    activeTo = rB,
                    // Inicializamos con los valores globales por si acaso
                    offsetSalida = globalOffsetSalida,
                    offsetLlegada = globalOffsetLlegada,
                    offsetPosicion = globalOffsetPosicion
                };
                connections.Add(newConn);
            }
        }
    }

    void InitializeConnectionsVisuals()
    {
        foreach (var c in connections)
        {
            c.lineBackground = CreateLineImage($"BG_{c.nodeA.name}_{c.nodeB.name}", lockedColor);
            c.lineForeground = CreateLineImage($"FG_{c.nodeA.name}_{c.nodeB.name}", unlockedColor);

            SkillNode sA = c.nodeA.GetComponent<SkillNode>();
            SkillNode sB = c.nodeB.GetComponent<SkillNode>();

            if (sA.IsUnlocked && sB.IsUnlocked)
                SetLineInstant(c, 1, 1, LineState.Unlocked);
            else if (sA.IsUnlocked || sB.IsUnlocked)
            {
                c.activeFrom = sA.IsUnlocked ? c.nodeA : c.nodeB;
                c.activeTo = sA.IsUnlocked ? c.nodeB : c.nodeA;
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

    void LateUpdate()
    {
        foreach (var c in connections)
        {
            if (c.state == LineState.Hidden)
            {
                if (c.nodeA.GetComponent<SkillNode>().IsUnlocked || c.nodeB.GetComponent<SkillNode>().IsUnlocked)
                {
                    c.activeFrom = c.nodeA.GetComponent<SkillNode>().IsUnlocked ? c.nodeA : c.nodeB;
                    c.activeTo = (c.activeFrom == c.nodeA) ? c.nodeB : c.nodeA;
                    StartCoroutine(WaitAndDiscover(c));
                }
                continue;
            }

            // Aplicamos el posicionamiento usando los offsets específicos de la conexión
            PositionLine(c, c.lineBackground);
            PositionLine(c, c.lineForeground);

            if (c.state == LineState.Locked)
            {
                if (c.nodeA.GetComponent<SkillNode>().IsUnlocked && c.nodeB.GetComponent<SkillNode>().IsUnlocked)
                {
                    StartCoroutine(AnimateUnlock(c));
                }
            }
        }
    }

    private IEnumerator WaitAndDiscover(Connection c)
    {
        c.state = LineState.Discovering;
        c.lineBackground.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < discoveryDuration)
        {
            elapsed += Time.deltaTime;
            c.lineBackground.fillAmount = Mathf.Lerp(0, 1, elapsed / discoveryDuration);
            yield return null;
        }
        c.lineBackground.fillAmount = 1;
        c.state = LineState.Locked;
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
    }

    Image CreateLineImage(string name, Color color)
    {
        Image img = Instantiate(linePrefab, fixedCanvas);
        img.name = name;
        img.color = color;
        img.fillAmount = 0;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.rectTransform.pivot = new Vector2(0f, 0.5f);
        img.gameObject.SetActive(false);
        return img;
    }

    // MÉTODO ACTUALIZADO: Ahora recibe la conexión entera para leer sus offsets
    void PositionLine(Connection c, Image lineImg)
    {
        Vector2 A = WorldToUI(c.activeFrom.position);
        Vector2 B = WorldToUI(c.activeTo.position);
        Vector2 dir = B - A;
        float dist = dir.magnitude;

        if (dist < 0.1f) return;

        // Determinar qué valores usar: los de la conexión o los globales
        float offSalida = c.overrideGlobalOffsets ? c.offsetSalida : globalOffsetSalida;
        float offLlegada = c.overrideGlobalOffsets ? c.offsetLlegada : globalOffsetLlegada;
        Vector2 offPos = c.overrideGlobalOffsets ? c.offsetPosicion : globalOffsetPosicion;

        RectTransform rt = lineImg.rectTransform;

        // Posicionamiento con offset
        rt.anchoredPosition = A + (dir.normalized * offSalida) + offPos;

        // El largo se calcula restando ambos offsets para que no atraviese los iconos
        float finalLength = dist - offSalida - offLlegada;
        rt.sizeDelta = new Vector2(Mathf.Max(0, finalLength), lineThickness);

        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    Vector2 WorldToUI(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fixedCanvas, RectTransformUtility.WorldToScreenPoint(null, worldPos), null, out Vector2 localPos);
        return localPos;
    }
}