using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreeCameraUI : MonoBehaviour, IDragHandler
{
    public RectTransform content;

    [Header("Movimiento")]
    public float speed = 1f;
    [Tooltip("Controla cómo crece la velocidad con el zoom. 1 = lineal, >1 = más agresivo, <1 = más suave")]
    public float speedExponent = 1.0f;

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;

    public void OnDrag(PointerEventData eventData)
    {
        float scale = content.localScale.x;
        float speedFactor = Mathf.Pow(scale, speedExponent);
        content.anchoredPosition += eventData.delta * speed * speedFactor;
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
        Vector3 newScale = content.localScale + Vector3.one * delta * zoomSpeed;

        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        newScale.z = 1f;

        content.localScale = newScale;
    }
}