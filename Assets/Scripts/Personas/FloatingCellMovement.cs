using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f; // Ajusta esto para que el Virus lo sienta
    private Vector2 direccion;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Iniciamos movimiento diagonal
        direccion = new Vector2(1, 1).normalized;
    }

    void FixedUpdate()
    {
        // Movimiento constante y fluido
        rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        // 1. Si choca con una Pared -> REBOTA
        if (otro.CompareTag("Pared"))
        {
            // Nota: Al ser Trigger, necesitamos calcular la normal manualmente 
            // o usar una aproximación. Lo más preciso es usar la dirección del impacto:
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;

            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        // 2. Si choca con cualquier otra cosa -> EMPUJA
        else
        {
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                // Calculamos dirección desde el centro de la bola hacia el objeto
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;

                // Aplicamos impulso instantáneo
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
}