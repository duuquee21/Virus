using UnityEngine;

public class Movement : MonoBehaviour
{
    public float velocidadBase = 5f;
    private Vector2 direccion;
    private Rigidbody2D rb;
    private bool estaEmpujado = false;
    private bool estaGirando = false;
    private PersonaInfeccion personaInfeccion;


    private GameObject jugadorVirus;
    private ManagerAnimacionJugador managerAnimacionJugador;
    public float fuerzaAtraccion = 10f; // Ajusta este valor a tu gusto

    private bool efectoIniciado = false;
    private Vector3 posicionInicialEfecto;
    private Vector3 escalaInicialEfecto;
    private float tiempoEfecto = 0f;
    public float duracionAbsorcion = 1.5f; // Segundos que tarda en desaparecer



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        float angulo = Random.Range(0f, 360f);
        direccion = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad),
                                Mathf.Sin(angulo * Mathf.Deg2Rad)).normalized;
        personaInfeccion = GetComponent<PersonaInfeccion>();

        jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
        {
            managerAnimacionJugador = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
        }
    }

    void FixedUpdate()
    {
        if (managerAnimacionJugador != null && !managerAnimacionJugador.playable)
        {
            EjecutarEfectoAbsorcion();
            return; // Bloquea el resto del movimiento
        }

        // 1. Definimos la velocidad objetivo
        float velocidadObjetivo = velocidadBase;

        // Si está infectado, su velocidad objetivo sube a 20f
        if (personaInfeccion != null && personaInfeccion.alreadyInfected)
        {
            velocidadObjetivo = 20f;
        }

        if (!estaEmpujado)
        {
            float aceleracionRapida = 50f;
            Vector2 velocidadActual = rb.linearVelocity;

            // --- CORRECCIÓN AQUÍ ---
            // 1. Empezamos con la velocidad de patrulla normal
            Vector2 velocidadDeseada = direccion * velocidadObjetivo;

            // 2. Lógica de atracción
            if (managerAnimacionJugador != null && !managerAnimacionJugador.playable)
            {
                // Calculamos el centro de la pantalla en coordenadas del mundo
                // Usamos la cámara principal para asegurar que el centro sea el que ve el jugador
                Vector2 centroPantallaMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));

                // Calculamos la dirección hacia ese punto central
                Vector2 direccionHaciaCentro = (centroPantallaMundo - (Vector2)transform.position).normalized;

                // Sumamos la fuerza de atracción hacia el centro
                velocidadDeseada += direccionHaciaCentro * fuerzaAtraccion;
            }
          

            // 3. Aplicamos el resultado final al Rigidbody
            rb.linearVelocity = Vector2.MoveTowards(velocidadActual, velocidadDeseada, aceleracionRapida * Time.fixedDeltaTime);
            // -----------------------

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
            Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;

            // Rebote de la direccion base
            direccion = Vector2.Reflect(direccion, normal).normalized;

            if (estaEmpujado)
            {
                // Calculamos el nuevo vector de rebote
                Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);

                if (personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 6.5f && Guardado.instance.nivelParedInfectiva == 6)
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
            PersonaInfeccion scriptAtacante = otro.GetComponent<PersonaInfeccion>();

            // NUEVA CONDICIÓN: Si el otro está infectado, ignoramos el rebote manual
            if (scriptAtacante != null && scriptAtacante.alreadyInfected)
            {
                // Solo el objeto con el ID menor ejecuta la lógica para evitar duplicidad
                if (gameObject.GetInstanceID() < otro.gameObject.GetInstanceID())
                {
                    personaInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);


                    scriptAtacante.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                }
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
                {
                    normalChoque = ((Vector2)transform.position - (Vector2)otro.transform.position).normalized;
                }

                if (normalChoque == Vector2.zero) normalChoque = rbAtacante.linearVelocity.normalized;

                float velocidadImpacto = rbAtacante.linearVelocity.magnitude;
                float mTransmision = 1f;
                float velocidadMaximaEnChoque = Mathf.Max(rb.linearVelocity.magnitude, rbAtacante.linearVelocity.magnitude);
                bool hayImpactoFuerte = velocidadMaximaEnChoque > 6.5f;
                // Solo procesamos el avance de fase si somos el "dueño" de la colisión actual
               
                if (gameObject.GetInstanceID() < otro.gameObject.GetInstanceID())
                {
                    if (hayImpactoFuerte)
                    {
                        if (Guardado.instance.nivelParedInfectiva > personaInfeccion.faseActual)
                        {
                            personaInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                        }

                        if (Guardado.instance.nivelParedInfectiva > scriptAtacante.faseActual)
                        {
                            scriptAtacante.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                        }
                    }

                }
                else if (hayImpactoFuerte)
                {
                    InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, true);
                }
                else if (!hayImpactoFuerte)
                {
                    InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, false);
                }

                if (Guardado.instance.carambolaSupremaActiva)
                {
                    // Megapro (Suprema): Siempre transmite el 100%
                    mTransmision = hayImpactoFuerte ? 0.75f : 1f;
                }
                else if (Guardado.instance.carambolaProActiva)
                {
                    // Pro: 100% en velocidad baja, 50% en velocidad alta
                    mTransmision = hayImpactoFuerte ? 0.5f : 1f;
                }
                else if (Guardado.instance.carambolaNormalActiva)
                {
                    // Normal: 100% en velocidad baja, 15% en velocidad alta
                    mTransmision = hayImpactoFuerte ? 0.15f : 1f;
                }

                // 4. APLICACIÓN DE VELOCIDADES CON LÓGICA DE FASE MÁXIMA
                float fuerzaExtra = hayImpactoFuerte ? 1.2f : 1f;
                // Lógica para el receptor (this)
                if (personaInfeccion.EsFaseMaxima() && Guardado.instance.nivelParedInfectiva == 6)
                {
                    rb.linearVelocity = normalChoque * 22.5f;
                }
                else
                {
                    rb.linearVelocity = normalChoque * (velocidadImpacto * mTransmision * fuerzaExtra);
                }

                // Lógica para el atacante (otro)
                float factorReboteAtacante = hayImpactoFuerte ? 0.8f : mTransmision;
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
    private void EjecutarEfectoAbsorcion()
    {
        if (!efectoIniciado)
        {
            efectoIniciado = true;
            posicionInicialEfecto = transform.position;
            escalaInicialEfecto = transform.localScale;
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // Evita colisiones durante el efecto
        }

        tiempoEfecto += Time.fixedDeltaTime;
        float progreso = Mathf.Clamp01(tiempoEfecto / duracionAbsorcion);

        Vector3 centroMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        centroMundo.z = transform.position.z;

        transform.position = Vector3.Lerp(posicionInicialEfecto, centroMundo, progreso);
        transform.localScale = Vector3.Lerp(escalaInicialEfecto, Vector3.zero, progreso);

        if (progreso >= 1f) gameObject.SetActive(false);
    }
}