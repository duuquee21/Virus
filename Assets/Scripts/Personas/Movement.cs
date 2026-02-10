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
            if (personaInfeccion.alreadyInfected)
            {
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, 22.5f);
            }
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
            if (personaInfeccion.alreadyInfected)
            {
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, 22.5f);
            }
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

                if (personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 6.5f && Guardado.instance.paredInfectivaActiva)
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
        else if (!personaInfeccion.alreadyInfected && otro.CompareTag("Persona"))
        {
            if (Guardado.instance == null) return;

            if (!Guardado.instance.carambolaNormalActiva &&
            !Guardado.instance.carambolaProActiva &&
            !Guardado.instance.carambolaSupremaActiva)
                return;

            Rigidbody2D rbAtacante = otro.GetComponent<Rigidbody2D>();
            Movement movAtacante = otro.GetComponent<Movement>();
            PersonaInfeccion scriptAtacante = otro.GetComponent<PersonaInfeccion>();

            if (rbAtacante != null && movAtacante != null && scriptAtacante != null)
            {
                // 1. CÁLCULO DE FÍSICA
                Vector2 puntoContacto = otro.ClosestPoint(transform.position);
                Vector2 normalChoque = ((Vector2)transform.position - puntoContacto).normalized;
                if (normalChoque == Vector2.zero) normalChoque = rbAtacante.linearVelocity.normalized;

                float velocidadImpacto = rbAtacante.linearVelocity.magnitude;
                float mTransmision = 1f;
                bool esVelocidadBaja = velocidadImpacto <= 6.5f;

                // 2. LÓGICA DE FASES
                if (!esVelocidadBaja && Guardado.instance.paredInfectivaActiva)
                {
                    personaInfeccion.IntentarAvanzarFasePorChoque();
                    scriptAtacante.IntentarAvanzarFasePorChoque();
                    Debug.Log("<color=green>Impacto fuerte detectado: Intentando avanzar fase.</color>");
                }
                else if(!esVelocidadBaja)
                {
                    InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white,true);
                }
                else if(esVelocidadBaja)
                {
                    InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, false);
                }

                if (Guardado.instance.carambolaSupremaActiva)
                {
                    // Megapro (Suprema): Siempre transmite el 100%
                    mTransmision = 1f;
                }
                else if (Guardado.instance.carambolaProActiva)
                {
                    // Pro: 100% en velocidad baja, 50% en velocidad alta
                    mTransmision = esVelocidadBaja ? 1f : 0.75f;
                }
                else if (Guardado.instance.carambolaNormalActiva)
                {
                    // Normal: 100% en velocidad baja, 15% en velocidad alta
                    mTransmision = esVelocidadBaja ? 1f : 0.15f;
                }

                // 4. APLICACIÓN DE VELOCIDADES CON LÓGICA DE FASE MÁXIMA
                float fuerzaExtra = esVelocidadBaja ? 1.2f : 1f;

                // Lógica para el receptor (this)
                if (personaInfeccion.EsFaseMaxima()&&Guardado.instance.paredInfectivaActiva)
                {
                    rb.linearVelocity = normalChoque * 22.5f;
                }
                else
                {
                    rb.linearVelocity = normalChoque * (velocidadImpacto * mTransmision * fuerzaExtra);
                }

                // Lógica para el atacante (otro)
                float factorReboteAtacante = esVelocidadBaja ? 0.8f : mTransmision;
                if (scriptAtacante.EsFaseMaxima())
                {
                    rbAtacante.linearVelocity = -normalChoque * 22.5f;
                }
                else
                {
                    rbAtacante.linearVelocity = -normalChoque * (velocidadImpacto * factorReboteAtacante * fuerzaExtra);
                }

                // 5. ACTUALIZACIÓN DE ESTADOS DE MOVIMIENTO
                this.estaEmpujado = true;
                this.direccion = rb.linearVelocity.normalized;
                movAtacante.SetEstaEmpujado(true, rbAtacante.linearVelocity.normalized);
            }
        }
    }
    public void SetEstaEmpujado(bool estado, Vector2 nuevaDir)
    {
        estaEmpujado = estado;
        if (estado) direccion = nuevaDir;
    }
    public void CambiarDireccion(Vector2 nuevaDireccion) => direccion = nuevaDireccion.normalized;
    public Vector2 GetDireccion() => direccion;
}