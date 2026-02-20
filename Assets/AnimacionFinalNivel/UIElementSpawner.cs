using UnityEngine;

public class UIElementSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public RectTransform imagePrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 1f;
    public Vector2 scaleRange = new Vector2(0.5f, 1.5f);

    [Header("Movement")]
    public RectTransform targetPoint;
    public float moveSpeed = 200f;

    [Header("Rotation")]
    public float minTorque = -200f;
    public float maxTorque = 200f;

    [Header("Spawn Distance")]
    public float spawnOffset = 200f;

    private RectTransform parentRect;

    void Start()
    {
        parentRect = GetComponent<RectTransform>();
        InvokeRepeating(nameof(SpawnElement), 0f, spawnInterval);
    }

    void SpawnElement()
    {
        RectTransform instance = Instantiate(imagePrefab, transform);

        // Escala aleatoria
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        instance.localScale = Vector3.one * randomScale;

        // Posición en L
        instance.anchoredPosition = GetLSpawnPosition();

        // Movimiento
        UIFlyToTarget mover = instance.gameObject.AddComponent<UIFlyToTarget>();
        mover.target = targetPoint;
        mover.speed = moveSpeed;
        mover.rotationSpeed = Random.Range(minTorque, maxTorque);
    }

    Vector2 GetLSpawnPosition()
    {
        Vector2 size = parentRect.rect.size;

        bool spawnLeftSide = Random.value > 0.5f;

        if (spawnLeftSide)
        {
            // Línea vertical izquierda
            float y = Random.Range(-size.y / 2, size.y / 2);
            return new Vector2(-size.x / 2 - spawnOffset, y);
        }
        else
        {
            // Línea horizontal superior
            float x = Random.Range(-size.x / 2, size.x / 2);
            return new Vector2(x, size.y / 2 + spawnOffset);
        }
    }
}