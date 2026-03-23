using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image fondo;
    private TextMeshProUGUI texto;
    private Quaternion rotacionOriginal;
    private Coroutine corrutinaActual;

    [Header("Colores")]
    public Color colorFondoHover = Color.black;
    public Color colorTextoHover = Color.green;

    [Header("Configuraci�n del Balanceo")]
    public float anguloShake = 5f;
    public float velocidadGiro = 40f;
    public int repeticiones = 1;

    void Awake()
    {
        fondo = GetComponent<Image>();
        texto = GetComponentInChildren<TextMeshProUGUI>();
        rotacionOriginal = transform.localRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Matar cualquier movimiento previo para evitar conflictos
        ResetearEstado();

        // 2. Aplicar colores de Hover
        fondo.color = colorFondoHover;
        texto.color = colorTextoHover;

        // 3. Iniciar el shake
        corrutinaActual = StartCoroutine(EfectoBalanceoSuave());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 1. Detener todo inmediatamente
        ResetearEstado();

        // 2. Forzar vuelta a la normalidad est�tica
        fondo.color = Color.white;
        texto.color = Color.black;
        transform.localRotation = rotacionOriginal;
    }

    private void ResetearEstado()
    {
        if (corrutinaActual != null)
        {
            StopCoroutine(corrutinaActual);
            corrutinaActual = null;
        }
    }

    IEnumerator EfectoBalanceoSuave()
    {
        for (int i = 0; i < repeticiones; i++)
        {
            yield return StartCoroutine(GirarA(anguloShake));
            yield return StartCoroutine(GirarA(-anguloShake));
        }
        // Volver al centro suavemente al terminar el ciclo
        yield return StartCoroutine(GirarA(0));
    }

    IEnumerator GirarA(float anguloTarget)
    {
        Quaternion destino = Quaternion.Euler(0, 0, anguloTarget);
    
        float tiempoSeguridad = 0; 
        // Cambiamos el bucle para que use tiempo real
        while (Quaternion.Angle(transform.localRotation, destino) > 0.01f && tiempoSeguridad < 0.5f)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, 
                destino, 
                // CAMBIO: Usamos unscaledDeltaTime
                Time.unscaledDeltaTime * velocidadGiro
            );
        
            // CAMBIO: También el contador de seguridad debe ser independiente
            tiempoSeguridad += Time.unscaledDeltaTime;
        
            // yield return null sigue funcionando porque se ejecuta cada frame físico, 
            // pero lo que pase dentro depende del reloj que elijas arriba.
            yield return null;
        }
        transform.localRotation = destino;
    }
}