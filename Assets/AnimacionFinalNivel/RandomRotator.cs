using System.Collections.Generic;
using UnityEngine;

public class RandomTransformModifier : MonoBehaviour
{
    [Header("Configuración de Objetos")]
    [Tooltip("Arrastra aquí los objetos que quieras modificar")]
    public List<GameObject> objetosAModificar;

    [Header("Ajustes de Escala")]
    public float escalaMinima = 0.8f;
    public float escalaMaxima = 1.5f;

    [ContextMenu("Aleatorizar Rotación y Escala")]
    public void AplicarTransformacionesAleatorias()
    {
        if (objetosAModificar == null || objetosAModificar.Count == 0)
        {
            Debug.LogWarning("La lista de objetos está vacía. ¡Añade algunos en el Inspector!");
            return;
        }

        foreach (GameObject obj in objetosAModificar)
        {
            if (obj != null)
            {
                // --- Rotación Aleatoria (Eje Z) ---
                float anguloAleatorio = Random.Range(0f, 360f);
                Vector3 rotacionActual = obj.transform.eulerAngles;
                obj.transform.eulerAngles = new Vector3(rotacionActual.x, rotacionActual.y, anguloAleatorio);

                // --- Escala Aleatoria ---
                float escalaUniforme = Random.Range(escalaMinima, escalaMaxima);
                obj.transform.localScale = new Vector3(escalaUniforme, escalaUniforme, escalaUniforme);
            }
        }

        Debug.Log($"Se ha aplicado rotación y escala aleatoria a {objetosAModificar.Count} objetos.");
    }
}