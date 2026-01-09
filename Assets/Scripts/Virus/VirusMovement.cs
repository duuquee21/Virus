using UnityEngine;

public class VirusMovement : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 50f; 
    private Rigidbody2D rb;
    private Vector2 movementInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Guardamos la dirección, y usamos .normalized para que no corra más en diagonal
        movementInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        
        if (movementInput.magnitude > 0)
        {
            rb.AddForce(movementInput * moveSpeed);
        }
    }
}