using System.Collections;
using UnityEngine;

public class StaticBumper : MonoBehaviour
{
    [Header("Configuración de Fuerzas")]
    public float fuerzaReboteBola = 1.0f; // Multiplicador para el rebote de la bola
    public float fuerzaEmpujeObjetos = 15f; // Fuerza de empuje para el Virus/Enemigos


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

        if (otro.CompareTag("Virus") )
        {
            // Si es un choque con otro virus, la vibración es positiva
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, 1));
            if (audioSource != null && reboteVirusClip != null)
            {
                audioSource.PlayOneShot(reboteVirusClip);
            }
        }
        else
        {
            // Si es un choque con una pared, la vibración es negativa
            if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot, -1));
        }


        // 1. Intentamos obtener el Rigidbody2D del objeto que entró

        Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();


        if (rbOtro != null)

        {

            // Calculamos la dirección desde el centro de este objeto hacia el otro

            Vector2 direccionHaciaAfuera = (otro.transform.position - transform.position).normalized;

            // 2. CASO ESPECIAL: LA BOLA

            // Verificamos si es la bola para que cambie su vector de dirección

            FloatingCellMovement movBola = otro.GetComponent<FloatingCellMovement>();

            if (movBola != null)

            {
                // Calculamos la normal del rebote basada en la posición

                Vector2 puntoImpacto = otro.ClosestPoint(transform.position);

                Vector2 normal = ((Vector2)otro.transform.position - puntoImpacto).normalized;

                // Reflejamos su dirección actual

                Vector2 nuevaDireccion = Vector2.Reflect(movBola.GetDireccion(), normal);

                movBola.CambiarDireccion(nuevaDireccion);

                // Opcional: Le damos un pequeño impulso extra para que no se pegue

                rbOtro.AddForce(direccionHaciaAfuera * fuerzaReboteBola, ForceMode2D.Impulse);

            }

            // 3. RESTO DE OBJETOS (Virus, Enemigos, etc.)

            else
            {
                // Aplicamos el empujón físico a cualquier cosa que tenga Rigidbody

                rbOtro.AddForce(direccionHaciaAfuera * fuerzaEmpujeObjetos, ForceMode2D.Impulse);
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