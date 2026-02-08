using System.Collections;
using UnityEngine;

public class StaticBumper : MonoBehaviour
{
    [Header("Configuración de Fuerzas")]
    public float fuerzaReboteBola = 1.0f; // Multiplicador para el rebote de la bola
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f; // Ajusta esto para que el Virus lo sienta
    public float fuerzaEmpujeCelulas = 15f; // Fuerza de empuje al chocar con paredes



    private Material mat;
    private Coroutine jellyAnim;
    // Control de múltiples impactos
    private Vector4[] impacts = new Vector4[4]; // Array para el shader
    private bool[] slotOcupado = new bool[4];

    private SpriteRenderer sr;
    private MaterialPropertyBlock propBlock;

    public AudioSource audioSource;
    public AudioClip reboteVirusClip;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
      
        mat = GetComponent<SpriteRenderer>().material;
        // Inicializar el bloque de propiedades
        ActualizarShader();
    }

    private void ActualizarShader()
    {
        // Esta es la clave: le pasamos los datos al bloque, y el bloque al renderer
        sr.GetPropertyBlock(propBlock);
        propBlock.SetVectorArray("_Impacts", impacts);
        sr.SetPropertyBlock(propBlock);
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
        else if (otro.CompareTag("Coral"))
        {
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));
        }
        else
        {
            // Si es un choque con una pared, la vibración es negativa
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, -1));
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();

            if (rbOtro != null && rbOtro.linearVelocity.magnitude > 5f)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
            }
        }
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