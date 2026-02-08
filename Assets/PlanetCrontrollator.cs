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

            if (rb != null && scriptInfeccion != null)
            {
                float fuerzaImpacto = rb.linearVelocity.magnitude;

                if (fuerzaImpacto > 5f)
                {
                    // REGLA: Si la persona ya es fase máxima, NO quitamos vida
                    if (scriptInfeccion.EsFaseMaxima())
                    {
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

    void Die()
    {
        // Lógica de destrucción o Game Over
        Destroy(gameObject);
    }
}
