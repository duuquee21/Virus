using UnityEngine;
using System.Collections;

public class DetectorMortal : MonoBehaviour
{
    [Header("Ajustes de Destrucción")]
    public float tiempoEsperaCoral = 0.2f; // Segundos antes de que el padre desaparezca

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Persona"))
        {
            PersonaInfeccion persona = other.GetComponent<PersonaInfeccion>();

            if (persona != null)
            {
                // Verificamos si es Fase 5 o está infectado (Círculo final)
                if (persona.faseActual >= 5 || persona.alreadyInfected)
                {
                    Debug.Log("<color=cyan>[IMPACTO]</color> Fase 5 detectada. Iniciando secuencia de destrucción.");

                    // Iniciamos la cuenta atrás para el padre y el coral
                    StartCoroutine(SecuenciaDestruccionPadre(transform.parent != null ? transform.parent.gameObject : gameObject));

                    // El proyectil (persona) se destruye al impactar
                    Destroy(other.gameObject);
                    return;
                }

                // Lógica normal para fases menores (0-4)
                persona.SendMessage("Desaparecer", SendMessageOptions.DontRequireReceiver);
                Destroy(other.gameObject);
            }
        }
    }

    private IEnumerator SecuenciaDestruccionPadre(GameObject objetoADestruir)
    {
        // 1. (Opcional) Feedback visual: cambiar color a rojo transparente o parpadeo
        SpriteRenderer[] renderers = objetoADestruir.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers) sr.color = new Color(1, 0, 0, 0.5f);

        // 2. Desactivamos el collider para que no bloquee a nadie mientras "muere"
        Collider2D col = objetoADestruir.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. Esperamos el tiempo que hayas puesto en el inspector
        yield return new WaitForSeconds(tiempoEsperaCoral);

        // 4. Finalmente, lo eliminamos
        Debug.Log("<color=red>[ESTRUCTURA]</color> Eliminada tras el retraso.");
        Destroy(objetoADestruir);
    }
}