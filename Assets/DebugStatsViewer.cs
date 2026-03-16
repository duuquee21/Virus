using UnityEngine;

public class DebugStatsViewer : MonoBehaviour
{
    private bool showMenu = false;
    private Vector2 scrollPosition;
    public KeyCode toggleKey = KeyCode.F2;

    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle buttonStyle;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showMenu = !showMenu;
    }

    void OnGUI()
    {
        if (!showMenu) return;

        InitStyles();

        GUI.Box(new Rect(610, 10, 620, 990), "MONITOR DE ESTADISTICAS", boxStyle);

        if (Guardado.instance == null)
        {
            GUI.Label(new Rect(630, 50, 500, 40), "ERROR: Guardado.instance no encontrado", labelStyle);
            return;
        }

        scrollPosition = GUI.BeginScrollView(
            new Rect(625, 60, 585, 870),
            scrollPosition,
            new Rect(0, 0, 540, 5200)
        );

        int y = 0;

        Header("[Guardado]", ref y);
        Label($"totalInfected: {Guardado.instance.totalInfected}", ref y);
        Label($"coinMultiplier: {Guardado.instance.coinMultiplier}", ref y);
        Label($"startingCoins: {Guardado.instance.startingCoins}", ref y);
        Label($"spawnSpeedBonus: {Guardado.instance.spawnSpeedBonus:F2}", ref y);
        Label($"populationBonus: {Guardado.instance.populationBonus:F2}", ref y);
        Label($"zoneDiscountActive: {Guardado.instance.zoneDiscountActive}", ref y);
        Label($"coinsPerZoneDaily: {Guardado.instance.coinsPerZoneDaily}", ref y);
        Label($"keepUpgradesOnReset: {Guardado.instance.keepUpgradesOnReset}", ref y);
        Label($"keepZonesUnlocked: {Guardado.instance.keepZonesUnlocked}", ref y);

        y += 10;

        Header("[Virus]", ref y);
        Label($"radiusMultiplier: {Guardado.instance.radiusMultiplier:F2}", ref y);
        Label($"speedMultiplier: {Guardado.instance.speedMultiplier:F2}", ref y);
        Label($"infectSpeedMultiplier: {Guardado.instance.infectSpeedMultiplier:F2}", ref y);
        Label($"extraBaseTime: {Guardado.instance.extraBaseTime:F2}", ref y);
        Label($"addTimeOnPhaseChance: {Guardado.instance.addTimeOnPhaseChance * 100f:F0}%", ref y);
        Label($"doubleUpgradeChance: {Guardado.instance.doubleUpgradeChance * 100f:F0}%", ref y);
        Label($"randomSpawnPhaseChance: {Guardado.instance.randomSpawnPhaseChance * 100f:F0}%", ref y);
        Label($"spawnBaseOnMaxPhaseChance: {Guardado.instance.spawnBaseOnMaxPhaseChance * 100f:F0}%", ref y);

        y += 10;

        Header("[Daño Extra]", ref y);
        Label($"dañoExtraHexagono: {Guardado.instance.dañoExtraHexagono}", ref y);
        Label($"dañoExtraPentagono: {Guardado.instance.dañoExtraPentagono}", ref y);
        Label($"dañoExtraCuadrado: {Guardado.instance.dañoExtraCuadrado}", ref y);
        Label($"dañoExtraTriangulo: {Guardado.instance.dañoExtraTriangulo}", ref y);
        Label($"dañoExtraCirculo: {Guardado.instance.dañoExtraCirculo}", ref y);

        y += 10;

        Header("[Coins Extra]", ref y);
        Label($"coinsExtraHexagono: {Guardado.instance.coinsExtraHexagono}", ref y);
        Label($"coinsExtraPentagono: {Guardado.instance.coinsExtraPentagono}", ref y);
        Label($"coinsExtraCuadrado: {Guardado.instance.coinsExtraCuadrado}", ref y);
        Label($"coinsExtraTriangulo: {Guardado.instance.coinsExtraTriangulo}", ref y);
        Label($"coinsExtraCirculo: {Guardado.instance.coinsExtraCirculo}", ref y);

        y += 10;

        Header("[Entorno / Especiales]", ref y);
        Label($"paredInfectivaActiva: {Guardado.instance.paredInfectivaActiva}", ref y);
        Label($"nivelParedInfectiva: {Guardado.instance.nivelParedInfectiva}", ref y);
        Label($"virusReboteActiva: {Guardado.instance.virusReboteActiva}", ref y);
        Label($"destroyCoralOnInfectedImpact: {Guardado.instance.destroyCoralOnInfectedImpact}", ref y);
        Label($"probabilidadDuplicarChoque: {Guardado.instance.probabilidadDuplicarChoque * 100f:F0}%", ref y);
        Label($"nivelCarambola: {Guardado.instance.nivelCarambola}", ref y);

        y += 10;

        Header("[Coral / Hoja / Agujero]", ref y);
        Label($"coralInfeciosoActivo: {Guardado.instance.coralInfeciosoActivo}", ref y);
        Label($"coralCapacity: {Guardado.instance.coralCapacity}", ref y);
        Label($"hojaNegraData: {Guardado.instance.hojaNegraData}", ref y);
        Label($"hojaSpawnRate: {Guardado.instance.hojaSpawnRate:F2}", ref y);
        Label($"hojaFases: {Guardado.instance.hojaFases}", ref y);
        Label($"agujeroNegroData: {Guardado.instance.agujeroNegroData}", ref y);
        Label($"agujeroSpawnRate: {Guardado.instance.agujeroSpawnRate:F2}", ref y);

        y += 10;

        Header("[Levels]", ref y);
        Label($"speedLevel: {Guardado.instance.speedLevel}", ref y);
        Label($"radiusLevel: {Guardado.instance.radiusLevel}", ref y);
        Label($"capacityLevel: {Guardado.instance.capacityLevel}", ref y);
        Label($"timeLevel: {Guardado.instance.timeLevel}", ref y);
        Label($"infectionSpeedLevel: {Guardado.instance.infectionSpeedLevel}", ref y);

        y += 10;

        if (Guardado.instance.infectSpeedPerPhase != null)
        {
            Header("[infectSpeedPerPhase]", ref y);
            for (int i = 0; i < Guardado.instance.infectSpeedPerPhase.Length; i++)
            {
                Label($"infectSpeedPerPhase[{i}]: {Guardado.instance.infectSpeedPerPhase[i]:F2}", ref y);
            }
        }

        y += 10;

        if (Guardado.instance.probParedInfectiva != null)
        {
            Header("[probParedInfectiva]", ref y);
            for (int i = 0; i < Guardado.instance.probParedInfectiva.Length; i++)
            {
                Label($"probParedInfectiva[{i}]: {Guardado.instance.probParedInfectiva[i]:F2}", ref y);
            }
        }

        y += 10;

        if (LevelManager.instance != null)
        {
            Header("[LevelManager]", ref y);
            Label($"ContagionCoins: {LevelManager.instance.ContagionCoins}", ref y);
            Label($"isGameActive: {LevelManager.instance.isGameActive}", ref y);
            Label($"timerStarted: {LevelManager.instance.timerStarted}", ref y);
            Label($"gameDuration: {LevelManager.instance.gameDuration:F2}", ref y);
        }

        y += 10;

        if (PopulationManager.instance != null)
        {
            Header("[PopulationManager]", ref y);
            Label($"spawnInterval: {PopulationManager.instance.spawnInterval:F2}", ref y);
            Label($"initialPopulation: {PopulationManager.instance.initialPopulation}", ref y);
        }

        y += 10;

        Header("[PersonaInfeccion - Global]", ref y);
        Label($"dañoTotalZona: {PersonaInfeccion.dañoTotalZona:F2}", ref y);
        Label($"dañoTotalChoque: {PersonaInfeccion.dañoTotalChoque:F2}", ref y);
        Label($"dañoTotalCarambola: {PersonaInfeccion.dañoTotalCarambola:F2}", ref y);

        y += 10;

        if (PersonaInfeccion.evolucionesEntreFases != null)
        {
            Header("[evolucionesEntreFases]", ref y);
            for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
            {
                Label($"evolucionesEntreFases[{i}]: {PersonaInfeccion.evolucionesEntreFases[i]}", ref y);
            }
        }

        y += 10;

        if (PersonaInfeccion.evolucionesPorChoque != null)
        {
            Header("[evolucionesPorChoque]", ref y);
            for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
            {
                Label($"evolucionesPorChoque[{i}]: {PersonaInfeccion.evolucionesPorChoque[i]}", ref y);
            }
        }

        y += 10;

        if (PersonaInfeccion.evolucionesCarambola != null)
        {
            Header("[evolucionesCarambola]", ref y);
            for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
            {
                Label($"evolucionesCarambola[{i}]: {PersonaInfeccion.evolucionesCarambola[i]}", ref y);
            }
        }

        GUI.EndScrollView();

        if (GUI.Button(new Rect(625, 945, 585, 35), "CERRAR (F2)", buttonStyle))
            showMenu = false;
    }

    void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 22;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.green;

        subHeaderStyle = new GUIStyle(GUI.skin.label);
        subHeaderStyle.fontSize = 18;
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.normal.textColor = Color.cyan;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 18;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.alignment = TextAnchor.UpperCenter;
        boxStyle.normal.textColor = Color.white;
    }

    void Header(string t, ref int y)
    {
        GUI.Label(new Rect(0, y, 520, 34), t, headerStyle);
        y += 34;
    }

    void Label(string t, ref int y)
    {
        GUI.Label(new Rect(10, y, 520, 24), t, labelStyle);
        y += 24;
    }
}