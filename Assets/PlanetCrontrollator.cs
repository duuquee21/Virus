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
        currentHealth = maxHealth;
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
                // Obtenemos el daño según la forma
                float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();

                // CASO 1: YA ESTÁ INFECTADO (Explosión al chocar)
                if (scriptInfeccion.alreadyInfected)
                {
                    InfectionFeedback.instance.PlayUltraEffect(collision.transform.position, Color.white);
                    TakeDamage(dañoCalculado);
                    Destroy(collision.gameObject);
                    return;
                }

                // CASO 2: IMPACTO FÍSICO (Forma no infectada)
                if (rb != null)
                {
                    // Usamos linearVelocity para versiones modernas de Unity, o velocity para antiguas
                    float fuerzaImpacto = rb.linearVelocity.magnitude;

                    if (fuerzaImpacto > 6.5f)
                    {
                        // El planeta siempre recibe daño si el golpe es fuerte
                        TakeDamage(dañoCalculado);

                        // LÓGICA DE PARED INFECTIVA POR FASES
                        if (Guardado.instance != null && Guardado.instance.paredInfectivaActiva)
                        {
                            int nivelPared = Guardado.instance.nivelParedInfectiva;

                            // IMPORTANTE: Asegúrate que en PersonaInfeccion la variable se llame 'faseActual'
                            // Hexágono = 0, Pentágono = 1, Cuadrado = 2, Triángulo = 3, Círculo = 4
                            int faseForma = scriptInfeccion.faseActual;

                            // Si nivel es 1, infecta fase 0. Si nivel es 2, infecta 0 y 1...
                            if (nivelPared > faseForma)
                            {
                                Debug.Log($"<color=green>[Pared]</color> Nivel {nivelPared} INFECTA a Fase {faseForma}");
                                scriptInfeccion.IntentarAvanzarFasePorChoque();
                            }
                            else
                            {
                                // El nivel de la pared es muy bajo para esta forma
                                Debug.Log($"<color=orange>[Pared]</color> Nivel {nivelPared} es insuficiente para Fase {faseForma}");
                                InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(collision.transform.position, Color.white);
                            }
                        }
                        else
                        {
                            // Si la habilidad no está comprada, solo hace efecto visual de chispa
                            InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(collision.transform.position, Color.white);
                        }
                    }
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        ActualizarUI();

        Debug.Log($"<color=red>Daño al Planeta: {amount}. Vida: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ActualizarUI()
    {
        if (healthBar != null)
        {
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