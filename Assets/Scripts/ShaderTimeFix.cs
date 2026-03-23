using UnityEngine;
// Importante si usas UI (Image)
using UnityEngine.UI; 

public class ShaderTimeFix : MonoBehaviour
{
    // AHORA: Asigna el material directamente aquí en el Inspector
    [Header("Asigna aquí el Material del Fondo")]
    public Material materialDelFondo; 

    // ID para optimizar
    private static readonly int RealTimeID = Shader.PropertyToID("_RealTime");

    void Update()
    {
        // Usamos Time.unscaledTime, que nunca se detiene
        if (materialDelFondo != null)
        {
            // Enviamos el tiempo real acumulado al shader
            materialDelFondo.SetFloat(RealTimeID, Time.unscaledTime);
        }
        else
        {
            // Un mensaje de aviso por si se nos olvida asignarlo
            Debug.LogWarning("¡Ojo! No has asignado el material en el script ShaderTimeFix.", gameObject);
        }
    }
}