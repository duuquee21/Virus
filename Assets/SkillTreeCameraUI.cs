using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreeCameraUI : MonoBehaviour, IDragHandler
{
    public RectTransform content;

    [Header("Movimiento")]
    public float speed = 1f;

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;

    public void OnDrag(PointerEventData eventData)
    {
        // Dividimos por la escala actual para que el movimiento sea consistente al hacer zoom
        content.anchoredPosition += eventData.delta * speed / content.localScale.x;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            ApplyZoom(scroll);
        }
    }

    void ApplyZoom(float delta)
    {
        // Calculamos la nueva escala
        Vector3 newScale = content.localScale + Vector3.one * delta * zoomSpeed;

        // Limitamos el zoom para que no sea infinito ni negativo
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        newScale.z = 1f;

        content.localScale = newScale;
    }
}