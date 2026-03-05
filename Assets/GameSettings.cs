using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings instance;

    public bool shakeEnabled = true;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            shakeEnabled = PlayerPrefs.GetInt("ShakeEnabled", 1) == 1;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetShake(bool value)
    {
        shakeEnabled = value;
        PlayerPrefs.SetInt("ShakeEnabled", value ? 1 : 0);
        PlayerPrefs.Save();
    }
}