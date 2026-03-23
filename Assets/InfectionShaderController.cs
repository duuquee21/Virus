using UnityEngine;
using System.Collections;

public class InfectionShaderController : MonoBehaviour
{
    public static InfectionShaderController instance;

    [Header("Configuración del Shader")]
    public Material materialInfeccion;

    [Tooltip("El nombre de la NUEVA variable en tu shader (ej: _CustomTime)")]
    public string nombreVariableTiempo = "_CustomTime";

    [Header("Ajustes de Velocidad")]
    public float velocidadNormal = 1f;
    public float velocidadPico = 5f;
    public float tiempoRecuperacion = 1.5f;

    private float velocidadActual;
    private float tiempoAcumulado = 0f;
    private Coroutine rutinaRecuperacion;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Empezamos con la velocidad base
        velocidadActual = velocidadNormal;
    }

    private void Update()
    {
        // 1. Calculamos el tiempo de forma continua y sin saltos matemáticos
        tiempoAcumulado += Time.deltaTime * velocidadActual;

        // 2. Le pasamos este valor acumulado al shader en cada frame
        if (materialInfeccion != null)
        {
            materialInfeccion.SetFloat(nombreVariableTiempo, tiempoAcumulado);
        }
    }

    public void AcelerarShaderDeGolpe()
    {
        // Si ya está frenando, cancelamos ese frenado para meter un nuevo acelerón
        if (rutinaRecuperacion != null)
            StopCoroutine(rutinaRecuperacion);

        rutinaRecuperacion = StartCoroutine(FrenarPocoAPoco());
    }

    private IEnumerator FrenarPocoAPoco()
    {
        // Forzamos la velocidad al máximo de golpe
        velocidadActual = velocidadPico;
        float tiempoPasado = 0f;

        // Vamos reduciendo la velocidad poco a poco hasta la normal
        while (tiempoPasado < tiempoRecuperacion)
        {
            tiempoPasado += Time.deltaTime;
            velocidadActual = Mathf.Lerp(velocidadPico, velocidadNormal, tiempoPasado / tiempoRecuperacion);
            yield return null;
        }

        // Aseguramos que se estabiliza
        velocidadActual = velocidadNormal;
        rutinaRecuperacion = null;
    }
}