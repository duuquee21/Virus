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

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Ajustamos el Drag para que no se deslice infinitamente
        rb.linearDamping = linearDrag;

        ApplySpeedMultiplier();
    }

    void Update()
    {
        // GetAxis (sin Raw) ya tiene un pequeño suavizado integrado por Unity
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        movementInput = new Vector2(moveX, moveY);
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


    public void SetSpeed(float newSpeed)
    {
        baseMoveSpeed = newSpeed;
        ApplySpeedMultiplier();
    }
}