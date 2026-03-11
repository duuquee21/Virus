using UnityEngine;
using System.Collections;

public class BlackHoleController : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject circuloPrefab;

    [Header("Configuración de Spawn")]
    public float radioDeAparicionAleatoria = 5f;

    [Header("Configuración de Escala")]
    public float radioInicial = 10f;
    public float radioFinal = 1f;
    public float tiempoReduccion = 2f;
    public float velocidadRotacion = 720f;

    [Header("Fuerzas y Atracción")]
    public float fuerzaAtraccion = 15f;
    // NUEVA VARIABLE: Independiente de la escala visual
    public float radioDeAtraccionEfectiva = 7f;
    public float radioDeInfeccionFinal = 5f;
    public float fuerzaExpansionInfeccion = 25f;

    [Header("Visuales de Explosión")]
    public float escalaExplosionMutiplicador = 3f;
    public float tiempoExplosionGrow = 0.4f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnBlackHole();
        }
    }

    public void SpawnBlackHole()
    {
        if (circuloPrefab == null) return;

        Vector2 desplazamientoAleatorio = Random.insideUnitCircle * radioDeAparicionAleatoria;
        Vector3 posicionSpawn = transform.position + new Vector3(desplazamientoAleatorio.x, desplazamientoAleatorio.y, 0);

        GameObject nuevoAgujero = Instantiate(circuloPrefab, posicionSpawn, Quaternion.identity);
        ParticleSystem ps = nuevoAgujero.GetComponentInChildren<ParticleSystem>();

        StartCoroutine(ExecuteBlackHoleSequence(nuevoAgujero, ps));
    }

    IEnumerator ExecuteBlackHoleSequence(GameObject objeto, ParticleSystem ps)
    {
        SpriteRenderer sr = objeto.GetComponent<SpriteRenderer>();
        // Ya no necesitamos estrictamente el collider para la atracción
        float elapsed = 0;

        if (ps != null) ps.Play();
        if (sr != null) { sr.sortingOrder = 32767; sr.color = new Color(0, 0, 0, 0); }

        bool emisionDetenida = false;

        while (elapsed < tiempoReduccion)
        {
            if (objeto == null) yield break;
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

            // LLAMADA MODIFICADA: Usamos la posición del objeto y el nuevo radio
            AtraerPersonas(objeto.transform.position);

            yield return null;
        }

        // --- FASE 2: FLASH ---
        sr.color = Color.white;
        objeto.transform.localScale = new Vector3(radioFinal * 1.2f, radioFinal * 1.2f, 1);
        yield return new WaitForSeconds(0.05f);
        sr.color = Color.black;

        // --- FASE 3: EXPLOSIÓN ---
        ExplotarEInfectar(objeto.transform.position);

        float elapsedExplosion = 0;
        Vector3 escalaPostFlash = objeto.transform.localScale;
        Vector3 escalaObjetivo = escalaPostFlash * escalaExplosionMutiplicador;

        while (elapsedExplosion < tiempoExplosionGrow)
        {
            if (objeto == null) yield break;
            elapsedExplosion += Time.deltaTime;
            float tEx = elapsedExplosion / tiempoExplosionGrow;
            objeto.transform.localScale = Vector3.Lerp(escalaPostFlash, escalaObjetivo, tEx);
            if (sr != null) sr.color = new Color(0, 0, 0, 1 - tEx);
            yield return null;
        }

        Destroy(objeto);
    }

    // NUEVA LÓGICA DE ATRACCIÓN
    // Dentro de AtraerPersonas en BlackHoleController.cs

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
                    // Activamos el estado para que Movement.cs no intente frenar la atracción
                    if (mov != null) mov.SetEstaEmpujado(true, rbPersona.linearVelocity.normalized);

                    Vector2 direccionHaciaCentro = (Vector2)centro - rbPersona.position;
                    float distancia = direccionHaciaCentro.magnitude;

                    // Aplicamos fuerza continua. 
                    // Usamos ForceMode2D.Force para que sea una aceleración constante 
                    // y permita que los rebotes cambien el vector de velocidad.
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

    // Opcional: Para ver el radio en el Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeAtraccionEfectiva);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioDeInfeccionFinal);
    }
}