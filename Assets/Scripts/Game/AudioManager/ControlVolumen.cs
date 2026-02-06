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
            
            float volumenActual = AudioListener.volume;
            
           
            sliderVolumen.value = volumenActual;
        }
    }

    
    public void CambiarVolumen(float valor)
    {
        
        AudioListener.volume = valor;

        
        PlayerPrefs.SetFloat("VolumenGlobal", valor);
        PlayerPrefs.Save();
    }
}