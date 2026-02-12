using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;

    [Header("Muerte y Efectos")]
    public List<Transform> spawnPointsEfectos; // Asignar en Inspector
    public float intensidadVibracion = 0.1f;
    public AnimacionFinalNivel animacionFinalNivel;

    private bool isDead = false;
    private Dictionary<int, float> lastImpactTimes = new Dictionary<int, float>();
    private float cooldownTime = 0.1f;

    [Header("Configuración Fragmentos")]
    public GameObject prefabFragmentos;
    public Vector3 offsetFragmentos = Vector3.zero;
    public Vector3 escalaFragmentos = Vector3.one;

    private SpriteRenderer spriteRenderer;
    private Vector3 posicionOriginal;

    void Start()
    {
        currentHealth = maxHealth;
        posicionOriginal = transform.position;
        ActualizarUI();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Intentar obtener el componente si no está asignado
        if (animacionFinalNivel == null) animacionFinalNivel = GetComponent<AnimacionFinalNivel>();
    }

    private void ProcesarImpacto(GameObject obj, Vector3 posicion)
    {
        if (isDead) return;

        int id = obj.GetInstanceID();
        if (lastImpactTimes.ContainsKey(id) && Time.time < lastImpactTimes[id] + cooldownTime)
            return;

        lastImpactTimes[id] = Time.time;

        PersonaInfeccion scriptInfeccion = obj.GetComponent<PersonaInfeccion>();
        if (scriptInfeccion == null) return;

        float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();

        // Verificamos si este golpe matará al objeto
        bool seraGolpeLetal = (currentHealth - dañoCalculado) <= 0;

        if (scriptInfeccion.alreadyInfected)
        {
            InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);
            TakeDamage(dañoCalculado);
            Destroy(obj);
            return;
        }

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fuerzaImpacto = rb.linearVelocity.magnitude;
            if (fuerzaImpacto > 6.5f)
            {
                TakeDamage(dañoCalculado);

                // Solo ejecutamos el feedback de pared si NO ha muerto
                if (!seraGolpeLetal)
                {
                    if (Guardado.instance != null)
                    {
                        if (Guardado.instance.nivelParedInfectiva > scriptInfeccion.faseActual)
                            scriptInfeccion.IntentarAvanzarFasePorChoque();
                        else
                            InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(posicion, Color.white);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Persona")) ProcesarImpacto(collision.gameObject, collision.transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Persona")) ProcesarImpacto(collision.gameObject, collision.transform.position);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        ActualizarUI();

        if (currentHealth <= 0) Die();
    }

    void ActualizarUI()
    {
        if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // En lugar de destruir/desactivar todo ya, empezamos la secuencia de muerte
        StartCoroutine(SecuenciaMuerte());
    }

    private IEnumerator SecuenciaMuerte()
    {
        GetComponent<Collider2D>().enabled = false;
        float tiempoPasado = 0;
        float duracionMuerte = 4f;

        while (tiempoPasado < duracionMuerte)
        {
            transform.position = posicionOriginal + (Vector3)Random.insideUnitCircle * intensidadVibracion;
            if (spawnPointsEfectos != null && spawnPointsEfectos.Count > 0)
            {
                if (Random.value > 0.85f)
                {
                    Transform puntoAleatorio = spawnPointsEfectos[Random.Range(0, spawnPointsEfectos.Count)];
                    InfectionFeedback.instance.PlayPhaseChangeEffect(puntoAleatorio.position, Color.white);
                }
            }
            tiempoPasado += Time.deltaTime;
            yield return null;
        }

        transform.position = posicionOriginal;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // --- LÓGICA DE FRAGMENTOS MODIFICADA ---
        if (prefabFragmentos != null)
        {
            GameObject restos = Instantiate(prefabFragmentos, transform.position + offsetFragmentos, transform.rotation);
            restos.transform.localScale = escalaFragmentos;

            // Intentamos obtener el gestor del objeto instanciado
            GestorDeFragmentos gestor = restos.GetComponent<GestorDeFragmentos>();

            if (gestor != null)
            {
                // Nos suscribimos al evento: "Cuando termines, ejecuta FinalizarNivel"
                gestor.OnFragmentosAgotados += FinalizarNivel;
            }
            else
            {
                // Si por error el prefab no tiene el script, terminamos de una vez
                FinalizarNivel();
            }
        }
        else
        {
            // Si no hay fragmentos, la animación salta directo
            FinalizarNivel();
        }
    }

    // Método que será llamado por la señal de los fragmentos
    private void FinalizarNivel()
    {
        Debug.Log("Todos los fragmentos absorbidos. Iniciando animación final.");
        if (animacionFinalNivel != null)
        {
            animacionFinalNivel.Ejecutar();
        }
    }
}