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

            if (scriptInfeccion != null)
            {
                // OBTENEMOS EL DAÑO SEGÚN LA FORMA (Círculo=5, Triángulo=4, etc.)
                // Esta función la añadimos en el script PersonaInfeccion abajo
                float dañoDeEstaFigura = scriptInfeccion.ObtenerDañoTotal();

                // CASO 1: YA ESTÁ INFECTADO (Explosión)
                if (scriptInfeccion.alreadyInfected)
                {
                    InfectionFeedback.instance.PlayUltraEffect(collision.transform.position, Color.white);

                    // Hace el doble de su daño base al explotar
                    TakeDamage(dañoDeEstaFigura * 2);

                    Destroy(collision.gameObject);
                    return;
                }

                // CASO 2: IMPACTO FÍSICO (Persona normal golpeando el planeta)
                if (rb != null)
                {
                    float fuerzaImpacto = rb.linearVelocity.magnitude;

                    if (fuerzaImpacto > 6.5f)
                    {
                        // Aplicamos el daño correspondiente a su forma
                        TakeDamage(dañoDeEstaFigura);

                        // Lógica de Paredes Infectivas (Bajar de fase al chocar)
                        if (Guardado.instance != null && Guardado.instance.paredInfectivaActiva)
                        {
                            scriptInfeccion.IntentarAvanzarFasePorChoque();
                        }
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
