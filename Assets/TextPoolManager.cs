using System.Collections.Generic;
using UnityEngine;

public class TextPooler : MonoBehaviour
{
    public static TextPooler Instance;

    [Header("Ajustes del Pool")]
    public GameObject textPrefab;
    public int initialPoolSize = 20;

    private List<GameObject> pool = new List<GameObject>();

    void Awake()
    {
        Instance = this;

        // Pre-instanciar los objetos al inicio (carga controlada)
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewInstance();
        }
    }

    private GameObject CreateNewInstance()
    {
        GameObject obj = Instantiate(textPrefab, transform);
        obj.SetActive(false);
        pool.Add(obj);
        return obj;
    }

    public GameObject SpawnText(Vector3 position, string text)
    {
        GameObject obj = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy)
            {
                obj = pool[i];
                break;
            }
        }

        if (obj == null)
        {
            obj = CreateNewInstance();
        }

        obj.transform.position = position;
        obj.SetActive(true); // Activar primero para que OnEnable de FloatingText se ejecute

        FloatingText ft = obj.GetComponent<FloatingText>();
        if (ft != null) ft.SetText(text);

        return obj; // <--- ESTA LÍNEA ES VITAL
    }
}