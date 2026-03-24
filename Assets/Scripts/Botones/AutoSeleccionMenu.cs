using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class AutoSeleccionMenu : MonoBehaviour
{
    [Header("Botón que se seleccionará al abrir el menú")]
    public GameObject primerBoton;

    // OnEnable se ejecuta justo en el instante en que el menú se hace visible (SetActive(true))
    void OnEnable()
    {
        StartCoroutine(SeleccionarConRetraso());
    }

    private IEnumerator SeleccionarConRetraso()
    {
        // Esperamos un frame para que Unity termine de encender el panel y el EventSystem esté listo
        yield return null;

        if (primerBoton != null && EventSystem.current != null)
        {
            // 1. Limpiamos cualquier selección fantasma que haya quedado
            EventSystem.current.SetSelectedGameObject(null);

            // 2. Le decimos a Unity: "¡Oye, haz como si el jugador hubiera hecho clic aquí!"
            EventSystem.current.SetSelectedGameObject(primerBoton);
        }
    }
}