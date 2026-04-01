using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f;
    public float fuerzaEmpujeCelulas = 15f;
    private Vector2 direccion;
    private Rigidbody2D rb;
    private Material mat;
    private Coroutine jellyAnim;

    // Control de múltiples impactos
    private Vector4[] impacts = new Vector4[4];
    private bool[] slotOcupado = new bool[4];

    private SpriteRenderer sr;
    private MaterialPropertyBlock propBlock;

    AudioSource audioSource;
    public AudioClip reboteVirusClip;

    // --- NUEVO: Control de volumen desde el Inspector ---
    [Range(0f, 1f)]
    public float volumenRebote = 1f;

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

        float currentSpeed = velocidadBase;

        if (currentSpeed > velocityThreshold)
        {
            if (!moveParticles.isEmitting) moveParticles.Play();

            float angle = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            float invertedRotation = (angle + 270f + 180f) % 360f;
            moveParticles.transform.rotation = Quaternion.Euler(0, 0, invertedRotation);

            var main = moveParticles.main;
            main.startRotation = -invertedRotation * Mathf.Deg2Rad;

            var emission = moveParticles.emission;
            float speedPercent = Mathf.Clamp01(currentSpeed / 10f);
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
        ActualizarShader();
    }

    private void OnEnable()
    {
        LevelTransitioner.OnTransitionStart += Desaparecer;
    }

    private void OnDisable()
    {
        LevelTransitioner.OnTransitionStart -= Desaparecer;
    }

    public void Desaparecer()
    {
        if (InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayEffect(transform.position, Color.white, true);
        }

        Destroy(gameObject);
    }

    private void ActualizarShader()
    {
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

        int slot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (!slotOcupado[i]) { slot = i; break; }
        }

        if (otro.CompareTag("Virus"))
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));

            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
            }

            // Sonido con volumen ajustable
            if (audioSource != null && reboteVirusClip != null)
            {
                audioSource.PlayOneShot(reboteVirusClip, volumenRebote);
            }
        }
        else if (otro.CompareTag("Pared"))
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));
            Vector2 normal = ((Vector2)transform.position - puntoGlobal).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        else if (otro.CompareTag("Coral"))
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));

            Vector2 normal = ((Vector2)transform.position - puntoGlobal).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
            rb.position += direccion * 0.1f;
        }
        else
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, -1));
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();

            if (rbOtro != null && rbOtro.linearVelocity.magnitude > 5f &&
                Guardado.instance != null && Guardado.instance.virusReboteActiva)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);

                // Sonido con volumen ajustable
                if (audioSource != null && reboteVirusClip != null)
                {
                    audioSource.PlayOneShot(reboteVirusClip, volumenRebote);
                }
            }
        }
    }

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
            else if (signo > 0)
            {
                float deformacion = Mathf.Sin(t * Mathf.PI * 10.0f) * decaimiento * 0.6f;
                impacts[slot].z = deformacion;
            }

            ActualizarShader();
            yield return null;
        }

        impacts[slot].z = 0;
        ActualizarShader();
        slotOcupado[slot] = false;
    }
}