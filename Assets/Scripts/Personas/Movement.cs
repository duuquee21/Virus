using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float waitTime = 1f;

    // Margen para que no se salga justo del borde
    public float screenPadding = 0.5f;

    private Vector2 targetPosition;
    private float waitCounter;
    private bool isWalking;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        PickNewTarget();
    }

    void Update()
    {
        if (isWalking)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                isWalking = false;
                waitCounter = waitTime;
            }
        }
        else
        {
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
                PickNewTarget();
        }
    }

    void PickNewTarget()
    {
        // Convertimos los bordes de la pantalla a mundo
        Vector2 minWorld = cam.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 maxWorld = cam.ViewportToWorldPoint(new Vector2(1, 1));

        // Aplicamos padding
        minWorld += Vector2.one * screenPadding;
        maxWorld -= Vector2.one * screenPadding;

        float randomX = Random.Range(minWorld.x, maxWorld.x);
        float randomY = Random.Range(minWorld.y, maxWorld.y);

        targetPosition = new Vector2(randomX, randomY);
        isWalking = true;
    }
}
