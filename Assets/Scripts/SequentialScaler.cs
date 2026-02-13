using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ManualSetCycler : MonoBehaviour
{
    [Header("--- Tamaños Generales ---")]
    public Vector3 smallScale = Vector3.one * 0.01f; // Casi invisible
    public Vector3 bigScale = Vector3.one;

    [Header("--- FASE 1: CRECER (Aparecer) ---")]
    [Tooltip("Cuánto tarda CADA objeto en hacerse grande")]
    public float growDuration = 1.0f; // Lento
    
    [Tooltip("Tiempo de espera entre que empieza a crecer uno y el siguiente")]
    public float growStagger = 0.2f;

    [Header("--- FASE 2: ENCOGER (Desaparecer) ---")]
    [Tooltip("Cuánto tarda CADA objeto en hacerse pequeño (Ponlo bajo para que sea rápido)")]
    public float shrinkDuration = 0.3f; // Rápido
    
    [Tooltip("Tiempo de espera entre que empieza a encoger uno y el siguiente")]
    public float shrinkStagger = 0.05f; // Muy seguido

    [Header("--- Curvas ---")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("--- Conjuntos de Objetos ---")]
    public List<ObjectSet> objectSets;

    [System.Serializable]
    public class ObjectSet
    {
        public string name;
        public List<GameObject> objects;
    }

    private int currentSetIndex = 0;
    private bool isBusy = false;

    private void Start()
    {
        InitializeSets();
    }

    [ContextMenu("▶ Ejecutar Transición")]
    public void TriggerTransition(float transicion, float frenado)
    {
        if (isBusy) 
        {
            Debug.LogWarning("Animación en curso...");
            return;
        }

        if (objectSets == null || objectSets.Count < 2)
        {
            Debug.LogError("Error: Necesitas al menos 2 conjuntos en la lista.");
            return;
        }

        StartCoroutine(ExecuteTransitionSequence(transicion, transicion));
    }

    private void InitializeSets()
    {
        for (int i = 0; i < objectSets.Count; i++)
        {
            bool isActiveSet = (i == currentSetIndex);
            foreach (var obj in objectSets[i].objects)
            {
                if (obj != null)
                {
                    obj.SetActive(isActiveSet);
                    // El activo empieza pequeño para crecer luego? No, el activo inicial ya debería verse.
                    // Asumiremos que al inicio del juego el primer set ya está GRANDE y visible.
                    if(isActiveSet) obj.transform.localScale = smallScale; 
                }
            }
        }
    }

    private IEnumerator ExecuteTransitionSequence(float tiempoAceleracion, float tiempoFrenado)
    {
        isBusy = true;

        int nextSetIndex = (currentSetIndex + 1) % objectSets.Count;
        ObjectSet setSaliente = objectSets[currentSetIndex];
        ObjectSet setEntrante = objectSets[nextSetIndex];

        // --- FASE 1: CRECER (Acompaña la aceleración del mapa) ---
        foreach (var obj in setSaliente.objects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                StartCoroutine(AnimateScale(obj, smallScale, bigScale, tiempoAceleracion));
            }
        }

        yield return new WaitForSeconds(tiempoAceleracion);

        // --- FASE 2: CAMBIAZO ---
        foreach (var obj in setSaliente.objects) if (obj != null) obj.SetActive(false);

        foreach (var obj in setEntrante.objects)
        {
            if (obj != null)
            {
                // Aparecen ya en tamaño GRANDE y se quedan así durante el frenado
                obj.transform.localScale = bigScale;
                obj.SetActive(true);
            }
        }

        // --- FASE 3: ESPERA DEL FRENADO ---
        // Esperamos el tiempo que el mapa tarda en frenar SIN hacer nada en las escalas
        yield return new WaitForSeconds(tiempoFrenado);

        // --- FASE 4: ENCOGER (Impacto Post-Frenado) ---
        // Ahora que el mapa se detuvo, ejecutamos el encogimiento para el "golpe" visual
        foreach (var obj in setEntrante.objects)
        {
            if (obj != null)
            {
                StartCoroutine(AnimateScale(obj, bigScale, smallScale, tiempoFrenado));
            }
        }

        // Esperamos a que termine esta última animación antes de liberar el script
        yield return new WaitForSeconds(tiempoFrenado);

        currentSetIndex = nextSetIndex;
        isBusy = false;
    }

    // He modificado esta función para que acepte la duración como parámetro "timeToScale"
    private IEnumerator AnimateScale(GameObject target, Vector3 startSize, Vector3 endSize, float timeToScale)
    {
        float timer = 0f;

        while (timer < timeToScale)
        {
            timer += Time.deltaTime;
            float progress = timer / timeToScale;
            
            float curveValue = animationCurve.Evaluate(progress);

            target.transform.localScale = Vector3.LerpUnclamped(startSize, endSize, curveValue);
            yield return null;
        }
        
        target.transform.localScale = endSize;
    }
}