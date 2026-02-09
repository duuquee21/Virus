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

            // Si ya está infectado, lógica de daño explosivo y destrucción (se mantiene igual)
            if (scriptInfeccion != null && scriptInfeccion.alreadyInfected)
            {
                InfectionFeedback.instance.PlayUltraEffect(collision.transform.position, Color.white);
                TakeDamage(damageAmount * 2);
                Destroy(collision.gameObject);
                return;
            }

            if (rb != null && scriptInfeccion != null)
            {
                float fuerzaImpacto = rb.linearVelocity.magnitude;

                // Solo actuamos si el impacto es suficientemente fuerte
                if (fuerzaImpacto > 6.5f)
                {
                    // --- LÓGICA DE DAÑO AL PLANETA (Se mantiene siempre) ---
                    TakeDamage(damageAmount);

                    // --- LÓGICA DE HABILIDAD: AVANCE DE FASE ---
                    // Solo si la habilidad está activa en el guardado, la persona baja de fase al chocar
                    if (Guardado.instance != null && Guardado.instance.paredInfectivaActiva)
                    {
                        scriptInfeccion.IntentarAvanzarFasePorChoque();
                        Debug.Log("<color=cyan>Habilidad Pared Activa: Persona baja de fase por impacto.</color>");
                    }
                    else
                    {
                        Debug.Log("Impacto detectado, pero no tienes la habilidad de 'Paredes Infectivas'.");
                    }
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
