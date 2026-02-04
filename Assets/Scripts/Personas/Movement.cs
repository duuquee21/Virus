using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float waitTime = 1f;
    public float screenPadding = 0.5f;

    [Header("Límites de Oscilación Aleatoria")]
    public Vector2 rangoFrecuencia = new Vector2(3f, 6f);
    public Vector2 rangoMagnitud = new Vector2(0.05f, 0.2f);

    private float frecuenciaIndividual;
    private float magnitudIndividual;
    private float offsetFase; // Para que no todos oscilen sincronizados

    private Vector2 targetPosition;
    private float waitCounter;
    private bool isWalking;
    private Vector3 posSinOscilacion;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        posSinOscilacion = transform.position;

        // Asignamos valores únicos para esta instancia
        frecuenciaIndividual = Random.Range(rangoFrecuencia.x, rangoFrecuencia.y);
        magnitudIndividual = Random.Range(rangoMagnitud.x, rangoMagnitud.y);
        offsetFase = Random.Range(0f, 2f * Mathf.PI); // Un punto aleatorio en la onda seno

        PickNewTarget();
    }

    void Update()
    {
        // Calculamos la oscilación usando sus valores únicos
        // Sumamos offsetFase para desincronizarlo de los demás
        float seno = Mathf.Sin((Time.time * frecuenciaIndividual) + offsetFase);
        float offsetVisual = seno * magnitudIndividual;

        if (isWalking)
        {
            posSinOscilacion = Vector2.MoveTowards(
                posSinOscilacion,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector2.Distance(posSinOscilacion, targetPosition) < 0.1f)
            {
                isWalking = false;
                waitCounter = waitTime;
            }
        }
        else
        {
            waitCounter -= Time.deltaTime;
            if (waitCounter <= 0)
                PickNewTarget();
        }

        // Aplicamos la posición final (Base + Oscilación)
        transform.position = posSinOscilacion + new Vector3(0, offsetVisual, 0);
    }

    void PickNewTarget()
    {
        Vector2 minWorld = cam.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 maxWorld = cam.ViewportToWorldPoint(new Vector2(1, 1));

        minWorld += Vector2.one * screenPadding;
        maxWorld -= Vector2.one * screenPadding;

        float randomX = Random.Range(minWorld.x, maxWorld.x);
        float randomY = Random.Range(minWorld.y, maxWorld.y);

        targetPosition = new Vector2(randomX, randomY);
        isWalking = true;
    }
}