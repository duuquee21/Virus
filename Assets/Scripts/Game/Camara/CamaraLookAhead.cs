using UnityEngine;

public class CamaraLookAhead : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; 

    [Header("Configuración Básica")]
    public Vector3 offset = new Vector3(0, 10, -10); 
    public float smoothTime = 0.2f; 

    [Header("Look Ahead (El efecto del video)")]
    public bool activarLookAhead = true;
    public float lookAheadDistance = 3f; 
    public float lookAheadSpeed = 2f;    
    public float movementThreshold = 0.1f;

    [Header("Limites Map")]
    public BoxCollider2D mapBounds;


    private Vector3 currentVelocity; 
    private Vector3 lookAheadPos;    
    private Vector3 lastTargetPos;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null)
        {
            lastTargetPos = target.position;
            
            transform.parent = null; 
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // POSICION BASE
        Vector3 targetBasicPos = target.position + offset;

        // calcular adelantamiento
        if (activarLookAhead)
        {
            // posicion del jugador
            Vector3 moveDir = (target.position - lastTargetPos).normalized;
            float moveSpeed = (target.position - lastTargetPos).magnitude;

           
            if (moveSpeed > movementThreshold * Time.deltaTime)
            {
               
                Vector3 targetLookAhead = moveDir * lookAheadDistance;
                
               
                lookAheadPos = Vector3.Lerp(lookAheadPos, targetLookAhead, Time.deltaTime * lookAheadSpeed);
            }
            else
            {
                
                lookAheadPos = Vector3.Lerp(lookAheadPos, Vector3.zero, Time.deltaTime * lookAheadSpeed);
            }
        }
        else
        {
            lookAheadPos = Vector3.zero;
        }

      
        Vector3 desiredPos = targetBasicPos + lookAheadPos;

        //aplicar los limites

        if (mapBounds != null)
        {
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            Bounds b = mapBounds.bounds;

            float minX = b.min.x + camWidth;
            float maxX = b.max.x -  camWidth;
            float minY = b.min.y + camHeight;
            float maxY = b.max.y - camHeight;

            // restringir X e Y 

            desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);
        }
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);

       
        lastTargetPos = target.position;
    }
}