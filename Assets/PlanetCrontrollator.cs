using UnityEngine;
using UnityEngine.UI;

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("Estad�sticas")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;

    void Start()
    {
        currentHealth = maxHealth; // Inicializamos con la vida m�xima
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

                // CASO 1: YA EST� INFECTADO (Explosi�n)
                if (scriptInfeccion.alreadyInfected)
                {
                    InfectionFeedback.instance.PlayUltraEffect(collision.transform.position, Color.white);

                    // Hace el doble de su daño base al explotar
                    TakeDamage(dañoCalculado);

                    Destroy(collision.gameObject);
                    return;
                }
         

                // CASO 2: IMPACTO F�SICO
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
                        if (Guardado.instance != null && !Guardado.instance.paredInfectivaActiva)
                        {
                            // CASO 3: CARAMBOLA NORMAL ACTIVA (Golpeando el planeta con una persona no infectada)
                            InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(collision.transform.position, Color.white);
                            
                        }
                    }
                    else
                    {
                        // Si no hace da�o, es porque la velocidad es baja
                        
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

        Debug.Log($"<color=red>Da�o recibido: {amount}. Vida restante: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ActualizarUI()
    {
        if (healthBar != null)
        {
            // Ahora la divisi�n siempre es correcta
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