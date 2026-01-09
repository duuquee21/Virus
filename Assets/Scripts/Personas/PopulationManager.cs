using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    public GameObject personPrefab;

    public float spawnInterval = 3f;
    public float maxPopulation = 15f;

    // area de aparicion

    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);

    private float timer;
    

    
    void Update()
    {
        timer += Time.deltaTime;

        //cantidad de personas

        int currentCount = FindObjectsOfType<Movement>().Length;

        if (timer >= spawnInterval && currentCount < maxPopulation)
        {
            SpawnPerson();
            timer = 0;
        }
    }

    void SpawnPerson()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        Instantiate(personPrefab, spawnPos, Quaternion.identity);

    }
}
