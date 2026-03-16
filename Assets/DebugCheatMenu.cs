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
    private GUIStyle boxStyle;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showMenu = !showMenu;
    }

    void OnGUI()
    {
        if (!showMenu) return;

        InitStyles();

        GUI.Box(new Rect(10, 10, 580, 990), "PANEL DE CHEATS", boxStyle);

        if (Guardado.instance == null)
        {
            GUI.Label(new Rect(30, 50, 500, 40), "ERROR: Guardado.instance no encontrado", labelStyle);
            return;
        }

        scrollPosition = GUI.BeginScrollView(
            new Rect(25, 60, 545, 870),
            scrollPosition,
            new Rect(0, 0, 500, 2600)
        );

        int y = 0;
        int btnH = 40;

        // -------------------------------------------------
        // CONTROL DE TIEMPO
        // -------------------------------------------------
        Header("CONTROL DE TIEMPO", ref y);

        GUI.backgroundColor = Color.yellow;
        if (Btn("TEST RAPIDO (3 Segundos)", ref y, 50))
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.gameDuration = 3f;
                Debug.Log("<color=yellow>Modo Test: Partidas de 3s activadas.</color>");
            }
        }

        GUI.backgroundColor = Color.white;
        if (Btn("TIEMPO NORMAL (20 Segundos)", ref y, 50))
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.gameDuration = 20f;
                Debug.Log("<color=white>Modo Normal: Partidas de 20s restauradas.</color>");
            }
        }

        y += 20;

        // -------------------------------------------------
        // MEJORAS DE CONTAGIO
        // -------------------------------------------------
        Header("MEJORAS DE CONTAGIO", ref y);

        if (Btn("Spawn Interval -20%", ref y, 45))
        {
            Guardado.instance.AddSpawnSpeedBonus(0.20f);
            Guardado.instance.SaveData();
        }

        if (Btn("Spawn Interval -60%", ref y, 45))
        {
            Guardado.instance.AddSpawnSpeedBonus(0.60f);
            Guardado.instance.SaveData();
        }

        if (Btn("Spawn Interval -100% (MAX)", ref y, 45))
        {
            Guardado.instance.AddSpawnSpeedBonus(1.00f);
            Guardado.instance.SaveData();
        }

        if (Btn("Infect Speed +50%", ref y, 45))
        {
            Guardado.instance.SetInfectSpeedMultiplier(1.5f);
            Guardado.instance.SaveData();
        }

        if (Btn("Infect Speed +100% (MAX)", ref y, 45))
        {
            Guardado.instance.SetInfectSpeedMultiplier(2.0f);
            Guardado.instance.SaveData();
        }

        y += 20;

        // -------------------------------------------------
        // RECURSOS
        // -------------------------------------------------
        Header("RECURSOS BASICOS", ref y);

        if (Btn("+5000 Monedas de Contagio", ref y, btnH))
        {
            if (LevelManager.instance != null)
                LevelManager.instance.AddCoins(5000);
        }

        y += 20;

        // -------------------------------------------------
        // VIRUS STATS
        // -------------------------------------------------
        Header("VIRUS STATS", ref y);

        Label($"Radio Multiplier actual: {Guardado.instance.radiusMultiplier:F2}", ref y);
        if (Btn("Radio +0.5", ref y, btnH))
        {
            Guardado.instance.SetRadiusMultiplier(Guardado.instance.radiusMultiplier + 0.5f);
            Guardado.instance.SaveData();

            if (VirusRadiusController.instance != null)
                VirusRadiusController.instance.ApplyScale();
        }

        Label($"Velocidad Multiplier actual: {Guardado.instance.speedMultiplier:F2}", ref y);
        if (Btn("Velocidad +0.5", ref y, btnH))
        {
            Guardado.instance.SetSpeedMultiplier(Guardado.instance.speedMultiplier + 0.5f);
            Guardado.instance.SaveData();

            if (VirusMovement.instance != null)
                VirusMovement.instance.ApplySpeedMultiplier();
        }

        y += 20;

        // -------------------------------------------------
        // ARBOL DE HABILIDADES
        // -------------------------------------------------
        Header("ARBOL DE HABILIDADES", ref y);

        if (Btn("Mejora Inicial Aleatoria", ref y, btnH))
        {
            Guardado.instance.AssignRandomInitialUpgrade();
            Guardado.instance.SaveData();
        }

        if (Btn("Multiplicador Monedas x6", ref y, btnH))
        {
            Guardado.instance.SetCoinMultiplier(6);
            Guardado.instance.SaveData();
        }

        if (Btn("Empezar con 50.000 Coins", ref y, btnH))
        {
            Guardado.instance.SetStartingCoins(50000);
            Guardado.instance.SaveData();
        }

        if (Btn("Activar Descuento Zonas", ref y, btnH))
        {
            Guardado.instance.ActivateZoneDiscount();
            Guardado.instance.SaveData();
        }

        if (Btn("Ingreso Pasivo (1000/zona)", ref y, btnH))
        {
            Guardado.instance.SetZonePassiveIncome(1000);
            Guardado.instance.SaveData();
        }

        y += 20;

        // -------------------------------------------------
        // PRUEBAS HABILIDAD FASE FINAL
        // -------------------------------------------------
        Header("PRUEBAS HABILIDAD FASE FINAL", ref y);

        if (Btn("Set SpawnBaseOnMaxPhaseChance = 20%", ref y, btnH))
        {
            Guardado.instance.spawnBaseOnMaxPhaseChance = 0.20f;
            Guardado.instance.SaveData();
            Debug.Log("<color=lime>[DEBUG]</color> spawnBaseOnMaxPhaseChance = 20%");
        }

        if (Btn("Set SpawnBaseOnMaxPhaseChance = 100%", ref y, btnH))
        {
            Guardado.instance.spawnBaseOnMaxPhaseChance = 1.00f;
            Guardado.instance.SaveData();
            Debug.Log("<color=lime>[DEBUG]</color> spawnBaseOnMaxPhaseChance = 100%");
        }

        if (Btn("Reset SpawnBaseOnMaxPhaseChance = 0%", ref y, btnH))
        {
            Guardado.instance.spawnBaseOnMaxPhaseChance = 0f;
            Guardado.instance.SaveData();
            Debug.Log("<color=lime>[DEBUG]</color> spawnBaseOnMaxPhaseChance = 0%");
        }

        y += 20;

        // -------------------------------------------------
        // ESTADOS ESPECIALES
        // -------------------------------------------------
        Header("ESTADOS ESPECIALES", ref y);

        bool keepUpgrades = GUI.Toggle(
            new Rect(0, y, 470, 35),
            Guardado.instance.keepUpgradesOnReset,
            " Persistencia Mejoras",
            toggleStyle
        );
        if (keepUpgrades != Guardado.instance.keepUpgradesOnReset)
        {
            Guardado.instance.keepUpgradesOnReset = keepUpgrades;
            Guardado.instance.SaveData();
        }
        y += 40;

        bool keepZones = GUI.Toggle(
            new Rect(0, y, 470, 35),
            Guardado.instance.keepZonesUnlocked,
            " Mantener Zonas",
            toggleStyle
        );
        if (keepZones != Guardado.instance.keepZonesUnlocked)
        {
            Guardado.instance.keepZonesUnlocked = keepZones;
            Guardado.instance.SaveData();
        }
        y += 50;

        // -------------------------------------------------
        // SISTEMA
        // -------------------------------------------------
        Header("SISTEMA", ref y);

        GUI.backgroundColor = Color.red;
        if (Btn("RESET TOTAL Y RECARGAR", ref y, 60))
        {
            Guardado.instance.ResetAllProgress();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUI.backgroundColor = Color.white;

        GUI.EndScrollView();

        if (GUI.Button(new Rect(25, 945, 545, 35), "CERRAR (F1)", buttonStyle))
            showMenu = false;
    }

    void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 24;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;

        toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fontSize = 18;
        toggleStyle.normal.textColor = Color.white;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 18;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.alignment = TextAnchor.UpperCenter;
        boxStyle.normal.textColor = Color.white;
    }

    void Header(string t, ref int y)
    {
        GUI.Label(new Rect(0, y, 470, 40), t, headerStyle);
        y += 42;
    }

    void Label(string t, ref int y)
    {
        GUI.Label(new Rect(10, y, 470, 26), t, labelStyle);
        y += 24;
    }

    bool Btn(string t, ref int y, int h)
    {
        bool pressed = GUI.Button(new Rect(0, y, 460, h), t, buttonStyle);
        y += h + 10;
        return pressed;
    }
}