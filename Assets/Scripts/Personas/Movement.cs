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
            // Detenemos la velocidad física para usar movimiento cinemático
            rb.linearVelocity = Vector2.zero;

            // Si ya no hay impulso, detenemos el giro residual pero MANTENEMOS la rotación actual
            if (!estaGirando && rb.angularVelocity != 0)
            {
                rb.angularVelocity = 0;
                // Eliminamos la línea: transform.rotation = Quaternion.identity;
            }

            // Movimiento constante en la nueva dirección capturada tras el empuje
            rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
        }
        else
        {
            // Detectamos cuando la inercia baja para retomar el control
            if (rb.linearVelocity.magnitude <= velocidadBase)
            {
                // Capturamos la inercia final como nueva dirección de marcha
                if (rb.linearVelocity.magnitude > 0.1f)
                {
                    direccion = rb.linearVelocity.normalized;
                }

                estaEmpujado = false;
                estaGirando = false;
            }
        }
    }

    public void AplicarEmpuje(Vector2 direccionEmpuje, float fuerza, float torque)
    {
        estaEmpujado = true;
        estaGirando = true;

        rb.AddForce(direccionEmpuje * fuerza, ForceMode2D.Impulse);

        // Torque aleatorio para rotación orgánica
        float direccionGiro = Random.Range(-1f, 1f);
        rb.AddTorque(direccionGiro * torque, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Pared"))
        {
            // Calculamos la velocidad actual del Rigidbody
            float velocidadChoque = rb.linearVelocity.magnitude;

            // DEBUG: Esto aparecerá en tu consola para que calibres el número
            Debug.Log($"<color=white>Velocidad de impacto:</color> <b>{velocidadChoque:F2}</b>");

            // Si la velocidad es mayor a 5 y es un choque físico (estaEmpujado)
            if (velocidadChoque > 5f)
            {
                PersonaInfeccion scriptInfeccion = GetComponent<PersonaInfeccion>();
                if (scriptInfeccion != null)
                {
                    scriptInfeccion.IntentarAvanzarFasePorChoque();
                }
            }

            // --- Lógica de rebote normal ---
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;
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