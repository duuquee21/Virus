using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleController : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject circuloPrefab;

    [Header("Audio")]
    public AudioClip blackHoleSFX;
    [Range(0f, 1f)] public float volumenSFX = 1f;
    private AudioSource sfxSource; // Referencia al componente de audio

    [Header("Configuración de Spawn Automático")]
    public float frecuenciaSpawn = 3.0f;
    private float nextSpawnTime;

    private int agujerosActivos = 0;

    [Header("Configuración de Spawn Posición")]
    public float radioDeAparicionAleatoria = 5f;

    [Header("Configuración de Escala")]
    public float radioInicial = 10f;
    public float radioFinal = 1f;
    public float tiempoReduccion = 2f;
    public float velocidadRotacion = 720f;

    [Header("Fuerzas y Atracción")]
    public float fuerzaAtraccion = 15f;
    public float radioDeAtraccionEfectiva = 7f;
    public float radioDeInfeccionFinal = 5f;
    public float fuerzaExpansionInfeccion = 25f;

    [Header("Visuales de Explosión")]
    public float escalaExplosionMutiplicador = 3f;
    public float tiempoExplosionGrow = 0.4f;

    private List<GameObject> agujerosInstanciados = new List<GameObject>();

    void Start()
    {
        // Busca el objeto por nombre y extrae su AudioSource
        GameObject sourceObj = GameObject.Find("SFXSource");
        if (sourceObj != null)
        {
            sfxSource = sourceObj.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'SFXSource' en la escena.");
        }
    }

    void Update()
    {
        if (Guardado.instance.agujeroNegroData &&
            Time.time > nextSpawnTime &&
            LevelManager.instance.isGameActive)
        {
            while (agujerosActivos < Guardado.instance.cantidadMaxAgujeros)
            {
                SpawnBlackHole();
            }

            nextSpawnTime = Time.time + Guardado.instance.agujeroSpawnRate;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (agujerosActivos < Guardado.instance.cantidadMaxAgujeros)
            {
                SpawnBlackHole();
            }
        }
    }

    private void OnDisable()
    {
        ClearActiveEffects();
    }

    public void SpawnBlackHole()
    {
        if (circuloPrefab == null) return;

        agujerosActivos++;

        // --- REPRODUCCIÓN DE AUDIO ---
        if (sfxSource != null && blackHoleSFX != null)
        {
            sfxSource.PlayOneShot(blackHoleSFX, volumenSFX);
        }

        Vector2 desplazamientoAleatorio = Random.insideUnitCircle * radioDeAparicionAleatoria;
        Vector3 posicionSpawn = transform.position + new Vector3(desplazamientoAleatorio.x, desplazamientoAleatorio.y, 0);
        GameObject nuevoAgujero = Instantiate(circuloPrefab, posicionSpawn, Quaternion.identity, transform);
        agujerosInstanciados.Add(nuevoAgujero);

        ParticleSystem ps = nuevoAgujero.GetComponentInChildren<ParticleSystem>();
        StartCoroutine(ExecuteBlackHoleSequence(nuevoAgujero, ps));
    }

    IEnumerator ExecuteBlackHoleSequence(GameObject objeto, ParticleSystem ps)
    {
        if (objeto == null) yield break;

        SpriteRenderer sr = objeto.GetComponent<SpriteRenderer>();
        float elapsed = 0;

        if (ps != null) ps.Play();

        bool emisionDetenida = false;

        while (elapsed < tiempoReduccion)
        {
            if (objeto == null)
            {
                DecrementarContador();
                yield break;
            }

            elapsed += Time.deltaTime;

            if (!emisionDetenida && ps != null && (tiempoReduccion - elapsed) <= 0.4f)
            {
                ps.Stop();
                emisionDetenida = true;
            }

            float t = elapsed / tiempoReduccion;
            float currentScale = Mathf.Lerp(radioInicial, radioFinal, t);

            objeto.transform.localScale = new Vector3(currentScale, currentScale, 1);
            if (sr != null) sr.color = new Color(0, 0, 0, t);
            objeto.transform.Rotate(0, 0, velocidadRotacion * Time.deltaTime);

            AtraerPersonas(objeto.transform.position);

            yield return null;
        }

        if (objeto == null) { DecrementarContador(); yield break; }

        if (sr != null)
        {
            sr.color = Color.white;
            objeto.transform.localScale = new Vector3(radioFinal * 1.2f, radioFinal * 1.2f, 1);
        }

        yield return new WaitForSeconds(0.05f);

        if (objeto == null) { DecrementarContador(); yield break; }

        if (sr != null) sr.color = Color.black;

        ExplotarEInfectar(objeto.transform.position);

        float elapsedExplosion = 0;
        Vector3 escalaPostFlash = objeto.transform.localScale;
        Vector3 escalaObjetivo = escalaPostFlash * escalaExplosionMutiplicador;

        while (elapsedExplosion < tiempoExplosionGrow)
        {
            if (objeto == null)
            {
                DecrementarContador();
                yield break;
            }

            elapsedExplosion += Time.deltaTime;
            float tEx = elapsedExplosion / tiempoExplosionGrow;
            objeto.transform.localScale = Vector3.Lerp(escalaPostFlash, escalaObjetivo, tEx);
            if (sr != null) sr.color = new Color(0, 0, 0, 1 - tEx);
            yield return null;
        }

        if (objeto != null)
        {
            agujerosInstanciados.Remove(objeto);
            Destroy(objeto);
        }
        DecrementarContador();
    }

    private void DecrementarContador()
    {
        agujerosActivos--;
        if (agujerosActivos < 0) agujerosActivos = 0;
    }

    void AtraerPersonas(Vector3 centro)
    {
        Collider2D[] personas = Physics2D.OverlapCircleAll(centro, radioDeAtraccionEfectiva);
        foreach (var col in personas)
        {
            if (col.CompareTag("Persona"))
            {
                Rigidbody2D rbPersona = col.GetComponent<Rigidbody2D>();
                Movement mov = col.GetComponent<Movement>();
                if (rbPersona != null)
                {
                    if (mov != null) mov.SetEstaEmpujado(true, rbPersona.linearVelocity.normalized);
                    Vector2 direccionHaciaCentro = (Vector2)centro - rbPersona.position;
                    float distancia = direccionHaciaCentro.magnitude;
                    float intensidad = fuerzaAtraccion / (distancia + 0.8f);
                    rbPersona.AddForce(direccionHaciaCentro.normalized * intensidad * 10f, ForceMode2D.Force);
                }
            }
        }
    }

    void ExplotarEInfectar(Vector3 centro)
    {
        Collider2D[] afectados = Physics2D.OverlapCircleAll(centro, radioDeInfeccionFinal);
        foreach (var col in afectados)
        {
            if (col.CompareTag("Persona"))
            {
                PersonaInfeccion pInfeccion = col.GetComponent<PersonaInfeccion>();
                if (pInfeccion != null)
                {
                    pInfeccion.SetInfector(this.transform);
                    pInfeccion.IntentarAvanzarFase();
                }
            }
        }
    }

    public void ClearActiveEffects()
    {
        StopAllCoroutines();

        for (int i = agujerosInstanciados.Count - 1; i >= 0; i--)
        {
            if (agujerosInstanciados[i] != null)
            {
                agujerosInstanciados[i].SetActive(false);
                Destroy(agujerosInstanciados[i]);
            }
        }

        agujerosInstanciados.Clear();
        agujerosActivos = 0;
        nextSpawnTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeAtraccionEfectiva);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioDeInfeccionFinal);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeAparicionAleatoria);
    }
}