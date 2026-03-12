using UnityEngine;
using System.Collections;
using TMPro;

public class DetectorMortal : MonoBehaviour
{
    [Header("Configuración de Capacidad")]
    public GameObject prefabTexto;
    private TextMeshPro textoInstanciado;
    private int capacidadActual;

    [Header("Ajustes de Destrucción")]
    public float tiempoEsperaCoral = 0.2f;

    private void Start()
    {
        if (prefabTexto != null)
        {
            GameObject objTexto = Instantiate(prefabTexto, transform.position, Quaternion.identity, transform);
            textoInstanciado = objTexto.GetComponent<TextMeshPro>();

            // --- SOLUCIÓN DE CAPAS ---
            AjustarCapaTexto();
        }

        if (Guardado.instance != null)
        {
            capacidadActual = Guardado.instance.coralCapacity;
            ActualizarInterfaz();
        }
    }

    private void AjustarCapaTexto()
    {
        if (textoInstanciado == null) return;

        // Intentamos obtener el SpriteRenderer del objeto actual o del padre
        SpriteRenderer srPadre = GetComponentInParent<SpriteRenderer>();

        if (srPadre != null)
        {
            // Copiamos la capa exacta del padre
            textoInstanciado.sortingLayerID = srPadre.sortingLayerID;
            // Lo ponemos justo una unidad por encima
            textoInstanciado.sortingOrder = srPadre.sortingOrder + 1;
        }
        else
        {
            // Si no hay SpriteRenderer, al menos asegúrate de que no use "Overlay"
            // Puedes asignar una capa específica manualmente si lo prefieres
            textoInstanciado.sortingOrder = 1;
        }
    }

    // ... Resto del código (Update, OnTriggerEnter2D, etc.) se mantiene igual ...

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Persona"))
        {
            PersonaInfeccion persona = other.GetComponent<PersonaInfeccion>();
            if (persona == null) return;
            ReducirCapacidad();

            if (!Guardado.instance.coralInfeciosoActivo)
            {
                if (persona.faseActual >= 5 || persona.alreadyInfected)
                {
                    StartCoroutine(SecuenciaDestruccionPadre(transform.parent != null ? transform.parent.gameObject : gameObject));
                    Destroy(other.gameObject);
                }
                else
                {
                    persona.SendMessage("Desaparecer", SendMessageOptions.DontRequireReceiver);
                    Destroy(other.gameObject);
                }
            }
            else
            {
                persona.IntentarAvanzarFase();
            }
        }
    }

    private void ReducirCapacidad()
    {
        capacidadActual--;
        ActualizarInterfaz();
        if (capacidadActual <= 0) EjecutarDesaparecerEnPadre();
    }

    private void ActualizarInterfaz()
    {
        if (textoInstanciado != null) textoInstanciado.text = capacidadActual.ToString();
    }

    private void EjecutarDesaparecerEnPadre()
    {
        if (transform.parent != null)
        {
            FloatingCellMovement movement = transform.parent.GetComponent<FloatingCellMovement>();
            if (movement != null) movement.Desaparecer();
        }
    }

    private IEnumerator SecuenciaDestruccionPadre(GameObject objetoADestruir)
    {
        SpriteRenderer[] renderers = objetoADestruir.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers) sr.color = new Color(1, 0, 0, 0.5f);

        Collider2D col = objetoADestruir.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(tiempoEsperaCoral);
        Destroy(objetoADestruir);
    }
}