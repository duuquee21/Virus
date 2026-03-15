using UnityEngine;
using UnityEngine.UI;

public class ControlVolumenVFX : MonoBehaviour
{
    public Slider sliderVFX;

    void Start()
    {
        float volumenGuardado = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        sliderVFX.value = volumenGuardado;

        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("SFXVol", volumenGuardado);
    }

    public void CambiarVolumenVFX(float valor)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.UpdateMixerVolume("SFXVol", valor);

        PlayerPrefs.SetFloat("SFXVolume", valor);
        PlayerPrefs.Save();
    }
}