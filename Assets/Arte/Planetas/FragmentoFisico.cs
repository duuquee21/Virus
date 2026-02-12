using UnityEngine;
using System.Collections;

public class FragmentoHaciaAgujero : MonoBehaviour
{
    [Header("Ajustes de Explosión Inicial")]
    public float fuerzaExplosion = 5f;
    public float duracionExplosion = 0.3f;

    [Header("Ajustes de Atracción")]
    public Transform jugador;
    public float velocidadAtraccion = 10f;
    public float velocidadRotacion = 360f;
    public float distanciaDestruccion = 0.2f;

    private Vector3 escalaInicial;
    private float distanciaInicial;
    private bool puedeSeguirAlJugador = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        escalaInicial = transform.localScale;

        if (jugador == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Virus");
            if (playerObj != null) jugador = playerObj.transform;
        }

        // Iniciamos la fase de explosión hacia afuera
        StartCoroutine(FaseExplosion());
    }

    IEnumerator FaseExplosion()
    {
        if (rb != null)
        {
            rb.gravityScale = 0;

            // Calculamos dirección desde el centro de la pantalla hacia afuera
            Vector2 centroPantalla = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            Vector2 direccionHaciaFuera = ((Vector2)transform.position - centroPantalla).normalized;

            // Añadimos una variación aleatoria para que no sea una explosión perfecta
            direccionHaciaFuera = Quaternion.Euler(0, 0, Random.Range(-20f, 20f)) * direccionHaciaFuera;

            // Aplicamos el impulso inicial
            rb.AddForce(direccionHaciaFuera * fuerzaExplosion, ForceMode2D.Impulse);

            // Esperamos un momento mientras el fragmento "vuela" hacia afuera
            yield return new WaitForSeconds(duracionExplosion);

            // Frenamos el Rigidbody para que la succión sea limpia
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (jugador != null)
        {
            distanciaInicial = Vector2.Distance(transform.position, jugador.position);
            puedeSeguirAlJugador = true;
        }
    }

    void Update()
    {
        if (jugador == null || !puedeSeguirAlJugador) return;

        float distanciaActual = Vector2.Distance(transform.position, jugador.position);

        // 1. Mover hacia el jugador
        transform.position = Vector2.MoveTowards(transform.position, jugador.position, velocidadAtraccion * Time.deltaTime);

        // 2. Rotación
        transform.Rotate(Vector3.forward, velocidadRotacion * Time.deltaTime);

        // 3. Escala proporcional
        float porcentajeRestante = distanciaActual / distanciaInicial;
        transform.localScale = escalaInicial * Mathf.Clamp01(porcentajeRestante);

        // 4. Destrucción y aviso al jugador
        if (distanciaActual < distanciaDestruccion || transform.localScale.x <= 0.01f)
        {
            PlayerFeedBakcManager feedbackManager = jugador.GetComponent<PlayerFeedBakcManager>();
            if (feedbackManager != null)
            {
                feedbackManager.OnFragmentReached(transform.position);
            }

            Destroy(gameObject);
        }
    }
}