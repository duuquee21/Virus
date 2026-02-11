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
        // 1. Definimos la velocidad objetivo
        float velocidadObjetivo = velocidadBase;

        // Si está infectado, su velocidad objetivo sube a 20f
        if (personaInfeccion != null && personaInfeccion.alreadyInfected)
        {
            velocidadObjetivo = 20f;
        }

        if (!estaEmpujado)
        {
            // 2. RECUPERACIÓN DE VELOCIDAD
            // Usamos MoveTowards para que si la velocidad bajó por un choque, 
            // suba rápidamente de nuevo hacia la velocidad objetivo (20f si está infectado)
            float aceleracionRapida = 50f; // Ajusta este valor para una recuperación más o menos súbita

            Vector2 velocidadActual = rb.linearVelocity;
            Vector2 velocidadDeseada = direccion * velocidadObjetivo;

            rb.linearVelocity = Vector2.MoveTowards(velocidadActual, velocidadDeseada, aceleracionRapida * Time.fixedDeltaTime);

            // Limpieza de rotación
            if (!estaGirando && rb.angularVelocity != 0)
            {
                rb.angularVelocity = 0;
            }
        }
        else
        {
            // Lógica cuando ha sido empujado (Knockback)
            // Limitamos la velocidad máxima si está infectado para que no salga disparado al infinito
            if (personaInfeccion != null && personaInfeccion.alreadyInfected)
            {
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, 25f);
            }

            // Si la velocidad cae por debajo de la base, recuperamos el control
            if (rb.linearVelocity.magnitude <= velocidadObjetivo)
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
            // --- LÓGICA DE PARED (Sin cambios) ---
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;

            if (estaEmpujado)
            {
                Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);
                if (personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 6.5f && Guardado.instance.nivelParedInfectiva == 6)
                {
                    Vector2 direccionRebote = Vector2.Reflect(rb.linearVelocity, normal).normalized;
                    rb.linearVelocity = direccionRebote * 30f;
                }
                else
                {
                    rb.linearVelocity = nuevaVelocidad;
                }
            }
        }
        else if (!personaInfeccion.alreadyInfected && otro.CompareTag("Persona"))
        {
            PersonaInfeccion scriptAtacante = otro.GetComponent<PersonaInfeccion>();

            if (scriptAtacante != null && scriptAtacante.alreadyInfected)
            {
                personaInfeccion.IntentarAvanzarFasePorChoque();
                scriptAtacante.IntentarAvanzarFasePorChoque();
                return;
            }
            if (Guardado.instance == null) return;

            if (!Guardado.instance.carambolaNormalActiva &&
            !Guardado.instance.carambolaProActiva &&
            !Guardado.instance.carambolaSupremaActiva)
                return;

            Rigidbody2D rbAtacante = otro.GetComponent<Rigidbody2D>();
            Movement movAtacante = otro.GetComponent<Movement>();
           
            if (rbAtacante != null && movAtacante != null && scriptAtacante != null)
            {
                // 1. CÁLCULO DE FÍSICA
                Vector2 puntoContacto = otro.ClosestPoint(transform.position);
                Vector2 normalChoque = ((Vector2)transform.position - puntoContacto).normalized;

                if (normalChoque.sqrMagnitude < 0.01f)
                    normalChoque = ((Vector2)transform.position - (Vector2)otro.transform.position).normalized;

                if (normalChoque == Vector2.zero) normalChoque = rbAtacante.linearVelocity.normalized;

                float velocidadImpacto = rbAtacante.linearVelocity.magnitude;
                float mTransmision = 1f;    
                float velocidadMaximaEnChoque = Mathf.Max(rb.linearVelocity.magnitude, rbAtacante.linearVelocity.magnitude);
                bool hayImpactoFuerte = velocidadMaximaEnChoque > 6.5f;

                // --- LÓGICA DE EVOLUCIÓN UNIFICADA (AQUÍ ESTÁ LA LIMPIEZA) ---
                bool huboEvolucion = false;

                // Chequeo 1: ¿Evoluciona "Este"?
                if (hayImpactoFuerte && (Guardado.instance.nivelParedInfectiva > personaInfeccion.faseActual))
                {
                    personaInfeccion.IntentarAvanzarFasePorChoque();
                    huboEvolucion = true;
                    Debug.Log("Impacto fuerte detectado (Evoluciona Propio).");
                }

                // Chequeo 2: ¿Evoluciona el "Otro"?
                if (hayImpactoFuerte && (Guardado.instance.nivelParedInfectiva > scriptAtacante.faseActual))
                {
                    scriptAtacante.IntentarAvanzarFasePorChoque();
                    huboEvolucion = true;
                    Debug.Log("Impacto fuerte detectado (Evoluciona Otro).");
                }

                // --- GESTIÓN DE SONIDO CENTRALIZADA ---
                // Solo decidimos el sonido una vez, basándonos en si hubo evolución o no
                if (huboEvolucion)
                {
                    // Si al menos uno evolucionó -> Sonido Musical (Do-Re-Mi)
                    if (InfectionFeedback.instance != null) 
                        InfectionFeedback.instance.PlayPhaseChangeSound();
                }
                else if (hayImpactoFuerte)
                {
                    // Golpe fuerte sin evolución -> Sonido impacto fuerte
                    if (InfectionFeedback.instance != null)
                        InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, true);
                }
                else
                {
                    // Golpe flojo -> Sonido impacto suave
                    if (InfectionFeedback.instance != null)
                        InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, false);
                }
                // ----------------------------------------

                // --- RESTO DE FÍSICAS (Rebotes y velocidades) ---
                if (Guardado.instance.carambolaSupremaActiva) mTransmision = hayImpactoFuerte ? 1f : 1f;
                else if (Guardado.instance.carambolaProActiva) mTransmision = hayImpactoFuerte ? 0.5f : 1f;
                else if (Guardado.instance.carambolaNormalActiva) mTransmision = hayImpactoFuerte ? 0.15f : 1f;

                float fuerzaExtra = hayImpactoFuerte ? 1.2f : 1f;
                
                // Aplicar velocidad a "Este"
                if (personaInfeccion.EsFaseMaxima() && Guardado.instance.nivelParedInfectiva == 6)
                {
                    rb.linearVelocity = normalChoque * 22.5f;
                }
                else
                {
                    rb.linearVelocity = normalChoque * (velocidadImpacto * mTransmision * fuerzaExtra);
                }

                // Aplicar velocidad al "Otro"
                float factorReboteAtacante = hayImpactoFuerte ? 0.8f : mTransmision;
                if (scriptAtacante.EsFaseMaxima())
                {   
                    rbAtacante.linearVelocity = -normalChoque * 22.5f;
                }
                else
                {
                    rbAtacante.linearVelocity = -normalChoque * (velocidadImpacto * factorReboteAtacante * fuerzaExtra);
                }

                // Actualizar estados
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