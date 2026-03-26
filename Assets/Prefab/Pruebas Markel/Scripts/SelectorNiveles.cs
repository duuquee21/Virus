using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectorNiveles : MonoBehaviour
{
    [Header("Datos")]
    public NivelSO[] niveles; // Arrastra aquí tus 5 ScriptableObjects
    private int indiceActual = 0;

    [Header("Referencias UI")]
    public Image displayImagen;
    public TextMeshProUGUI textoNombre;
    public Animator animador; // Para las animaciones de cambio

    void Start() => ActualizarInterfaz();

    public void SiguienteNivel()
    {
        // Si es mayor que el último, vuelve al 0
        indiceActual = (indiceActual + 1) % niveles.Length;
        CambiarConAnimacion();
    }

    public void AnteriorNivel()
    {
        // Si es menor que 0, va al último
        indiceActual--;
        if (indiceActual < 0) indiceActual = niveles.Length - 1;
        CambiarConAnimacion();
    }

    void CambiarConAnimacion()
    {
        // Disparamos un trigger en el Animator
        animador.SetTrigger("Cambio");
    }

    // Este método lo llamaremos DESDE la animación o con un pequeño delay
    public void ActualizarInterfaz()
    {
        displayImagen.sprite = niveles[indiceActual].imagenNivel;
        textoNombre.text = niveles[indiceActual].nombreNivel;
    }

    public void JugarNivel()
    {
        Debug.Log("Cargando: " + niveles[indiceActual].nombreEscena);
        // UnityEngine.SceneManagement.SceneManager.LoadScene(niveles[indiceActual].nombreEscena);
    }
}