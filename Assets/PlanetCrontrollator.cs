using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetCrontrollator : MonoBehaviour
{

    [Header("Estadísticas")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;
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

    public enum TipoImpacto
    {
        Zona,
        Choque,
        Carambola
    }

 [Header("Efectos de Daño")]
public GameObject damageTextPrefab;

    void Start()
    {
        currentHealth = maxHealth;
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

        // CASO 1: YA ESTÁ INFECTADO (Explosión) => Carambola
        if (scriptInfeccion.alreadyInfected)
        {
            InfectionFeedback.instance.PlayUltraEffect(posicion, Color.white);

            TakeDamage(dañoCalculado);
            RegistrarDaño(dañoCalculado, fase, TipoImpacto.Carambola);

            TakeDamage(dañoCalculado, posicion); // <--- Pasar posición aquí
            Destroy(obj);
            return;
        }

        // CASO 2: IMPACTO FÍSICO
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fuerzaImpacto = rb.linearVelocity.magnitude;
            if (fuerzaImpacto > 6.5f)
            {
                TakeDamage(dañoCalculado);
                RegistrarDaño(dañoCalculado, fase, tipoImpacto);
                TakeDamage(dañoCalculado, posicion); // <--- Y aquí

                if (Guardado.instance != null)
                {
                    if (Guardado.instance.nivelParedInfectiva > scriptInfeccion.faseActual)
                        scriptInfeccion.IntentarAvanzarFasePorChoque(PersonaInfeccion.TipoChoque.Wall);
                    else
                        InfectionFeedback.instance.PlayBasicImpactEffectAgainstWall(posicion, Color.white);
                }
            }
        }
    }

    private void RegistrarDaño(float daño, int fase, TipoImpacto tipoImpacto)
    {
        int idx = Mathf.Clamp(fase, 0, 4);

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
            // En Trigger no hay "puntos de contacto" reales, usamos la posición del objeto
            ProcesarImpacto(collision.gameObject, collision.transform.position);
        }

        if (EndDayResultsPanel.instance != null)
            EndDayResultsPanel.instance.RefreshResults();
    }

    private void ApplyDamageAndRegister(float daño, TipoImpacto tipoImpacto)
    {
        TakeDamage(daño);

        switch (tipoImpacto)
        {
            case TipoImpacto.Zona:
                PersonaInfeccion.dañoTotalZona += daño;
                break;

            case TipoImpacto.Choque:
                PersonaInfeccion.dañoTotalChoque += daño;
                break;

            case TipoImpacto.Carambola:
                PersonaInfeccion.dañoTotalCarambola += daño;
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Die();
        }
    }

    public void TakeDamage(float amount)
    public void TakeDamage(float amount, Vector3 spawnPos)
    {
        if (isInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        ActualizarUI();

        Debug.Log($"<color=red>Daño recibido: {amount}. Vida restante: {currentHealth}</color>");


        // --- INSTANCIAR EL NÚMERO ---
        if (damageTextPrefab != null)
        {
            GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            textObj.GetComponent<FloatingText>().SetText("-" + amount.ToString("F0")); // "F0" quita decimales
        }
        // ----------------------------

        if (currentHealth <= 0) Die();
    }

    // Sobrecarga por si quieres llamar a TakeDamage sin posición (por seguridad)
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    void ActualizarUI()
    {
        if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;
    }

    public void ResetHealthToInitial()
    {
        currentHealth = maxHealth;
        lastImpactTimes.Clear();
        isInvulnerable = false;

        enabled = true;

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

        Debug.Log("<color=green>Planeta reseteado completamente</color>");
    }

    void Die()
    {
        if (nivelFinal)
        {
            animacionFinalPlaneta.EjecutarSecuenciaVibracion();
        }
        else
        {
            StartCoroutine(VibrarYPasarNivel());
        }
    }



    IEnumerator VibrarYPasarNivel()
    {
        float tiempo = 0f;

        while (tiempo < delayMuerte)
        {
            transform.position = posOriginal + (Vector3)Random.insideUnitCircle * fuerzaVibracion;
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.position = posOriginal;
        LevelManager.instance.NextMapTransition();
    }
}