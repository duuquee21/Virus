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

            if (rb != null)
            {
                // 1. Detectamos el impacto fuerte para el daño de la pared
                float fuerzaImpacto = rb.linearVelocity.magnitude;

                if (fuerzaImpacto > 5f)
                {
                    Debug.Log($"<color=cyan>Impacto fuerte en pared:</color> <b>{fuerzaImpacto:F2}</b>");
                    TakeDamage(damageAmount);
                }

                // NOTA: Hemos eliminado el "rb.linearVelocity = Vector2.zero" 
                // para permitir que la Persona rebote y no se quede pegada.
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
