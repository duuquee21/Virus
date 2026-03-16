using UnityEngine;
using System.Collections.Generic;

public class InfectionManager : MonoBehaviour
{
    public static InfectionManager instance;

    [Header("Configuración de Procesamiento")]
    [Tooltip("Lista de todas las personas activas en el nivel")]
    public List<PersonaInfeccion> personasEnEscena = new List<PersonaInfeccion>();

    [Header("Ajustes Globales")]
    public float velocidadRetrocesoFueraDeZona = 2f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Método para que las personas se registren al aparecer (puedes llamarlo en el Start de PersonaInfeccion)
    public void RegistrarPersona(PersonaInfeccion persona)
    {
        if (!personasEnEscena.Contains(persona))
            personasEnEscena.Add(persona);
    }

    public void DesregistrarPersona(PersonaInfeccion persona)
    {
        if (personasEnEscena.Contains(persona))
            personasEnEscena.Remove(persona);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Obtenemos los multiplicadores de Guardado una sola vez por frame (Optimización)
        float globalMultiplier = 1f;
        float[] phaseMultipliers = null;

        if (Guardado.instance != null)
        {
            globalMultiplier = Guardado.instance.infectSpeedMultiplier;
            phaseMultipliers = Guardado.instance.infectSpeedPerPhase;
        }

        // Procesamos a todas las personas en un único bucle
        for (int i = personasEnEscena.Count - 1; i >= 0; i--)
        {
            PersonaInfeccion p = personasEnEscena[i];

            // Si la persona fue destruida o ya está infectada, saltamos
            if (p == null) { personasEnEscena.RemoveAt(i); continue; }
            if (p.alreadyInfected) continue;

            // --- LÓGICA DE INFECCIÓN ---
            float resistenciaActual = (p.faseActual < p.resistenciaPorFase.Length) ? p.resistenciaPorFase[p.faseActual] : 1f;
            float tiempoNecesario = PersonaInfeccion.globalInfectTime * resistenciaActual;

            if (p.IsInsideZone)
            {
                // Solo infectamos si el jugador es jugable
                // Nota: La lógica de "Atracción al centro" se queda en el script de la Persona 
                // para no interferir con sus físicas individuales.

                float multiplier = globalMultiplier;
                if (phaseMultipliers != null && p.faseActual < phaseMultipliers.Length)
                {
                    multiplier *= phaseMultipliers[p.faseActual];
                }

                p.ModificarTiempoInfeccion(dt * multiplier);

                if (p.ObtenerTiempoInfeccion() >= tiempoNecesario)
                {
                    p.IntentarAvanzarFase();
                }
            }
            else
            {
                // Si está fuera de la zona, el progreso retrocede
                p.ModificarTiempoInfeccion(-dt * velocidadRetrocesoFueraDeZona);
            }

            // --- ACTUALIZACIÓN VISUAL ---
            // Solo actualizamos las barras si el tiempo ha cambiado (ahorro de CPU)
            float progreso = Mathf.Clamp01(p.ObtenerTiempoInfeccion() / tiempoNecesario);
            p.ActualizarBarrasDesdeManager(progreso);
        }
    }
}