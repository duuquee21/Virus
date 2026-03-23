using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias Visuales")]
    [Tooltip("Arrastra aquí el objeto HIJO que se va a mover y pintar")]
    public Transform elementoVisual;

    private Image imagenVisual;
    private TextMeshProUGUI texto;
    private Quaternion rotacionOriginal;
    private Coroutine corrutinaActual;

    [Header("Colores")]
    public Color colorFondoHover = Color.black;
    public Color colorTextoHover = Color.green;

    [Header("Configuración del Balanceo")]
    public float anguloShake = 5f;
    public float velocidadGiro = 40f;
    public int repeticiones = 1;

    void Awake()
    {
        // Obtenemos los componentes del HIJO visual, no del padre invisible
        if (elementoVisual != null)
        {
            imagenVisual = elementoVisual.GetComponent<Image>();
            texto = elementoVisual.GetComponentInChildren<TextMeshProUGUI>();
            rotacionOriginal = elementoVisual.localRotation;
        }
        else
        {
            Debug.LogError("¡Aviso! No has asignado el 'Elemento Visual' en el inspector del botón.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (elementoVisual == null) return;

        ResetearEstado();

        if (imagenVisual != null) imagenVisual.color = colorFondoHover;
        if (texto != null) texto.color = colorTextoHover;

        corrutinaActual = StartCoroutine(EfectoBalanceoSuave());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (elementoVisual == null) return;

        ResetearEstado();

        if (imagenVisual != null) imagenVisual.color = Color.white;
        if (texto != null) texto.color = Color.black;
    }

    private void ResetearEstado()
    {
        StopAllCoroutines();
        corrutinaActual = null;

        // Enderezamos el elemento visual
        if (elementoVisual != null)
            elementoVisual.localRotation = rotacionOriginal;
    }

    IEnumerator EfectoBalanceoSuave()
    {
        for (int i = 0; i < repeticiones; i++)
        {
            yield return StartCoroutine(GirarA(anguloShake));
            yield return StartCoroutine(GirarA(-anguloShake));
        }
        yield return StartCoroutine(GirarA(0));
    }

    IEnumerator GirarA(float anguloTarget)
    {
        Quaternion destino = Quaternion.Euler(0, 0, anguloTarget);
        float tiempoSeguridad = 0;

        // Ahora rotamos el 'elementoVisual'
        while (Quaternion.Angle(elementoVisual.localRotation, destino) > 0.01f && tiempoSeguridad < 0.5f)
        {
            elementoVisual.localRotation = Quaternion.Slerp(
                elementoVisual.localRotation,
                destino,
                Time.unscaledDeltaTime * velocidadGiro
            );

            tiempoSeguridad += Time.unscaledDeltaTime;
            yield return null;
        }
        elementoVisual.localRotation = destino;
    }
}