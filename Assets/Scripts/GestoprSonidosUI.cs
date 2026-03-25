using UnityEngine;

public class GestorSonidosUI : MonoBehaviour
{
    // Esta variable estática nos permite acceder a este script desde CUALQUIER otro script sin buscarlo
    public static GestorSonidosUI Instancia;

    [Header("Configuración Global")]
    public AudioSource audioSource;
    public AudioClip sonidoHoverGlobal;
    public AudioClip sonidoClickGlobal;

    void Awake()
    {
        // Configuramos el Singleton
        if (Instancia == null)
        {
            Instancia = this;
            // Opcional: Descomenta la siguiente línea si quieres que la música siga al cambiar de escena
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // Evitamos que haya dos gestores duplicados
        }

        // Si se nos olvidó poner un AudioSource, lo busca
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void ReproducirHover()
    {
        if (audioSource != null && sonidoHoverGlobal != null)
        {
            audioSource.PlayOneShot(sonidoHoverGlobal);
        }
    }

    public void ReproducirClick()
    {
        if (audioSource != null && sonidoClickGlobal != null)
        {
            audioSource.PlayOneShot(sonidoClickGlobal);
        }
    }
}