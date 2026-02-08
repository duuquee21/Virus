using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlanetCrontrollator : MonoBehaviour
{
    public float health = 100f;
    public float damageAmount = 1;
    public Image healthBar; // Arrastra aquí el Fill de tu barra de vida
    



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Persona"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            PersonaInfeccion scriptInfeccion = collision.gameObject.GetComponent<PersonaInfeccion>();

            if (scriptInfeccion != null&&scriptInfeccion.alreadyInfected)
            {
                InfectionFeedback.instance.PlayEffect(collision.transform.position, Color.white,2);
                TakeDamage(damageAmount*2);
                Destroy(collision.gameObject);
                return;
            }

            if (rb != null && scriptInfeccion != null)
            {
                float fuerzaImpacto = rb.linearVelocity.magnitude;

                if (fuerzaImpacto > 6.5f)
                {
                    // REGLA: Si la persona ya es fase máxima, NO quitamos vida
                    if (scriptInfeccion.EsFaseMaxima())
                    {
                        TakeDamage(damageAmount);
                        Debug.Log("<color=green>Impacto de Fase Final: Planeta Protegido.</color>");
                    }
                    else
                    {
                        // Si no es fase máxima, recibe daño normal
                        TakeDamage(damageAmount);
                        Debug.Log("<color=red>Impacto Fuerte: Planeta Dañado.</color>" + damageAmount+ health);
                    }

                    // En ambos casos llamamos a la lógica de la persona para que se infecte o avance
                    scriptInfeccion.IntentarAvanzarFasePorChoque();
                }
            }
        }
    }
    void TakeDamage(float amount)
    {
        health -= amount;

        // 4. Actualizamos la interfaz visual (Health Bar)
        if (healthBar != null)
        {
            // Asumiendo que el fillAmount va de 0 a 1 y la vida inicial era 100
            healthBar.fillAmount = health / 100f;
        }

        Debug.Log("Vida restante: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    // --- ACTUALIZA ESTA FUNCIÓN EN PlanetCrontrollator.cs ---

    void Die()
    {
        // En lugar de destruir el planeta, avisamos al LevelManager
        if (LevelManager.instance != null)
        {
            LevelManager.instance.NextMapTransition();
        }

        // Desactivamos el script para que no se llame a Die() varias veces por choques extra
        this.enabled = false;

        // Opcional: Podrías desactivar el SpriteRenderer aquí para que "desaparezca" visualmente
        // GetComponent<SpriteRenderer>().enabled = false;
    }
}
