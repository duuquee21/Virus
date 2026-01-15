using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Arrastra aquí al VirusPlayer
    public Vector3 offset = new Vector3(0,0,-10); // Mantiene la cámara alejada
    
    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}