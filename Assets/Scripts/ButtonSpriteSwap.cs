using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite spriteNormal;
    public Sprite spriteHighlight;
    public float escalaAumento = 1.2f;

    private Image _image;
    private Vector3 _escalaOriginal;

    void Awake()
    {
        _image = GetComponent<Image>();
        // Guardamos la escala real que tiene al empezar el juego
        _escalaOriginal = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (spriteHighlight != null) _image.sprite = spriteHighlight;

        // Forzamos la escala directamente multiplicando la original
        transform.localScale = _escalaOriginal * escalaAumento;

        // Debug para confirmar que el código llega aquí
        Debug.Log("Creciendo a: " + transform.localScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (spriteNormal != null) _image.sprite = spriteNormal;

        // Volvemos a la escala original
        transform.localScale = _escalaOriginal;
    }
}