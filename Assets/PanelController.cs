using UnityEngine;

public class PanelController : MonoBehaviour
{
    // Referencia al objeto del panel que queremos mostrar/ocultar
    public GameObject panel;

    // Función para mostrar el panel
    public void MostrarPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    // Función para ocultar el panel
    public void OcultarPanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // Función extra para alternar (si está prendido lo apaga, y viceversa)
    public void AlternarPanel()
    {
        if (panel != null)
        {
            bool estadoActual = panel.activeSelf;
            panel.SetActive(!estadoActual);
        }
    }
}