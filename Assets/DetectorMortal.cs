using UnityEngine;
using System.Collections;
using TMPro; // Necesario para el texto

public class DetectorMortal : MonoBehaviour
{
    [Header("Configuración de Capacidad")]
    [Header("Configuración de Capacidad")]
    public GameObject prefabTexto; // Arrastra aquí tu Prefab de TextMeshPro desde la carpeta Assets
    private TextMeshPro textoInstanciado; // Esta será nuestra referencia interna
    private int capacidadActual;

    [Header("Ajustes de Destrucción")]
    public float tiempoEsperaCoral = 0.2f;

    private void Start()
    {
        if (prefabTexto != null)
        {
            // Añadimos 'transform' al final para que sea HIJO de este objeto
            GameObject objTexto = Instantiate(prefabTexto, transform.position, Quaternion.identity, transform);

            // Referencia al componente para actualizar el número
            textoInstanciado = objTexto.GetComponent<TextMeshPro>();

            // Forzar el Order in Layer máximo
            MeshRenderer mesh = objTexto.GetComponent<MeshRenderer>();
            if (mesh != null) mesh.sortingOrder = 32767;
        }

        if (Guardado.instance != null)
        {
            capacidadActual = Guardado.instance.coralCapacity;
            ActualizarInterfaz();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Persona"))
        {
            PersonaInfeccion persona = other.GetComponent<PersonaInfeccion>();
            if (persona == null) return;
            // Reducir capacidad cada vez que entra alguien
            ReducirCapacidad();

            // Lógica si el coral infeccioso NO está activo
            if (!Guardado.instance.coralInfeciosoActivo)
            {
              

                if (persona.faseActual >= 5 || persona.alreadyInfected)
                {
                    Debug.Log("<color=cyan>[IMPACTO]</color> Fase 5 detectada.");
                    StartCoroutine(SecuenciaDestruccionPadre(transform.parent != null ? transform.parent.gameObject : gameObject));
                    Destroy(other.gameObject);
                }
                else
                {
                    persona.SendMessage("Desaparecer", SendMessageOptions.DontRequireReceiver);
                    Destroy(other.gameObject);
                }
            }
            // Lógica si el coral infeccioso SÍ está activo
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

        if (capacidadActual <= 0)
        {
            EjecutarDesaparecerEnPadre();
        }
    }

    private void ActualizarInterfaz()
    {
        if (textoInstanciado != null)
        {
            textoInstanciado.text = capacidadActual.ToString();
        }
    }

    private void EjecutarDesaparecerEnPadre()
    {
        // Buscamos el componente fLOATINGcELLmOVEMENT en el objeto padre
        if (transform.parent != null)
        {
            FloatingCellMovement movement = transform.parent.GetComponent<FloatingCellMovement>();
            if (movement != null)
            {
                movement.Desaparecer();
            }
            else
            {
                Debug.LogError("No se encontró el script FloatingCellMovement en el padre.");
            }
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