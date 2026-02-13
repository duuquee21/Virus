using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Necesario para el Dictionary

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;
    public bool nivelFinal = false;

    // SISTEMA DE SEGURIDAD: Guarda el tiempo del último impacto por cada objeto
    private Dictionary<int, float> lastImpactTimes = new Dictionary<int, float>();
    private float cooldownTime = 0.1f;

    private AnimacionFinalPlaneta animacionFinalPlaneta;

    [Header("Estado")]
    public bool isInvulnerable = false; // Nueva variable


    void Start()
    {
        currentHealth = maxHealth;
        ActualizarUI();
        animacionFinalPlaneta = GetComponent<AnimacionFinalPlaneta>();
    }

    // Método centralizado para procesar el impacto y evitar repetición
    private void ProcesarImpacto(GameObject obj, Vector3 posicion)
    {
        int id = obj.GetInstanceID();

        if (lastImpactTimes.ContainsKey(id) && Time.time < lastImpactTimes[id] + cooldownTime)
            return;

        lastImpactTimes[id] = Time.time;

        PersonaInfeccion scriptInfeccion = obj.GetComponent<PersonaInfeccion>();
        if (scriptInfeccion == null) return;

        float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();

        // CASO 1: YA ESTÁ INFECTADO (Explosión)
        if (scriptInfeccion.alreadyInfected)
        {
            InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);
            TakeDamage(dañoCalculado);
            Destroy(obj);
            return;
        }

        // CASO 2: IMPACTO FÍSICO
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fuerzaImpacto = rb.linearVelocity.magnitude;
            if (fuerzaImpacto > 6.5f)
            {
                TakeDamage(dañoCalculado);

                if (Guardado.instance != null)
                {
                    if (Guardado.instance.nivelParedInfectiva > scriptInfeccion.faseActual)
                        scriptInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Wall);
                    else
                        InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(posicion, Color.white);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Persona"))
        {
            ProcesarImpacto(collision.gameObject, collision.transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Persona"))
        {
            ProcesarImpacto(collision.gameObject, collision.transform.position);
        }
    }

    // Limpieza opcional: Para evitar que el diccionario crezca infinitamente
    private void Update()
    {
        // Cada cierto tiempo podrías limpiar IDs antiguos, 
        // aunque para un juego pequeño no es crítico.
    }
    public void TakeDamage(float amount)
    {
        // Si es invulnerable, ignoramos el daño por completo
        if (isInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        ActualizarUI();
        Debug.Log($"<color=red>Daño recibido: {amount}. Vida restante: {currentHealth}</color>");
        if (currentHealth <= 0) Die();
    }

    void ActualizarUI()
    {
        if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;
    }

    public void ResetHealthToInitial()
    {
        currentHealth = maxHealth;
        lastImpactTimes.Clear(); // limpiamos el sistema de seguridad
        this.enabled = true;     // por si se había desactivado al morir
        ActualizarUI();

        Debug.Log("<color=green>Planeta reseteado a vida máxima</color>");
    }

    void Die()
    {
        if (nivelFinal)
        {
            animacionFinalPlaneta.EjecutarSecuenciaVibracion();
        }
        else
        {
           LevelManager.instance.NextMapTransition();
        }
    }
}