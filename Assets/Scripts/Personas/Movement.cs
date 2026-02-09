using UnityEngine;

public class Movement : MonoBehaviour
{
    public float velocidadBase = 5f;
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
        if (!estaEmpujado)
        {
            // En lugar de MovePosition, asignamos velocidad constante.
            // Esto ayuda a que el motor detecte mejor los triggers al moverse.
            rb.linearVelocity = direccion * velocidadBase;

            if (!estaGirando && rb.angularVelocity != 0)
            {
                rb.angularVelocity = 0;
            }
        }
        else
        {
            // Lógica de frenado por fricción natural o manual
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
    /// <param name="direccionEmpuje">Vector de direcci�n (se normalizar� internamente).</param>
    /// <param name="fuerza">Magnitud fija del impacto.</param>
    /// <param name="torque">Fuerza de rotaci�n.</param>
    public void AplicarEmpuje(Vector2 direccionEmpuje, float fuerza, float torque)
    {
        estaEmpujado = true;
        estaGirando = true;

        // NORMALIZACI�N: Esto hace que la distancia no importe. 
        // El vector solo indica "hacia d�nde", y la variable 'fuerza' decide "cu�nto".
        Vector2 direccionNormalizada = direccionEmpuje.normalized;

        // Reiniciamos la velocidad actual para que empujes previos no se sumen de forma extra�a
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
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;

            // Rebote de la direcci�n base
            direccion = Vector2.Reflect(direccion, normal).normalized;

            if (estaEmpujado)
            {
                // Calculamos el nuevo vector de rebote
                Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);

                if (personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 5f && Guardado.instance.paredInfectivaActiva)
                {
                    Debug.Log("<color=blue>Rebote de Fase Final: Velocidad Constante Aplicada.</color>");

                    // 1. Calculamos la direcci�n del rebote (normalizada, vale 1)
                    Vector2 direccionRebote = Vector2.Reflect(rb.linearVelocity, normal).normalized;

                    // 2. Definimos la velocidad fija que queremos
                    float velocidadFija = 30f;

                    // 3. Asignamos: Direcci�n * Velocidad deseada
                    rb.linearVelocity = direccionRebote * velocidadFija;
                }
                else
                {
                    // Rebote normal
                    rb.linearVelocity = nuevaVelocidad;
                }
            }
        }
    }

    public void CambiarDireccion(Vector2 nuevaDireccion) => direccion = nuevaDireccion.normalized;
    public Vector2 GetDireccion() => direccion;
}