using UnityEngine;

/// <summary>
/// Muestra un contador de FPS en pantalla para verificar que Application.targetFrameRate
/// (o el valor configurado en GameSettings) está funcionando.
/// Agregar este componente a cualquier GameObject activo en la escena.
/// </summary>
public class DebugFPSCounter : MonoBehaviour
{
    [Tooltip("Cuántos frames promediar para estabilizar el número.")]
    public int sampleSize = 30;

    [Tooltip("Mostrar el frame rate objetivo (targetFrameRate) además del FPS actual.")]
    public bool showTargetFPS = true;

    private float[] frameTimes;
    private int frameIndex;

    void Awake()
    {
        frameTimes = new float[Mathf.Max(1, sampleSize)];
        frameIndex = 0;
    }

    void Update()
    {
        frameTimes[frameIndex] = Time.unscaledDeltaTime;
        frameIndex = (frameIndex + 1) % frameTimes.Length;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        float sum = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
            sum += frameTimes[i];

        float avgDelta = sum / frameTimes.Length;
        float fps = avgDelta > 0 ? 1f / avgDelta : 0f;

        string fpsText = $"FPS: {fps:0.0}";
        if (showTargetFPS)
            fpsText += $"  (target: {Application.targetFrameRate})";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(10, 10, 10, 10);
        style.richText = false;

        Rect rect = new Rect(10, 10, 260, 24);
        GUI.Label(rect, fpsText, style);
    }
}
