using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetHealthBarUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image fillImage;
    public TextMeshProUGUI percentText;

    public void Setup(string nombre, float porcentaje)
    {
        nameText.text = nombre;

        fillImage.fillAmount = porcentaje;

        int percentInt = Mathf.RoundToInt(porcentaje * 100f);
        percentText.text = percentInt + "%";
    }
}