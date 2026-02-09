using UnityEngine;

public class Movement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public LayerMask capaPared;
    public float radioDeteccion = 0.4f; // Ajusta al tamaño de tu personaje
    private Vector2 direccion;
    private Rigidbody2D rb;
    private bool estaEmpujado = false;
    private bool estaGirando = false;
    private PersonaInfeccion personaInfeccion;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        float angulo = Random.Range(0f, 360f);
        direccion = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad),
                                Mathf.Sin(angulo * Mathf.Deg2Rad)).normalized;
        personaInfeccion = GetComponent<PersonaInfeccion>();
    }

    void FixedUpdate()
    {
        // 1. Detección reforzada
        ValidarColisionInminente();

        if (!estaEmpujado)
        {
            rb.linearVelocity = Vector2.zero;
            if (!estaGirando && rb.angularVelocity != 0) rb.angularVelocity = 0;

            rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
        }
        else
        {
            if (rb.linearVelocity.magnitude <= velocidadBase)
            {
                if (rb.linearVelocity.magnitude > 0.1f) direccion = rb.linearVelocity.normalized;
                estaEmpujado = false;
                estaGirando = false;
            }
        }
    }

    private void ValidarColisionInminente()
    {
        Vector2 velActual = estaEmpujado ? rb.linearVelocity : direccion * velocidadBase;
        // Aumentamos el margen de detección (0.5f extra)
        float distanciaCheck = velActual.magnitude * Time.fixedDeltaTime + 0.5f;

        // Lanzamos el CircleCast
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radioDeteccion, velActual.normalized, distanciaCheck, capaPared);

        if (hit.collider != null)
        {
            // Si detectamos impacto, ejecutamos el rebote
            EjecutarLogicaRebote(hit.collider, hit.normal);
        }
    }

    public void AplicarEmpuje(Vector2 direccionEmpuje, float fuerza, float torque)
    {
        estaEmpujado = true;
        estaGirando = true;
        Vector2 direccionNormalizada = direccionEmpuje.normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direccionNormalizada * fuerza, ForceMode2D.Impulse);
        float direccionGiro = Random.Range(-1f, 1f);
        rb.AddTorque(direccionGiro * torque, ForceMode2D.Impulse);
    }

    private void EjecutarLogicaRebote(Collider2D otro, Vector2 normal)
    {
        // VITAL: Solo rebotar si nos movemos HACIA la pared (evita atravesar al rebotar)
        Vector2 velCheck = estaEmpujado ? rb.linearVelocity : direccion;
        if (Vector2.Dot(velCheck, normal) >= 0) return;

        // --- TU LÓGICA ORIGINAL ---
        direccion = Vector2.Reflect(direccion, normal).normalized;

        if (estaEmpujado)
        {
            Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);
            if (personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 5f)
            {
                rb.linearVelocity = nuevaVelocidad.normalized * 30f;
            }
            else
            {
                rb.linearVelocity = nuevaVelocidad;
            }
        }

        // --- EL TRUCO PARA EL ÁNGULO RECTO ---
        // Empujamos al objeto físicamente hacia afuera de la normal de la pared
        // para que no haya forma de que el siguiente frame esté dentro.
        float pushBack = 0.25f;
        transform.position = (Vector2)transform.position + (normal * pushBack);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Pared"))
        {
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;

            // Si la normal es (0,0) porque el centro ya entró, usamos la inversa de la dirección
            if (normal == Vector2.zero) normal = -direccion;

            EjecutarLogicaRebote(otro, normal);
        }
    }

    public void CambiarDireccion(Vector2 nuevaDireccion) => direccion = nuevaDireccion.normalized;
    public Vector2 GetDireccion() => direccion;
}