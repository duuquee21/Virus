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
    private static Dictionary<Vector2Int, HashSet<Movement>> espacialGrid = new Dictionary<Vector2Int, HashSet<Movement>>();
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
        if (personaInfeccion != null && personaInfeccion.alreadyInfected)
        {
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * 30f;
            }
            else
            {
                rb.linearVelocity = direccion * 50f;
            }
        }
        else if (rb.linearVelocity.magnitude > 50f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * 30f;
        }
    }

    private void PredecirColisionParedes()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.1f) return;

        float distanciaFrame = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 direccionMovimiento = rb.linearVelocity.normalized;
        float miRadio = circleCollider.radius * transform.localScale.x;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, miRadio, direccionMovimiento, distanciaFrame, capaParedes);

        if (hit.collider != null && hit.collider.CompareTag("Pared"))
        {
            PlanetCrontrollator planeta = hit.collider.GetComponent<PlanetCrontrollator>();
            if (planeta != null)
            {
                // FILTRO DE VELOCIDAD: Solo daña si va rápido (v > 6.5)
                if (rb.linearVelocity.magnitude > 6.5f)
                {
                    planeta.ProcesarImpacto(this.gameObject, hit.point, PlanetCrontrollator.TipoImpacto.Choque);
                }
            }

            if (!gameObject.activeInHierarchy) return;

            ProcesarReboteContraPared(hit.point, hit.normal);
            transform.position = hit.centroid + (hit.normal * 0.05f);
        }
    }

    private void ProcesarReboteContraPared(Vector2 puntoImpacto, Vector2 normal)
    {
        direccion = Vector2.Reflect(direccion, normal).normalized;

        if (estaEmpujado)
        {
            Vector2 nuevaVelocidad = Vector2.Reflect(rb.linearVelocity, normal);

            if (personaInfeccion != null && personaInfeccion.EsFaseMaxima() && rb.linearVelocity.magnitude > 6.5f && Guardado.instance != null && Guardado.instance.nivelParedInfectiva == 6)
            {
                Vector2 direccionRebote = Vector2.Reflect(rb.linearVelocity, normal).normalized;
                float velocidadFija = 30f;
                rb.linearVelocity = direccionRebote * velocidadFija;
            }
            else
            {
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
            float aceleracion = isInfectado ? 500f : 50f;
            Vector2 velocidadDeseada = direccion * velocidadObjetivo;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velocidadDeseada, aceleracion * Time.fixedDeltaTime);
        }
        else
        {
            tiempoEmpujeRestante -= Time.fixedDeltaTime;
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

    public bool EstaEmpujado() => estaEmpujado;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Pared"))
        {
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
            rb.isKinematic = true;
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
            rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal);
        }
    }

    private void OnDestroy()
    {
        if (circleCollider != null)
        {
            Vector2Int posGrid = ObtenerPosicionGrid();
            if (espacialGrid.ContainsKey(posGrid))
            {
                espacialGrid[posGrid].Remove(this);
                if (espacialGrid[posGrid].Count == 0) espacialGrid.Remove(posGrid);
            }
        }
    }

    private Vector2Int ObtenerPosicionGrid()
    {
        Vector3 pos = transform.position;
        return new Vector2Int(Mathf.FloorToInt(pos.x / tamañoCelda), Mathf.FloorToInt(pos.y / tamañoCelda));
    }

    private void ActualizarPosicionGrid()
    {
        if (circleCollider == null) return;
        Vector2Int nuevaPosicion = ObtenerPosicionGrid();
        if (nuevaPosicion != ultimaPosicionGrid)
        {
            if (espacialGrid.ContainsKey(ultimaPosicionGrid))
            {
                espacialGrid[ultimaPosicionGrid].Remove(this);
                if (espacialGrid[ultimaPosicionGrid].Count == 0) espacialGrid.Remove(ultimaPosicionGrid);
            }
            if (!espacialGrid.ContainsKey(nuevaPosicion)) espacialGrid[nuevaPosicion] = new HashSet<Movement>();
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
        objetosColisionadosEsteFrame.Clear();

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
                        if (objetosColisionadosEsteFrame.Contains(otra)) continue;

                        Vector2 otraPosicion = (Vector2)otra.transform.position;
                        float otroRadio = otra.circleCollider.radius * otra.transform.localScale.x;
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
        if (this.gameObject.GetInstanceID() > otra.gameObject.GetInstanceID()) return;
        Rigidbody2D rbOtra = otra.rb;
        PersonaInfeccion otroPersona = otra.personaInfeccion;
        if (Guardado.instance == null || rbOtra == null) return;

        int nivelPermitido = Guardado.instance.nivelCarambola;
        if (personaInfeccion.faseActual > nivelPermitido || otroPersona.faseActual > nivelPermitido) return;

        Vector2 direccionRebote = ((Vector2)transform.position - otraPosicion);
        if (direccionRebote.sqrMagnitude < 0.001f) direccionRebote = UnityEngine.Random.insideUnitCircle.normalized;
        else direccionRebote.Normalize();

        float velocidadDeIntercambio = (rb.linearVelocity.magnitude + rbOtra.linearVelocity.magnitude) * 0.5f;

        if (velocidadDeIntercambio > 6.5f)
        {
            Guardado g = Guardado.instance;
            bool algunaFiguraEvoluciono = false;

            if (personaInfeccion.faseActual >= 0 && personaInfeccion.faseActual < g.probParedInfectiva.Length)
            {
                if (UnityEngine.Random.value < g.probParedInfectiva[personaInfeccion.faseActual] * 0.25f)
                {
                    personaInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                    algunaFiguraEvoluciono = true;
                }
            }
            if (otroPersona.faseActual >= 0 && otroPersona.faseActual < g.probParedInfectiva.Length)
            {
                if (UnityEngine.Random.value < g.probParedInfectiva[otroPersona.faseActual] * 0.25f)
                {
                    otroPersona.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Carambola);
                    algunaFiguraEvoluciono = true;
                }
            }

            if (!algunaFiguraEvoluciono && InfectionFeedback.instance != null) InfectionFeedback.instance.PlayBasicImpactEffect(otraPosicion, Color.white, true);

            float factorRestitucion = 1.2f;
            rb.linearVelocity = direccionRebote * (velocidadDeIntercambio * factorRestitucion);
            rbOtra.linearVelocity = -direccionRebote * (velocidadDeIntercambio * factorRestitucion);

            this.estaEmpujado = true;
            this.direccion = rb.linearVelocity.normalized;
            otra.SetEstaEmpujado(true, rbOtra.linearVelocity.normalized);
        }
    }
}