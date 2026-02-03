using UnityEngine;
using System.Collections.Generic;

public class PopulationManager : MonoBehaviour
{
    public GameObject personPrefab;

    public float spawnInterval = 3f;
    public float maxPopulation = 15f;
    float baseSpawnInterval;
    public float baseMaxPopulation = 15f;

    public int initialPopulation = 10;

    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);

    private float timer;
    private int personsSpawnedToday = 0;
    private int shiniesRemainingToSpawn = 0; // Cambiado a int para control exacto

    private List<int> shinyIndices = new List<int>();

    // Este método ahora recibe el número exacto (0, 1 o 2) desde LevelManager
    public void ConfigureRound(int shinyCount)
    {
        shiniesRemainingToSpawn = shinyCount;
        personsSpawnedToday = 0;
        timer = 0;

        shinyIndices.Clear();

        if (shiniesRemainingToSpawn > 0)
        {
            // Generamos tantos índices como shinies haya pedido el LevelManager
            for (int i = 0; i < shiniesRemainingToSpawn; i++)
            {
                int newIndex;
                int safety = 0;

                do
                {
                    // Rango: desde la primera persona hasta la población inicial + margen
                    newIndex = Random.Range(1, initialPopulation + 5);
                    safety++;
                }
                while (shinyIndices.Contains(newIndex) && safety < 50);

                shinyIndices.Add(newIndex);
            }
            Debug.Log("<color=cyan>PopulationManager:</color> Índices Shiny para hoy: " + string.Join(", ", shinyIndices));
        }

        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

        // Spawn inicial
        for (int i = 0; i < initialPopulation; i++)
        {
            SpawnPerson();
        }
    }

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;
        int currentCount = GameObject.FindGameObjectsWithTag("Persona").Length;

        if (timer >= spawnInterval && currentCount < maxPopulation)
        {
            SpawnPerson();
            timer = 0;
        }

        if (Guardado.instance != null)
        {
            float bonus = Guardado.instance.populationBonus;
            maxPopulation = baseMaxPopulation * (1f + bonus);
        }
    }

    void SpawnPerson()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject newPerson = Instantiate(personPrefab, spawnPos, Quaternion.identity);
        personsSpawnedToday++;

        // Verificamos si esta persona específica debe ser Shiny
        if (shinyIndices.Contains(personsSpawnedToday))
        {
            PersonaInfeccion infeccion = newPerson.GetComponent<PersonaInfeccion>();
            if (infeccion != null)
            {
                infeccion.MakeShiny();
                Debug.Log("<color=yellow>¡Ha nacido un Shiny!</color> Persona nº: " + personsSpawnedToday);
            }
        }
    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;
        float bonus = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval * (1f - bonus);
        if (spawnInterval < 0.3f) spawnInterval = 0.3f;
    }
}