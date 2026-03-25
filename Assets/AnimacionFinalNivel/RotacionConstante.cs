using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class HexagonoInteractivoFinal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuración de Rotación (Hexágono)")]
    public Vector3 velocidadRotacionMax = new Vector3(0, 0, 100);
    [Range(0.1f, 20f)] public float suavizadoRegresoRotacion = 10f;
    [Range(0.1f, 10f)] public float suavizadoFrenadoRotacion = 2f;

    [Header("Configuración de Posición (Hexágono)")]
    public Vector2 offsetPosicion = new Vector2(0f, 20f);
    [Range(0.1f, 20f)] public float suavizadoMovimiento = 5f;

    [Header("Configuración del Pop (Click)")]
    public float escalaPop = 1.15f;
    public float velocidadPop = 15f;

    [Header("Configuración del Balanceo (SOLO TEXTO)")]
    public TextMeshProUGUI textoOpcional;
    public float anguloShakeTexto = 10f;
    public float velocidadGiroShakeTexto = 60f;
    public int repeticionesShakeTexto = 1;

    [Header("Configuración Visual")]
    public Sprite spriteNormal;
    public Sprite spriteAlPasarRaton;
    public Color colorNormal = Color.white;
    public Color colorAlPasarRaton = Color.yellow;

    [Space]
    public Color colorTextoNormal = Color.white;
    public Color colorTextoHover = Color.yellow;

    [Header("Configuración de Audio")]
    public AudioClip sonidoHover;
    public AudioClip sonidoClick;

    private Vector3 posicionOriginal;
    private Vector3 escalaOriginal;
    private float anguloObjetivoZ;
    private Vector3 posicionObjetivo;
    private Vector3 velocidadActualRotacion;
    private Quaternion rotacionOriginalTexto;

    private bool estaEncima = false;
    private Image uiImage;
    private AudioSource audioSource;

    private Coroutine corrutinaPop;
    private Coroutine corrutinaBalanceoTexto;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        escalaOriginal = transform.localScale;

        if (textoOpcional != null)
            rotacionOriginalTexto = textoOpcional.transform.localRotation;
    }

    void Start()
    {
        posicionOriginal = transform.localPosition;
        posicionObjetivo = posicionOriginal;
        velocidadActualRotacion = velocidadRotacionMax;
        ActualizarVisuales(spriteNormal, colorNormal, colorTextoNormal);
    }

    void OnDisable()
    {
        PararYResetearTodo();
    }

    void Update()
    {
        if (estaEncima)
        {
            float zActual = Mathf.LerpAngle(transform.localEulerAngles.z, anguloObjetivoZ, Time.deltaTime * suavizadoRegresoRotacion);
            transform.localRotation = Quaternion.Euler(0, 0, zActual);
            velocidadActualRotacion = Vector3.Lerp(velocidadActualRotacion, Vector3.zero, Time.deltaTime * suavizadoFrenadoRotacion);
        }
        else
        {
            velocidadActualRotacion = Vector3.Lerp(velocidadActualRotacion, velocidadRotacionMax, Time.deltaTime * suavizadoFrenadoRotacion);
            transform.Rotate(velocidadActualRotacion * Time.deltaTime);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, posicionObjetivo, Time.deltaTime * suavizadoMovimiento);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        estaEncima = true;
        posicionObjetivo = posicionOriginal + (Vector3)offsetPosicion;

        CalcularSiguienteLado();
        ActualizarVisuales(spriteAlPasarRaton, colorAlPasarRaton, colorTextoHover);
        ReproducirSonido(sonidoHover);

        if (textoOpcional != null)
        {
            // Si ya hay una animación corriendo, no hacemos nada (así permitimos que termine la anterior)
            // O podrías usar StopCoroutine si prefieres que se reinicie al entrar de nuevo.
            if (corrutinaBalanceoTexto == null)
            {
                corrutinaBalanceoTexto = StartCoroutine(EfectoBalanceoTexto());
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        estaEncima = false;
        posicionObjetivo = posicionOriginal;
        ActualizarVisuales(spriteNormal, colorNormal, colorTextoNormal);

        // NO reseteamos aquí para que la animación que ya empezó pueda terminar su ciclo sola.
    }

    private void ResetearRotacionTextoInmediato()
    {
        if (corrutinaBalanceoTexto != null)
        {
            StopCoroutine(corrutinaBalanceoTexto);
            corrutinaBalanceoTexto = null;
        }
        if (textoOpcional != null)
        {
            textoOpcional.transform.localRotation = rotacionOriginalTexto;
        }
    }

    private void PararYResetearTodo()
    {
        estaEncima = false;
        ResetearRotacionTextoInmediato();

        if (corrutinaPop != null)
        {
            StopCoroutine(corrutinaPop);
            corrutinaPop = null;
        }
        transform.localScale = escalaOriginal;
        transform.localPosition = posicionOriginal;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ReproducirSonido(sonidoClick);
        if (corrutinaPop != null) StopCoroutine(corrutinaPop);
        corrutinaPop = StartCoroutine(EfectoPopEscala());
    }

    IEnumerator EfectoBalanceoTexto()
    {
        // Se ejecuta el número exacto de veces configurado
        for (int i = 0; i < repeticionesShakeTexto; i++)
        {
            yield return StartCoroutine(GirarTextoA(anguloShakeTexto));
            yield return StartCoroutine(GirarTextoA(-anguloShakeTexto));
        }

        // REGRESO FINAL GARANTIZADO AL CENTRO (siempre se ejecuta tras el bucle)
        yield return StartCoroutine(GirarTextoA(0));

        corrutinaBalanceoTexto = null;
    }

    IEnumerator GirarTextoA(float anguloTarget)
    {
        if (textoOpcional == null) yield break;

        Quaternion destino = rotacionOriginalTexto * Quaternion.Euler(0, 0, anguloTarget);
        float tiempoLimite = 0.5f;
        float tiempoTranscurrido = 0;

        while (Quaternion.Angle(textoOpcional.transform.localRotation, destino) > 0.05f && tiempoTranscurrido < tiempoLimite)
        {
            textoOpcional.transform.localRotation = Quaternion.Slerp(
                textoOpcional.transform.localRotation,
                destino,
                Time.unscaledDeltaTime * velocidadGiroShakeTexto
            );
            tiempoTranscurrido += Time.unscaledDeltaTime;
            yield return null;
        }

        textoOpcional.transform.localRotation = destino;
    }

    private void CalcularSiguienteLado()
    {
        float zActual = transform.localEulerAngles.z;
        if (velocidadRotacionMax.z >= 0)
        {
            anguloObjetivoZ = Mathf.Ceil(zActual / 60f) * 60f;
            if (Mathf.Approximately(zActual, anguloObjetivoZ)) anguloObjetivoZ += 60f;
        }
        else
        {
            anguloObjetivoZ = Mathf.Floor(zActual / 60f) * 60f;
            if (Mathf.Approximately(zActual, anguloObjetivoZ)) anguloObjetivoZ -= 60f;
        }
    }

    IEnumerator EfectoPopEscala()
    {
        Vector3 escalaObjetivo = escalaOriginal * escalaPop;
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadPop;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaObjetivo, t);
            yield return null;
        }
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadPop * 0.8f;
            transform.localScale = Vector3.Lerp(escalaObjetivo, escalaOriginal, t);
            yield return null;
        }
        transform.localScale = escalaOriginal;
        corrutinaPop = null;
    }

    private void ActualizarVisuales(Sprite nuevoSprite, Color nuevoColor, Color nuevoColorTexto)
    {
        if (uiImage != null)
        {
            if (nuevoSprite != null) uiImage.sprite = nuevoSprite;
            uiImage.color = nuevoColor;
        }
        if (textoOpcional != null) textoOpcional.color = nuevoColorTexto;
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}