using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SyncToggleMovement : MonoBehaviour
{
    void Start()
    {
        Toggle toggle = GetComponent<Toggle>();

        if (toggle != null && Guardado.instance != null)
        {
            // SetIsOnWithoutNotify cambia el check del UI SIN ejecutar tu toogleMovement()
            // Ahora: ON = Mouse, OFF = Keyboard or Controller
            toggle.SetIsOnWithoutNotify(Guardado.instance.inputType == Guardado.InputType.Mouse);
        }
    }
}