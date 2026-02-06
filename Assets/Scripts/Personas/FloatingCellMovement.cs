using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float turnSmoothness = 5f;
    public float acceleration = 8f;

    [Header("Organic Oscillation")]
    public float oscillationAmplitude = 0.6f;
    public float oscillationFrequency = 3f;

    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private float oscillationTimer;
    private Camera mainCam;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        mainCam = Camera.main;
    }

    void Start()
    {
        targetDirection = Random.insideUnitCircle.normalized;
        currentDirection = targetDirection;
    }

    void FixedUpdate()
    {
        HandleMovement();
        CheckCameraBounds(); // Nueva lógica de rebote de pantalla
    }

    void HandleMovement()
    {
        currentDirection = Vector2.Lerp(currentDirection, targetDirection, Time.fixedDeltaTime * turnSmoothness).normalized;
        Vector2 baseVelocity = currentDirection * moveSpeed;

        oscillationTimer += Time.fixedDeltaTime * oscillationFrequency;
        float wave = Mathf.Sin(oscillationTimer) * oscillationAmplitude;
        Vector2 perpendicular = new Vector2(-currentDirection.y, currentDirection.x);

        Vector2 desiredVelocity = baseVelocity + (perpendicular * wave);
        Vector2 force = (desiredVelocity - rb.linearVelocity) * acceleration;
        rb.AddForce(force);
    }

    void CheckCameraBounds()
    {
        // Convertimos la posición del objeto a coordenadas de pantalla (0 a 1)
        Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);
        Vector2 bounceNormal = Vector2.zero;

        // Detectar si sale por la izquierda o derecha
        if (viewportPos.x <= 0.05f) bounceNormal = Vector2.right;
        else if (viewportPos.x >= 0.95f) bounceNormal = Vector2.left;

        // Detectar si sale por arriba o abajo
        if (viewportPos.y <= 0.05f) bounceNormal = Vector2.up;
        else if (viewportPos.y >= 0.95f) bounceNormal = Vector2.down;

        // Si ha tocado un borde, rebotamos
        if (bounceNormal != Vector2.zero)
        {
            ApplyBounce(bounceNormal);
        }
    }

    void ApplyBounce(Vector2 normal)
    {
        // Reflejamos la dirección
        Vector2 reflectedDir = Vector2.Reflect(currentDirection, normal).normalized;

        targetDirection = reflectedDir;
        currentDirection = reflectedDir;
        rb.linearVelocity = reflectedDir * moveSpeed;

        // Pequeño empuje para alejarlo del borde y que no se quede bucleado
        transform.position += (Vector3)normal * 0.1f;
    }

    // Mantenemos esto por si también tienes paredes físicas en el escenario
    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.contacts[0].normal;
        ApplyBounce(normal);
    }
}