using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Referencias Visuales")]
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
        if (elementoVisual != null)
        {
            imagenVisual = elementoVisual.GetComponent<Image>();
            texto = elementoVisual.GetComponentInChildren<TextMeshProUGUI>();
            rotacionOriginal = elementoVisual.localRotation;
        }
    }

    // 🖱️ --- RATÓN ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 🛡️ PROTECCIÓN: Solo se activa si el sistema confirma que el ratón manda
        // Esto evita que el botón "brille" solo porque el cursor estaba ahí quieto al abrirse el panel
        if (MenuGamepadNavigator.usandoRaton)
        {
            ActivarEfecto();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DesactivarEfecto();
    }

    // 🎮 --- MANDO ---
    public void OnSelect(BaseEventData eventData)
    {
        // 🛡️ PROTECCIÓN: Si estamos con ratón, el mando NO puede activar el efecto visual
        if (MenuGamepadNavigator.usandoRaton) return;

        ActivarEfecto();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        DesactivarEfecto();
    }

    // --- LÓGICA DE EFECTOS ---
    private void ActivarEfecto()
    {
        if (elementoVisual == null) return;
        ResetearEstado();

        if (imagenVisual != null) imagenVisual.color = colorFondoHover;
        if (texto != null) texto.color = colorTextoHover;

        corrutinaActual = StartCoroutine(EfectoBalanceoSuave());
    }

    private void DesactivarEfecto()
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