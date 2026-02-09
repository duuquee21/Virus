using UnityEngine;

public class ControladorPulso : MonoBehaviour
{
    [Header("Referencias")]
    
    public Transform spriteVisual;

   
    public RadiusLineRenderer lineVisual;

    [Header("Configuración del Pulso")]
    public float radioBase = 2.5f;     // El tamaño normal del radio
    public float intensidad = 0.2f;    // Cuánto crece y se encoge (ej: +- 0.2 metros)
    public float velocidad = 3f;       // Qué tan rápido late

    [Header("Opcional: Transparencia")]
    public bool animarTransparencia = true;
    public SpriteRenderer spriteRenderer; // Para cambiar la opacidad
    public float alphaMin = 0.3f;
    public float alphaMax = 0.6f;

    void Update()
    {
        //calcular onda
        float onda = Mathf.Sin(Time.time * velocidad);

        
        float radioActual = radioBase + (onda * intensidad);

        // actualizar borde
        if (lineVisual != null)
        {
            lineVisual.DrawCircle(radioActual);
        }

        // actualizar relleno
        if (spriteVisual != null)
        {
            
            float escala = radioActual * 2f;
            spriteVisual.localScale = new Vector3(escala, escala, 1f);
        }

        
        if (animarTransparencia && spriteRenderer != null)
        {
            
            float t = (onda + 1f) / 2f;

            Color colorActual = spriteRenderer.color;
            colorActual.a = Mathf.Lerp(alphaMin, alphaMax, t);
            spriteRenderer.color = colorActual;
        }
    }
}