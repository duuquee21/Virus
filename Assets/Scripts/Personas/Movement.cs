using UnityEngine;

public class Movement : MonoBehaviour
{
    public float velocidadBase = 5f;
    private Vector2 direccion;
    private Rigidbody2D rb;
    private bool estaEmpujado = false;
    private bool estaGirando = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        float angulo = Random.Range(0f, 360f);
        direccion = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad),
                                Mathf.Sin(angulo * Mathf.Deg2Rad)).normalized;
    }

    void FixedUpdate()
    {
        if (!estaEmpujado)
        {
            rb.linearVelocity = Vector2.zero;

            if (!estaGirando && rb.angularVelocity != 0)
            {
                rb.angularVelocity = 0;
            }

            rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
        }
        else
        {
            if (rb.linearVelocity.magnitude <= velocidadBase)
            {
                if (rb.linearVelocity.magnitude > 0.1f)
                {
                    direccion = rb.linearVelocity.normalized;
                }

                estaEmpujado = false;
                estaGirando = false;
            }
        }
    }

    /// <summary>
    /// Aplica un empuje con fuerza constante.
    /// </summary>
    /// <param name="direccionEmpuje">Vector de dirección (se normalizará internamente).</param>
    /// <param name="fuerza">Magnitud fija del impacto.</param>
    /// <param name="torque">Fuerza de rotación.</param>
    public void AplicarEmpuje(Vector2 direccionEmpuje, float fuerza, float torque)
    {
        estaEmpujado = true;
        estaGirando = true;

        // NORMALIZACIÓN: Esto hace que la distancia no importe. 
        // El vector solo indica "hacia dónde", y la variable 'fuerza' decide "cuánto".
        Vector2 direccionNormalizada = direccionEmpuje.normalized;

        // Reiniciamos la velocidad actual para que empujes previos no se sumen de forma extraña
        rb.linearVelocity = Vector2.zero;

        // Aplicamos la fuerza constante
        rb.AddForce(direccionNormalizada * fuerza, ForceMode2D.Impulse);

        float direccionGiro = Random.Range(-1f, 1f);
        rb.AddTorque(direccionGiro * torque, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Pared"))
        {
            float velocidadChoque = rb.linearVelocity.magnitude;

        

            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;

            transform.position = (Vector2)transform.position + (normal * 0.1f);
            direccion = Vector2.Reflect(direccion, normal).normalized;

            if (estaEmpujado)
            {
                rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal);
            }
        }
    }

    public void CambiarDireccion(Vector2 nuevaDireccion) => direccion = nuevaDireccion.normalized;
    public Vector2 GetDireccion() => direccion;
}