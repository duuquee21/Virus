using UnityEngine;

public class VirusMovement : MonoBehaviour
{
    public static VirusMovement instance;

    [Header("Configuración de Velocidad")]
    public float baseMoveSpeed = 80f;
    private float currentFinalSpeed;

    [Header("Suavizado")]
    [Range(0.1f, 20f)]
    public float acceleration = 5f; // Cuanto más bajo, más tarda en arrancar y frenar
    public float linearDrag = 2f;    // Resistencia al movimiento (ayuda a frenar suavemente)

    private Rigidbody2D rb;
    private Vector2 movementInput;

    private ManagerAnimacionJugador managerAnimacionJugador;

    [Header("Efecto Gelatina")]
    public SpriteRenderer spriteRenderer;
    public float jellySensitivity = 0.05f; // Qué tanto se deforma
    public float jellyLerpSpeed = 10f;    // Suavizado del retorno a la forma original
    public float maxDeform = 0.3f;
    private Material jellyMat;
    private Vector2 currentJellyVector;

    [Header("Efectos de Partículas")]
    public ParticleSystem moveParticles;
    public float velocityThreshold = 0.1f; // Velocidad mínima para activar partículas

    [Header("Ajustes de Emisión")]
    public float minEmission = 10f;  // Partículas cuando se mueve lento
    public float maxEmission = 50f;  // Partículas a máxima velocidad
    public float speedForMaxEmission = 80f; // Velocidad a la que se alcanza el máximo (tu baseMoveSpeed)

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        managerAnimacionJugador = GetComponent<ManagerAnimacionJugador>();

        // Ajustamos el Drag para que no se deslice infinitamente
        rb.linearDamping = linearDrag;

        ApplySpeedMultiplier();

        if (spriteRenderer != null)
            jellyMat = spriteRenderer.material; // Acceder a .material crea una copia local automática
    }


    void Update()
    {
      
        // 1. Verificamos si existe el manager y si NO es jugable
        if (managerAnimacionJugador != null && !managerAnimacionJugador.playable)
        {
            // Reseteamos el input a cero para que se detenga inmediatamente
            movementInput = Vector2.zero;
            return;
        }

        // 2. Si es jugable, procesamos el movimiento normalmente
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        // Permitir controlar al virus durante la transición entre zonas.
        // Esto evita que se quede “arrastrando” la dirección anterior mientras el mapa gira.
        if (LevelManager.instance == null || LevelManager.instance.isGameActive || LevelManager.instance.isTransitioning)
        {
            movementInput = new Vector2(moveX, moveY);
        }

        if (jellyMat != null)
        {
            // 1. Calculamos la deformación deseada
            Vector2 targetJelly = rb.linearVelocity * jellySensitivity;

            // 2. ¡NUEVO!: Limitamos la deformación máxima (ejemplo: 0.3f)
            // Cambia el 0.3f por un valor mayor si quieres que se deforme más
            targetJelly = Vector2.ClampMagnitude(targetJelly, maxDeform);

            // 3. Suavizamos y enviamos al shader
            currentJellyVector = Vector2.Lerp(currentJellyVector, targetJelly, Time.deltaTime * jellyLerpSpeed);
            jellyMat.SetVector("_VelocityDir", currentJellyVector);
        }
        HandleParticles();

    }

    void FixedUpdate()
    {
        // Calculamos la velocidad deseada
        Vector2 targetVelocity = movementInput * currentFinalSpeed;

        // Calculamos la diferencia entre la velocidad actual y la deseada
        Vector2 velocityChange = targetVelocity - rb.linearVelocity;

        // Aplicamos una fuerza proporcional a la aceleración
        rb.AddForce(velocityChange * acceleration);
    }

    /// <summary>
    /// Detiene el movimiento del jugador y limpia el input.
    /// Útil para transiciones donde queremos que el personaje deje de moverse inmediatamente.
    /// </summary>
    public void StopMovement()
    {
        movementInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void HandleParticles()
    {
        if (moveParticles == null) return;

        // Usamos la magnitud de la velocidad actual del Rigidbody
        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed > velocityThreshold)
        {
            if (!moveParticles.isEmitting) moveParticles.Play();

            // --- 1. ROTACIÓN ---
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            float invertedRotation = (angle + 270f + 180f) % 360f;
            moveParticles.transform.rotation = Quaternion.Euler(0, 0, invertedRotation);

            var main = moveParticles.main;
            main.startRotation = -invertedRotation * Mathf.Deg2Rad;

            // --- 2. EMISIÓN DINÁMICA (Optimizada) ---
            var emission = moveParticles.emission;

            // Calculamos qué tan rápido vamos respecto al máximo actual
            // Usamos currentFinalSpeed para que se adapte si compras mejoras de velocidad
            float maxReferenceSpeed = currentFinalSpeed > 0 ? currentFinalSpeed : speedForMaxEmission;
            float speedPercent = Mathf.Clamp01(currentSpeed / maxReferenceSpeed);

            // Aplicamos un Lerp entre el mínimo y el máximo
            // Si quieres que casi no salgan partículas al ir lento, pon minEmission en un valor bajo (ej. 2)
            float dynamicRate = Mathf.Lerp(minEmission, maxEmission, speedPercent);

            emission.rateOverTime = dynamicRate;

            // OPCIONAL: También puedes variar el tamaño o la vida de la partícula según la velocidad
            // main.startSize = Mathf.Lerp(0.1f, 0.5f, speedPercent);
        }
        else
        {
            // Si la velocidad es casi cero, dejamos de emitir
            var emission = moveParticles.emission;
            emission.rateOverTime = 0;
            if (moveParticles.isEmitting) moveParticles.Stop();
        }
    }

    public void ApplySpeedMultiplier()
    {
        float skillMultiplier = 1f;
        if (Guardado.instance != null)
        {
            skillMultiplier = Guardado.instance.speedMultiplier;
        }

        currentFinalSpeed = baseMoveSpeed * skillMultiplier;
    }

    public Vector2 GetMovementDirection()
    {
        return movementInput.normalized;
    }


    // Cambia este método en tu script VirusMovement.cs
    public void SetSpeed(float newSpeed)
    {
        baseMoveSpeed = newSpeed;

        float skillMultiplier = 1f;
        if (Guardado.instance != null)
        {
            skillMultiplier = Guardado.instance.speedMultiplier;
        }

        // Actualizamos la velocidad final que usa el FixedUpdate
        currentFinalSpeed = baseMoveSpeed * skillMultiplier;

        Debug.Log("Velocidad actualizada en el motor físico: " + currentFinalSpeed);
    }
}