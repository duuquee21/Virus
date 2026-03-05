using TMPro;
using UnityEngine;

public class ControlFPS : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    int[] fpsValues = { 30, 60, 120, 144, 240 };

    void Start()
    {
        QualitySettings.vSyncCount = 0;

        int savedFPS = PlayerPrefs.GetInt("FPSLimit", 120);

        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == savedFPS)
            {
                dropdown.value = i;
                break;
            }
        }

        Application.targetFrameRate = savedFPS;
    }

    public void ChangeFPS(int index)
    {
        int fps = fpsValues[index];

        Application.targetFrameRate = fps;

        PlayerPrefs.SetInt("FPSLimit", fps);
        PlayerPrefs.Save();
    }
}