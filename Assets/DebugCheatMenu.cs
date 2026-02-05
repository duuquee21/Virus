using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugCheatMenu : MonoBehaviour
{
    private bool showMenu = false;
    private Vector2 scrollPosition;
    public KeyCode toggleKey = KeyCode.F1;

    // Estilos personalizados para tamaño
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle toggleStyle;
    private GUIStyle specialButtonStyle; // Nuevo estilo para destacar contagio

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) showMenu = !showMenu;
    }

    void OnGUI()
    {
        if (!showMenu) return;

        // --- CONFIGURACIÓN DE ESTILOS GIGANTES ---
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 25;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.cyan;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 18;

            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.fontSize = 20;

            specialButtonStyle = new GUIStyle(GUI.skin.button);
            specialButtonStyle.fontSize = 20;
            specialButtonStyle.fontStyle = FontStyle.Bold;
            specialButtonStyle.normal.textColor = Color.green;
        }

        // Ventana
        GUI.Box(new Rect(10, 10, 500, 850), "<b>PANEL DE CONTROL TOTAL</b>");

        if (Guardado.instance == null)
        {
            GUI.Label(new Rect(30, 50, 400, 40), "ERROR: Guardado.instance no encontrado", labelStyle);
            return;
        }

        // Área de scroll
        scrollPosition = GUI.BeginScrollView(new Rect(25, 60, 450, 700), scrollPosition, new Rect(0, 0, 420, 2500));

        int y = 0;
        int btnH = 40;

        // --- SECCIÓN: CONTROL DE TIEMPO ---
        // --- SECCIÓN: CONTROL DE TIEMPO ---
        Header("⏰ CONTROL DE TIEMPO", ref y);

        GUI.backgroundColor = Color.yellow;
        if (Btn("TEST RÁPIDO (3 Segundos)", ref y, 50))
        {
            // Cambia el tiempo base para que todas las rondas sean cortas
            LevelManager.instance.gameDuration = 3f;
            Debug.Log("<color=yellow>Modo Test: Partidas de 3s activadas.</color>");
        }

        GUI.backgroundColor = Color.white;
        if (Btn("TIEMPO NORMAL (20 Segundos)", ref y, 50))
        {
            // Restaura el valor original de tu LevelManager
            LevelManager.instance.gameDuration = 20f;
            Debug.Log("<color=white>Modo Normal: Partidas de 20s restauradas.</color>");
        }
        y += 20;

        // --- SECCIÓN: LAS 5 DE CONTAGIO (ESPECÍFICAS) ---
        Header("⚡ MEJORAS DE CONTAGIO", ref y);
        if (Btn("1. Spawn Interval -20%", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(0.20f);
        if (Btn("2. Spawn Interval -60%", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(0.60f);
        if (Btn("3. Spawn Interval -100% (MAX)", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(1.00f);
        if (Btn("4. Infect Speed +50%", ref y, 45)) Guardado.instance.SetInfectSpeedMultiplier(1.5f);
        if (Btn("5. Infect Speed +100% (MAX)", ref y, 45)) Guardado.instance.SetInfectSpeedMultiplier(2.0f);
        y += 20;

        // --- RECURSOS ---
        Header("RECURSOS BÁSICOS", ref y);
        if (Btn("+5000 Monedas", ref y, btnH)) LevelManager.instance.contagionCoins += 5000;
        if (Btn("+1000 ADN Shiny", ref y, btnH)) Guardado.instance.AddShinyDNA(1000);
        y += 20;

        // --- VIRUS STATS ---
        Header("VIRUS STATS", ref y);
        Label($"Radio: {Guardado.instance.radiusMultiplier:F2}", ref y);
        if (Btn("Radio +0.5", ref y, btnH))
        {
            Guardado.instance.radiusMultiplier += 0.5f;
            if (VirusRadiusController.instance) VirusRadiusController.instance.ApplyScale();
        }

        Label($"Velocidad: {Guardado.instance.speedMultiplier:F2}", ref y);
        if (Btn("Velocidad +0.5", ref y, btnH))
        {
            Guardado.instance.speedMultiplier += 0.5f;
            // Si tienes VirusMovement.instance implementado, descomenta la siguiente línea:
            // if(VirusMovement.instance) VirusMovement.instance.ApplySpeedMultiplier();
        }
        y += 20;

        // --- TODAS LAS HABILIDADES DEL ÁRBOL ---
        Header("ÁRBOL DE HABILIDADES (TODO)", ref y);
        if (Btn("Mejora Inicial Aleatoria", ref y, btnH)) Guardado.instance.AssignRandomInitialUpgrade();
        if (Btn("Multiplicador Monedas x6", ref y, btnH)) Guardado.instance.SetCoinMultiplier(6);
        if (Btn("Empezar con 50.000 Coins", ref y, btnH)) Guardado.instance.SetStartingCoins(50000);
        if (Btn("Cura: Añadir 10 Días", ref y, btnH))
        {
            Guardado.instance.AddBonusDays(10);
            LevelManager.instance.RecalculateTotalDaysUntilCure();
        }
        if (Btn("Multiplicador Shiny x10", ref y, btnH)) Guardado.instance.SetShinyMultiplier(10);
        if (Btn("Valor Shiny +3", ref y, btnH)) Guardado.instance.IncreaseShinyValueSum(3);
        if (Btn("Activar Descuento Zonas", ref y, btnH)) Guardado.instance.ActivateZoneDiscount();
        if (Btn("ADN Pasivo por Zona", ref y, btnH)) Guardado.instance.SetShinyPassiveIncome(1);
        if (Btn("Captura Shiny +100%", ref y, btnH)) Guardado.instance.SetShinyCaptureMultiplier(2.0f);
        y += 20;

        // --- TOGGLES ---
        Header("ESTADOS ESPECIALES", ref y);
        //Guardado.instance.doubleShinySkill = GUI.Toggle(new Rect(0, y, 400, 35), Guardado.instance.doubleShinySkill, " Habilidad Doble Shiny", toggleStyle); y += 40;
        Guardado.instance.guaranteedShiny = GUI.Toggle(new Rect(0, y, 400, 35), Guardado.instance.guaranteedShiny, " Shiny Garantizado", toggleStyle); y += 40;
        Guardado.instance.keepUpgradesOnReset = GUI.Toggle(new Rect(0, y, 400, 35), Guardado.instance.keepUpgradesOnReset, " Persistencia Mejoras", toggleStyle); y += 50;

        // --- SISTEMA ---
        Header("SISTEMA", ref y);
        if (Btn("TERMINAR DÍA", ref y, btnH)) LevelManager.instance.Invoke("EndSession", 0);

        y += 30;
        GUI.backgroundColor = Color.red;
        if (Btn("RESET TOTAL", ref y, 60))
        {
            PlayerPrefs.DeleteAll();
            Guardado.instance.HardResetVariables();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUI.backgroundColor = Color.white;

        GUI.EndScrollView();

        if (GUI.Button(new Rect(25, 780, 450, 50), "CERRAR (F1)", buttonStyle)) showMenu = false;
    }

    void Header(string t, ref int y) { GUI.Label(new Rect(0, y, 400, 40), t, headerStyle); y += 45; }
    void Label(string t, ref int y) { GUI.Label(new Rect(0, y, 400, 30), t, labelStyle); y += 35; }
    bool Btn(string t, ref int y, int h)
    {
        bool p = GUI.Button(new Rect(0, y, 400, h), t, buttonStyle);
        y += (h + 10);
        return p;
    }
}