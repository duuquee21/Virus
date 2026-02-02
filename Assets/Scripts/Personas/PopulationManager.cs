using UnityEngine;
using System.Collections.Generic; // <--- 1. NECESARIO PARA USAR LISTAS

public class PopulationManager : MonoBehaviour
{
    public GameObject personPrefab;

    public float spawnInterval = 3f;
    public float maxPopulation = 15f;
    float baseSpawnInterval;
    public float baseMaxPopulation = 15f;
    
    public int initialPopulation = 10;

    // area de aparicion
    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);
    
    // DATOS INTERNOS DE LA RONDA
    private float timer;
    private int personsSpawnedToday = 0; 
    private bool spawnShinyToday = false; 
    
    // 2. CAMBIO: En vez de 'int shinyIndex', usamos una lista
    private List<int> shinyIndices = new List<int>();

    // probabilidad shiny 
    public float shinyChance = 10f;
    
    public void ConfigureRound(bool hasShiny)
    {
        spawnShinyToday = hasShiny;
        personsSpawnedToday = 0;
        timer = 0;
        
        // Limpiamos la lista del día anterior para no mezclar
        shinyIndices.Clear();

        if (spawnShinyToday)
        {
            // 3. CAMBIO: Calculamos cuántos Shinies tocan hoy
            // Base (1) + Los que hayas comprado en el árbol
            int totalShinies = 1;
            
            if (Guardado.instance != null)
            {
                totalShinies += Guardado.instance.extraShiniesPerRound;
            }

            // Generamos los números aleatorios (ej: persona 3 y persona 8)
            for (int i = 0; i < totalShinies; i++)
            {
                int newIndex;
                int safety = 0;
                
                // Buscamos un número que no esté repetido
                do
                {
                    // Rango: desde el 1 hasta los iniciales + 10 extra
                    newIndex = Random.Range(1, initialPopulation + 10);
                    safety++;
                } 
                while (shinyIndices.Contains(newIndex) && safety < 50);

                shinyIndices.Add(newIndex);
            }
        }

        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

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
        
        // Seguridad por si Guardado no existe
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
        
        // 4. CAMBIO: Verificar si el número actual está en la LISTA
        if (spawnShinyToday && shinyIndices.Contains(personsSpawnedToday))
        {
            PersonaInfeccion infeccion = newPerson.GetComponent<PersonaInfeccion>();
            if (infeccion != null)
            {
                infeccion.MakeShiny();
                Debug.Log("Ha nacido el shiny número: " + personsSpawnedToday);
            }
        }
    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;

        float bonus = Guardado.instance.spawnSpeedBonus;

        spawnInterval = baseSpawnInterval * (1f - bonus);

        if (spawnInterval < 0.3f)
            spawnInterval = 0.3f; // límite de seguridad
    }
}