using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetCrontrollator : MonoBehaviour
{
    [Header("UI")]
    public Image healthBar;
    public Image healthBarOutLine;
    public bool nivelFinal = false;

    [Header("Anti-Spam Impactos")]
    private readonly Dictionary<int, float> lastImpactTimes = new Dictionary<int, float>();
    private float cooldownTime = 0.1f;

    private AnimacionFinalPlaneta animacionFinalPlaneta;

    [Header("Estado")]
    public bool isInvulnerable = false;

    [Header("Ajustes de Muerte")]
    public float delayMuerte = 1.5f;
    public float fuerzaVibracion = 0.1f;

    // --- NUEVO SISTEMA DE GUARDADO DE JERARQUÍA ---
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
    // ----------------------------------------------

    [Header("Efectos de Daño")]
    public GameObject damageTextPrefab;

    private float maxHealth;
    private float currentHealth;

    private bool resultsDirty = false;

    public enum TipoImpacto
    {
        Zona,
        Choque,
        Carambola
    }

    void Start()
    {
        MapData map = MapSequenceManager.instance.GetCurrentMap();

        maxHealth = map.maxHealth;

        // Asegurar salud al máximo SIEMPRE
        currentHealth = maxHealth;
        map.currentHealth = maxHealth;

        ActualizarUI();

        animacionFinalPlaneta = GetComponent<AnimacionFinalPlaneta>();

        // --- GUARDAR TRANSFORMACIONES DE LA FAMILIA ---
        GuardarEstadoJerarquia();
    }
    void Update()
    {
        // Si hubo cambios en los resultados O en la vida, actualizamos
        if (resultsDirty || uiDirty)
        {
            if (resultsDirty)
            {
                if (EndDayResultsPanel.instance != null)
                    EndDayResultsPanel.instance.RefreshResults();
                resultsDirty = false;
            }

            if (uiDirty)
            {
                ActualizarUI();
                uiDirty = false;
            }
        }
    }
    private void GuardarEstadoJerarquia()
    {
        transformacionesOriginales = new Dictionary<Transform, TransformData>();

        // Si tiene padre, el "root" a guardar es el padre. Si no, es este mismo objeto.
        Transform root = transform.parent != null ? transform.parent : transform;

        // Guardamos el padre
        transformacionesOriginales[root] = new TransformData(root.localPosition, root.localRotation);

        // Guardamos todos los hijos del padre (esto incluye a los hermanos y a este mismo objeto)
        foreach (Transform child in root)
        {
            transformacionesOriginales[child] = new TransformData(child.localPosition, child.localRotation);
        }
    }

    public void ProcesarImpacto(GameObject obj, Vector3 posicion, TipoImpacto tipoImpacto)
    {
        int id = obj.GetInstanceID();
        float time = Time.time;

        // Usar TryGetValue para buscar solo una vez en el diccionario
        if (lastImpactTimes.TryGetValue(id, out float lastTime) && time < lastTime + cooldownTime)
            return;

        lastImpactTimes[id] = time;

        if (!obj.TryGetComponent<PersonaInfeccion>(out var scriptInfeccion)) return;

        float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();
        int fase = scriptInfeccion.faseActual;

        // Si ya está infectado (Círculo final), destruye al impactar y hace daño ultra
        if (scriptInfeccion.alreadyInfected)
        {
            InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);
            RegistrarDaño(dañoCalculado, fase, TipoImpacto.Carambola);
            TakeDamage(dañoCalculado, posicion);
            Destroy(obj);
            return;
        }

        if (obj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Mantenemos tu optimización de sqrMagnitude (6.5 * 6.5 = 42.25)
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



    private bool uiDirty = false; // Nueva variable

    public void TakeDamage(float amount, Vector3 spawnPos)
    {
        if (isInvulnerable) return;

        MapData map = MapSequenceManager.instance.GetCurrentMap();
        map.currentHealth -= amount;
        map.currentHealth = Mathf.Clamp(map.currentHealth, 0, map.maxHealth);

        currentHealth = map.currentHealth;
        maxHealth = map.maxHealth;

        // EN LUGAR DE ActualizarUI() directamente:
        uiDirty = true;

        if (damageTextPrefab != null)
        {
            TextPooler.Instance.SpawnText(spawnPos, "-" + amount.ToString("F0"));
        }

        if (currentHealth <= 0)
            Die();
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
            {
                healthBar.transform.parent.localPosition = Vector3.zero;
            }
        }
    }

    public void ResetHealthToInitial()
    {
        MapData map = MapSequenceManager.instance.GetCurrentMap();

        map.currentHealth = map.maxHealth;

        currentHealth = map.currentHealth;
        maxHealth = map.maxHealth;

        lastImpactTimes.Clear();
        isInvulnerable = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // --- RESTAURAR TRANSFORMACIONES DE LA FAMILIA ---
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

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    void Die()
    {
        if (nivelFinal)
        {
            animacionFinalPlaneta.EjecutarSecuenciaVibracion();
        }
        else
        {
            MapSequenceManager.instance.NextMap();
        }
    }
}