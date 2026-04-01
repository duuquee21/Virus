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

    [Header("Sonidos")]
    public AudioClip clipComer;
    public AudioClip clipDestruir;
    private AudioSource audioManagerSource;

    [Header("Efectos Visuales")]
    public GameObject prefabParticulasComer;

    [Header("Game Feel - Texto")]
    public float multiplicadorEscalaPop = 1.5f; // Cuánto crecerá el texto (1.5 = 50% más grande)
    public float tiempoEfectoPop = 0.15f; // Cuánto dura la animación completa de crecer y encoger
    private Vector3 escalaOriginalTexto;
    private Coroutine corrutinaPopActiva;

    private void Start()
    {
        if (prefabTexto != null)
        {
            GameObject objTexto = Instantiate(prefabTexto, transform.position, Quaternion.identity, transform);
            textoInstanciado = objTexto.GetComponent<TextMeshPro>();

            // Guardamos la escala original para saber a qué tamaño debe volver
            escalaOriginalTexto = textoInstanciado.transform.localScale;

            AjustarCapaTexto();
        }

        if (Guardado.instance != null)
        {
            capacidadActual = Guardado.instance.coralCapacity;
            ActualizarInterfaz();
        }

        // --- BUSCAR EL AUDIOMANAGER ---
        GameObject audioManagerObj = GameObject.Find("SFXSource");
        if (audioManagerObj != null)
        {
            audioManagerSource = audioManagerObj.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No se encontró ningún GameObject llamado 'AudioManager' en la escena.");
        }
    }

    private void AjustarCapaTexto()
    {
        if (textoInstanciado == null) return;

        SpriteRenderer srPadre = GetComponentInParent<SpriteRenderer>();

        if (srPadre != null)
        {
            textoInstanciado.sortingLayerID = srPadre.sortingLayerID;
            textoInstanciado.sortingOrder = srPadre.sortingOrder + 1;
        }
        else
        {
            textoInstanciado.sortingOrder = 1;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Persona"))
        {
            PersonaInfeccion persona = other.GetComponent<PersonaInfeccion>();
            if (persona == null) return;

            bool causaraDestruccion = (capacidadActual - 1 <= 0);

            if (!Guardado.instance.coralInfeciosoActivo && (persona.faseActual >= 5 || persona.alreadyInfected))
            {
                causaraDestruccion = true;
            }

            if (causaraDestruccion)
            {
                if (audioManagerSource != null && clipDestruir != null)
                    audioManagerSource.PlayOneShot(clipDestruir);
            }
            else
            {
                if (audioManagerSource != null && clipComer != null)
                    audioManagerSource.PlayOneShot(clipComer);

                if (prefabParticulasComer != null)
                {
                    GenerarParticulas(other.transform.position);
                }
            }

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

                Movement mov = persona.GetComponent<Movement>();
                if (mov != null)
                {
                    Vector2 direccionAleatoria = Random.insideUnitCircle.normalized;
                    mov.AplicarEmpuje(direccionAleatoria, persona.fuerzaRetroceso, persona.fuerzaRotacion);
                }
            }
        }
    }

    private void GenerarParticulas(Vector3 posicion)
    {
        GameObject particulasObj = Instantiate(prefabParticulasComer, posicion, Quaternion.identity);

        ParticleSystem ps = particulasObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float tiempoDeVidaTotal = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(particulasObj, tiempoDeVidaTotal);
        }
        else
        {
            Destroy(particulasObj, 2f);
        }
    }

    private void ReducirCapacidad()
    {
        capacidadActual--;
        ActualizarInterfaz();

        // --- DISPARAR EL EFECTO POP ---
        if (textoInstanciado != null && gameObject.activeInHierarchy)
        {
            // Si ya hay una animación en curso, la detenemos para no causar conflictos de escala
            if (corrutinaPopActiva != null)
            {
                StopCoroutine(corrutinaPopActiva);
            }
            corrutinaPopActiva = StartCoroutine(EfectoPopTexto());
        }

        if (capacidadActual <= 0) EjecutarDesaparecerEnPadre();
    }

    private void ActualizarInterfaz()
    {
        if (textoInstanciado != null) textoInstanciado.text = capacidadActual.ToString();
    }

    // --- NUEVA CORRUTINA PARA EL EFECTO POP ---
    private IEnumerator EfectoPopTexto()
    {
        float tiempoMedio = tiempoEfectoPop / 2f;
        Vector3 escalaObjetivo = escalaOriginalTexto * multiplicadorEscalaPop;

        // Fase 1: Crecer rápido
        float tiempo = 0;
        while (tiempo < tiempoMedio)
        {
            if (textoInstanciado == null) yield break; // Seguridad por si se destruye en el proceso

            tiempo += Time.deltaTime;
            textoInstanciado.transform.localScale = Vector3.Lerp(escalaOriginalTexto, escalaObjetivo, tiempo / tiempoMedio);
            yield return null;
        }

        // Fase 2: Volver a la normalidad
        tiempo = 0;
        while (tiempo < tiempoMedio)
        {
            if (textoInstanciado == null) yield break;

            tiempo += Time.deltaTime;
            textoInstanciado.transform.localScale = Vector3.Lerp(escalaObjetivo, escalaOriginalTexto, tiempo / tiempoMedio);
            yield return null;
        }

        // Asegurarnos de que quede exactamente en la escala original al terminar
        if (textoInstanciado != null)
        {
            textoInstanciado.transform.localScale = escalaOriginalTexto;
        }
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