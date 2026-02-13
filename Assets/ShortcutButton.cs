using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ShortcutButton : MonoBehaviour
{
    [Header("Configuración del Atajo")]
    [Tooltip("La tecla que activará este botón")]
    public KeyCode teclaAtajo;

    private Button miBoton;

    void Awake()
    {
        miBoton = GetComponent<Button>();
    }

    void Update()
    {
        // Detectamos la tecla asignada
        if (Input.GetKeyDown(teclaAtajo))
        {
            // Verificamos que el botón pueda ser pulsado (interactable) 
            // y que el objeto esté visible en el juego
            if (miBoton.interactable && gameObject.activeInHierarchy)
            {
                miBoton.onClick.Invoke();
            }
        }
    }
}