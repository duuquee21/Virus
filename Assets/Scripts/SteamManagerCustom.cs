using UnityEngine;
using Steamworks;

public class SteamManagerCustom : MonoBehaviour
{
    public static SteamManagerCustom Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Esta es la función que llamaremos desde otros scripts
    public void UnlockAchievement(string id)
    {
        if (!SteamManager.Initialized) return;

        SteamUserStats.GetAchievement(id, out bool achieved);

        if (!achieved)
        {
            SteamUserStats.SetAchievement(id);
            SteamUserStats.StoreStats();
            Debug.Log($"Logro {id} desbloqueado!");
        }
    }
}