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

    // SISTEMA DE SEGURIDAD: Guarda el tiempo del último impacto por cada objeto
    private Dictionary<int, float> lastImpactTimes = new Dictionary<int, float>();
    private float cooldownTime = 0.1f;

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarUI();
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

    void Die()
    {
        if (LevelManager.instance != null) LevelManager.instance.NextMapTransition();
        this.enabled = false;
    }
}