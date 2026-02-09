using UnityEngine;
using UnityEngine.UI;

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;

    void Start()
    {
        currentHealth = maxHealth; // Inicializamos con la vida máxima
        ActualizarUI();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Persona"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            PersonaInfeccion scriptInfeccion = collision.gameObject.GetComponent<PersonaInfeccion>();

            if (scriptInfeccion != null)
            {
                // Obtenemos el daño (Círculo 5, Triángulo 4, etc.)
                float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();

                // CASO 1: YA ESTÁ INFECTADO (Explosión)
                if (scriptInfeccion.alreadyInfected)
                {
                    InfectionFeedback.instance.PlayUltraEffect(collision.transform.position, Color.white);
                    TakeDamage(dañoCalculado * 2);
                    Destroy(collision.gameObject);
                    return;
                }

                // CASO 2: IMPACTO FÍSICO
                if (rb != null)
                {
                    // IMPORTANTE: Debug para ver si la fuerza es suficiente
                    float fuerzaImpacto = rb.linearVelocity.magnitude;

                    if (fuerzaImpacto > 6.5f)
                    {
                        TakeDamage(dañoCalculado);

                        if (Guardado.instance != null && Guardado.instance.paredInfectivaActiva)
                        {
                            scriptInfeccion.IntentarAvanzarFasePorChoque();
                        }
                    }
                    else
                    {
                        // Si no hace daño, es porque la velocidad es baja
                        
                    }
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Evita vida negativa

        ActualizarUI();

        Debug.Log($"<color=red>Daño recibido: {amount}. Vida restante: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ActualizarUI()
    {
        if (healthBar != null)
        {
            // Ahora la división siempre es correcta
            healthBar.fillAmount = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        if (LevelManager.instance != null)
        {
            LevelManager.instance.NextMapTransition();
        }
        this.enabled = false;
    }
}