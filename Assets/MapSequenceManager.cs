using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public string mapName;
    public float maxHealth;

    [HideInInspector]
    public float currentHealth;
}

public class MapSequenceManager : MonoBehaviour
{
    public static MapSequenceManager instance;

    [Header("Orden de Mapas")]
    public List<MapData> maps = new List<MapData>();

    private int currentMapIndex = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadIndexFromPrefs();
        InitializeMapHealthIfNeeded();
    }

    private void LoadIndexFromPrefs()
    {
        if (maps == null || maps.Count == 0)
        {
            currentMapIndex = 0;
            return;
        }

        currentMapIndex = Mathf.Clamp(PlayerPrefs.GetInt("CurrentMapIndex", 0), 0, maps.Count - 1);
    }

    private void InitializeMapHealthIfNeeded()
    {
        if (maps == null) return;

        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].currentHealth <= 0f || maps[i].currentHealth > maps[i].maxHealth)
            {
                maps[i].currentHealth = maps[i].maxHealth;
            }
        }
    }

    public void ResetAllMapHealth()
    {
        if (maps == null) return;

        for (int i = 0; i < maps.Count; i++)
        {
            maps[i].currentHealth = maps[i].maxHealth;
        }
    }

    public float GetCurrentMapHealth()
    {
        MapData map = GetCurrentMap();
        return map != null ? map.maxHealth : 0f;
    }

    public void NextMap()
    {
        if (maps == null || maps.Count == 0) return;

        currentMapIndex++;

        if (currentMapIndex >= maps.Count)
        {
            Debug.Log("Juego completado");
            currentMapIndex = maps.Count - 1;
            return;
        }

        if (LevelManager.instance != null)
            LevelManager.instance.NextMapTransition();
    }

    public void ResetToFirstMap()
    {
        currentMapIndex = 0;
        SaveCurrentMapIndex();
        ResetAllMapHealth();
    }

    public void SetCurrentMapIndex(int index, bool resetHealth = false)
    {
        if (maps == null || maps.Count == 0)
        {
            currentMapIndex = 0;
            return;
        }

        currentMapIndex = Mathf.Clamp(index, 0, maps.Count - 1);
        SaveCurrentMapIndex();

        if (resetHealth)
            ResetAllMapHealth();
    }

    private void SaveCurrentMapIndex()
    {
        PlayerPrefs.SetInt("CurrentMapIndex", currentMapIndex);
        PlayerPrefs.Save();
    }

    public MapData GetCurrentMap()
    {
        if (maps == null || maps.Count == 0) return null;
        return maps[currentMapIndex];
    }

    public MapData GetMap(int index)
    {
        if (maps == null || maps.Count == 0) return null;
        index = Mathf.Clamp(index, 0, maps.Count - 1);
        return maps[index];
    }

    public int GetCurrentMapIndex()
    {
        return currentMapIndex;
    }
}