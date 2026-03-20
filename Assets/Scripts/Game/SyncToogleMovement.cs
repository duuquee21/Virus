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
            // Como asumo que si el toggle está ON = ratón, le pasamos !UseKeyboard
            toggle.SetIsOnWithoutNotify(!Guardado.instance.UseKeyboard);
        }
    }
}