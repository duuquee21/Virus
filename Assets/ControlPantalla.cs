using UnityEngine;

public class ControlPantalla : MonoBehaviour
{
    public void PantallaCompleta()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }

    public void Ventana()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }
}