using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FeedbackAnimacion : MonoBehaviour
{
    [Header("Objetos a Afectar")]
    [Tooltip("Arrastra aquí todos los Sprites que quieres que brillen")]
    public List<SpriteRenderer> objetosFeedback = new List<SpriteRenderer>();

    [Header("Configuración de Flash")]
    public Color colorFlash = Color.white;
    public float duracionFlash = 0.1f;

    // Estructura para recordar cómo era cada objeto antes del flash
    private struct DatosOriginales
    {
        public SpriteRenderer renderer;
        public Color color;
    }

    private List<DatosOriginales> listaDatos = new List<DatosOriginales>();
    private Coroutine corrutinaFeedback;

    void Awake()
    {
        // Guardamos los colores originales de todos los objetos en la lista
        foreach (SpriteRenderer sr in objetosFeedback)
        {
            if (sr != null)
            {
                listaDatos.Add(new DatosOriginales
                {
                    renderer = sr,
                    color = sr.color
                });
            }
        }
    }

    public void EjecutarFeedback()
    {
        if (corrutinaFeedback != null)
        {
            StopCoroutine(corrutinaFeedback);
            ResetearEstadoOriginal();
        }

        corrutinaFeedback = StartCoroutine(RutinaFeedback());
    }

    IEnumerator RutinaFeedback()
    {
        // 1. Aplicar flash de color a todos los objetos
        foreach (var dato in listaDatos)
        {
            if (dato.renderer != null)
            {
                dato.renderer.color = colorFlash;
            }
        }

        // 2. Esperar
        yield return new WaitForSeconds(duracionFlash);

        // 3. Restaurar color original
        ResetearEstadoOriginal();

        corrutinaFeedback = null;
    }

    void ResetearEstadoOriginal()
    {
        foreach (var dato in listaDatos)
        {
            if (dato.renderer != null)
            {
                dato.renderer.color = dato.color;
            }
        }
    }
}