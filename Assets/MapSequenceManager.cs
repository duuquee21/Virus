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

    void Start()
    {
        for (int i = 0; i < maps.Count; i++)
        {
            maps[i].currentHealth = maps[i].maxHealth;
        }
    }
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public float GetCurrentMapHealth()
    {
        return maps[currentMapIndex].maxHealth;
    }

    public void NextMap()
    {
        currentMapIndex++;

        if (currentMapIndex >= maps.Count)
        {
            Debug.Log("Juego completado");
            return;
        }

        LevelManager.instance.NextMapTransition();
    }

    public MapData GetCurrentMap()
    {
        return maps[currentMapIndex];
    }

    public int GetCurrentMapIndex()
    {
        return currentMapIndex;
    }
}