using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("UI")]
    public Image healthBar;
    public Image healthBarOutLine;
    public bool nivelFinal = false;
    [Header("Identidad del mapa")]
    public int mapIndex = 0;
    [Header("Anti-Spam Impactos")]
    private readonly Dictionary<int, float> lastImpactTimes = new Dictionary<int, float>();
    private float cooldownTime = 0.1f;

    [Header("Estado")]
    public bool isInvulnerable = false;

    [Header("Ajustes de Muerte")]
    public float delayMuerte = 1.5f;
    public float fuerzaVibracion = 0.1f;


    [Header("Optimizacion")]
    [SerializeField] private float uiRefreshInterval = 0.05f;
    [SerializeField] private float ultraFxCooldown = 0.04f;
    [SerializeField] private int maxDestroysPerFrame = 8;
    [SerializeField] private bool refrescarResultadosSoloSiPanelVisible = true;

    private float lastUIRefreshTime = -999f;
    private float lastUltraFxTime = -999f;
    private readonly Queue<GameObject> pendingDestroyQueue = new Queue<GameObject>();
    [Header("Efectos de Daño")]
    public GameObject damageTextPrefab;

    [Header("Batch de daño")]
    public bool agruparDanio = true;
    [Range(0.01f, 0.10f)] public float ventanaBatchDanio = 0.03f;
    public bool combinarTextosDanio = true;
    public int maxTextosDanioPorSegundo = 12;

    private float pendingDamage = 0f;
    private Vector3 pendingDamagePosSum = Vector3.zero;
    private int pendingDamageHits = 0;
    private float nextDamageFlushTime = -1f;

    private float textWindowStart = 0f;
    private int textsSpawnedThisWindow = 0;

    private AnimacionFinalPlaneta animacionFinalPlaneta;

    private float maxHealth;
    private float currentHealth;

    private bool resultsDirty = false;
    private bool uiDirty = false;
    private bool muriendo = false;
    private bool initializedHealth = false;

    private struct TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;

        public TransformData(Vector3 pos, Quaternion rot)
        {
            localPosition = pos;
            localRotation = rot;
        }
    }

    private Dictionary<Transform, TransformData> transformacionesOriginales;

    public enum TipoImpacto
    {
        Zona,
        Choque,
        Carambola
    }

    void Awake()
    {
        GuardarEstadoJerarquia();
        MapData map = GetOwnMapData();
        if (map != null)
        {
            maxHealth = map.maxHealth;
            currentHealth = map.currentHealth > 0f ? map.currentHealth : map.maxHealth;
            map.currentHealth = currentHealth;
        }

        ActualizarUI();

        animacionFinalPlaneta = GetComponent<AnimacionFinalPlaneta>();
        GuardarEstadoJerarquia();

        textWindowStart = Time.time;
    }

    void Update()
    {
        if (pendingDamageHits > 0 && Time.time >= nextDamageFlushTime)
            FlushPendingDamage();

        ProcesarDestroyQueue();

        if (Time.time - textWindowStart >= 1f)
        {
            textWindowStart = Time.time;
            textsSpawnedThisWindow = 0;
        }

        if (resultsDirty)
        {
            bool panelVisible = EndDayResultsPanel.instance != null &&
                                EndDayResultsPanel.instance.gameObject.activeInHierarchy;

            if (!refrescarResultadosSoloSiPanelVisible || panelVisible)
            {
                EndDayResultsPanel.instance?.RefreshResults();
                resultsDirty = false;
            }
        }

        if (uiDirty && Time.time >= lastUIRefreshTime + uiRefreshInterval)
        {
            ActualizarUI();
            uiDirty = false;
            lastUIRefreshTime = Time.time;
        }
    }

    private MapData GetOwnMapData()
    {
        if (MapSequenceManager.instance == null) return null;
        return MapSequenceManager.instance.GetMap(mapIndex);
    }
    private void GuardarEstadoJerarquia()
    {
        transformacionesOriginales = new Dictionary<Transform, TransformData>();

        Transform root = transform.parent != null ? transform.parent : transform;

        transformacionesOriginales[root] = new TransformData(root.localPosition, root.localRotation);

        foreach (Transform child in root)
        {
            transformacionesOriginales[child] = new TransformData(child.localPosition, child.localRotation);
        }
    }

    private void DesactivarImpactable(GameObject obj)
    {
        if (obj == null) return;

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        obj.SetActive(false);
    }

    private void ProcesarDestroyQueue()
    {
        int cantidad = Mathf.Min(maxDestroysPerFrame, pendingDestroyQueue.Count);

        for (int i = 0; i < cantidad; i++)
        {
            GameObject go = pendingDestroyQueue.Dequeue();
            if (go != null)
                Destroy(go);
        }
    }

    public void ProcesarImpacto(GameObject obj, Vector3 posicion, TipoImpacto tipoImpacto)
    {
        if (muriendo || isInvulnerable) return;
        if (obj == null) return;

        int id = obj.GetInstanceID();
        float time = Time.time;

        if (lastImpactTimes.TryGetValue(id, out float lastTime) && time < lastTime + cooldownTime)
            return;

        lastImpactTimes[id] = time;

        if (!obj.TryGetComponent<PersonaInfeccion>(out var scriptInfeccion))
            return;

        float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();
        int fase = scriptInfeccion.faseActual;

        if (scriptInfeccion.alreadyInfected)
        {
            if (InfectionFeedback.instance != null && Time.time >= lastUltraFxTime + ultraFxCooldown)
            {
                InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);
                lastUltraFxTime = Time.time;
            }

            RegistrarDaño(dañoCalculado, fase, TipoImpacto.Carambola);
            TakeDamage(dañoCalculado, posicion);

            DesactivarImpactable(obj);
            pendingDestroyQueue.Enqueue(obj);
            return;
        }

        if (obj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            if (rb.linearVelocity.sqrMagnitude > 42.25f)
            {
                RegistrarDaño(dañoCalculado, fase, tipoImpacto);
                TakeDamage(dañoCalculado, posicion);

                if (Guardado.instance != null)
                {
                    int faseActual = (int)scriptInfeccion.faseActual;
                    float nivelHabilidad = Guardado.instance.probParedInfectiva[faseActual];
                    float probabilidadDeRomper = nivelHabilidad * 0.25f;

                    if (Random.value <= probabilidadDeRomper)
                    {
                        scriptInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Wall);
                    }
                    else
                    {
                        if (InfectionFeedback.instance != null)
                            InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(posicion, Color.white);
                    }
                }
            }
        }
    }

    private void RegistrarDaño(float daño, int fase, TipoImpacto tipoImpacto)
    {
        int idx = Mathf.Clamp(fase, 0, 4);

        PersonaInfeccion.golpesAlPlanetaPorFase[idx]++;

        switch (tipoImpacto)
        {
            case TipoImpacto.Zona:
                PersonaInfeccion.dañoTotalZona += daño;
                PersonaInfeccion.dañoZonaPorFase[idx] += daño;
                break;

            case TipoImpacto.Choque:
                PersonaInfeccion.dañoTotalChoque += daño;
                PersonaInfeccion.dañoChoquePorFase[idx] += daño;
                break;

            case TipoImpacto.Carambola:
                PersonaInfeccion.dañoTotalCarambola += daño;
                PersonaInfeccion.dañoCarambolaPorFase[idx] += daño;
                break;
        }

        resultsDirty = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Persona"))
            ProcesarImpacto(collision.gameObject, collision.transform.position, TipoImpacto.Zona);
    }

    public void TakeDamage(float amount, Vector3 spawnPos)
    {
        if (isInvulnerable || muriendo) return;

        if (!agruparDanio)
        {
            ApplyDamageImmediate(amount, spawnPos);
            return;
        }

        pendingDamage += amount;
        pendingDamagePosSum += spawnPos;
        pendingDamageHits++;

        if (nextDamageFlushTime < 0f)
            nextDamageFlushTime = Time.time + ventanaBatchDanio;
    }

    private void ApplyDamageImmediate(float amount, Vector3 spawnPos)
    {
        MapData map = GetOwnMapData();
        if (map == null) return;

        map.currentHealth -= amount;
        map.currentHealth = Mathf.Clamp(map.currentHealth, 0, map.maxHealth);

        currentHealth = map.currentHealth;
        maxHealth = map.maxHealth;

        uiDirty = true;

        if (damageTextPrefab != null && CanSpawnDamageText())
        {
            if (TextPooler.Instance != null)
                TextPooler.Instance.SpawnText(spawnPos, "-" + amount.ToString("F0"));
        }

        if (currentHealth <= 0f)
            Die();
    }
    private void FlushPendingDamage()
    {
        if (pendingDamageHits <= 0) return;

        if (isInvulnerable || muriendo)
        {
            ClearPendingDamage();
            return;
        }

        float totalDamage = pendingDamage;
        Vector3 avgPos = pendingDamagePosSum / pendingDamageHits;

        MapData map = GetOwnMapData();
        if (map == null)
        {
            ClearPendingDamage();
            return;
        }

        map.currentHealth -= totalDamage;
        map.currentHealth = Mathf.Clamp(map.currentHealth, 0, map.maxHealth);

        currentHealth = map.currentHealth;
        maxHealth = map.maxHealth;

        uiDirty = true;

        if (damageTextPrefab != null && combinarTextosDanio && CanSpawnDamageText())
        {
            if (TextPooler.Instance != null)
                TextPooler.Instance.SpawnText(avgPos, "-" + totalDamage.ToString("F0"));
        }

        ClearPendingDamage();

        if (currentHealth <= 0f)
            Die();
    }

    private bool CanSpawnDamageText()
    {
        if (textsSpawnedThisWindow >= maxTextosDanioPorSegundo)
            return false;

        textsSpawnedThisWindow++;
        return true;
    }

    void ActualizarUI()
    {
        if (healthBar != null)
        {
            float fill = currentHealth / maxHealth;
            healthBar.fillAmount = fill;

            if (healthBarOutLine != null)
                healthBarOutLine.fillAmount = fill + 0.0012f;

            if (healthBar.transform.parent != null)
                healthBar.transform.parent.localPosition = Vector3.zero;
        }
    }



    public void ResetHealthToInitial()
    {
        MapData map = GetOwnMapData();
        if (map != null)
        {
            maxHealth = map.maxHealth;
            map.currentHealth = map.maxHealth;
            currentHealth = map.currentHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        lastImpactTimes.Clear();

        isInvulnerable = false;
        muriendo = false;

        ClearPendingDamage();

        uiDirty = false;
        resultsDirty = false;

        textWindowStart = Time.time;
        textsSpawnedThisWindow = 0;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (transformacionesOriginales != null)
        {
            foreach (var kvp in transformacionesOriginales)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.localPosition = kvp.Value.localPosition;
                    kvp.Key.localRotation = kvp.Value.localRotation;
                }
            }
        }

        ActualizarUI();
    }

    public void ClearPendingDamage()
    {
        pendingDamage = 0f;
        pendingDamagePosSum = Vector3.zero;
        pendingDamageHits = 0;
        nextDamageFlushTime = -1f;
    }
    public void SetHealthDirect(float value)
    {
        MapData map = GetOwnMapData();
        if (map != null)
        {
            maxHealth = map.maxHealth;
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            map.currentHealth = currentHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        }

        ClearPendingDamage();

        if (currentHealth > 0f)
            muriendo = false;

        uiDirty = true;
        ActualizarUI();
    }
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    void Die()
    {
        if (muriendo) return;

        muriendo = true;
        isInvulnerable = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        ClearPendingDamage();

        // 1. Forzamos la transición visual directamente para que haga la animación
        LevelTransitioner transitioner = FindFirstObjectByType<LevelTransitioner>();
        if (transitioner != null)
        {
            transitioner.StartLevelTransition();
        }

        // 2. Llamamos al SequenceManager para que intente avanzar el índice o actualizar su estado
        if (MapSequenceManager.instance != null)
        {
            MapSequenceManager.instance.NextMap();
        }
    }

    private void OnEnable()
    {
        // Verificamos que ya se hayan guardado las posiciones (para evitar errores en el primer frame)
        if (transformacionesOriginales != null)
        {
            RestaurarPosicionOriginal();
        }
    }

    private void RestaurarPosicionOriginal()
    {
        if (transformacionesOriginales == null) return;

        foreach (var kvp in transformacionesOriginales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localPosition = kvp.Value.localPosition;
                kvp.Key.localRotation = kvp.Value.localRotation;
            }
        }

        // Si quieres que la UI también se resetee visualmente al activarse
        ActualizarUI();
    }
}