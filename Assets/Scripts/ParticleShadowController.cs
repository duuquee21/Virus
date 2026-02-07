using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleShadowController : MonoBehaviour
{
    [Header("Posicionamiento de Sombra")]
    public Vector3 offset = new Vector3(0.15f, -0.15f, 0.05f);

    [Header("Ajustes de Color")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);

    [Header("Capa y Orden")]
    public string sortingLayerName = "Default";
    public int shadowSortingOrder = -1;

    private ParticleSystem _parentSystem;
    private ParticleSystem _shadowSystem;
    private ParticleSystemRenderer _shadowRenderer;

    void Start()
    {
        _parentSystem = GetComponent<ParticleSystem>();

        // Crear el objeto que contendrá la sombra
        GameObject shadowObj = new GameObject(gameObject.name + "_Shadow");

        // Lo hacemos hijo, pero con rotación y escala global independiente para el offset
        shadowObj.transform.SetParent(transform);
        shadowObj.transform.localPosition = offset;
        shadowObj.transform.localRotation = Quaternion.identity;
        shadowObj.transform.localScale = Vector3.one;

        // Clonamos el sistema de partículas completo
        // Esto copia la forma, velocidad, gravedad y tiempos.
        _shadowSystem = shadowObj.AddComponent<ParticleSystem>();

        // Usamos este truco para copiar TODA la configuración del inspector
        UnityEditorInternal.ComponentUtility.CopyComponent(_parentSystem);
        UnityEditorInternal.ComponentUtility.PasteComponentValues(_shadowSystem);

        // Ajustamos la sombra para que no emita sus propios scripts si los tuviera
        var main = _shadowSystem.main;
        main.startColor = shadowColor;

        // Configuramos el Renderer de la sombra
        _shadowRenderer = shadowObj.GetComponent<ParticleSystemRenderer>();
        _shadowRenderer.sortingLayerName = sortingLayerName;
        _shadowRenderer.sortingOrder = shadowSortingOrder;

        // Desactivamos colisiones en la sombra para ahorrar recursos
        var collision = _shadowSystem.collision;
        if (collision.enabled) collision.enabled = false;
    }

    void LateUpdate()
    {
        if (_parentSystem == null || _shadowSystem == null) return;

        // Sincronización de Play/Stop
        if (_parentSystem.isPlaying && _shadowSystem.isPaused)
            _shadowSystem.Play();
        else if (_parentSystem.isStopped && _shadowSystem.isPlaying)
            _shadowSystem.Stop();

        // Si el sistema original se destruye o desactiva
        if (!_parentSystem.gameObject.activeInHierarchy)
            _shadowSystem.gameObject.SetActive(false);
        else
            _shadowSystem.gameObject.SetActive(true);
    }
}