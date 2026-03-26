using UnityEngine;

public class OrbitaSistemaUI : MonoBehaviour
{
    public RectTransform[] planetas;   // IMÁGENES UI
    public float radioX = 300f;
    public float radioY = 120f;

    public float escalaMin = 0.6f;
    public float escalaMax = 1.4f;
    public float suavizado = 6f;

    float anguloObjetivo = 0f;
    float anguloActual = 0f;
    float separacionAngulo;

    void Start()
    {
        separacionAngulo = (2 * Mathf.PI) / planetas.Length;
    }

    void Update()
    {
        anguloActual = Mathf.Lerp(anguloActual, anguloObjetivo, Time.deltaTime * suavizado);

        for (int i = 0; i < planetas.Length; i++)
        {
            float angulo = anguloActual + i * separacionAngulo;

            float x = Mathf.Cos(angulo) * radioX;
            float y = Mathf.Sin(angulo) * radioY;

            // POSICIÓN UI
            planetas[i].anchoredPosition = new Vector2(x, y);

            float t = (Mathf.Sin(angulo) + 1f) / 2f;
            float escala = Mathf.Lerp(escalaMin, escalaMax, t);
            planetas[i].localScale = Vector3.one * escala;

            // ORDEN VISUAL (el de delante encima)
            planetas[i].SetSiblingIndex(Mathf.RoundToInt(t * 100));
        }
    }

    public void Siguiente()
    {
        anguloObjetivo -= separacionAngulo;
    }

    public void Anterior()
    {
        anguloObjetivo += separacionAngulo;
    }

    // 🔥 IMPORTANTE: detectar cuál está al frente
    public RectTransform GetPlanetaAlFrente()
    {
        RectTransform frente = planetas[0];
        float maxEscala = planetas[0].localScale.x;

        foreach (var p in planetas)
        {
            if (p.localScale.x > maxEscala)
            {
                maxEscala = p.localScale.x;
                frente = p;
            }
        }

        return frente;
    }
}
