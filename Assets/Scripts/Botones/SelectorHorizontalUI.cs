using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class SelectorHorizontalUI : MonoBehaviour
{
    [Header("Referencias Visuales")]
    public TextMeshProUGUI textoOpcion; // El texto del medio (ej: "Español", "60 FPS")

    [Header("Configuración")]
    public List<string> opciones = new List<string>(); // Aquí escribes tus opciones en el Inspector
    public int indiceActual = 0; // Por defecto empieza en la primera opción

    [Header("Eventos")]
    // Esto te permitirá arrastrar funciones (como cambiar el idioma) directamente en el Inspector
    public UnityEvent<int> onValueChanged;

    void Start()
    {
        ActualizarTexto();
    }

    public void Siguiente()
    {
        if (opciones.Count == 0) return;

        indiceActual++;
        // Si nos pasamos de la última, volvemos a la primera (efecto ruleta)
        if (indiceActual >= opciones.Count) indiceActual = 0;

        ActualizarTexto();
        onValueChanged.Invoke(indiceActual);
    }

    public void Anterior()
    {
        if (opciones.Count == 0) return;

        indiceActual--;
        // Si bajamos de la primera, vamos a la última (efecto ruleta)
        if (indiceActual < 0) indiceActual = opciones.Count - 1;

        ActualizarTexto();
        onValueChanged.Invoke(indiceActual);
    }

    public void EstablecerIndice(int nuevoIndice)
    {
        if (nuevoIndice >= 0 && nuevoIndice < opciones.Count)
        {
            indiceActual = nuevoIndice;
            ActualizarTexto();
        }
    }

    private void ActualizarTexto()
    {
        if (textoOpcion != null && opciones.Count > 0)
        {
            textoOpcion.text = opciones[indiceActual];
        }
    }
}