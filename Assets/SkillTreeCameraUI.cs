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

    [Header("Configuración de Movimiento")]
    public float dragSmoothness = 15f;

    private float _targetZoom;
    private Vector2 _targetPosition;
    private Camera _uiCamera;

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
        // Si es Zoom Out, este factor será menor a 1.
        float multiplier = _targetZoom / oldZoom;

        // 3. Calculamos cuánto se movería ese punto debido a la escala
        // Al restar el movimiento, compensamos para que el punto bajo el mouse no se mueva visualmente
        Vector2 offset = mouseLocalPos * (multiplier - 1f);

        // Aplicamos el ajuste a la posición objetivo (multiplicado por la escala actual para normalizar)
        _targetPosition -= offset * oldZoom;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Movimiento 1:1 independiente del nivel de Zoom
        _targetPosition += eventData.delta / content.localScale.x;
    }
}