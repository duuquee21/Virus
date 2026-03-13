# Guía de Debugging y Recomendaciones Adicionales

## 🐛 Cómo Debuggear si el Problema Persiste

### 1. ** Agregar Logs para Rastrear Guardados**

Modifica `Guardado.cs` para agregar logging detallado:

```csharp
public void SaveData()
{
    Debug.Log("<color=cyan>[GUARDADO]</color> Guardando datos permanentes...");
    
    // ... resto del código ...
    
    PlayerPrefs.Save();
    Debug.Log("<color=green>[GUARDADO]</color> ✓ Datos permanentes guardados");
}

public void SaveEvolutionData()
{
    Debug.Log("<color=cyan>[GUARDADO]</color> Guardando datos de evolución...");
    // ... resto del código ...
    PlayerPrefs.Save();
    Debug.Log("<color=green>[GUARDADO]</color> ✓ Datos de evolución guardados");
}
```

### 2. **Agregar Logs en SkillNode**

```csharp
public void TryUnlock()
{
    // ... validaciones ...
    
    Debug.Log($"<color=yellow>[SKILL]</color> Desbloqueando {skillNameKey}");
    Debug.Log($"<color=yellow>[SKILL]</color> repeatLevel: {repeatLevel}, unlocked: {unlocked}");
    
    ApplyEffect();
    
    Debug.Log($"<color=green>[SKILL]</color> ✓ Efecto aplicado");
}

public void SaveNodeState()
{
    if (string.IsNullOrEmpty(saveID)) return;

    Debug.Log($"<color=cyan>[NODE]</color> Guardando nodo {saveID}: unlocked={unlocked}, repeat={repeatLevel}");
    PlayerPrefs.SetInt("Skill_" + saveID + "_Unlocked", unlocked ? 1 : 0);
    PlayerPrefs.SetInt("Skill_" + saveID + "_Repeat", repeatLevel);
    Debug.Log($"<color=green>[NODE]</color> ✓ Nodo {saveID} guardado");
}

public void LoadNodeState()
{
    if (string.IsNullOrEmpty(saveID)) return;

    Debug.Log($"<color=cyan>[NODE]</color> Cargando nodo {saveID}...");
    // ... resto del código ...
    Debug.Log($"<color=green>[NODE]</color> ✓ Nodo {saveID} cargado: unlocked={unlocked}, repeat={repeatLevel}");
}
```

### 3. **Verificar Integridad de Datos**

Crea un método para auditar los datos:

```csharp
public void AuditarDatos()
{
    Debug.Log("\n<color=magenta">========== AUDITORÍA DE DATOS ==========</color>");
    
    Debug.Log($"Guardado.instance values:");
    Debug.Log($"  coinMultiplier: {coinMultiplier}");
    Debug.Log($"  radiusMultiplier: {radiusMultiplier}");
    Debug.Log($"  speedMultiplier: {speedMultiplier}");
    Debug.Log($"  startingCoins: {startingCoins}");
    Debug.Log($"  keepUpgradesOnReset: {keepUpgradesOnReset}");
    Debug.Log($"  keepZonesUnlocked: {keepZonesUnlocked}");
    
    Debug.Log($"\nPlayerPrefs values:");
    Debug.Log($"  CoinMultiplier: {PlayerPrefs.GetInt("CoinMultiplier", -1)}");
    Debug.Log($"  RadiusMult: {PlayerPrefs.GetFloat("RadiusMult", -1f)}");
    Debug.Log($"  SpeedMult: {PlayerPrefs.GetFloat("SpeedMult", -1f)}");
    Debug.Log($"  StartingCoins: {PlayerPrefs.GetInt("StartingCoins", -1)}");
    Debug.Log($"  KeepUpgrades: {PlayerPrefs.GetInt("KeepUpgrades", -1)}");
    Debug.Log($"  KeepZones: {PlayerPrefs.GetInt("KeepZones", -1)}");
    
    Debug.Log($"\nSkillNode runtime state:");
    Debug.Log($"  runtimeUnlocked.Count: {SkillNode.runtimeUnlocked.Count}");
    Debug.Log($"  runtimeRepeat.Count: {SkillNode.runtimeRepeat.Count}");
    
    Debug.Log("\n<color=magenta">====================================</color>\n");
}
```

---

## ⚕️ Problemas Potenciales Similares

Basándome en el patrón encontrado, aquí hay otros puntos que podrían estar rotos:

