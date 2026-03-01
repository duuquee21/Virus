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

    void Awake()
    {
        _parentSystem = GetComponent<ParticleSystem>();
        CreateShadowSystem();
    }

    void CreateShadowSystem()
    {
        GameObject shadowObj = new GameObject(gameObject.name + "_Shadow_Runtime");
        shadowObj.transform.SetParent(transform);
        shadowObj.transform.localPosition = offset;
        shadowObj.transform.localRotation = Quaternion.identity;
        shadowObj.transform.localScale = Vector3.one;

        _shadowSystem = shadowObj.AddComponent<ParticleSystem>();
        _shadowRenderer = shadowObj.GetComponent<ParticleSystemRenderer>();

        // 1. Reset para evitar errores de configuración
        _shadowSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 2. Sincronizar Semilla para que el movimiento sea idéntico
        _shadowSystem.useAutoRandomSeed = false;
        _shadowSystem.randomSeed = _parentSystem.randomSeed;

        SyncParticleModules();

        // 3. Configurar Renderer
        _shadowRenderer.sortingLayerName = sortingLayerName;
        _shadowRenderer.sortingOrder = shadowSortingOrder;

        var parentRenderer = _parentSystem.GetComponent<ParticleSystemRenderer>();
        if (parentRenderer != null)
        {
            _shadowRenderer.material = parentRenderer.material;
            _shadowRenderer.renderMode = parentRenderer.renderMode;
        }
    }

    void SyncParticleModules()
    {
        // --- MÓDULO MAIN (Rotación Inicial) ---
        var mainRef = _parentSystem.main;
        var shadowMain = _shadowSystem.main;

        shadowMain.duration = mainRef.duration;
        shadowMain.loop = mainRef.loop;
        shadowMain.startLifetime = mainRef.startLifetime;
        shadowMain.startSpeed = mainRef.startSpeed;
        shadowMain.startSize = mainRef.startSize;

        // Copiamos la rotación inicial (eje Z por defecto en 2D/UI)
        shadowMain.startRotation = mainRef.startRotation;
        shadowMain.startRotationMultiplier = mainRef.startRotationMultiplier;

        shadowMain.gravityModifier = mainRef.gravityModifier;
        shadowMain.simulationSpace = mainRef.simulationSpace;
        shadowMain.scalingMode = mainRef.scalingMode;
        shadowMain.maxParticles = mainRef.maxParticles;
        shadowMain.startColor = shadowColor;

        // --- MÓDULO EMISSION ---
        var emRef = _parentSystem.emission;
        var emShadow = _shadowSystem.emission;
        emShadow.enabled = emRef.enabled;
        emShadow.rateOverTime = emRef.rateOverTime;

        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emRef.burstCount];
        emRef.GetBursts(bursts);
        emShadow.SetBursts(bursts);

        // --- MÓDULO SHAPE ---
        var shapeRef = _parentSystem.shape;
        var shapeShadow = _shadowSystem.shape;
        shapeShadow.enabled = shapeRef.enabled;
        shapeShadow.shapeType = shapeRef.shapeType;
        shapeShadow.radius = shapeRef.radius;
        shapeShadow.scale = shapeRef.scale;

        // --- MÓDULO SIZE OVER LIFETIME ---
        var sizeRef = _parentSystem.sizeOverLifetime;
        var sizeShadow = _shadowSystem.sizeOverLifetime;
        sizeShadow.enabled = sizeRef.enabled;
        sizeShadow.size = sizeRef.size;

        // --- MÓDULO ROTATION OVER LIFETIME (Eje Z) ---
        var rotRef = _parentSystem.rotationOverLifetime;
        var rotShadow = _shadowSystem.rotationOverLifetime;
        rotShadow.enabled = rotRef.enabled;
        // Sincronizamos la rotación en Z (y las demás por si acaso)
        rotShadow.x = rotRef.x;
        rotShadow.y = rotRef.y;
        rotShadow.z = rotRef.z;
        rotShadow.separateAxes = rotRef.separateAxes;

        // Desactivar colisiones
        var shadowCol = _shadowSystem.collision;
        shadowCol.enabled = false;
    }

    void LateUpdate()
    {
        if (_parentSystem == null || _shadowSystem == null) return;

        // Sincronización de estados
        if (_parentSystem.isPlaying && !_shadowSystem.isPlaying)
            _shadowSystem.Play();
        else if (_parentSystem.isStopped && !_shadowSystem.isStopped)
            _shadowSystem.Stop();

        if (_parentSystem.isPaused && !_shadowSystem.isPaused)
            _shadowSystem.Pause();

        // Si el objeto original se desactiva
        if (_shadowSystem.gameObject.activeSelf != _parentSystem.gameObject.activeInHierarchy)
            _shadowSystem.gameObject.SetActive(_parentSystem.gameObject.activeInHierarchy);
    }
}