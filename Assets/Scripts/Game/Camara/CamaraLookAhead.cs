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

    
    private Vector3 currentVelocity; 
    private Vector3 lookAheadPos;    
    private Vector3 lastTargetPos;   

    void Start()
    {
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

      
        Vector3 finalPos = targetBasicPos + lookAheadPos;

        
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, smoothTime);

       
        lastTargetPos = target.position;
    }
}