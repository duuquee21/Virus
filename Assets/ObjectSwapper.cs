using UnityEngine;

public class ObjectSwapper : MonoBehaviour
{
    [Header("Objetos")]
    public GameObject[] grupoA;
    public GameObject[] grupoB;

    [Header("Física del Giro")]
    public float aceleracion = 800f;    // Cuánto aumenta la velocidad
    public float frenado = 600f;       // Cuánto disminuye tras el cambio
    public float velocidadMaxima = 2000f;

    private float velocidadActual = 0f;
    private bool intercambiado = false;
    private bool detenido = false;

    void Start()
    {
        ActualizarVisibilidad(true);
    }

    void Update()
    {
        if (detenido) return; // Si ya se paró, no hace nada más

        // --- LÓGICA DE VELOCIDAD ---
        if (!intercambiado)
        {
            // FASE 1: Acelerar hasta el máximo
            velocidadActual += aceleracion * Time.deltaTime;

            if (velocidadActual >= velocidadMaxima)
            {
                RealizarIntercambio();
            }
        }
        else
        {
            // FASE 2: Frenar hasta llegar a cero
            velocidadActual -= frenado * Time.deltaTime;

            if (velocidadActual <= 0)
            {
                velocidadActual = 0;
                detenido = true;
                Debug.Log("Transformación completada.");
            }
        }

        // --- APLICAR ROTACIÓN ---
        transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
    }

    void RealizarIntercambio()
    {
        ActualizarVisibilidad(false); // Apaga A, enciende B
        intercambiado = true;
        // Forzamos que la velocidad empiece desde el máximo para el frenado
        velocidadActual = velocidadMaxima;
        Debug.Log("¡Cambiando objetos!");
    }

    void ActualizarVisibilidad(bool mostrarA)
    {
        foreach (GameObject g in grupoA) if (g) g.SetActive(mostrarA);
        foreach (GameObject g in grupoB) if (g) g.SetActive(!mostrarA);
    }
}