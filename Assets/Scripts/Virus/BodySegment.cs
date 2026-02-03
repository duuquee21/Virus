using UnityEngine;

public class BodySegment : MonoBehaviour
{
    public Transform target;   // El segmento que está justo delante
    public float distance = 0.7f; // Distancia de separación

    void Update()
    {
        if (target == null) return;

        // Calcular la dirección hacia el objetivo
        Vector3 direction = (transform.position - target.position).normalized;

        // Reposicionar el segmento a la distancia exacta
        transform.position = target.position + direction * distance;

        // Opcional: Que el cuadrado mire hacia el siguiente segmento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}