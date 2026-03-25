using UnityEngine;

public class ControlFPS : MonoBehaviour
{
    [Header("Referencia al nuevo Selector")]
    public SelectorHorizontalUI selectorFPS;

    int[] fpsValues = { 30, 60, 120, 144, 240 };

    // 🚀 TRUCO PRO: Sigue siendo útil para el primer milisegundo de arranque
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AplicarFPSAlArrancar()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = PlayerPrefs.GetInt("FPSLimit", 120);
        Debug.Log("<color=yellow>[Arranque]</color> FPS limitados inicialmente a: " + Application.targetFrameRate);
    }

    void Start()
    {
        // 🛑 EL ARREGLO ESTÁ AQUÍ 🛑
        // Cuando abres el menú, leemos el PlayerPrefs y forzamos al motor DE NUEVO,
        // por si un script "ninja" nos había pisado el valor a 30 por defecto.
        int savedFPS = PlayerPrefs.GetInt("FPSLimit", 120);

        // 1. Aplicamos los FPS REALES al motor de nuevo. Esto es lo que faltaba.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = savedFPS;
        Debug.Log("<color=yellow>[Ajustes]</color> Refrescando FPS reales al abrir el menú: " + savedFPS);

        // 2. Sincronizamos la ruleta visual
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == savedFPS)
            {
                if (selectorFPS != null)
                {
                    selectorFPS.EstablecerIndice(i);
                }
                break;
            }
        }
    }

    public void ChangeFPS(int index)
    {
        int fps = fpsValues[index];

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;

        PlayerPrefs.SetInt("FPSLimit", fps);
        PlayerPrefs.Save();

        Debug.Log("<color=yellow>[Ajustes]</color> FPS cambiados manualmente a: " + fps);
    }
}