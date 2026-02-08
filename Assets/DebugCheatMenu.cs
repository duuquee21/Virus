using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugCheatMenu : MonoBehaviour
{
    private bool showMenu = false;
    private Vector2 scrollPosition;
    public KeyCode toggleKey = KeyCode.F1;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle toggleStyle;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) showMenu = !showMenu;
    }

    void OnGUI()
    {
        if (!showMenu) return;

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
        }

        GUI.Box(new Rect(10, 10, 500, 850), "<b>PANEL DE CONTROL TOTAL (MODO INFINITO)</b>");

        if (Guardado.instance == null)
        {
            GUI.Label(new Rect(30, 50, 400, 40), "ERROR: Guardado.instance no encontrado", labelStyle);
            return;
        }

        scrollPosition = GUI.BeginScrollView(new Rect(25, 60, 450, 700), scrollPosition, new Rect(0, 0, 420, 1200));

        int y = 0;
        int btnH = 40;

        // --- SECCIÓN: CONTROL DE TIEMPO ---
        Header("⏰ CONTROL DE TIEMPO", ref y);

        GUI.backgroundColor = Color.yellow;
        if (Btn("TEST RÁPIDO (3 Segundos)", ref y, 50))
        {
            LevelManager.instance.gameDuration = 3f;
            Debug.Log("<color=yellow>Modo Test: Partidas de 3s activadas.</color>");
        }

        GUI.backgroundColor = Color.white;
        if (Btn("TIEMPO NORMAL (20 Segundos)", ref y, 50))
        {
            LevelManager.instance.gameDuration = 20f;
            Debug.Log("<color=white>Modo Normal: Partidas de 20s restauradas.</color>");
        }
        y += 20;

        // --- SECCIÓN: MEJORAS DE CONTAGIO ---
        Header("⚡ MEJORAS DE CONTAGIO", ref y);
        if (Btn("Spawn Interval -20%", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(0.20f);
        if (Btn("Spawn Interval -60%", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(0.60f);
        if (Btn("Spawn Interval -100% (MAX)", ref y, 45)) Guardado.instance.AddSpawnSpeedBonus(1.00f);
        if (Btn("Infect Speed +50%", ref y, 45)) Guardado.instance.SetInfectSpeedMultiplier(1.5f);
        if (Btn("Infect Speed +100% (MAX)", ref y, 45)) Guardado.instance.SetInfectSpeedMultiplier(2.0f);
        y += 20;

        // --- RECURSOS ---
        Header("💰 RECURSOS BÁSICOS", ref y);
        if (Btn("+5000 Monedas de Contagio", ref y, btnH)) LevelManager.instance.AddCoins(5000);
        y += 20;

        // --- VIRUS STATS ---
        Header("🦠 VIRUS STATS", ref y);
        Label($"Radio Multiplier: {Guardado.instance.radiusMultiplier:F2}", ref y);
        if (Btn("Radio +0.5", ref y, btnH))
        {
            Guardado.instance.SetRadiusMultiplier(Guardado.instance.radiusMultiplier + 0.5f);
            if (VirusRadiusController.instance) VirusRadiusController.instance.ApplyScale();
        }

        Label($"Velocidad Multiplier: {Guardado.instance.speedMultiplier:F2}", ref y);
        if (Btn("Velocidad +0.5", ref y, btnH))
        {
            Guardado.instance.SetSpeedMultiplier(Guardado.instance.speedMultiplier + 0.5f);
            if (VirusMovement.instance != null) VirusMovement.instance.ApplySpeedMultiplier();
        }
        y += 20;

        // --- HABILIDADES DEL ÁRBOL ---
        Header("🌳 ÁRBOL DE HABILIDADES", ref y);
        if (Btn("Mejora Inicial Aleatoria", ref y, btnH)) Guardado.instance.AssignRandomInitialUpgrade();
        if (Btn("Multiplicador Monedas x6", ref y, btnH)) Guardado.instance.SetCoinMultiplier(6);
        if (Btn("Empezar con 50.000 Coins", ref y, btnH)) Guardado.instance.SetStartingCoins(50000);

        // ELIMINADO EL BOTÓN DE AÑADIR DÍAS/CURA AQUÍ

        if (Btn("Activar Descuento Zonas", ref y, btnH)) Guardado.instance.ActivateZoneDiscount();
        if (Btn("Ingreso Pasivo (1000/zona)", ref y, btnH)) Guardado.instance.SetZonePassiveIncome(1000);
        y += 20;

        // --- TOGGLES ---
        Header("⚙️ ESTADOS ESPECIALES", ref y);
        Guardado.instance.keepUpgradesOnReset = GUI.Toggle(new Rect(0, y, 400, 35), Guardado.instance.keepUpgradesOnReset, " Persistencia Mejoras", toggleStyle); y += 40;
        Guardado.instance.keepZonesUnlocked = GUI.Toggle(new Rect(0, y, 400, 35), Guardado.instance.keepZonesUnlocked, " Mantener Zonas", toggleStyle); y += 50;

        // --- SISTEMA ---
        Header("🖥️ SISTEMA", ref y);
        GUI.backgroundColor = Color.red;
        if (Btn("RESET TOTAL Y RECARGAR", ref y, 60))
        {
            Guardado.instance.ResetAllProgress();
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