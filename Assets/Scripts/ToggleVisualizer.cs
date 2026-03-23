using UnityEngine;
using UnityEngine.UI;

public class ToggleVisualizer : MonoBehaviour
{
    public GameObject objetoParaMostrar; // Arrastra aquí el "Padre" de tus dos imágenes
    private Toggle miToggle;

    void Awake()
    {
        miToggle = GetComponent<Toggle>();
        // Nos suscribimos al evento de cambio de valor
        miToggle.onValueChanged.AddListener(ActualizarVisibilidad);
        
        // Sincronizamos el estado inicial
        ActualizarVisibilidad(miToggle.isOn);
    }

    void ActualizarVisibilidad(bool estado)
    {
        if (objetoParaMostrar != null)
        {
            objetoParaMostrar.SetActive(estado);
        }
    }
}