using UnityEngine;
using System.Collections.Generic;

public class CollisionManager : MonoBehaviour
{
    public static CollisionManager Instance;
    public LayerMask wallLayer; // Configura esto en el Inspector

    private void Awake() => Instance = this;

    // Detecta si habrá una colisión en el trayecto de este frame
    public bool CheckWallCollision(Vector2 startPos, Vector2 velocity, float radius, out RaycastHit2D hit)
    {
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        // Lanzamos un círculo hacia adelante para ver si golpea la pared
        hit = Physics2D.CircleCast(startPos, radius, velocity.normalized, distance, wallLayer);
        return hit.collider != null;
    }
}