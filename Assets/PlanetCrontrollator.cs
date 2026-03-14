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
    private Vector3 posOriginal;

    [Header("Efectos de Daño")]
    public GameObject damageTextPrefab;

    private float maxHealth;
    private float currentHealth;

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
        posOriginal = transform.position;
    }

    private void ProcesarImpacto(GameObject obj, Vector3 posicion, TipoImpacto tipoImpacto)
    {
        int id = obj.GetInstanceID();

        if (lastImpactTimes.ContainsKey(id) && Time.time < lastImpactTimes[id] + cooldownTime)
            return;

        lastImpactTimes[id] = Time.time;

        PersonaInfeccion scriptInfeccion = obj.GetComponent<PersonaInfeccion>();
        if (scriptInfeccion == null) return;

        float dañoCalculado = scriptInfeccion.ObtenerDañoTotal();
        int fase = scriptInfeccion.faseActual;

        if (scriptInfeccion.alreadyInfected)
        {
            InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);
            RegistrarDaño(dañoCalculado, fase, TipoImpacto.Carambola);
            TakeDamage(dañoCalculado, posicion);
            Destroy(obj);
            return;
        }

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fuerzaImpacto = rb.linearVelocity.magnitude;

            if (fuerzaImpacto > 6.5f)
            {
                RegistrarDaño(dañoCalculado, fase, tipoImpacto);
                TakeDamage(dañoCalculado, posicion);

                if (Guardado.instance != null)
                {
                    // Obtenemos el tipo de figura actual (0=Hex, 1=Pent, etc.)
                    int faseActual = (int)scriptInfeccion.faseActual;

                    // Calculamos la probabilidad: Cada nivel de habilidad otorga 25% (0.25f)
                    // El índice 1 corresponde a Pentágonos, 2 a Cuadrados, etc.
                    float nivelHabilidad = Guardado.instance.probParedInfectiva[faseActual];
                    float probabilidadDeRomper = nivelHabilidad * 0.25f;

                    // Generamos un número aleatorio entre 0 y 1 para decidir si se rompe
                    if (Random.value <= probabilidadDeRomper)
                    {
                        // Éxito: La pared infectiva funciona y la figura avanza/se rompe
                        scriptInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Wall);
                    }
                    else
                    {
                        // Fallo: Solo efecto visual de impacto normal
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

        if (EndDayResultsPanel.instance != null)
            EndDayResultsPanel.instance.RefreshResults();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Persona"))
            ProcesarImpacto(collision.gameObject, collision.transform.position, TipoImpacto.Zona);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Persona"))
            ProcesarImpacto(collision.gameObject, collision.transform.position, TipoImpacto.Choque);
    }

    public void TakeDamage(float amount, Vector3 spawnPos)
    {
        if (isInvulnerable) return;

        MapData map = MapSequenceManager.instance.GetCurrentMap();

        map.currentHealth -= amount;
        map.currentHealth = Mathf.Clamp(map.currentHealth, 0, map.maxHealth);

        currentHealth = map.currentHealth;
        maxHealth = map.maxHealth;

        ActualizarUI();

        if (damageTextPrefab != null)
        {
            GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            textObj.GetComponent<FloatingText>().SetText("-" + amount.ToString("F0"));
        }

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    void ActualizarUI()
    {
        if (healthBar != null)
        {
            // Actualizar el llenado de la barra
            float fill = currentHealth / maxHealth;
            healthBar.fillAmount = fill;

            if (healthBarOutLine != null)
                healthBarOutLine.fillAmount = fill + 0.0012f;

            // Resetear la posición local del padre a (0, 0, 0)
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

        transform.position = posOriginal;

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