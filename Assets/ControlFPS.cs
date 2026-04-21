using UnityEngine;

public class ControlFPS : MonoBehaviour
{
    [Header("Referencia al nuevo Selector")]
    public SelectorHorizontalUI selectorFPS;

    int[] fpsValues = { 30, 60, 120, 144, 244 };

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

        // 2. Sincronizamos la ruleta visual y las opciones del selector
        if (selectorFPS != null)
        {
            // Opciones y valores sincronizados
            selectorFPS.opciones = new System.Collections.Generic.List<string>();
            int defaultIndex = 0;
            for (int i = 0; i < fpsValues.Length; i++)
            {
                selectorFPS.opciones.Add(fpsValues[i].ToString());
                if (fpsValues[i] == savedFPS) defaultIndex = i;
            }
            selectorFPS.EstablecerIndice(defaultIndex);
            selectorFPS.SendMessage("ActualizarTexto", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void ChangeFPS(int index)
    {

        // Seguridad: clamp del índice para evitar errores
        if (index < 0 || index >= fpsValues.Length)
            index = 0;

        int fps = fpsValues[index];

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;

        PlayerPrefs.SetInt("FPSLimit", fps);
        PlayerPrefs.Save();

        // Actualiza visualmente el texto del selector
        if (selectorFPS != null)
        {
            selectorFPS.EstablecerIndice(index);
            selectorFPS.SendMessage("ActualizarTexto", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log("<color=yellow>[Ajustes]</color> FPS cambiados manualmente a: " + fps);
    }
}