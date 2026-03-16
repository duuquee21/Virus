using UnityEngine;

public class MostrarFPS : MonoBehaviour
{
    private float deltaTime = 0.0f;

    void Update()
    {
        // Calculamos el tiempo entre frames de manera suavizada
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle estilo = new GUIStyle();

        // Posicionamiento y tamaño
        Rect rect = new Rect(10, 10, w, h * 2 / 100);
        estilo.alignment = TextAnchor.UpperLeft;
        estilo.fontSize = h * 2 / 50; // Tamaño de la fuente

        // Color del texto (Verde en este caso)
        estilo.normal.textColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

        // Cálculos matemáticos
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        // Texto final a mostrar
        string texto = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, texto, estilo);
    }
}