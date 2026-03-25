using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerUpHandler, IPointerDownHandler, ISubmitHandler
{
    [Header("Referencias Visuales")]
    public Transform elementoVisual;

    private Image imagenVisual;
    private TextMeshProUGUI texto;
    private Quaternion rotacionOriginal;
    private Coroutine corrutinaActual;

    private Color colorFondoOriginal;
    private Color colorTextoOriginal;

    [Header("Colores")]
    public Color colorFondoHover = Color.black;
    public Color colorTextoHover = Color.green;

    [Header("Sonidos (Opcional)")]
    public AudioClip sonidoHoverEspecifico;
    public AudioClip sonidoClickEspecifico;

    [Header("Comportamiento")]
    public bool mantenerColorAlClicar = true;
    public bool estaSeleccionado = false;

    private bool elPunteroEstaEncima = false;

    [Header("Configuración del Balanceo")]
    public float anguloShake = 5f;
    public float velocidadGiro = 40f;
    public int repeticiones = 1;

    void Awake()
    {
        Button btnNativo = GetComponent<Button>();
        if (btnNativo != null) btnNativo.transition = Selectable.Transition.None;

        if (elementoVisual != null)
        {
            imagenVisual = elementoVisual.GetComponent<Image>();
            texto = elementoVisual.GetComponentInChildren<TextMeshProUGUI>();
            rotacionOriginal = elementoVisual.localRotation;

            if (imagenVisual != null) colorFondoOriginal = imagenVisual.color;
            if (texto != null) colorTextoOriginal = texto.color;
        }
    }

    void OnEnable()
    {
        ResetearSeleccion();
        if (elementoVisual != null) elementoVisual.localRotation = rotacionOriginal;
    }

    // 🔊 LÓGICA DE SONIDO
    private void EjecutarSonidoHover()
    {
        if (sonidoHoverEspecifico != null && GestorSonidosUI.Instancia != null && GestorSonidosUI.Instancia.audioSource != null)
        {
            GestorSonidosUI.Instancia.audioSource.PlayOneShot(sonidoHoverEspecifico);
        }
        else if (GestorSonidosUI.Instancia != null)
        {
            GestorSonidosUI.Instancia.ReproducirHover();
        }
    }

    private void EjecutarSonidoClick()
    {
        if (sonidoClickEspecifico != null && GestorSonidosUI.Instancia != null && GestorSonidosUI.Instancia.audioSource != null)
        {
            GestorSonidosUI.Instancia.audioSource.PlayOneShot(sonidoClickEspecifico);
        }
        else if (GestorSonidosUI.Instancia != null)
        {
            GestorSonidosUI.Instancia.ReproducirClick();
        }
    }

    // 🖱️ --- RATÓN ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        elPunteroEstaEncima = true;
        if (MenuGamepadNavigator.usandoRaton) ActivarEfecto();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        elPunteroEstaEncima = false;
        DesactivarEfecto();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (MenuGamepadNavigator.usandoRaton)
        {
            EjecutarSonidoClick();

            if (imagenVisual != null) imagenVisual.color = colorFondoHover;
            if (texto != null) texto.color = colorTextoHover;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (MenuGamepadNavigator.usandoRaton) EjecutarEfectoSoltar();
    }

    // 🎮 --- MANDO ---
    public void OnSelect(BaseEventData eventData)
    {
        elPunteroEstaEncima = true;
        if (MenuGamepadNavigator.usandoRaton) return;
        ActivarEfecto();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (MenuGamepadNavigator.usandoRaton) return;

        elPunteroEstaEncima = false;
        DesactivarEfecto();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (MenuGamepadNavigator.usandoRaton) return;

        EjecutarSonidoClick();
        EjecutarEfectoSoltar();
    }

    // --- LÓGICA DE EFECTOS ---
    private void ActivarEfecto()
    {
        if (elementoVisual == null) return;
        StopAllCoroutines();

        EjecutarSonidoHover();

        if (imagenVisual != null) imagenVisual.color = colorFondoHover;
        if (texto != null) texto.color = colorTextoHover;

        corrutinaActual = StartCoroutine(EfectoBalanceo(1f));
    }

    private void DesactivarEfecto()
    {
        if (elementoVisual == null) return;
        StopAllCoroutines();

        // Evaluamos si el botón NO está seleccionado
        if (!estaSeleccionado)
        {
            if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
            if (texto != null) texto.color = colorTextoOriginal;

            // 🔄 Solo vibra al salir si NO ha sido pulsado
            corrutinaActual = StartCoroutine(EfectoBalanceo(-1f));
        }
        else
        {
            // 🛑 Si YA está pulsado, no vibra. Solo lo devolvemos al centro suavemente
            // por si te saliste con el ratón antes de que terminara la animación de entrada.
            corrutinaActual = StartCoroutine(GirarA(0));
        }
    }

    private void EjecutarEfectoSoltar()
    {
        if (elementoVisual == null) return;

        if (mantenerColorAlClicar)
        {
            estaSeleccionado = !estaSeleccionado;

            if (estaSeleccionado)
            {
                if (imagenVisual != null) imagenVisual.color = colorFondoHover;
                if (texto != null) texto.color = colorTextoHover;
            }
            else
            {
                if (!elPunteroEstaEncima)
                {
                    if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
                    if (texto != null) texto.color = colorTextoOriginal;
                }
                else
                {
                    if (imagenVisual != null) imagenVisual.color = colorFondoHover;
                    if (texto != null) texto.color = colorTextoHover;
                }
            }
        }
    }

    public void ResetearSeleccion()
    {
        estaSeleccionado = false;
        elPunteroEstaEncima = false;

        if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
        if (texto != null) texto.color = colorTextoOriginal;
    }

    // --- CORRUTINAS DE ANIMACIÓN ---
    IEnumerator EfectoBalanceo(float multiplicadorDireccion)
    {
        for (int i = 0; i < repeticiones; i++)
        {
            yield return StartCoroutine(GirarA(anguloShake * multiplicadorDireccion));
            yield return StartCoroutine(GirarA(-anguloShake * multiplicadorDireccion));
        }
        yield return StartCoroutine(GirarA(0));
    }

    IEnumerator GirarA(float anguloTarget)
    {
        Quaternion destino = rotacionOriginal * Quaternion.Euler(0, 0, anguloTarget);
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