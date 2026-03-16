using UnityEngine;
using TMPro;

/// <summary>
/// Muestra un contador de FPS directamente en el menú de ajustes.
/// Añade este componente al GameObject principal del panel de ajustes y asigna un TextMeshProUGUI.
/// </summary>
public class SettingsFPSDisplay : MonoBehaviour
{
    [Tooltip("Componente TextMeshProUGUI donde se mostrará el contador.")]
    public TMP_Text fpsLabel;

    [Tooltip("Cuántos frames promediar para estabilizar el número.")]
    public int sampleSize = 30;

    [Tooltip("Con qué frecuencia en segundos se actualiza el texto (para bajar el coste de Update).\n0 = cada frame.")]
    public float updateInterval = 0.1f;

    private float[] frameTimes;
    private int frameIndex;
    private float timer;

    void Awake()
    {
        frameTimes = new float[Mathf.Max(1, sampleSize)];
        frameIndex = 0;
        timer = 0f;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy || fpsLabel == null) return;

        frameTimes[frameIndex] = Time.unscaledDeltaTime;
        frameIndex = (frameIndex + 1) % frameTimes.Length;

        timer += Time.unscaledDeltaTime;
        if (updateInterval > 0f && timer < updateInterval) return;

        timer = 0f;
        float sum = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
            sum += frameTimes[i];

        float avgDelta = sum / frameTimes.Length;
        float fps = avgDelta > 0f ? 1f / avgDelta : 0f;

        fpsLabel.text = $"FPS: {fps:0.0}  (target: {Application.targetFrameRate})";
    }
}
