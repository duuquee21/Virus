using UnityEngine;

public class CameraDirectionalFollow : MonoBehaviour
{
    [Header("Offset base fijo")]
    public Vector3 baseOffset = new Vector3(0, 0, -10f);

    [Header("Movimiento direccional")]
    public float lookAheadDistance = 1.2f;
    public float smoothSpeed = 5f;

    [Header("Referencia al movimiento")]
    public VirusMovement virusMovement;

    private Vector3 anchorPosition;   // Punto fijo de la cámara
    private Vector3 currentVelocity;
    private Vector3 currentDirectionalOffset;

    void Start()
    {
        // Guardamos la posición inicial como ancla
        anchorPosition = transform.position;
    }

    void LateUpdate()
    {
        if (virusMovement == null) return;

        Vector2 moveDir = virusMovement.GetMovementDirection();

        Vector3 targetOffset = new Vector3(
            moveDir.x,
            moveDir.y,
            0f
        ) * lookAheadDistance;

        // Suavizamos SOLO el offset
        currentDirectionalOffset = Vector3.SmoothDamp(
            currentDirectionalOffset,
            targetOffset,
            ref currentVelocity,
            1f / smoothSpeed
        );

        // Cámara = posición fija + pequeño empuje
        transform.position = anchorPosition + baseOffset + currentDirectionalOffset;
    }
}
