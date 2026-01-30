using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreeCameraUI : MonoBehaviour, IDragHandler
{
    public RectTransform content;

    public float speed = 1f;

    public void OnDrag(PointerEventData eventData)
    {
        content.anchoredPosition += eventData.delta * speed;
    }
}
