using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class SelectorHorizontalUI : MonoBehaviour
{
    // Para evitar repeticiones rápidas
    private float dpadCooldown = 0.25f;
    private float lastDpadTime = 0f;

    void Update()
    {
        // Solo responde si está seleccionado en el EventSystem
        if (!gameObject.activeInHierarchy || !UnityEngine.EventSystems.EventSystem.current) return;
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != gameObject) return;

        // Soporte para Input System
        try {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && Time.unscaledTime - lastDpadTime > dpadCooldown)
            {
                if (gamepad.dpad.left.wasPressedThisFrame)
                {
                    Anterior();
                    lastDpadTime = Time.unscaledTime;
                }
                if (gamepad.dpad.right.wasPressedThisFrame)
                {
                    Siguiente();
                    lastDpadTime = Time.unscaledTime;
                }
            }
        } catch { }
    }
    [Header("Referencias Visuales")]
    public TextMeshProUGUI textoOpcion; // El texto del medio (ej: "Espa�ol", "60 FPS")

    [Header("Configuraci�n")]
    public List<string> opciones = new List<string>(); // Aqu� escribes tus opciones en el Inspector
    public int indiceActual = 0; // Por defecto empieza en la primera opci�n

    [Header("Eventos")]
    // Esto te permitir� arrastrar funciones (como cambiar el idioma) directamente en el Inspector
    public UnityEvent<int> onValueChanged;

    void Start()
    {
        ActualizarTexto();
    }

    public void Siguiente()
    {
        if (opciones.Count == 0) return;

        indiceActual++;
        // Si nos pasamos de la �ltima, volvemos a la primera (efecto ruleta)
        if (indiceActual >= opciones.Count) indiceActual = 0;

        ActualizarTexto();
        onValueChanged.Invoke(indiceActual);
    }

    public void Anterior()
    {
        if (opciones.Count == 0) return;

        indiceActual--;
        // Si bajamos de la primera, vamos a la �ltima (efecto ruleta)
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