using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f; // Ajusta esto para que el Virus lo sienta
    private Vector2 direccion;
    private Rigidbody2D rb;
    private Material mat;
    private Coroutine jellyAnim;
    // Control de múltiples impactos
    private Vector4[] impacts = new Vector4[4]; // Array para el shader
    private bool[] slotOcupado = new bool[4];

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mat = GetComponent<SpriteRenderer>().material;
        direccion = new Vector2(1, 1).normalized;
        // Inicializar array
        mat.SetVectorArray("_Impacts", impacts);
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

        // Si hay slot libre (máximo 4 choques a la vez), lanzamos la vibración
        if (slot != -1) StartCoroutine(DoJelly(puntoLocal, slot));

        if (otro.CompareTag("Pared"))
        {
            Vector2 normal = ((Vector2)transform.position - puntoGlobal).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        else
        {
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
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

    IEnumerator DoJelly(Vector3 localPos, int slot)
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
            float deformacion = Mathf.Sin(t * Mathf.PI * 10.0f) * decaimiento * 0.6f;

            impacts[slot].z = deformacion; // Actualizamos solo la deformación de este slot
            mat.SetVectorArray("_Impacts", impacts);
            yield return null;
        }

        impacts[slot].z = 0;
        mat.SetVectorArray("_Impacts", impacts);
        slotOcupado[slot] = false;
    }
}