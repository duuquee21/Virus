using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SequentialScaler : MonoBehaviour
{
    [Header("--- Configuración de Tamaños ---")]
    public Vector3 initialScale = Vector3.one * 0.1f;
    public Vector3 targetScale = Vector3.one;

    [Header("--- Configuración de Tiempo ---")]
    [Tooltip("Tiempo que tarda un solo objeto en crecer por completo")]
    public float scaleDuration = 1.0f;

    [Tooltip("Tiempo de espera entre que empieza el objeto 1 y el objeto 2")]
    public float staggerDelay = 0.2f;

    [Tooltip("Tiempo de espera al finalizar un conjunto antes de cambiar al siguiente")]
    public float delayBetweenSets = 0.5f;

    [Tooltip("Curva de animación para suavizar el escalado (Ej: EaseOutBack queda muy bien)")]
    public AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("--- Datos ---")]
    public List<ObjectSet> objectSets; // Lista de tus conjuntos

    // Estructura para organizar los conjuntos en el inspector
    [System.Serializable]
    public class ObjectSet
    {
        public string setName = "Conjunto X";
        public List<GameObject> objects;
    }

    private int currentSetIndex = 0;
    private bool isAnimating = false;
    private Coroutine activeSequence;

    private void Start()
    {
        // Inicializar: Ocultar todo excepto el primer conjunto y ponerlo en tamaño inicial
        ResetAllSets();

        // Opcional: Si quieres que arranque solo al dar Play, descomenta la línea de abajo:
        // StartSequence();
    }

    // --- LÓGICA PRINCIPAL ---

    [ContextMenu("? Iniciar / Reiniciar Secuencia")] // Esto crea el botón en el menú del componente
    public void StartSequence()
    {
        if (objectSets == null || objectSets.Count == 0)
        {
            Debug.LogError("¡No has asignado conjuntos de objetos!");
            return;
        }

        // Detenemos cualquier animación previa para reiniciar limpiamente
        StopAllCoroutines();

        // Reiniciamos estados visuales
        ResetAllSets();

        // Arrancamos la rutina principal
        activeSequence = StartCoroutine(ProcessCurrentSet());
    }

    private IEnumerator ProcessCurrentSet()
    {
        isAnimating = true;

        // 1. Obtener el conjunto actual
        ObjectSet currentSet = objectSets[currentSetIndex];

        // 2. Asegurarnos de que este conjunto está activo y en tamaño inicial
        foreach (var obj in currentSet.objects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                obj.transform.localScale = initialScale;
            }
        }

        // 3. Lanzar la animación de escalado uno por uno
        List<Coroutine> runningScales = new List<Coroutine>();

        foreach (var obj in currentSet.objects)
        {
            if (obj != null)
            {
                // Iniciar escalado individual
                runningScales.Add(StartCoroutine(ScaleObject(obj)));

                // Esperar un poco antes de lanzar el siguiente (efecto cascada)
                yield return new WaitForSeconds(staggerDelay);
            }
        }

        // 4. Esperar a que el último objeto termine de crecer + el tiempo que tarde en escalar
        // Calculamos el tiempo restante aproximado para no complicar la espera
        float remainingTime = scaleDuration;
        yield return new WaitForSeconds(remainingTime);

        // 5. Espera extra opcional antes de cambiar de conjunto
        yield return new WaitForSeconds(delayBetweenSets);

        // 6. CAMBIO DE CONJUNTO
        // Desactivar el conjunto actual
        foreach (var obj in currentSet.objects)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Calcular siguiente índice (Bucle: 0 -> 1 -> 2 -> 0)
        currentSetIndex = (currentSetIndex + 1) % objectSets.Count;

        // 7. Repetir el proceso con el nuevo conjunto (Recursión via bucle while o llamada)
        // Usaremos llamada recursiva a la corrutina para mantener el flujo
        activeSequence = StartCoroutine(ProcessCurrentSet());
    }

    private IEnumerator ScaleObject(GameObject target)
    {
        float timer = 0f;

        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / scaleDuration;

            // Aplicamos la curva para que no sea lineal aburrido
            float curveValue = animationCurve.Evaluate(progress);

            target.transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, curveValue);

            yield return null;
        }

        // Asegurar tamaño final exacto
        target.transform.localScale = targetScale;
    }

    // --- UTILIDADES ---

    private void ResetAllSets()
    {
        // Recorremos todos los conjuntos
        for (int i = 0; i < objectSets.Count; i++)
        {
            bool isCurrentSet = (i == currentSetIndex);

            foreach (var obj in objectSets[i].objects)
            {
                if (obj != null)
                {
                    // Solo activamos los objetos del conjunto actual, el resto se apagan
                    obj.SetActive(isCurrentSet);
                    // Todos se reinician al tamaño pequeño
                    obj.transform.localScale = initialScale;
                }
            }
        }
    }
}