using UnityEngine;

public class UIFlyToTarget : MonoBehaviour
{
    public RectTransform target;
    public float speed = 200f;
    public float rotationSpeed = 100f;
    public float destroyDelay = 2f; // ← NUEVO

    private RectTransform rect;
    private bool hasArrived = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (target == null) return;

        if (!hasArrived)
        {
            // Movimiento hacia el punto
            rect.anchoredPosition = Vector2.MoveTowards(
                rect.anchoredPosition,
                target.anchoredPosition,
                speed * Time.deltaTime
            );

            // Rotación mientras viaja
            rect.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            // Detectar llegada
            if (Vector2.Distance(rect.anchoredPosition, target.anchoredPosition) < 5f)
            {
                hasArrived = true;

                // Opcional: parar rotación al llegar
                rotationSpeed = 0f;

                // Destruir tras delay
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}