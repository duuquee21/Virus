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

    public AudioSource audioSource;
    public AudioClip reboteVirusClip;

    [Header("Transición")]
    public float fuerzaAtraccionCentro = 15f;

    private Vector3 centerPoint;

    [Header("Inercia y Atracción")]
    public float aceleracionInercia = 50f;
    public float fuerzaAtraccionTransicion = 20f; // Fuerza de succión durante el giro
    private bool isAttractingToCenter = false;

    [Header("Ajustes de Suavizado")]
    public float radioFrenadoCentro = 2f; // Distancia a la que empieza a frenar
    [Range(0f, 1f)]
    public float amortiguacionSuave = 0.92f; // Cuanto más bajo, más frena (0.90 a 0.98 es ideal)


    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        direccion = new Vector2(1, 1).normalized;
        rb = GetComponent<Rigidbody2D>();
     
        mat = GetComponent<SpriteRenderer>().material;
        // Inicializar el bloque de propiedades
        ActualizarShader();
    }
    void OnEnable()
    {
        LevelTransitioner.OnTransitionStart += StartAttraction;
        LevelTransitioner.OnImpactShake += StopAttraction;
    }

    void OnDisable()
    {
        LevelTransitioner.OnTransitionStart -= StartAttraction;
        LevelTransitioner.OnImpactShake -= StopAttraction;
    }

    private void StartAttraction() => isAttractingToCenter = true;
    private void StopAttraction(float intensity)
    {
        isAttractingToCenter = false;

        // --- LA CLAVE AQUÍ ---
        // Si la célula se estaba moviendo hacia el centro, actualizamos su 
        // variable 'direccion' para que al salir de la succión siga esa trayectoria.
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            direccion = rb.linearVelocity.normalized;
        }
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
        if (isAttractingToCenter)
        {
            // Lógica única de atracción y frenado suave
            MoverHaciaCentroConInercia();
        }
        else
        {
            // Movimiento normal de patrulla
            if (LevelManager.instance != null && !LevelManager.instance.isGameActive)
            {
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, aceleracionInercia * Time.fixedDeltaTime);
                return;
            }

            Vector2 velocidadDeseada = direccion * velocidadBase;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velocidadDeseada, aceleracionInercia * Time.fixedDeltaTime);
        }

        // Limpieza de rotación
        if (rb.angularVelocity != 0)
            rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, 0, 10f * Time.fixedDeltaTime);
    }
    private void MoverHaciaCentroConInercia()
    {
        Vector2 centroMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector2 vectorHaciaCentro = (centroMundo - rb.position);
        float distancia = vectorHaciaCentro.magnitude;

        // 1. Definimos la velocidad deseada proporcional a la distancia
        // A más distancia, más rápido corre al centro.
        Vector2 velocidadDeseada = vectorHaciaCentro.normalized * (distancia * fuerzaAtraccionTransicion);

        // 2. Aplicamos el frenado suave basado en el radio
        if (distancia < radioFrenadoCentro)
        {
            // Calculamos un multiplicador que va de 1 (en el borde del radio) a 0 (en el centro)
            float t = distancia / radioFrenadoCentro;

            // Usamos SmoothStep para que la curva de frenado sea de seda (no lineal)
            float multiplicadorSuave = Mathf.SmoothStep(0f, 1f, t);

            velocidadDeseada *= multiplicadorSuave;

            // Aplicamos una fricción extra a la velocidad actual para "matar" el rebote
            rb.linearVelocity *= amortiguacionSuave;
        }

        // 3. Aplicamos el movimiento final
        // Usamos una aceleración más alta durante la transición para que responda rápido pero suave
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velocidadDeseada, (aceleracionInercia * 1.5f) * Time.fixedDeltaTime);

        // 4. Corte de seguridad: si está muy muy cerca, pararlo del todo
        if (distancia < 0.02f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = centroMundo; // Ajuste fino final
        }
    }
    private void MoverHaciaCentro()
    {
        Vector2 currentPos = rb.position;
        Vector2 directionToCenter = ((Vector2)centerPoint - currentPos);
        float distancia = directionToCenter.magnitude;

        // Si ya está muy cerca, dejamos de aplicar fuerza para evitar que "orbite" frenéticamente
        if (distancia < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Fuerza proporcional: Cuanto más lejos, más fuerte tira.
        // Usamos AddForce para que el movimiento sea fluido y no un "teletransporte"
        rb.AddForce(directionToCenter.normalized * distancia * fuerzaAtraccionCentro);

        // Aplicamos un poco de drag artificial para que no salga disparada al pasar por el centro
        rb.linearVelocity *= 0.95f;
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
        else if (otro.CompareTag("Coral") ) // Añade el tag que use tu StaticBumper
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

            if(signo < 0)
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