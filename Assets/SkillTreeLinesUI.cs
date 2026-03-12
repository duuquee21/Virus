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

    private readonly List<Connection> connections = new List<Connection>();

    void Awake()
    {
        RebuildAll();
    }

    public void RebuildAll()
    {
        StopAllCoroutines();
        ClearExistingLineObjects();
        GenerateConnections();
        InitializeConnectionsVisuals();
    }

    void ClearExistingLineObjects()
    {
        if (fixedCanvas == null) return;

        List<Transform> toDelete = new List<Transform>();

        foreach (Transform child in fixedCanvas)
        {
            if (child.name.StartsWith("BG_") || child.name.StartsWith("FG_"))
            {
                toDelete.Add(child);
            }
        }

        foreach (Transform t in toDelete)
        {
            Destroy(t.gameObject);
        }

        connections.Clear();
    }

    void GenerateConnections()
    {
        SkillNode[] allNodes = FindObjectsOfType<SkillNode>(true);

        foreach (SkillNode childNode in allNodes)
        {
            if (childNode.requiredParentNodes == null) continue;

            foreach (SkillNode parentNode in childNode.requiredParentNodes)
            {
                if (parentNode == null) continue;

                RectTransform rA = parentNode.GetComponent<RectTransform>();
                RectTransform rB = childNode.GetComponent<RectTransform>();

                if (rA == null || rB == null) continue;
                if (connections.Exists(c => c.IsBetween(rA, rB))) continue;

                Connection newConn = new Connection
                {
                    nodeA = rA,
                    nodeB = rB,
                    activeFrom = rA,
                    activeTo = rB,
                    offsetSalida = globalOffsetSalida,
                    offsetLlegada = globalOffsetLlegada,
                    offsetPosicion = globalOffsetPosicion,
                    state = LineState.Hidden
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

            c.lineForeground.rectTransform.SetSiblingIndex(c.lineBackground.rectTransform.GetSiblingIndex() + 1);

            SkillNode sA = c.nodeA.GetComponent<SkillNode>();
            SkillNode sB = c.nodeB.GetComponent<SkillNode>();

            if (sA == null || sB == null)
            {
                SetLineInstant(c, 0f, 0f, LineState.Hidden);
                continue;
            }

            if (sA.IsUnlocked && sB.IsUnlocked)
            {
                c.activeFrom = c.nodeA;
                c.activeTo = c.nodeB;
                SetLineInstant(c, 1f, 1f, LineState.Unlocked);
            }
            else if (sA.IsUnlocked || sB.IsUnlocked)
            {
                c.activeFrom = sA.IsUnlocked ? c.nodeA : c.nodeB;
                c.activeTo = sA.IsUnlocked ? c.nodeB : c.nodeA;
                SetLineInstant(c, 1f, 0f, LineState.Locked);
            }
            else
            {
                c.activeFrom = c.nodeA;
                c.activeTo = c.nodeB;
                SetLineInstant(c, 0f, 0f, LineState.Hidden);
            }
        }
    }

    void SetLineInstant(Connection c, float bgFill, float fgFill, LineState state)
    {
        if (c.lineBackground != null)
        {
            c.lineBackground.gameObject.SetActive(bgFill > 0f);
            c.lineBackground.fillAmount = bgFill;
        }

        if (c.lineForeground != null)
        {
            c.lineForeground.gameObject.SetActive(fgFill > 0f);
            c.lineForeground.fillAmount = fgFill;
        }

        c.state = state;
    }

    public void ResetAllLinesVisuals()
    {
        StopAllCoroutines();

        foreach (var c in connections)
        {
            if (c.lineBackground != null)
            {
                c.lineBackground.fillAmount = 0f;
                c.lineBackground.gameObject.SetActive(false);
            }

            if (c.lineForeground != null)
            {
                c.lineForeground.fillAmount = 0f;
                c.lineForeground.gameObject.SetActive(false);
            }

            c.activeFrom = c.nodeA;
            c.activeTo = c.nodeB;
            c.state = LineState.Hidden;
        }
    }

    public void RefreshAllLinesFromNodes()
    {
        StopAllCoroutines();

        foreach (var c in connections)
        {
            SkillNode sA = c.nodeA.GetComponent<SkillNode>();
            SkillNode sB = c.nodeB.GetComponent<SkillNode>();

            if (sA == null || sB == null)
            {
                SetLineInstant(c, 0f, 0f, LineState.Hidden);
                continue;
            }

            if (sA.IsUnlocked && sB.IsUnlocked)
            {
                c.activeFrom = c.nodeA;
                c.activeTo = c.nodeB;
                SetLineInstant(c, 1f, 1f, LineState.Unlocked);
            }
            else if (sA.IsUnlocked || sB.IsUnlocked)
            {
                c.activeFrom = sA.IsUnlocked ? c.nodeA : c.nodeB;
                c.activeTo = sA.IsUnlocked ? c.nodeB : c.nodeA;
                SetLineInstant(c, 1f, 0f, LineState.Locked);
            }
            else
            {
                c.activeFrom = c.nodeA;
                c.activeTo = c.nodeB;
                SetLineInstant(c, 0f, 0f, LineState.Hidden);
            }
        }
    }

    void LateUpdate()
    {
        foreach (var c in connections)
        {
            SkillNode nodeA = c.nodeA != null ? c.nodeA.GetComponent<SkillNode>() : null;
            SkillNode nodeB = c.nodeB != null ? c.nodeB.GetComponent<SkillNode>() : null;

            if (nodeA == null || nodeB == null) continue;

            if (c.state == LineState.Hidden)
            {
                if (nodeA.IsUnlocked || nodeB.IsUnlocked)
                {
                    c.activeFrom = nodeA.IsUnlocked ? c.nodeA : c.nodeB;
                    c.activeTo = (c.activeFrom == c.nodeA) ? c.nodeB : c.nodeA;
                    StartCoroutine(WaitAndDiscover(c));
                }
                continue;
            }

            PositionLine(c, c.lineBackground);
            PositionLine(c, c.lineForeground);

            if (c.state == LineState.Locked)
            {
                if (nodeA.IsUnlocked && nodeB.IsUnlocked)
                {
                    StartCoroutine(AnimateUnlock(c));
                }
            }
        }
    }

    private IEnumerator WaitAndDiscover(Connection c)
    {
        c.state = LineState.Discovering;

        if (c.lineBackground != null)
        {
            c.lineBackground.gameObject.SetActive(true);
            c.lineBackground.fillAmount = 0f;
        }

        float elapsed = 0f;

        while (elapsed < discoveryDuration)
        {
            elapsed += Time.deltaTime;

            if (c.lineBackground != null)
            {
                c.lineBackground.fillAmount = Mathf.Lerp(0f, 1f, elapsed / discoveryDuration);
            }

            yield return null;
        }

        if (c.lineBackground != null)
        {
            c.lineBackground.fillAmount = 1f;
        }

        c.state = LineState.Locked;
    }

    private IEnumerator AnimateUnlock(Connection c)
    {
        c.state = LineState.Unlocking;

        if (c.lineForeground != null)
        {
            c.lineForeground.gameObject.SetActive(true);
            c.lineForeground.fillAmount = 0f;
        }

        float elapsed = 0f;

        while (elapsed < unlockDuration)
        {
            elapsed += Time.deltaTime;

            if (c.lineForeground != null)
            {
                c.lineForeground.fillAmount = Mathf.Lerp(0f, 1f, elapsed / unlockDuration);
            }

            yield return null;
        }

        if (c.lineForeground != null)
        {
            c.lineForeground.fillAmount = 1f;
        }

        c.state = LineState.Unlocked;
    }

    Image CreateLineImage(string name, Color color)
    {
        Image img = Instantiate(linePrefab, fixedCanvas);
        img.name = name;
        img.color = color;
        img.fillAmount = 0f;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.rectTransform.pivot = new Vector2(0f, 0.5f);
        img.gameObject.SetActive(false);
        img.rectTransform.SetAsFirstSibling();
        return img;
    }

    void PositionLine(Connection c, Image lineImg)
    {
        if (lineImg == null || c.activeFrom == null || c.activeTo == null) return;

        Vector2 A = WorldToUI(c.activeFrom.position);
        Vector2 B = WorldToUI(c.activeTo.position);
        Vector2 dir = B - A;
        float dist = dir.magnitude;

        if (dist < 0.1f) return;

        float offSalida = c.overrideGlobalOffsets ? c.offsetSalida : globalOffsetSalida;
        float offLlegada = c.overrideGlobalOffsets ? c.offsetLlegada : globalOffsetLlegada;
        Vector2 offPos = c.overrideGlobalOffsets ? c.offsetPosicion : globalOffsetPosicion;

        RectTransform rt = lineImg.rectTransform;

        rt.anchoredPosition = A + (dir.normalized * offSalida) + offPos;

        float finalLength = dist - offSalida - offLlegada;
        rt.sizeDelta = new Vector2(Mathf.Max(0f, finalLength), lineThickness);

        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    Vector2 WorldToUI(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fixedCanvas,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 localPos
        );

        return localPos;
    }
}