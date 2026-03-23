using UnityEngine;

public class DestroyOnGameEnd : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1f;
    void Update()
    {
        // Si el LevelManager existe y el juego ya NO está activo...
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    
}
