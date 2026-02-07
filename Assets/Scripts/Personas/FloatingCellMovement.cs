using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f;

    private Vector2 direccion;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        direccion = new Vector2(1, 1).normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direccion * velocidadBase;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Pared"))
        {
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        else
        {
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                Vector2 direccionEmpuje =
                    (otro.transform.position - transform.position).normalized;

                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
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
}
