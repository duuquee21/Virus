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
    private Vector3 escalaOriginal;
    private Coroutine corrutinaActual;
    private Coroutine corrutinaPop;

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

    [Header("Configuración del Balanceo (Hover)")]
    public float anguloShake = 5f;
    public float velocidadGiro = 40f;
    public int repeticiones = 1;

    [Header("Configuración del Pop (Click)")]
    public float escalaPop = 1.1f;
    public float velocidadPop = 15f;

    void Awake()
    {
        Button btnNativo = GetComponent<Button>();
        if (btnNativo != null) btnNativo.transition = Selectable.Transition.None;

        if (elementoVisual != null)
        {
            imagenVisual = elementoVisual.GetComponent<Image>();
            texto = elementoVisual.GetComponentInChildren<TextMeshProUGUI>();
            rotacionOriginal = elementoVisual.localRotation;
            escalaOriginal = elementoVisual.localScale;

            if (imagenVisual != null) colorFondoOriginal = imagenVisual.color;
            if (texto != null) colorTextoOriginal = texto.color;
        }
    }

    void OnEnable()
    {
        ResetearSeleccion();
        if (elementoVisual != null)
        {
            elementoVisual.localRotation = rotacionOriginal;
            elementoVisual.localScale = escalaOriginal;
        }
    }

    // 🔊 LÓGICA DE SONIDO
    private void EjecutarSonidoHover()
    {
        if (sonidoHoverEspecifico != null && GestorSonidosUI.Instancia != null && GestorSonidosUI.Instancia.audioSource != null)
            GestorSonidosUI.Instancia.audioSource.PlayOneShot(sonidoHoverEspecifico);
        else if (GestorSonidosUI.Instancia != null)
            GestorSonidosUI.Instancia.ReproducirHover();
    }

    private void EjecutarSonidoClick()
    {
        if (sonidoClickEspecifico != null && GestorSonidosUI.Instancia != null && GestorSonidosUI.Instancia.audioSource != null)
            GestorSonidosUI.Instancia.audioSource.PlayOneShot(sonidoClickEspecifico);
        else if (GestorSonidosUI.Instancia != null)
            GestorSonidosUI.Instancia.ReproducirClick();
    }

    // 🖱️ --- RATÓN ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        MenuGamepadNavigator.usandoRaton = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        elPunteroEstaEncima = true;
        ActivarEfecto();
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
            if (imagenVisual != null) imagenVisual.color = colorFondoHover;
            if (texto != null) texto.color = colorTextoHover;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (MenuGamepadNavigator.usandoRaton)
        {
            if (elPunteroEstaEncima)
            {
                EjecutarSonidoClick();
                EjecutarEfectoSoltar();
            }
            else
            {
                ActualizarColoresPorEstado();
            }
        }
    }

    // 🎮 --- MANDO ---
    public void OnSelect(BaseEventData eventData)
    {
        if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Input.GetButton("Submit"))
        {
            MenuGamepadNavigator.usandoRaton = false;
        }

        if (MenuGamepadNavigator.usandoRaton) return;

        ActivarEfecto();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // 🛑 EL ARREGLO: Quitamos la condición que bloqueaba el apagado. 
        // Si el juego dice que nos deseleccionemos (sea por mando o por ratón), obedecemos.
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

        if (corrutinaActual != null) StopCoroutine(corrutinaActual);

        EjecutarSonidoHover();

        if (imagenVisual != null) imagenVisual.color = colorFondoHover;
        if (texto != null) texto.color = colorTextoHover;

        corrutinaActual = StartCoroutine(EfectoBalanceo(1f));
    }

    private void DesactivarEfecto()
    {
        if (elementoVisual == null) return;

        if (!estaSeleccionado)
        {
            if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
            if (texto != null) texto.color = colorTextoOriginal;
        }
    }

    private void EjecutarEfectoSoltar()
    {
        if (elementoVisual == null) return;

        if (corrutinaPop != null) StopCoroutine(corrutinaPop);
        corrutinaPop = StartCoroutine(EfectoPopEscala());

        if (mantenerColorAlClicar)
        {
            estaSeleccionado = !estaSeleccionado;
            ActualizarColoresPorEstado();
        }
    }

    private void ActualizarColoresPorEstado()
    {
        if (estaSeleccionado || elPunteroEstaEncima)
        {
            if (imagenVisual != null) imagenVisual.color = colorFondoHover;
            if (texto != null) texto.color = colorTextoHover;
        }
        else
        {
            if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
            if (texto != null) texto.color = colorTextoOriginal;
        }
    }

    public void ResetearSeleccion()
    {
        estaSeleccionado = false;
        elPunteroEstaEncima = false;
        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        if (corrutinaPop != null) StopCoroutine(corrutinaPop);

        if (imagenVisual != null) imagenVisual.color = colorFondoOriginal;
        if (texto != null) texto.color = colorTextoOriginal;
        if (elementoVisual != null) elementoVisual.localScale = escalaOriginal;
    }

    // --- CORRUTINAS ---

    IEnumerator EfectoPopEscala()
    {
        Vector3 escalaObjetivo = escalaOriginal * escalaPop;

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadPop;
            elementoVisual.localScale = Vector3.Lerp(escalaOriginal, escalaObjetivo, t);
            yield return null;
        }
        elementoVisual.localScale = escalaObjetivo;

        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadPop * 0.75f;
            elementoVisual.localScale = Vector3.Lerp(escalaObjetivo, escalaOriginal, t);
            yield return null;
        }
        elementoVisual.localScale = escalaOriginal;
        corrutinaPop = null;
    }

    IEnumerator EfectoBalanceo(float multiplicadorDireccion)
    {
        for (int i = 0; i < repeticiones; i++)
        {
            yield return StartCoroutine(GirarA(anguloShake * multiplicadorDireccion));
            yield return StartCoroutine(GirarA(-anguloShake * multiplicadorDireccion));
        }
        yield return StartCoroutine(GirarA(0));
        corrutinaActual = null;
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