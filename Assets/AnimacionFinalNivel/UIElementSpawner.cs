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
    private Vector2 lastSpawnPosition;
    private bool hasLastSpawn = false;

    [Header("Spawn Separation")]
    public float minSpawnDistance = 120f;
    public int maxSpawnAttempts = 10;
    private RectTransform parentRect;

    void Start()
    {
        parentRect = GetComponent<RectTransform>();
        InvokeRepeating(nameof(SpawnElement), 0f, spawnInterval);
    }

    void SpawnElement()
    {
        if (imagePrefab == null || targetPoint == null || parentRect == null) return;

        RectTransform instance = Instantiate(imagePrefab, transform);

        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        instance.localScale = Vector3.one * randomScale;

        Vector2 spawnPos = GetSeparatedSpawnPosition();
        instance.anchoredPosition = spawnPos;

        lastSpawnPosition = spawnPos;
        hasLastSpawn = true;

        UIFlyToTarget mover = instance.gameObject.AddComponent<UIFlyToTarget>();
        mover.target = targetPoint;
        mover.speed = moveSpeed;
        mover.rotationSpeed = Random.Range(minTorque, maxTorque);
    }

    Vector2 GetSeparatedSpawnPosition()
    {
        Vector2 candidate = GetLSpawnPosition();

        if (!hasLastSpawn) return candidate;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            candidate = GetLSpawnPosition();

            if (Vector2.Distance(candidate, lastSpawnPosition) >= minSpawnDistance)
                return candidate;
        }

        return candidate;
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