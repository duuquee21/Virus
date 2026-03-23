using System.Collections.Generic;
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

    [Header("Spawn Separation")]
    public float minSpawnDistance = 120f;
    public int maxSpawnAttempts = 20;

    private RectTransform parentRect;
    private readonly List<RectTransform> activeElements = new List<RectTransform>();

    void Start()
    {
        parentRect = GetComponent<RectTransform>();
        InvokeRepeating(nameof(SpawnElement), 0f, spawnInterval);
    }

    void SpawnElement()
    {
        if (imagePrefab == null || targetPoint == null || parentRect == null)
            return;

        CleanupDestroyedElements();

        if (!TryGetSeparatedSpawnPosition(out Vector2 spawnPos))
            return;

        RectTransform instance = Instantiate(imagePrefab, transform);

        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        instance.localScale = Vector3.one * randomScale;
        instance.anchoredPosition = spawnPos;

        activeElements.Add(instance);

        UIFlyToTarget mover = instance.gameObject.AddComponent<UIFlyToTarget>();
        mover.target = targetPoint;
        mover.speed = moveSpeed;
        mover.rotationSpeed = Random.Range(minTorque, maxTorque);
    }

    bool TryGetSeparatedSpawnPosition(out Vector2 validPosition)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 candidate = GetLSpawnPosition();

            if (IsFarEnoughFromAll(candidate))
            {
                validPosition = candidate;
                return true;
            }
        }

        validPosition = Vector2.zero;
        return false;
    }

    bool IsFarEnoughFromAll(Vector2 candidate)
    {
        for (int i = 0; i < activeElements.Count; i++)
        {
            RectTransform element = activeElements[i];

            if (element == null)
                continue;

            if (Vector2.Distance(candidate, element.anchoredPosition) < minSpawnDistance)
                return false;
        }

        return true;
    }

    void CleanupDestroyedElements()
    {
        for (int i = activeElements.Count - 1; i >= 0; i--)
        {
            if (activeElements[i] == null)
                activeElements.RemoveAt(i);
        }
    }

    Vector2 GetLSpawnPosition()
    {
        Vector2 size = parentRect.rect.size;
        bool spawnLeftSide = Random.value > 0.5f;

        if (spawnLeftSide)
        {
            float y = Random.Range(-size.y / 2f, size.y / 2f);
            return new Vector2(-size.x / 2f - spawnOffset, y);
        }
        else
        {
            float x = Random.Range(-size.x / 2f, size.x / 2f);
            return new Vector2(x, size.y / 2f + spawnOffset);
        }
    }
}