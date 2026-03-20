
using UnityEngine;
using System.Collections.Generic;

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
    private float tiempoEmpujeRestante = 0f;
    public float duracionMinimaEmpuje = 0.25f;

    [Header("Ajustes de Transición")]
    public float friccionDuranteAnimacion = 15f; // Mayor valor = frenazo más seco

    // ===== SPATIAL HASH GRID =====
    private CircleCollider2D circleCollider;
    public static Dictionary<Vector2Int, HashSet<Movement>> espacialGrid = new Dictionary<Vector2Int, HashSet<Movement>>();
    private static float tamañoCelda = 5f;
    private Vector2Int ultimaPosicionGrid;
    private HashSet<Movement> objetosColisionadosEsteFrame = new HashSet<Movement>();


    [Header("Ajustes Anti-Tunneling")]
    public LayerMask capaParedes;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();

        float angulo = Random.Range(0f, 360f);
        direccion = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad),
                                Mathf.Sin(angulo * Mathf.Deg2Rad)).normalized;
        personaInfeccion = GetComponent<PersonaInfeccion>();

        jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
        {
            managerAnimacionJugador = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
        }

        // Registrar en el grid espacial
        ActualizarPosicionGrid();
        ultimaPosicionGrid = ObtenerPosicionGrid();
    }

    void FixedUpdate()
    {
        if (managerAnimacionJugador != null && !managerAnimacionJugador.playable)
        {
            EjecutarEfectoAbsorcion();
            return;
        }

        // 1. PREDICCIÓN DE IMPACTO
        PredecirColisionParedes();

        // 2. Movimiento
        ManejarMovimientoNormal();

        // 3. Grid y colisiones entre personas
        ActualizarPosicionGrid();
        DetectarColisionesCircleToCircle();

        // 4. MANTENER VELOCIDAD CONSTANTE (Ajuste Crítico)
        // Si ya está infectado, forzamos que la magnitud sea exactamente 50
        if (personaInfeccion != null && personaInfeccion.alreadyInfected)
        {
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * 30f;
            }
            else
            {
                // Si por alguna razón se detuvo (colisión frontal perfecta), 
                // usamos la variable 'direccion' para relanzarlo
                rb.linearVelocity = direccion * 50f;
            }
        }
        else if (rb.linearVelocity.magnitude > 50f) // Cap para no infectados
        {
            rb.linearVelocity = rb.linearVelocity.normalized * 30f;
        }
    }
    /// <summary>
    /// Lanza un "rayo" del tamaño del collider hacia adelante para ver si en este frame vamos a atravesar una pared.
    /// </summary>
    private void PredecirColisionParedes()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.1f) return;

        float distanciaFrame = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 direccionMovimiento = rb.linearVelocity.normalized;
        float miRadio = circleCollider.radius * transform.localScale.x;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, miRadio, direccionMovimiento, distanciaFrame, capaParedes);

        if (hit.collider != null && hit.collider.CompareTag("Pared"))
        {
            // === NUEVO: APLICAR DAÑO MANUALMENTE ===
            // Buscamos el script del planeta en el objeto con el que chocamos
            PlanetCrontrollator planeta = hit.collider.GetComponent<PlanetCrontrollator>();
            if (planeta != null)
            {
                // Le enviamos este gameObject, el punto del impacto y le decimos que es un Choque
                planeta.ProcesarImpacto(this.gameObject, hit.point, PlanetCrontrollator.TipoImpacto.Choque);
            }

            if (!gameObject.activeInHierarchy) return;
            // =======================================

            // Ejecutamos tu lógica exacta de rebote de inmediato
            ProcesarReboteContraPared(hit.point, hit.normal);

            // Separar ligeramente el objeto de la pared para evitar que se quede pegado
            transform.position = hit.centroid + (hit.normal * 0.05f);
        }
    }

    /// <summary>
    /// Contiene exactamente tu misma matemática de rebote que tenías en OnTriggerEnter2D / OnCollisionEnter2D
    /// </summary>
    private void ProcesarReboteContraPared(Vector2 puntoImpacto, Vector2 normal)
    {
        // Tu rebote de la direccion base
        direccion = Vector2.Reflect(direccion, normal).normalized;

        if (estaEmpujado)
        {
            // Tu cálculo del nuevo vector de rebote
            Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);

            if (personaInfeccion != null && personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 6.5f && Guardado.instance != null && Guardado.instance.nivelParedInfectiva == 6)
            {
                Debug.Log("<color=blue>Rebote de Fase Final: Velocidad Constante Aplicada.</color>");
                Vector2 direccionRebote = Vector2.Reflect(rb.linearVelocity, normal).normalized;
                float velocidadFija = 30f;
                rb.linearVelocity = direccionRebote * velocidadFija;
            }
            else
            {
                // Rebote normal
                rb.linearVelocity = nuevaVelocidad;
            }
        }
    }

    private void ManejarMovimientoNormal()
    {
        bool isInfectado = (personaInfeccion != null && personaInfeccion.alreadyInfected);
        float velocidadObjetivo = isInfectado ? 30f : velocidadBase;

        if (!estaEmpujado)
        {
            // Si está infectado, la aceleración es instantánea para evitar "rampeos"
            float aceleracion = isInfectado ? 500f : 50f;
            Vector2 velocidadDeseada = direccion * velocidadObjetivo;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velocidadDeseada, aceleracion * Time.fixedDeltaTime);
        }
        else
        {
            // Lógica de empuje
            tiempoEmpujeRestante -= Time.fixedDeltaTime;

            // Si está infectado, queremos que recupere el control casi de inmediato 
            // o que el empuje no lo frene por debajo de 50
            if (tiempoEmpujeRestante <= 0f)
            {
                if (isInfectado || rb.linearVelocity.magnitude <= 2f)
                {
                    estaEmpujado = false;
                }
            }

            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                direccion = rb.linearVelocity.normalized;
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
        tiempoEmpujeRestante = duracionMinimaEmpuje;

        Vector2 direccionNormalizada = direccionEmpuje.normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direccionEmpuje, 0.5f);
        if (hit.collider != null && hit.collider.CompareTag("Pared"))
        {
            direccionNormalizada = Vector2.Reflect(direccionNormalizada, hit.normal);
        }

        rb.linearVelocity = Vector2.zero;
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
        // NOTA: Las colisiones entre personas se detectan con circle-to-circle en DetectarColisionesCircleToCircle()
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Pared")) return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;

        direccion = Vector2.Reflect(direccion, normal).normalized;

        if (estaEmpujado)
        {
            Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);
            rb.linearVelocity = nuevaVelocidad;
        }
    }

    private void OnDestroy()
    {
        // Desregistrar del grid al destruir
        if (circleCollider != null)
        {
            Vector2Int posGrid = ObtenerPosicionGrid();
            if (espacialGrid.ContainsKey(posGrid))
            {
                espacialGrid[posGrid].Remove(this);
                if (espacialGrid[posGrid].Count == 0)
                {
                    espacialGrid.Remove(posGrid);
                }
            }
        }
    }

    // ===== MÉTODOS DEL SPATIAL HASH GRID =====

    private Vector2Int ObtenerPosicionGrid()
    {
        Vector3 pos = transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / tamañoCelda),
            Mathf.FloorToInt(pos.y / tamañoCelda)
        );
    }

    private void ActualizarPosicionGrid()
    {
        if (circleCollider == null) return;

        Vector2Int nuevaPosicion = ObtenerPosicionGrid();

        // Si cambió de celda, actualizar el diccionario
        if (nuevaPosicion != ultimaPosicionGrid)
        {
            // Remover de la celda anterior
            if (espacialGrid.ContainsKey(ultimaPosicionGrid))
            {
                espacialGrid[ultimaPosicionGrid].Remove(this);
                if (espacialGrid[ultimaPosicionGrid].Count == 0)
                {
                    espacialGrid.Remove(ultimaPosicionGrid);
                }
            }

            // Agregar a la nueva celda
            if (!espacialGrid.ContainsKey(nuevaPosicion))
            {
                espacialGrid[nuevaPosicion] = new HashSet<Movement>();
            }
            espacialGrid[nuevaPosicion].Add(this);
            ultimaPosicionGrid = nuevaPosicion;
        }
        else if (!espacialGrid.ContainsKey(nuevaPosicion) || !espacialGrid[nuevaPosicion].Contains(this))
        {
            // Primera vez o se perdió del registro, re-agregar
            if (!espacialGrid.ContainsKey(nuevaPosicion))
            {
                espacialGrid[nuevaPosicion] = new HashSet<Movement>();
            }
            espacialGrid[nuevaPosicion].Add(this);
            ultimaPosicionGrid = nuevaPosicion;
        }
    }

    private void DetectarColisionesCircleToCircle()
    {
        if (circleCollider == null || personaInfeccion == null) return;

        Vector2Int miPosGrid = ObtenerPosicionGrid();
        float miRadio = circleCollider.radius * transform.localScale.x;
        Vector2 miPosicion = (Vector2)transform.position;

        // Limpiar conjunto de colisiones del frame anterior
        objetosColisionadosEsteFrame.Clear();

        // Revisar 9 celdas (la actual + 8 adyacentes)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int celdaAdyacente = miPosGrid + new Vector2Int(x, y);

                if (espacialGrid.ContainsKey(celdaAdyacente))
                {
                    foreach (Movement otra in espacialGrid[celdaAdyacente])
                    {
                        if (otra == this || otra.personaInfeccion == null) continue;
                        if (objetosColisionadosEsteFrame.Contains(otra)) continue; // Ya procesada

                        Vector2 otraPosicion = (Vector2)otra.transform.position;
                        float otroRadio = otra.circleCollider.radius * otra.transform.localScale.x;

                        // Circle-to-Circle: comparar distancia al cuadrado vs suma de radios al cuadrado
                        float distanciaCuadrada = (miPosicion - otraPosicion).sqrMagnitude;
                        float sumaRadiosCuadrada = (miRadio + otroRadio) * (miRadio + otroRadio);

                        if (distanciaCuadrada < sumaRadiosCuadrada)
                        {
                            ProcesarColisionCircleToCircle(otra, otroRadio, otraPosicion, distanciaCuadrada);
                            objetosColisionadosEsteFrame.Add(otra);
                        }
                    }
                }
            }
        }
    }

    private void ProcesarColisionCircleToCircle(Movement otra, float otroRadio, Vector2 otraPosicion, float distanciaCuadrada)
    {
        // Evitar procesar dos veces la misma colisión
        if (this.gameObject.GetInstanceID() > otra.gameObject.GetInstanceID()) return;

        Rigidbody2D rbOtra = otra.rb;
        PersonaInfeccion otroPersona = otra.personaInfeccion;

        if (Guardado.instance == null || rbOtra == null) return;

        // 1. FILTRO DE NIVEL
        int nivelPermitido = Guardado.instance.nivelCarambola;
        if (personaInfeccion.faseActual > nivelPermitido || otroPersona.faseActual > nivelPermitido)
        {
            return;
        }



        // 3. REBOTE FÍSICO
        Vector2 direccionRebote = ((Vector2)transform.position - otraPosicion);
        if (direccionRebote.sqrMagnitude < 0.001f)
        {
            direccionRebote = UnityEngine.Random.insideUnitCircle.normalized;
        }
        else
        {
            direccionRebote.Normalize();
        }

        float velPropia = rb.linearVelocity.magnitude;
        float velOtra = rbOtra.linearVelocity.magnitude;
        float velocidadDeIntercambio = (velPropia + velOtra) * 0.5f;

        // Filtro de impacto fuerte
        bool hayImpactoFuerte = velocidadDeIntercambio > 6.5f;
        if (!hayImpactoFuerte)
        {
            return;
        }

        // Procesar probabilidades de evolución
        Guardado g = Guardado.instance;
        bool algunaFiguraEvoluciono = false;

        int fasePropia = personaInfeccion.faseActual;
        if (fasePropia >= 0 && fasePropia < g.probParedInfectiva.Length)
        {
            float prob = g.probParedInfectiva[fasePropia] * 0.25f;
            if (UnityEngine.Random.value < prob)
            {
                personaInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                algunaFiguraEvoluciono = true;
            }
        }

        int faseOtra = otroPersona.faseActual;
        if (faseOtra >= 0 && faseOtra < g.probParedInfectiva.Length)
        {
            float probOtra = g.probParedInfectiva[faseOtra] * 0.25f;
            if (UnityEngine.Random.value < probOtra)
            {
                otroPersona.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                algunaFiguraEvoluciono = true;
            }
        }

        if (!algunaFiguraEvoluciono && InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayBasicImpactEffect(otraPosicion, Color.white, true);
        }

        // 4. APLICAR REBOTE
        float factorRestitucion = 1.2f;
        rb.linearVelocity = direccionRebote * (velocidadDeIntercambio * factorRestitucion);
        rbOtra.linearVelocity = -direccionRebote * (velocidadDeIntercambio * factorRestitucion);

        // 5. ACTUALIZAR ESTADO
        this.estaEmpujado = true;
        this.direccion = rb.linearVelocity.normalized;
        otra.SetEstaEmpujado(true, rbOtra.linearVelocity.normalized);
    }
}