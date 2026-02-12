using UnityEngine;
using System.Collections.Generic;

public class ManagerAnimacionJugador : MonoBehaviour
{
    public GameObject radioInfeccion;
    public ParticleSystem sistemaParticulasGravedad; // Arrastra aquí tu Particle System
    public bool playable = true;

    // --- VARIABLES EFECTO JUGADOR ---
    private Rigidbody2D rb;
    private bool efectoIniciado = false;
    private Vector3 posicionInicialEfecto;
    private Vector3 escalaInicialEfecto;
    private float tiempoEfecto = 0f;
    public float duracionAbsorcion = 1.5f;

    // --- VARIABLES PARA CORALES ---
    private List<Transform> coralesEnEscena = new List<Transform>();
    private List<Vector3> posInicialesCorales = new List<Vector3>();
    private List<Vector3> escalaInicialesCorales = new List<Vector3>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Asegurarnos de que las partículas empiecen apagadas
        if (sistemaParticulasGravedad != null)
            sistemaParticulasGravedad.Stop();
    }

    void Update()
    {
        if (!playable)
        {
            EjecutarEfectoAbsorcion();
        }
        else
        {
            if (efectoIniciado) DetenerEfectoParticulas();
            efectoIniciado = false;
            tiempoEfecto = 0f;
        }
    }

    private void OnEnable()
    {
        // Reseteamos la escala a la original de juego (0.4)
        transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        // Reseteamos variables de control
        efectoIniciado = false;
        tiempoEfecto = 0f;
        playable = true;
        radioInfeccion.SetActive(true);

        // Aseguramos que el Rigidbody vuelva a funcionar
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.isKinematic = false; }
    }

    private void EjecutarEfectoAbsorcion()
    {
        if (!efectoIniciado)
        {
            efectoIniciado = true;
            posicionInicialEfecto = transform.position;
            escalaInicialEfecto = transform.localScale;

            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }

            // Activar partículas de gravedad
            if (sistemaParticulasGravedad != null)
                sistemaParticulasGravedad.Play();

            // Buscar y configurar Corales
            coralesEnEscena.Clear();
            posInicialesCorales.Clear();
            escalaInicialesCorales.Clear();

            GameObject[] objetosCoral = GameObject.FindGameObjectsWithTag("Coral");
            foreach (GameObject coral in objetosCoral)
            {
                coralesEnEscena.Add(coral.transform);
                posInicialesCorales.Add(coral.transform.position);
                escalaInicialesCorales.Add(coral.transform.localScale);

                Rigidbody2D rbCoral = coral.GetComponent<Rigidbody2D>();
                if (rbCoral != null) { rbCoral.linearVelocity = Vector2.zero; rbCoral.isKinematic = true; }
            }
        }

        tiempoEfecto += Time.deltaTime;
        float progreso = Mathf.Clamp01(tiempoEfecto / duracionAbsorcion);

        // Centro de la pantalla en coordenadas de mundo
        Vector3 centroMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        centroMundo.z = 0;

        // 3. Mover/Encoger Jugador
        transform.position = Vector3.Lerp(posicionInicialEfecto, centroMundo, progreso);
        transform.localScale = Vector3.Lerp(escalaInicialEfecto, Vector3.zero, progreso);

        // 4. Mover/Encoger Corales
        for (int i = 0; i < coralesEnEscena.Count; i++)
        {
            if (coralesEnEscena[i] != null)
            {
                coralesEnEscena[i].position = Vector3.Lerp(posInicialesCorales[i], centroMundo, progreso);
                coralesEnEscena[i].localScale = Vector3.Lerp(escalaInicialesCorales[i], Vector3.zero, progreso);

                if (progreso >= 1f) coralesEnEscena[i].gameObject.SetActive(false);
            }
        }

        if (progreso >= 1f)
        {
            DetenerEfectoParticulas();
            gameObject.SetActive(false);
        }
    }

    private void DetenerEfectoParticulas()
    {
        if (sistemaParticulasGravedad != null)
            sistemaParticulasGravedad.Stop();
    }

    public void ComienzoAnimacion()
    {
        playable = false;
        if (radioInfeccion != null) radioInfeccion.SetActive(false);
    }

    public void FinAnimacion()
    {
        playable = true;
        if (radioInfeccion != null) radioInfeccion.SetActive(true);
    }

    public void ResetearEstado()
    {
        // 1. Detener corrutinas o procesos de movimiento si los hubiera
        efectoIniciado = false;
        tiempoEfecto = 0f;
        playable = true;

        // 2. Reactivar el objeto y resetear transformación
        gameObject.SetActive(true);
        transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        // 3. Resetear Física
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
        }

        // 4. Detener partículas
        DetenerEfectoParticulas();

        // 5. Limpiar listas de corales para el siguiente mapa
        coralesEnEscena.Clear();
        posInicialesCorales.Clear();
        escalaInicialesCorales.Clear();

        Debug.Log("<color=green>ManagerAnimacionJugador: Estado Reiniciado</color>");
    }
}