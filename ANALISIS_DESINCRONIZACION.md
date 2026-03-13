# Análisis de Problemas en Sistema de Guardado

## 🔴 PROBLEMAS CRÍTICOS ENCONTRADOS

### 1. **FALTA DE SaveData() DESPUÉS DE APLICAR EFECTOS**

**Ubicación:** [SkillNode.cs](SkillNode.cs#L390-L800) - Método `ApplyEffect()`

**Problema:**
La mayoría de habilidades modifican `Guardado.instance` pero NO llaman a `SaveData()` inmediatamente.

**Ejemplos de habilidades SIN SaveData():**
- `AddSpawnSpeedBonus()` - modifica `spawnSpeedBonus` pero no guarda
- `AddRadiusMultiplier()` - modifica `radiusMultiplier` pero no guarda
- `AddPopulationBonus()` - modifica `populationBonus` pero no guarda
- `SetDuplicateProbability()` - modifica `probabilidadDuplicarChoque` pero no guarda
- `SetInfectionSpeedBonus()` - modifica `infectSpeedMultiplier` pero no guarda
- Todas las mejoras "Add*" de fases de velocidad de infección
- Todas las mejoras "Add*Chance" (AddTimeOnPhaseChance, DoubleUpgradeChance, etc.)

**Consecuencia:**
Si se produce un crash, cierre inesperado del juego o transición de escena, estos cambios se pierden porque solo se guardaban en memoria de `Guardado.instance`, no en `PlayerPrefs`.

**Ejemplo de código problemático:**
```csharp
case SkillEffectType.ReduceSpawnInterval20: 
    Guardado.instance.AddSpawnSpeedBonus(0.5f); 
    break;  // ❌ No guarda
```

**Código correcto debería ser:**
```csharp
case SkillEffectType.ReduceSpawnInterval20: 
    Guardado.instance.AddSpawnSpeedBonus(0.5f);
    Guardado.instance.SaveData();  // ✅ Guarda ahora
    break;
```

---

### 2. **DICCIONARIO `runtimeUnlocked` NO SE SINCRONIZA CON ESTADO REAL**

**Ubicación:** [SkillNode.cs](SkillNode.cs#L11-L12) y [SkillNode.cs](SkillNode.cs#L167-L195)

**Problema:**
- El diccionario `runtimeUnlocked` es **estático** y persiste mientras la escena está activa
- Se actualiza correctamente en `TryUnlock()` cuando se compra una habilidad
- Pero NO se limpia cuando se sale de la partida
- Cuando vuelves a cargar la misma partida, puede que el diccionario tenga datos stale (obsoletos)

**Código problemático:**
```csharp
private static readonly System.Collections.Generic.Dictionary<string, bool> runtimeUnlocked =
    new System.Collections.Generic.Dictionary<string, bool>();
```

Cuando se carga la partida nuevamente:
1. El diccionario sigue conteniendo datos de la partida anterior
2. Si los datos de PlayerPrefs cambiaron, el diccionario no se actualiza
3. Esto causa desincronización

**Flujo actual (problemático):**
```
Cargar Partida A
  ↓
  → Se cargan nodos de Partida A al diccionario
Cambiar a otra escena
  ↓
  → Diccionario NO se limpia
Cargar Partida A nuevamente
  ↓
  → El diccionario aún tiene datos de la carga anterior
  → Puede que NO coincida con PlayerPrefs si hubo cambios
```

---

### 3. **FALTA DE SINCRONIZACIÓN ENTRE DATOS PERMANENTES Y DATOS DE RUN**

**Ubicación:** [LevelManager.cs](LevelManager.cs#L520-L560) y [Guardado.cs](Guardado.cs#L462-L510)

**Problema:**
Cuando termina una partida, se llama a:
1. `SaveEvolutionData()` - Guarda dados de PersonaInfeccion (estadísticas de combate)
2. `SaveData()` - Guarda datos permanentes (upgrades del árbol de habilidades)
3. `SaveNodeState()` - Guarda estado de nodos individuales

Estos tres métodos NO verifican si están en conflicto entre sí.

**Ejemplo de desincronización:**
1. Desbloqueas "Daño +1 a Hexágono"
2. LevelManager.instance no ha actualizado UI
3. Se guarda nodo state
4. Se guarda data de Guardado
5. Pero si hay un crash aquí, dañoExtraHexagono puede estar sin guardar

---

### 4. **LÍNEA DUPLICADA EN SaveData()**

**Ubicación:** [Guardado.cs](Guardado.cs#L209-L210)

**Problema:**
```csharp
PlayerPrefs.SetInt("HojaNegraData", hojaNegraData ? 1 : 0);
PlayerPrefs.SetInt("HojaNegraData", hojaNegraData ? 1 : 0);  // ❌ DUPLICADO INÚTIL
```

No causa error pero es redundante y confuso.

---

### 5. **NO HAY ESPERA PARA QUE PlayerPrefs.Save() TERMINE**

**Ubicación:** Múltiples archivos - después de `PlayerPrefs.Save()`

**Problema:**
`PlayerPrefs.Save()` es asincrónico (especialmente en móvil). Si hay un cierre rápido después de guardado, los datos pueden no escribirse completamente.

---

### 6. **LoadEvolutionData() NO SE LLAMA AL CONTINUAR PARTIDA GUARDADA**

**Ubicación:** [LevelManager.cs](LevelManager.cs#L278-L310)

**Problema:**
En `RestoreFromSave()` se llama a `LoadEvolutionData()`, pero:
- ¿Se llama en todos los casos de carga?
- ¿Qué pasa si se sale de la escena sin guardar y se vuelve a cargar?

---

### 7. **DATOS ESTÁTICOS DE PersonaInfeccion NO SE RESETEAN CORRECTAMENTE**

**Ubicación:** [PersonaInfeccion.cs](../Scripts/Personas/PersonaInfeccion.cs#L5-L30)

**Problema:**
```csharp
public static float[] dañoZonaPorFase = new float[5];
public static float[] dañoChoquePorFase = new float[5];
public static int[] golpesAlPlanetaPorFase = new int[5];
```

Estos son **estáticos** y persisten entre escenas. Si no se resetean correctamente, pueden acumular datos de partidas anteriores.

**Verificar:** ¿Se llama `PersonaInfeccion.ResetearEstadisticas()` en el lugar correcto?

---

## 📋 PASOS PARA REPRODUCIR EL BUG

1. Desbloquea varias habilidades en una partida
2. Sales de la partida (sin cerrar el juego completamente)
3. Continúa la partida guardada
4. **Resultado esperado:** Las habilidades desbloqueadas se ven y funcionan
5. **Resultado actual:** Algunas habilidades pueden no aparecer desbloqueadas en el árbol

---

## ✅ SOLUCIONES RECOMENDADAS

### Solución 1: Guardar después de cada efecto de habilidad
Agregar `Guardado.instance.SaveData()` después de cada caso en `ApplyEffect()` que modifique datos permanentes.

### Solución 2: Limpiar diccionario runtime al cambiar escena
```csharp
public static void ClearRuntimeState()
{
    runtimeUnlocked.Clear();
    runtimeRepeat.Clear();
}
```
Llamar esto cuando se sale de una partida.

### Solución 3: Sincronización central de guardado
Crear un método centralizado que guarde TODO al mismo tiempo:
```csharp
public void SaveGameState()
{
    SaveData();              // Permanentes
    SaveEvolutionData();     // Estadísticas de run
    SaveNodeState();         // Estado de nodos (para cada nodo)
    PlayerPrefs.Save();
}
```

### Solución 4: Auditoría de todos los métodos Add*/Set* en Guardado
Verificar que TODOS los métodos que modifican datos llamen a `SaveData()` o usa una property automática que guarde.

### Solución 5: Usar un OnApplicationQuit para forzar guardado final
```csharp
void OnApplicationQuit()
{
    Guardado.instance.SaveData();
    Guardado.instance.SaveEvolutionData();
    // Guardar todos los nodos
}
```

---

## 🔍 VERIFICACIONES NECESARIAS

1. Revisar **TODOS** los casos en `ApplyEffect()` de SkillNode
2. Revisar **TODOS** los métodos públicos de Guardado que modifiquen datos
3. Confirmar que `PersonaInfeccion.ResetearEstadisticas()` existe y funciona
4. Verificar transiciones de escena y limpieza de datos
5. Revisar si hay corrutinas de guardado que pueden interrumpirse

