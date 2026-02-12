using UnityEngine;
using System.Collections;

public class AnimacionFinalPlaneta : MonoBehaviour
{
    [Header("Objetos")]
    public GameObject planetaActual;
    public GameObject[] sombras;
    public GameObject fragmentos;
    public GameObject circuloPrefab;
    public GameObject objetoFinalEscena;

    [Header("Configuración Fase 1 (Vibración y Encogimiento)")]
    public float duracionVibracionPlaneta = 0.8f;
    public float magnitudVibracionObjeto = 0.15f; // Renombrado para claridad
    public Color colorFaseEncogimiento = Color.white;

    [Header("Configuración Fase 2 (Gran Final)")]
    public float duracionVibracionJugador = 2.0f;
    public float escalaMaxCirculo = 10f;
    public float velocidadCirculo = 15f;
    public float escalaFinalObjetivo = 2.0f;
    public float velocidadCrecimientoFinal = 5f;
    public Color colorFaseCrecimiento = Color.white;

    [Header("Referencias")]
    public ManagerAnimacionJugador managerAnimacionJugador;
    private bool secuenciaEnCurso = false;

    private int orderOriginal;
    private SpriteRenderer srObjetoFinal;

    void Start()
    {
        if (objetoFinalEscena != null)
        {
            srObjetoFinal = objetoFinalEscena.GetComponent<SpriteRenderer>();
            if (srObjetoFinal != null)
            {
                orderOriginal = srObjetoFinal.sortingOrder;
            }
        }
    }

    public void EjecutarSecuenciaVibracion()
    {
        if (!secuenciaEnCurso)
        {
            if (managerAnimacionJugador != null)
                managerAnimacionJugador.ComienzoAnimacion();

            StartCoroutine(SecuenciaCompleta());
        }
    }

    private IEnumerator SecuenciaCompleta()
    {
        secuenciaEnCurso = true;

        // --- PREPARACIÓN ---
        if (planetaActual != null)
        {
            SpriteRenderer srPlaneta = planetaActual.GetComponent<SpriteRenderer>();
            if (srPlaneta != null) srPlaneta.enabled = false;
        }

        if (sombras != null)
        {
            foreach (GameObject sombra in sombras)
            {
                if (sombra != null) sombra.SetActive(false);
            }
        }

        // --- PASO 1: VIBRACIÓN Y ENCOGIMIENTO DEL OBJETO FINAL ---
        if (fragmentos != null) fragmentos.SetActive(true);

        // Guardamos la posición original del Objeto Final para la vibración
        Vector3 posOriginalObjeto = objetoFinalEscena.transform.localPosition;
        Vector3 escalaInicialObjeto = objetoFinalEscena.transform.localScale;
        float tiempo = 0;

        if (srObjetoFinal != null)
        {
            srObjetoFinal.sortingOrder = orderOriginal;
            srObjetoFinal.color = colorFaseEncogimiento;
        }

        while (tiempo < duracionVibracionPlaneta)
        {
            if (objetoFinalEscena != null)
            {
                // VIBRACIÓN: Aplicada al Objeto Final
                Vector2 desplazamiento = Random.insideUnitCircle * magnitudVibracionObjeto;
                objetoFinalEscena.transform.localPosition = posOriginalObjeto + new Vector3(desplazamiento.x, desplazamiento.y, 0);

                // ENCOGIMIENTO: Aplicado al Objeto Final
                objetoFinalEscena.transform.localScale = Vector3.Lerp(escalaInicialObjeto, Vector3.zero, tiempo / duracionVibracionPlaneta);
            }

            tiempo += Time.deltaTime;
            yield return null;
        }

        // Reset de posición y escala final del paso 1
        if (objetoFinalEscena != null)
        {
            objetoFinalEscena.transform.localPosition = posOriginalObjeto;
            objetoFinalEscena.transform.localScale = Vector3.zero;
        }

        // --- PASO 2: EL GRAN FINAL (FLASH Y CÍRCULOS) ---
        Vector3 centroPantalla = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        centroPantalla.z = 0;

        GameObject jugador = GameObject.FindGameObjectWithTag("Virus");

        if (jugador != null)
        {
            SpriteRenderer srJugador = jugador.GetComponent<SpriteRenderer>();
            Vector3 posOrg = jugador.transform.localPosition;
            Vector3 escOrg = jugador.transform.localScale;
            Color colOrg = srJugador.color;

            float t = 0;
            while (t < duracionVibracionJugador)
            {
                jugador.transform.localPosition = posOrg + (Vector3)Random.insideUnitCircle * 0.2f;
                srJugador.color = Color.Lerp(colOrg, Color.white, Mathf.PingPong(t * 15f, 1f));
                float pulso = Mathf.Sin(t * 20f) * 0.3f;
                jugador.transform.localScale = escOrg + new Vector3(pulso, pulso, 0);

                t += Time.deltaTime;
                yield return null;
            }
            jugador.transform.localPosition = posOrg;
            jugador.transform.localScale = escOrg;
            srJugador.color = colOrg;
        }

        GameObject circulo = Instantiate(circuloPrefab, centroPantalla, Quaternion.identity);
        circulo.transform.localScale = Vector3.zero;
        SpriteRenderer srCirculo = circulo.GetComponent<SpriteRenderer>();
        srCirculo.color = Color.white;

        while (circulo.transform.localScale.x < escalaMaxCirculo)
        {
            circulo.transform.localScale += Vector3.one * velocidadCirculo * Time.deltaTime;
            yield return null;
        }

        srCirculo.color = Color.black;
        while (circulo.transform.localScale.x > 0.05f)
        {
            circulo.transform.localScale -= Vector3.one * velocidadCirculo * Time.deltaTime;
            yield return null;
        }
        Destroy(circulo);

        // --- PASO 3: CRECIMIENTO FINAL DEL OBJETO ---
        if (objetoFinalEscena != null)
        {
            // Ahora se mueve al centro de la pantalla para el gran final
            objetoFinalEscena.transform.position = centroPantalla;

            if (srObjetoFinal != null)
            {
                srObjetoFinal.sortingOrder = 32767;
                srObjetoFinal.color = colorFaseCrecimiento;
            }
           
            while (objetoFinalEscena.transform.localScale.x < escalaFinalObjetivo)
            {
                objetoFinalEscena.transform.localScale += Vector3.one * velocidadCrecimientoFinal * Time.deltaTime;
                yield return null;
            }
           

        }
        if (LevelManager.instance != null) LevelManager.instance.NextMapTransition();

        if (managerAnimacionJugador != null)
            managerAnimacionJugador.FinAnimacion();

      

        secuenciaEnCurso = false;
    }
}