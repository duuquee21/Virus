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
    public void TriggerTransition()
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

        StartCoroutine(ExecuteTransitionSequence());
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

    private IEnumerator ExecuteTransitionSequence()
    {
        isBusy = true;

        // Definir quién se va y quién viene
        int nextSetIndex = (currentSetIndex + 1) % objectSets.Count;
        
        ObjectSet setSaliente = objectSets[currentSetIndex]; // El que está visible ahora
        ObjectSet setEntrante = objectSets[nextSetIndex];    // El que vendrá después

        // =================================================================================
        // PASO 1: EL CONJUNTO ACTUAL SE VUELVE GRANDE... 
        // ¡ESPERA! El conjunto actual YA es grande y visible. 
        // Según tu petición: "quiero que se haga grande... cambie... y se haga pequeño".
        // Esto implica que la secuencia empieza con el set INACTIVO haciéndose grande.
        // =================================================================================

        // Nota: Para que el efecto visual sea "Del set 1 pasamos al 2":
        // 1. Set 1 desaparece (se hace pequeño? O swap directo?)
        // Según tu prompt anterior: "Primero se hacen grandes... cambian... vuelven a tamaño inicial".
        // Voy a interpretar el flujo más común de UI/Juego:
        // Set A (Visible) -> Se hace pequeño -> Set B (Invisible) se hace Grande.
        
        // PERO, tu prompt dice textualmente: "Cojo 6 objetos y los voy haciendo mas grandes... 
        // cuando todos hayan alcanzado el tamaño... cambie por otros... y vuelvan a su tamaño inicial (pequeño)".
        
        // INTERPRETACIÓN CORRECTA DE TU PROMPT:
        // 1. Estado Inicial: Set A está PEQUEÑO (o invisible/pequeño).
        // 2. Animación: Set A crece secuencialmente hasta ser GRANDE.
        // 3. Swap: En el momento cumbre (todos grandes), cambiamos los modelos de A por los de B (Swap instantáneo).
        // 4. Animación: Set B (que ahora está grande porque sustituyó a A) se hace PEQUEÑO secuencialmente.
        
        // --- FASE 1: CRECER (Set Actual) ---
        // Aseguramos que el set actual está activo y pequeño antes de empezar
        foreach (var obj in setSaliente.objects)
        {
            if(obj != null) { obj.SetActive(true); obj.transform.localScale = smallScale; }
        }

        // Animación de crecer (LENTA)
        for (int i = 0; i < setSaliente.objects.Count; i++)
        {
            if (setSaliente.objects[i] != null)
            {
                StartCoroutine(AnimateScale(setSaliente.objects[i], smallScale, bigScale, growDuration));
                yield return new WaitForSeconds(growStagger);
            }
        }
        
        // Esperar a que el último termine de crecer
        yield return new WaitForSeconds(growDuration);

        // --- FASE 2: EL CAMBIAZO (Swap) ---
        // Apagamos el viejo
        foreach (var obj in setSaliente.objects) if (obj != null) obj.SetActive(false);

        // Encendemos el nuevo YA GRANDE
        foreach (var obj in setEntrante.objects)
        {
            if (obj != null)
            {
                obj.transform.localScale = bigScale;
                obj.SetActive(true);
            }
        }

        // Breve pausa técnica (opcional, 1 frame) para asegurar que Unity renderiza el cambio
        yield return null; 

        // --- FASE 3: ENCOGER (Set Nuevo) ---
        // Animación de encoger (RÁPIDA)
        for (int i = 0; i < setEntrante.objects.Count; i++)
        {
            if (setEntrante.objects[i] != null)
            {
                // De Grande a Pequeño, usando shrinkDuration
                StartCoroutine(AnimateScale(setEntrante.objects[i], bigScale, smallScale, shrinkDuration));
                yield return new WaitForSeconds(shrinkStagger);
            }
        }

        // Esperar a que termine de encogerse el último
        yield return new WaitForSeconds(shrinkDuration);

        // --- FINAL ---
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