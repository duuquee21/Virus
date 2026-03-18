using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f; // Ajusta esto para que el Virus lo sienta
    public float fuerzaEmpujeCelulas = 15f; // Fuerza de empuje al chocar con paredes
    private Vector2 direccion;
    private Rigidbody2D rb;
    private Material mat;
    private Coroutine jellyAnim;


    // Control de múltiples impactos
    private Vector4[] impacts = new Vector4[4]; // Array para el shader
    private bool[] slotOcupado = new bool[4];

    private SpriteRenderer sr;
    private MaterialPropertyBlock propBlock;

     AudioSource audioSource;
    public AudioClip reboteVirusClip;


    [Header("Efectos de Partículas")]
    public ParticleSystem moveParticles;
    public float velocityThreshold = 0.1f;
    public float minEmission = 5f;
    public float maxEmission = 30f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
        GameObject objAudio = GameObject.Find("SFXSource");
        if (objAudio != null)
        {
            audioSource = objAudio.GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        HandleParticles();
    }

    private void HandleParticles()
    {
        if (moveParticles == null) return;

        // Usamos la velocidad base y la dirección actual
        float currentSpeed = velocidadBase;

        if (currentSpeed > velocityThreshold)
        {
            if (!moveParticles.isEmitting) moveParticles.Play();

            // --- 1. ROTACIÓN (Igual al Virus) ---
            float angle = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            float invertedRotation = (angle + 270f + 180f) % 360f;
            moveParticles.transform.rotation = Quaternion.Euler(0, 0, invertedRotation);

            var main = moveParticles.main;
            main.startRotation = -invertedRotation * Mathf.Deg2Rad;

            // --- 2. EMISIÓN DINÁMICA ---
            var emission = moveParticles.emission;
            float speedPercent = Mathf.Clamp01(currentSpeed / 10f); // 10f es la referencia de velocidad
            emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, speedPercent);
        }
        else
        {
            var emission = moveParticles.emission;
            emission.rateOverTime = 0;
            if (moveParticles.isEmitting) moveParticles.Stop();
        }
    }

    void Start()
    {
        direccion = new Vector2(1, 1).normalized;
        rb = GetComponent<Rigidbody2D>();

        mat = GetComponent<SpriteRenderer>().material;
        // Inicializar el bloque de propiedades
        ActualizarShader();
    }

    private void OnEnable()
    {
        // Nos suscribimos al evento global de inicio de transición
        LevelTransitioner.OnTransitionStart += Desaparecer;
    }

    private void OnDisable()
    {
        // Desuscripción para evitar errores de memoria
        LevelTransitioner.OnTransitionStart -= Desaparecer;
    }

   public void Desaparecer()
    {
        // 1. Ejecutamos el feedback visual (el mismo que usan las personas)
        if (InfectionFeedback.instance != null)
        {
            // Usamos el efecto de impacto básico en blanco, similar a PersonaInfeccion
          
                InfectionFeedback.instance.PlayEffect(transform.position, Color.white,true);
            
        }

        // 2. Destruimos el objeto coral
        Destroy(gameObject);
    }


    private void ActualizarShader()
    {
        // Esta es la clave: le pasamos los datos al bloque, y el bloque al renderer
        sr.GetPropertyBlock(propBlock);
        propBlock.SetVectorArray("_Impacts", impacts);
        sr.SetPropertyBlock(propBlock);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("InfectionZone")) return;

        Vector2 puntoGlobal = otro.ClosestPoint(transform.position);
        Vector3 puntoLocal = transform.InverseTransformPoint(puntoGlobal);

        // Buscar un slot libre para la animación
        int slot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (!slotOcupado[i]) { slot = i; break; }
        }

        if (otro.CompareTag("Virus"))
        {
            // Si es un choque con otro virus, la vibración es positiva
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));

            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
            }
            if (audioSource != null && reboteVirusClip != null)
            {
                audioSource.PlayOneShot(reboteVirusClip);
            }
        }
        else if (otro.CompareTag("Pared"))
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));
            Vector2 normal = ((Vector2)transform.position - puntoGlobal).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        else if (otro.CompareTag("Coral")) // Añade el tag que use tu StaticBumper
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));

            // CALCULAR EL REBOTE
            // Obtenemos la normal del choque
            Vector2 normal = ((Vector2)transform.position - puntoGlobal).normalized;

            // Reflejamos la dirección actual usando la normal de la superficie
            direccion = Vector2.Reflect(direccion, normal).normalized;

            // Opcional: Pequeño empuje para evitar que se quede pegado
            rb.position += direccion * 0.1f;
        }
        else
        {
            // Si es un choque con una pared, la vibración es negativa
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, -1));
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();

            if (rbOtro != null && rbOtro.linearVelocity.magnitude > 5f && Guardado.instance.virusReboteActiva)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
                if (audioSource != null && reboteVirusClip != null)
                {
                    audioSource.PlayOneShot(reboteVirusClip);
                }
            }
        }
    }




    // Añade esto dentro de FloatingCellMovement.cs
    public void CambiarDireccion(Vector2 nuevaDireccion)
    {
        direccion = nuevaDireccion.normalized;
    }

    public Vector2 GetDireccion()
    {
        return direccion;
    }

    IEnumerator DoJelly(Vector3 localPos, int slot, int signo)
    {
        slotOcupado[slot] = true;
        impacts[slot].x = localPos.x;
        impacts[slot].y = localPos.y;

        float t = 0;
        float duracion = 1.5f;

        while (t < 1.0f)
        {
            t += Time.deltaTime / duracion;
            float decaimiento = Mathf.Exp(-t * 4.0f);

            if (signo < 0)
            {
                float deformacion = -Mathf.Sin(t * Mathf.PI * 10.0f) * decaimiento * 0.6f;
                impacts[slot].z = deformacion;
            }
            if (signo > 0)
            {
                float deformacion = Mathf.Sin(t * Mathf.PI * 10.0f) * decaimiento * 0.6f;
                impacts[slot].z = deformacion;
            }

            // Actualizamos mediante el PropertyBlock
            ActualizarShader();
            yield return null;
        }

        impacts[slot].z = 0;
        ActualizarShader();
        slotOcupado[slot] = false;
    }
}

