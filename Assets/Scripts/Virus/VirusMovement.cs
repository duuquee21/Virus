using UnityEngine;

public class VirusMovement : MonoBehaviour
{
    public static VirusMovement instance;

    [Header("Configuración")]
    public float moveSpeed = 80f;

    private Rigidbody2D rb;
    private Vector2 movementInput;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        if (movementInput.magnitude > 0)
        {
            rb.AddForce(movementInput * moveSpeed);
        }
    }

    // 👉 usado por upgrades
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}