### Problema A: **Métodos en Guardado.cs que NO guardan**
```csharp
// ❌ ESTOS podrían estar sin guardar:
public void AddCoinMultiplier(int extra) { coinMultiplier += extra; }
// ↓ ¿Llama a SaveData()? Solución: Agregar SaveData() al final
public void AddCoinMultiplier(int extra) { coinMultiplier += extra; SaveData(); }
```

### Problema B: **SettersMethods sin persistencia**
```csharp
public void SetRadiusMultiplier(float val)
{
    radiusMultiplier = val;
    // ⚠️ ¿Se llama a SaveData()?
}
```

### Problema C: **Cargar datos pero no sincronizar UI**
Si cargas datos de PlayerPrefs pero la UI no se actualiza, hay que llamar a:
```csharp
Guardado.instance.LoadData();
LevelManager.instance.UpdateUI();  // Actualizar UI después de cargar
```

---

## 🧪 Plan de Pruebas Recomendado

### Test 1: Guardado Inmediato
1. Inicia juego
2. Compra una habilidad
3. Instala esto en el menú para ver el estado:
   ```csharp
   Debug.Log($"SaveID=test, Unlocked={PlayerPrefs.GetInt("Skill_test_Unlocked", -1)}");
   ```
4. Cierra juego
5. Reabre y verifica que está guardado

### Test 2: Desincronización
1. Inicia partida
2. Compra habilidades que aumenten `radiusMultiplier`
3. Anota el valor mostrado en UI
4. Llama a `Guardado.instance.LoadData()` en consola
5. Verifica que el valor sea el mismo

### Test 3: Cambio de Escena
1. Compra una habilidad
2. Cambia de mapa/zona
3. Vuelve atrás
4. Verifica que la habilidad sigue desbloqueada

### Test 4: Guardado al Cerrar
1. Compra varias habilidades
2. **NO vayas al menú, cierra directamente la app**
3. Reabre el juego
4. Verifica que los cambios se guardaron (revisar OnApplicationQuit logs)

---

## 🔧 Soluciones Rápidas Adicionales

Si aún hay problemas, intenta estas cosas:

### Solución A: Forzar sincronización en UI
```csharp
// Agregar al método UpdateUI() de LevelManager:
public void UpdateUI()
{
    // Sincronizar datos de Guardado justo antes de mostrar
    if (Guardado.instance != null)
    {
        Guardado.instance.LoadData();  // Cargar datos frescos
    }
    
    // ... resto del código ...
}
```

### Solución B: Crear un método centralizado de guardado
```csharp
public void SaveEverything()
{
    SaveData();              // Permanentes
    SaveEvolutionData();     // Estadísticas
    
    SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
    foreach (SkillNode node in nodes)
    {
        node.SaveNodeState();
    }
    
    PlayerPrefs.Save();
    Debug.Log("<color=green">[SAVE]</color> TODO guardado completamente");
}
```

### Solución C: Agregar Delay a PlayerPrefs.Save()
En algunos casos, `PlayerPrefs.Save()` necesita un pequeño delay:
```csharp
public IEnumerator SaveDataWithDelay()
{
    SaveData();
    SaveEvolutionData();
    yield return new WaitForSeconds(0.1f);  // Esperar a que escriba en disco
    PlayerPrefs.Save();
    Debug.Log("Guardado completo con delay");
}
```

---

## 📋 Checklist Final

Antes de dar por resuelta la desincronización, verifica:

- [ ] Todas las habilidades que modifican datos ahora llaman a SaveData()
- [ ] OnApplicationQuit() existe en Guardado.cs
- [ ] ClearRuntimeState() se llama cuando vuelves al menú
- [ ] GameOver() guarda datos antes de limpiar
- [ ] No hay líneas duplicadas en el código
- [ ] Los logs muestran que los datos se guardan después de cada compra
- [ ] Al continuar partida, los nodos están desbloqueados correctamente
- [ ] Las estadísticas (velocidad, radio, etc.) se recuerdan entre partidas

---

## 🚀 Si Todo Funciona

Considera agregar estas mejoras:

1. **Validación de integridad**
   ```csharp
   // Verificar que no hay inconsistencias en carga
   if (loadedValue != expectedValue) Debug.LogErrorl
   ```

2. **Backup de datos**
   ```csharp
   // Guardar varias versiones por si una se corrompe
   ```

3. **Compresión/Encriptación**
   ```csharp
   // PlayerPrefs en algunos sistemas puede ser vulnerable
   ```

---

## 📞 Support Notes

Si los problemas persisten después de todas estas soluciones, posibles causas:
- Unity version mismatch en serialización
- PlayerPrefs corromptos (limpiar con DeleteAll y empezar nuevo)
- Script execution order incorrecta
- Valores por defecto inconsistentes entre LoadData() y SaveData()

