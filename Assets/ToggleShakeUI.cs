using UnityEngine;
using UnityEngine.UI;

public class ToggleShakeUI : MonoBehaviour
{
    public Toggle toggle;

    void Start()
    {
        toggle.isOn = GameSettings.instance.shakeEnabled;
    }
}