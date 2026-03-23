using UnityEngine;

public class VirusMovement : MonoBehaviour
{
    public static VirusMovement instance;

    [Header("Configuración de Velocidad")]
    public float baseMoveSpeed = 80f;
    private float currentFinalSpeed;

    [Header("Suavizado")]
    [Range(0.1f, 20f)]
    public float acceleration = 5f;
    public float linearDrag = 2f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private ManagerAnimacionJugador managerAnimacionJugador;

    [Header("Efecto Gelatina")]
    public SpriteRenderer spriteRenderer;
    public float jellySensitivity = 0.05f;
    public float jellyLerpSpeed = 10f;
    public float maxDeform = 0.3f;
    private Material jellyMat;
    private Vector2 currentJellyVector;

    [Header("Efectos de Partículas")]
    public ParticleSystem moveParticles;
    public float velocityThreshold = 0.1f;

    [Header("Ajustes de Emisión")]
    public float minEmission = 10f;
    public float maxEmission = 50f;
    public float speedForMaxEmission = 80f;

    [Header("Flecha de Dirección")]
    public GameObject arrowIndicator; // Arrastra el objeto de la flecha aquí
    public float arrowOrbitRadius = 2f; // Qué tan lejos del centro orbita


    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        managerAnimacionJugador = GetComponent<ManagerAnimacionJugador>();
        rb.linearDamping = linearDrag;

        ApplySpeedMultiplier();

        if (spriteRenderer != null)
            jellyMat = spriteRenderer.material;


        UpdateMovementVisuals();
    }

    void Update()
    {
        arrowOrbitRadius = VirusRadiusController.instance.CurrentFinalRadius;
        // 1. Verificamos si es jugable
        if (managerAnimacionJugador != null && !managerAnimacionJugador.playable)
        {
            movementInput = Vector2.zero;
            return;
        }
        if (arrowIndicator != null)
        {
            // La flecha solo debe estar activa si usamos ratón Y el juego está activo
            bool deberiaMostrarFlecha = Guardado.instance.inputType == Guardado.InputType.Mouse;

            if (arrowIndicator.activeSelf != deberiaMostrarFlecha)
            {
                arrowIndicator.SetActive(deberiaMostrarFlecha);
            }
        }

        // 2. Lógica de selección de Input
        if (LevelManager.instance == null || LevelManager.instance.isGameActive || LevelManager.instance.isTransitioning)
        {
            if (Guardado.instance.inputType == Guardado.InputType.Keyboard || Guardado.instance.inputType == Guardado.InputType.Controller)
            {
                // MODO TECLADO O MANDO: Usamos ejes
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveY = Input.GetAxisRaw("Vertical");
                movementInput = new Vector2(moveX, moveY).normalized;
            }
            else if (Guardado.instance.inputType == Guardado.InputType.Mouse)
            {
                // MODO RATÓN
                UpdateArrowDirection();
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 directionToMouse = (Vector2)mousePosition - rb.position;

                if (directionToMouse.magnitude > 0.2f)
                    movementInput = directionToMouse.normalized;
                else
                    movementInput = Vector2.zero;
            }
        }


        // Efecto Gelatina
        if (jellyMat != null)
        {
            Vector2 targetJelly = rb.linearVelocity * jellySensitivity;
            targetJelly = Vector2.ClampMagnitude(targetJelly, maxDeform);
            currentJellyVector = Vector2.Lerp(currentJellyVector, targetJelly, Time.deltaTime * jellyLerpSpeed);
            jellyMat.SetVector("_VelocityDir", currentJellyVector);
        }

        HandleParticles();
        
       
    }

    void FixedUpdate()
    {
        Vector2 targetVelocity = movementInput * currentFinalSpeed;
        Vector2 velocityChange = targetVelocity - rb.linearVelocity;
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

    public void UpdateMovementVisuals()
    {
        if (arrowIndicator != null)
        {
            // La flecha debe estar ACTIVADA solo para ratón
            arrowIndicator.SetActive(Guardado.instance.inputType == Guardado.InputType.Mouse);
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
    private void UpdateArrowDirection()
    {
        if (arrowIndicator == null) return;

        Vector2 direction = movementInput;

        // Si no hay movimiento, ocultamos o detenemos la actualización
        if (direction == Vector2.zero)
        {
            // Opcional: arrowIndicator.SetActive(false);
            return;
        }

        if (!arrowIndicator.activeSelf) arrowIndicator.SetActive(true);

        // USAMOS EL RADIO REAL
        float finalRadius = VirusRadiusController.instance.CurrentFinalRadius;

        float extraMargin = 2f;
        float adjustedRadius = VirusRadiusController.instance.CurrentFinalRadius + extraMargin;

        // Aplicamos la posición con el radio ajustado
        arrowIndicator.transform.localPosition = (Vector3)(direction.normalized * adjustedRadius);

        // Rotación (apuntando hacia afuera)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowIndicator.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
    }


}