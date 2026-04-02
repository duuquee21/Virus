using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreeCameraUI : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Referencias")]
    public RectTransform content; // El árbol de habilidades

    [Header("Configuración de Zoom")]
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;
    public float zoomSensitivity = 0.1f;
    public float zoomSmoothness = 10f;

    public float mouseDragSensitivity = 0.5f;

    [Header("Configuración de Movimiento (Ratón)")]
    public float dragSmoothness = 15f;

    // 🎮 --- NUEVO: CONFIGURACIÓN PARA EL MANDO ---
    [Header("Configuración de Movimiento (Mando)")]
    public float joystickPanSpeed = 1500f; // Velocidad de arrastre con el joystick
    public bool invertirJoystick = false; // True para mover como ratón, False para mover como cámara
    public string joystickDerechoX = "RightHorizontal";
    public string joystickDerechoY = "RightVertical";

    private float _targetZoom;
    private Vector2 _targetPosition;
    private Camera _uiCamera;
    public static SkillTreeCameraUI instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        _targetZoom = content.localScale.x;
        _targetPosition = content.anchoredPosition;

        // Detectar si el Canvas es Overlay o Camera
        Canvas canvas = GetComponentInParent<Canvas>();
        _uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
    }

    void Update()
    {
        // 🎮 --- LÓGICA DEL JOYSTICK DERECHO ---
        float h = Input.GetAxisRaw(joystickDerechoX);
        float v = Input.GetAxisRaw(joystickDerechoY);

        if (Mathf.Abs(h) > 0.3f || Mathf.Abs(v) > 0.3f)
        {
            // Calculamos el movimiento del mando
            Vector2 inputMando = new Vector2(h, v) * joystickPanSpeed * Time.unscaledDeltaTime;

            if (invertirJoystick)
            {
                inputMando = -inputMando;
            }

            // Aplicamos el movimiento al target position (dividido por el zoom para que la velocidad sea constante)
            _targetPosition += inputMando / _targetZoom;
        }

        // --- INTERPOLACIÓN SUAVE (Tu código original) ---

        // Interpolación suave de Escala
        float lerpZoom = Mathf.Lerp(content.localScale.x, _targetZoom, Time.deltaTime * zoomSmoothness);
        content.localScale = new Vector3(lerpZoom, lerpZoom, 1f);

        // Interpolación suave de Posición
        content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, _targetPosition, Time.deltaTime * dragSmoothness);
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y * zoomSensitivity;
        float oldZoom = _targetZoom;

        // Calculamos el nuevo zoom deseado
        _targetZoom = Mathf.Clamp(_targetZoom + scrollDelta, minZoom, maxZoom);

        // Si el zoom no ha cambiado (llegamos al límite), no hacemos cálculos
        if (Mathf.Approximately(oldZoom, _targetZoom)) return;

        // --- LÓGICA DE ANCLAJE AL MOUSE ---

        // 1. Obtenemos la posición del ratón relativa al 'content' (donde está el puntero en el mapa)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, _uiCamera, out Vector2 mouseLocalPos);

        // 2. Calculamos el factor de cambio entre el zoom nuevo y el actual
        float multiplier = _targetZoom / oldZoom;

        // 3. Calculamos cuánto se movería ese punto debido a la escala
        Vector2 offset = mouseLocalPos * (multiplier - 1f);

        // Aplicamos el ajuste a la posición objetivo
        _targetPosition -= offset * oldZoom;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Multiplicamos por la sensibilidad antes de aplicar el zoom
        _targetPosition += (eventData.delta * mouseDragSensitivity) / content.localScale.x;
    }
    // 📸 NUEVA FUNCIÓN: Centra la cámara en el nodo que el mando seleccione
    // 📸 NUEVA FUNCIÓN: Centra la cámara en el nodo que el mando seleccione
    // 📸 NUEVA FUNCIÓN: Centra la cámara en el nodo que el mando seleccione
    public void EnfocarEnNodo(RectTransform nodoElegido)
    {
        // 🛑 EL ESCUDO ANTI-RATÓN DEFINITIVO: 
        // Ignoramos si está pulsando (o acaba de soltar) el clic izquierdo (0), derecho (1) o la ruleta (2)
        if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0) ||
            Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) ||
            Input.GetMouseButton(2) || Input.GetMouseButtonUp(2))
        {
            return;
        }

        // Al invertir la posición del nodo, el panel se mueve suavemente en dirección contraria
        // para dejar ese nodo exactamente en el centro de la pantalla.
        _targetPosition = -nodoElegido.anchoredPosition;
    }

}