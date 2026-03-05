using UnityEngine;
using UnityEngine.UI;

public class ControlVolumenMaster : MonoBehaviour
{
    public Slider sliderMaster;

    void Start()
    {
        float volumenGuardado = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        sliderMaster.value = volumenGuardado;

        AudioManager.instance.UpdateMixerVolume("Master", volumenGuardado);
    }

    public void CambiarVolumenMaster(float valor)
    {
        AudioManager.instance.UpdateMixerVolume("Master", valor);

        PlayerPrefs.SetFloat("MasterVolume", valor);
        PlayerPrefs.Save();
    }
}