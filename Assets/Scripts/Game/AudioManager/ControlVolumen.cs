using UnityEngine;
using UnityEngine.UI;

public class ControlVolumen : MonoBehaviour
{
    [Header("Referencias")]
    public Slider sliderVolumen; 

    
    void OnEnable()
    {
        if (sliderVolumen != null)
        {
            float volumenActual = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            AudioListener.volume = volumenActual;

            if (AudioManager.instance != null)
                AudioManager.instance.UpdateMixerVolume("Master", volumenActual);

            sliderVolumen.value = volumenActual;
        }
    }

    
    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;

        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("Master", valor);

        PlayerPrefs.SetFloat("MasterVolume", valor);
        PlayerPrefs.Save();
    }
}