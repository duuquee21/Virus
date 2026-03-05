using UnityEngine;
using UnityEngine.UI;

public class ControlVolumenVFX : MonoBehaviour
{
    public Slider sliderVFX;

    void Start()
    {
        float volumenGuardado = PlayerPrefs.GetFloat("VFXVolume", 0.75f);
        sliderVFX.value = volumenGuardado;

        AudioManager.instance.UpdateMixerVolume("SFXVol", volumenGuardado);
    }

    public void CambiarVolumenVFX(float valor)
    {
        AudioManager.instance.UpdateMixerVolume("SFXVol", valor);

        PlayerPrefs.SetFloat("VFXVolume", valor);
        PlayerPrefs.Save();
    }
}