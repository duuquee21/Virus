using UnityEngine;

public class SetHeightOnEnable : MonoBehaviour
{
    [Tooltip("La altura (eje Y) a la que se forzará el objeto al activarse.")]
    public float targetHeight = 0.85f;

    private void OnEnable()
    {
        // Guardamos la posición actual
        Vector3 currentPosition = transform.position;

        // Modificamos solo el eje Y
        currentPosition.y = targetHeight;

        // Aplicamos la nueva posición al transform
        transform.position = currentPosition;
    }
}