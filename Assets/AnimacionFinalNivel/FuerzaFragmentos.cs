using UnityEngine;
using System.Collections;

public class FuerzaFragmentos2D : MonoBehaviour
{
    [Header("Ajustes de Explosión")]
    public float fuerzaMinima = 10f;
    public float fuerzaMaxima = 18f;
    public float torqueAleatorio = 20f;

    [Header("Ajustes de Retorno")]
    public float tiempoEspera = 0.75f;
    public float velocidadRetorno = 5f;
    public float distanciaMinimaDestruccion = 0.2f;

    private Rigidbody2D rb;
    private Vector3 escalaOriginal;
    private Transform objetivo;
    private bool debeRegresar = false;
    private float distanciaInicialAlObjetivo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        escalaOriginal = transform.localScale;
    }

    void OnEnable()
    {
        Vector2 centroCamaraMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Explotar(centroCamaraMundo);
        StartCoroutine(EsperarYRegresar());
    }

    public void Explotar(Vector2 puntoOrigen)
    {
        if (rb != null)
        {
            debeRegresar = false;
            transform.localScale = escalaOriginal;
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Vector2 direccion = ((Vector2)transform.position - puntoOrigen).normalized;
            if (direccion == Vector2.zero)
                direccion = Random.insideUnitCircle.normalized;

            float intensidad = Random.Range(fuerzaMinima, fuerzaMaxima);
            rb.AddForce(direccion * intensidad, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-torqueAleatorio, torqueAleatorio), ForceMode2D.Impulse);
        }
    }

    IEnumerator EsperarYRegresar()
    {
        yield return new WaitForSeconds(tiempoEspera);

        // CAMBIO AQUÍ: Ahora busca el objeto con el tag "suelo"
        GameObject suelo = GameObject.FindGameObjectWithTag("SpawnArea");

        if (suelo != null)
        {
            objetivo = suelo.transform;
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            distanciaInicialAlObjetivo = Vector2.Distance(transform.position, objetivo.position);
            debeRegresar = true;
        }
    }

    void Update()
    {
        if (debeRegresar && objetivo != null)
        {
            // 1. Mover hacia el suelo
            transform.position = Vector2.MoveTowards(transform.position, objetivo.position, velocidadRetorno * Time.deltaTime);

            // 2. Reducción de escala por distancia
            float distanciaActual = Vector2.Distance(transform.position, objetivo.position);
            float t = distanciaActual / distanciaInicialAlObjetivo;
            transform.localScale = escalaOriginal * t;

            if (distanciaActual < distanciaMinimaDestruccion)
            {
                // 1. Intentar obtenerlo del objetivo directamente
                FeedbackAnimacion feedback = objetivo.GetComponent<FeedbackAnimacion>();

                // 2. Si no está ahí, buscarlo en la escena (solo como plan B)
                if (feedback == null)
                {
                    feedback = Object.FindFirstObjectByType<FeedbackAnimacion>();
                }

                if (feedback != null)
                {
                    feedback.EjecutarFeedback();
                }

                Destroy(gameObject);
            }
        }
    }

    public static class ContadorGlobal
    {
        public static int recibosTotales = 0;
    }
}