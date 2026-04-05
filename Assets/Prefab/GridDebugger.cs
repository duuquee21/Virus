using UnityEngine;
using System.Collections.Generic;

public class GridDebugger : MonoBehaviour
{
    [Header("Configuración Visual")]
    public bool mostrarGrid = true;
    public bool mostrarPlayerCount = true;
    public Color colorGrid = new Color(0, 1, 0, 0.3f);
    public Color colorTexto = Color.cyan;

    [Header("Ajustes del Grid")]
    private float tamañoCelda = 0.5f;

    void OnDrawGizmos()
    {
        if (!mostrarGrid || !Application.isPlaying || Movement.espacialGrid == null) return;

        Gizmos.color = colorGrid;

        // Usamos un try-catch o simplemente iteramos con cuidado 
        // ya que el Grid puede cambiar en pleno frame
        foreach (var coord in Movement.espacialGrid.Keys)
        {
            Vector3 centroCelda = new Vector3(
                (coord.x * tamañoCelda) + (tamañoCelda * 0.5f),
                (coord.y * tamañoCelda) + (tamañoCelda * 0.5f),
                0
            );
            Gizmos.DrawWireCube(centroCelda, new Vector3(tamañoCelda, tamañoCelda, 0.1f));
        }
    }

    void OnGUI()
    {
        if (!mostrarPlayerCount || Movement.espacialGrid == null) return;

        GUIStyle estilo = new GUIStyle();
        estilo.fontSize = 22;
        estilo.normal.textColor = colorTexto;
        estilo.fontStyle = FontStyle.Bold;

        int totalEntidadesEnGrid = 0;
        int infectados = 0;
        int celdasActivas = 0;

        // Iteramos sobre el diccionario de forma segura
        foreach (var lista in Movement.espacialGrid.Values)
        {
            if (lista == null) continue;

            celdasActivas++;
            for (int i = 0; i < lista.Count; i++)
            {
                Movement m = lista[i];

                // --- SOLUCIÓN AL ERROR ---
                // Comprobamos si el objeto existe y no ha sido destruido
                if (m != null && m.gameObject != null)
                {
                    totalEntidadesEnGrid++;

                    // Intentamos obtener el componente de forma segura
                    PersonaInfeccion p = m.GetComponent<PersonaInfeccion>();
                    if (p != null && p.alreadyInfected)
                    {
                        infectados++;
                    }
                }
            }
        }

        // Dibujar UI
        GUILayout.BeginArea(new Rect(30, 30, 400, 250));
        GUILayout.Label($"--- DEBUG SPATIAL GRID ---", estilo);
        GUILayout.Label($"ENTIDADES ACTIVAS: {totalEntidadesEnGrid}", estilo);
        GUILayout.Label($"INFECTADOS: {infectados}", estilo);
        GUILayout.Label($"CELDAS OCUPADAS: {celdasActivas}", estilo);

        if (GUILayout.Button(mostrarGrid ? "OCULTAR GRID (Gizmos)" : "MOSTRAR GRID (Gizmos)"))
        {
            mostrarGrid = !mostrarGrid;
        }
        GUILayout.EndArea();
    }
}