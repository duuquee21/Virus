using UnityEngine;
using System.Collections.Generic;

public class PersonaManager : MonoBehaviour
{
    public static PersonaManager Instance;

    [Header("Referencias")]
    public Transform virusTransform;

    public List<PersonaInfeccion> todasLasPersonas = new List<PersonaInfeccion>();

    int indexProcesado = 0;
    public int personasPorFrame = 30; // Ajusta según necesidad

    void Awake() => Instance = this;



    void Update()
    {
        if (virusTransform == null || VirusRadiusController.instance == null) return;

        float dt = Time.deltaTime;

        // 1. Obtenemos el radio base del controlador
        float radioBase = VirusRadiusController.instance.CurrentFinalRadius;

        // 2. IMPORTANTE: Multiplicamos por la escala del objeto para tener el radio REAL en el mundo
        // Usamos lossyScale.x asumiendo que el virus crece de forma uniforme
        float radioRealMundo = radioBase * virusTransform.lossyScale.x;

        // 3. Elevamos al cuadrado una sola vez para la comparación de distancia (optimización)
        float radioSq = radioRealMundo * radioRealMundo;
        Vector2 virusPos = (Vector2)virusTransform.position;

        int total = todasLasPersonas.Count;
        if (total == 0) return;

        // Procesamos solo un grupo por frame para repartir la carga
        for (int i = 0; i < total; i++)
        {
            PersonaInfeccion p = todasLasPersonas[i];
            if (p == null) continue;

            Vector2 distDiff = (Vector2)p.transform.position - virusPos;
            bool estaDentro = distDiff.sqrMagnitude < radioSq;

            // Pasamos el deltaTime multiplicado para compensar si no se actualiza cada frame
            // O mejor: actualiza la lógica de infección para que sea independiente del frame
            p.ActualizacionOptimizada(estaDentro, virusTransform, dt);
        }
    }

    public void RegistrarPersona(PersonaInfeccion p) => todasLasPersonas.Add(p);
    public void DesregistrarPersona(PersonaInfeccion p) => todasLasPersonas.Remove(p);
}