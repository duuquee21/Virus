using UnityEngine;

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
    private int shinyIndex = -1;

   
    
    //probabilidad shiny 
    public float shinyChance = 10f;
    
    public void ConfigureRound(bool hasShiny)
    {
        spawnShinyToday = hasShiny;
        personsSpawnedToday = 0;
        timer = 0;

        if (spawnShinyToday)
        {
            shinyIndex = Random.Range(1,10);
        }
        else
        {
            shinyIndex = -1;
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
        float bonus = Guardado.instance.populationBonus;
        maxPopulation = baseMaxPopulation * (1f + bonus);

    }

    void SpawnPerson()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject newPerson = Instantiate(personPrefab, spawnPos, Quaternion.identity);
        
        personsSpawnedToday++;
        // verificar si es el shiny 

        if (spawnShinyToday && personsSpawnedToday == shinyIndex)
        {
            PersonaInfeccion infeccion = newPerson.GetComponent<PersonaInfeccion>();
            if (infeccion != null)
            {
                infeccion.MakeShiny();
                Debug.Log("Ha nacido el shiny");
            }
        }

    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;

        float bonus = Guardado.instance.spawnSpeedBonus;

        spawnInterval = baseSpawnInterval * (1f - bonus);

        if (spawnInterval < 0.3f)
            spawnInterval = 0.3f; // l�mite de seguridad
    }



}
