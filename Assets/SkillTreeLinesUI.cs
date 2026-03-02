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
        [HideInInspector] public Coroutine activeCoroutine; // Para evitar solapamiento de animaciones
    }

    [Header("Configuración de Líneas")]
    public RectTransform fixedCanvas;
    public Image linePrefab;
    public float lineThickness = 6f;

    [Header("Animación")]
    public float discoveryDuration = 0.4f;
    public float unlockDuration = 0.6f;

    [Header("Configuración de Color")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color unlockedColor = Color.white;

    [Header("Offsets")]
    public float globalOffsetSalida = 25f;
    public float globalOffsetLlegada = 25f;
    public Vector2 globalOffsetPosicion;

    public Connection[] connections;
    private Dictionary<RectTransform, List<Connection>> connectionsByFrom = new Dictionary<RectTransform, List<Connection>>();

    void Start()
    {
        foreach (var c in connections)
        {
            if (c.from == null || c.to == null) continue;

            // Crear visuales
            c.lineBackground = CreateLineImage($"BG_{c.from.name}_{c.to.name}", lockedColor);
            c.lineForeground = CreateLineImage($"FG_{c.from.name}_{c.to.name}", unlockedColor);

            // Orden de dibujado (detrás de los botones)
            c.lineBackground.transform.SetAsFirstSibling();
            c.lineForeground.transform.SetSiblingIndex(c.lineBackground.transform.GetSiblingIndex() + 1);

            // Indexar
            if (!connectionsByFrom.ContainsKey(c.from)) connectionsByFrom[c.from] = new List<Connection>();
            connectionsByFrom[c.from].Add(c);

            // ESTADO INICIAL según carga de partida
            SkillNode fromNode = c.from.GetComponent<SkillNode>();
            SkillNode toNode = c.to.GetComponent<SkillNode>();

            if (fromNode != null && fromNode.IsUnlocked)
            {
                if (toNode != null && toNode.IsUnlocked)
                {
                    // Ambos comprados: Línea blanca completa
                    c.state = LineState.Unlocked;
                    c.lineBackground.gameObject.SetActive(true);
                    c.lineBackground.fillAmount = 1;
                    c.lineForeground.gameObject.SetActive(true);
                    c.lineForeground.fillAmount = 1;
                }
                else
                {
                    // Solo origen comprado: Línea gris completa
                    c.state = LineState.Locked;
                    c.lineBackground.gameObject.SetActive(true);
                    c.lineBackground.fillAmount = 1;
                    c.lineForeground.gameObject.SetActive(false);
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
        // Asegúrate de que el Sprite de la imagen sea una barra blanca simple y el Type sea 'Filled'
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        return img;
    }

    void LateUpdate()
    {
        // Actualizamos posiciones y detectamos cambios de estado cada frame por si los nodos se mueven (Scroll)
        foreach (var c in connections)
        {
            if (c == null || c.from == null || c.to == null) continue;

            SkillNode fromNode = c.from.GetComponent<SkillNode>();
            SkillNode toNode = c.to.GetComponent<SkillNode>();

            // 1. Lógica de mostrar Gris (Discovery)
            if (fromNode.IsUnlocked && c.state == LineState.Hidden)
            {
                if (c.activeCoroutine == null)
                    c.activeCoroutine = StartCoroutine(AnimateDiscovery(c));
            }

            // 2. Lógica de mostrar Blanco (Unlock)
            if (toNode.IsUnlocked && c.state == LineState.Locked)
            {
                if (c.activeCoroutine == null)
                    c.activeCoroutine = StartCoroutine(AnimateUnlock(c));
            }

            // 3. Mantener posiciones actualizadas
            if (c.lineBackground.gameObject.activeInHierarchy)
            {
                PositionLine(c.lineBackground.rectTransform, c.from, c.to);
                PositionLine(c.lineForeground.rectTransform, c.from, c.to);
            }
        }
    }

    // Invocado por SkillNode cuando se compra o al iniciar
    public void ShowFrom(RectTransform nodeRT)
    {
        // El LateUpdate ya se encarga de detectar el cambio en IsUnlocked
        // pero podemos forzar un refresco si es necesario.
    }

    private IEnumerator AnimateDiscovery(Connection c)
    {
        c.state = LineState.Discovering;
        c.lineBackground.gameObject.SetActive(true);
        c.lineBackground.fillAmount = 0;

        float elapsed = 0f;
        while (elapsed < discoveryDuration)
        {
            elapsed += Time.deltaTime;
            c.lineBackground.fillAmount = Mathf.Lerp(0, 1, elapsed / discoveryDuration);
            yield return null;
        }

        c.lineBackground.fillAmount = 1;
        c.state = LineState.Locked;
        c.activeCoroutine = null;
    }

    private IEnumerator AnimateUnlock(Connection c)
    {
        c.state = LineState.Unlocking;
        c.lineForeground.gameObject.SetActive(true);
        c.lineForeground.fillAmount = 0;

        float elapsed = 0f;
        while (elapsed < unlockDuration)
        {
            elapsed += Time.deltaTime;
            c.lineForeground.fillAmount = Mathf.Lerp(0, 1, elapsed / unlockDuration);
            yield return null;
        }

        c.lineForeground.fillAmount = 1;
        c.state = LineState.Unlocked;
        c.activeCoroutine = null;
    }

    void PositionLine(RectTransform lineRT, RectTransform a, RectTransform b)
    {
        Vector2 A = WorldToUI(a.position);
        Vector2 B = WorldToUI(b.position);
        Vector2 dir = B - A;
        float dist = dir.magnitude;

        // Calculamos el inicio con el offset
        Vector2 startPos = A + (dir.normalized * globalOffsetSalida) + globalOffsetPosicion;

        // La distancia total restando ambos offsets de los círculos/nodos
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