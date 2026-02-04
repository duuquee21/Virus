using UnityEngine;

public class OrbitaSistema : MonoBehaviour
{
    public Transform[] planetas; // Tus planetas en la escena
    public float radioX = 10f;   // Ancho de la elipse
    public float radioY = 3f;    // Alto de la elipse (para el efecto inclinado)
    public float escalaMin = 0.5f; // Tamaño del planeta al fondo
    public float escalaMax = 2.0f; // Tamaño del planeta al frente
    public float suavizado = 5f;

    private float anguloObjetivo = 0f;
    private float anguloActual = 0f;
    private float separacionAngulo;

    void Start()
    {
        // Dividimos los 360 grados entre el número de planetas
        separacionAngulo = (2 * Mathf.PI) / planetas.Length;
    }

    void Update()
    {
        // Transición suave entre posiciones
        anguloActual = Mathf.Lerp(anguloActual, anguloObjetivo, Time.deltaTime * suavizado);

        for (int i = 0; i < planetas.Length; i++)
        {
            // Calculamos el ángulo individual de cada planeta
            float anguloPlaneta = anguloActual + (i * separacionAngulo);

            // 1. Posición en la elipse (X, Y)
            float x = Mathf.Cos(anguloPlaneta) * radioX;
            float y = Mathf.Sin(anguloPlaneta) * radioY;
            planetas[i].localPosition = new Vector3(x, y, 0);

            // 2. Efecto de Tamaño (Escala)
            // Usamos el valor de Y para saber si está "cerca" o "lejos"
            // t va de 0 a 1 (0 arriba/atrás, 1 abajo/adelante)
            float t = (Mathf.Sin(anguloPlaneta) + 1f) / 2f;
            float escala = Mathf.Lerp(escalaMin, escalaMax, t);
            planetas[i].localScale = new Vector3(escala, escala, 1);

            // 3. Orden de dibujado (Opcional)
            // Para que los planetas de delante tapen a los de atrás
            SpriteRenderer sr = planetas[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = Mathf.RoundToInt(t * 100);
        }
    }

    public void Siguiente() => anguloObjetivo -= separacionAngulo;
    public void Anterior() => anguloObjetivo += separacionAngulo;
}