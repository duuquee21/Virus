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

    [Header("Ajustes de Transición")]
    public float friccionDuranteAnimacion = 15f; // Mayor valor = frenazo más seco



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
     

        ManejarMovimientoNormal();
    }

    private void ManejarMovimientoNormal()
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

    public bool EstaEmpujado()
    {
        return estaEmpujado;
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
            if (Guardado.instance == null || scriptAtacante == null) return;

            // 1. FILTRO DE NIVEL (Si no hay nivel o la fase es superior, se ignoran)
            int nivelPermitido = Guardado.instance.nivelCarambola;
            if (personaInfeccion.faseActual > nivelPermitido || scriptAtacante.faseActual > nivelPermitido)
            {
                return; // No hay nivel suficiente para que estas fases interactúen
            }

            // 2. LÓGICA DE INFECCIÓN (Si uno ya está infectado, contagia al otro)
            if (scriptAtacante.alreadyInfected)
            {
                if (gameObject.GetInstanceID() < otro.gameObject.GetInstanceID())
                {
                    personaInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                    scriptAtacante.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                }
                return;
            }

            // 3. REBOTE FÍSICO (Solo si ambos son aptos por nivel pero ninguno estaba infectado aún)
            Rigidbody2D rbAtacante = otro.GetComponent<Rigidbody2D>();
            Movement movAtacante = otro.GetComponent<Movement>();

            if (rbAtacante != null && movAtacante != null)
            {
                Vector2 puntoContacto = otro.ClosestPoint(transform.position);
                Vector2 normalChoque = ((Vector2)transform.position - puntoContacto).normalized;
                if (normalChoque.sqrMagnitude < 0.01f) normalChoque = ((Vector2)transform.position - (Vector2)otro.transform.position).normalized;

                float velocidadImpacto = rbAtacante.linearVelocity.magnitude;
                float velocidadMaximaEnChoque = Mathf.Max(rb.linearVelocity.magnitude, rbAtacante.linearVelocity.magnitude);
                bool hayImpactoFuerte = velocidadMaximaEnChoque > 6.5f;

                // --- ELIMINADO: Aquí tenías código que volvía a llamar a avanzar fase ignorando filtros ---

                if (hayImpactoFuerte && InfectionFeedback.instance != null)
                {
                    InfectionFeedback.instance.PlayBasicImpactEffect(otro.transform.position, Color.white, true);
                }

                // Aplicación de velocidades
                float fuerzaExtra = hayImpactoFuerte ? 1.2f : 1f;
                rb.linearVelocity = normalChoque * (velocidadImpacto * fuerzaExtra);
                rbAtacante.linearVelocity = -normalChoque * (velocidadImpacto * fuerzaExtra);

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