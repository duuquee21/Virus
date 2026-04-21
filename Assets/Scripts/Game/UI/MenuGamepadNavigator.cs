// Soporte para Input System
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuGamepadNavigator : MonoBehaviour
{
    // Para navegación por flanco
    private float prevVertical = 0f;
    private float prevHorizontal = 0f;
    private bool verticalReady = true;
    private bool horizontalReady = true;
    [Header("Navegación")]
    public Selectable firstSelectable;
    public float moveCooldown = 0.5f;
    public float axisThreshold = 0.8f;

    [Header("Opciones")]
    public bool loopNavigation = true;
    public bool preferEventSystemFirst = true;

    [Header("Cancel (opcional)")]
    public GameObject cancelTarget;

    private float lastMoveTime;
    private Selectable lastSelected;

    public static Vector3 lastMousePosition;
    public static bool usandoRaton = true;

    void OnEnable()
    {
        lastMousePosition = Input.mousePosition;
        lastSelected = null;

        // Siempre arrancamos en modo mando (cursor oculto) hasta que se mueva el ratón
        usandoRaton = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(ForzarSeleccionInicialAlActivar());
    }

    private System.Collections.IEnumerator ForzarSeleccionInicialAlActivar()
    {
        yield return null; // Esperamos 1 frame a que el UI se asiente
        EnsureInitialSelection();

        // Si después de EnsureInitialSelection el EventSystem sigue vacío, 
        // lo intentamos una vez más con el firstSelectable
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (firstSelectable != null) Select(firstSelectable);
        }
    }

   void Update()
    {
        if (EventSystem.current == null) return;

        // =========================================================
        // 🖱️ LÓGICA DEL RATÓN (A prueba de cursores custom)
        // =========================================================
        // Leemos el láser del ratón directamente para saber si lo has movido físicamente
        float movX = Input.GetAxis("Mouse X");
        float movY = Input.GetAxis("Mouse Y");
        bool ratonMovido = Mathf.Abs(movX) > 0.1f || Mathf.Abs(movY) > 0.1f;
        bool clicRaton = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);

        if (ratonMovido || clicRaton)
        {
            if (!usandoRaton)
            {
                usandoRaton = true;
                
                // 🖱️ DESBLOQUEA Y MUESTRA EL CURSOR
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true; 

                // Apaga selección de nodos si venías de mando
                if (lastSelected != null)
                {
                    var botonScript = lastSelected.GetComponent<BotonInteractivo>();
                    if (botonScript != null) botonScript.OnDeselect(null);

                    var skillScript = lastSelected.GetComponent<SkillNode>();
                    if (skillScript != null) skillScript.OnDeselect(null);
                }

                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }

        // =========================================================
        // 🎮 LÓGICA DEL MANDO
        // =========================================================
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");

        bool tocandoMando = Mathf.Abs(v) >= axisThreshold || Mathf.Abs(h) >= axisThreshold ||
                            Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1);

#if ENABLE_INPUT_SYSTEM
        try {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.left.wasPressedThisFrame) { h = -1f; tocandoMando = true; }
                if (gamepad.dpad.right.wasPressedThisFrame) { h = 1f; tocandoMando = true; }
                if (gamepad.dpad.up.wasPressedThisFrame) { v = 1f; tocandoMando = true; }
                if (gamepad.dpad.down.wasPressedThisFrame) { v = -1f; tocandoMando = true; }
                if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame) { tocandoMando = true; }
            }
        } catch { }
#endif

        if (tocandoMando && usandoRaton)
        {
            usandoRaton = false;
            
            // 🎮 CONGELA Y OCULTA EL CURSOR CUSTOM
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false; 

            GameObject botonGuardado = lastSelected != null ? lastSelected.gameObject : null;
            EventSystem.current.SetSelectedGameObject(null);

            if (botonGuardado != null && botonGuardado.activeInHierarchy)
            {
                Select(botonGuardado.GetComponent<Selectable>());
            }
            else
            {
                EnsureInitialSelection();
            }
        }

        // =========================================================
        // ⚙️ NAVEGACIÓN Y CANCELACIÓN
        // =========================================================
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (!usandoRaton) HandleCancel();
            return;
        }

        if (!usandoRaton)
        {
            HandleNavigation(v, h); 
        }

        HandleCancel();
    }

    private void EnsureInitialSelection()
    {
        if (EventSystem.current == null) return;

        // 🌟 LA MAGIA DEL ÁRBOL DE HABILIDADES 🌟
        // Buscamos si estamos en un menú que tenga nodos de habilidad
        SkillNode[] nodosArbol = FindObjectsOfType<SkillNode>();

        if (nodosArbol.Length > 0)
        {
            SkillNode mejorNodo = null;
            float distanciaMinima = float.MaxValue;

            // Usamos el centro de la pantalla como punto de mira
            Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);

            // PASO 1: Buscar el nodo más cercano QUE PODAMOS PAGAR
            foreach (SkillNode nodo in nodosArbol)
            {
                // Solo nos interesan nodos visibles y que se puedan pulsar (desbloqueados o siguientes en la rama)
                if (nodo.button == null || !nodo.gameObject.activeInHierarchy || !nodo.button.interactable) continue;

                // Chequeamos si el jugador tiene monedas suficientes para este nodo
                bool puedePagar = false;
                if (LevelManager.instance != null)
                {
                    puedePagar = LevelManager.instance.ContagionCoins >= nodo.CoinCost;
                }

                if (puedePagar)
                {
                    // Calculamos cuál está más cerca del centro de la pantalla ahora mismo
                    Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(null, nodo.transform.position);
                    float distancia = Vector2.Distance(centroPantalla, posicionPantalla);

                    if (distancia < distanciaMinima)
                    {
                        distanciaMinima = distancia;
                        mejorNodo = nodo;
                    }
                }
            }

            // Si encontró uno perfecto (comprable), lo selecciona, centra la cámara y corta aquí
            if (mejorNodo != null)
            {
                Select(mejorNodo.button);
                return;
            }

            // PASO 2: Si somos pobres y no podemos pagar nada, pillamos el nodo interactuable más cercano al centro
            distanciaMinima = float.MaxValue;
            foreach (SkillNode nodo in nodosArbol)
            {
                if (nodo.button == null || !nodo.gameObject.activeInHierarchy || !nodo.button.interactable) continue;

                Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(null, nodo.transform.position);
                float distancia = Vector2.Distance(centroPantalla, posicionPantalla);

                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    mejorNodo = nodo;
                }
            }

            if (mejorNodo != null)
            {
                Select(mejorNodo.button);
                return;
            }
        }

        // --- LÓGICA ORIGINAL PARA OTROS MENÚS (Ajustes, Pantalla de Título, etc.) ---
        if (firstSelectable != null && firstSelectable.gameObject.activeInHierarchy && firstSelectable.interactable)
        {
            Select(firstSelectable);
        }
        else if (preferEventSystemFirst)
        {
            var s = GetComponentInChildren<Selectable>();
            if (s != null) Select(s);
        }
    }

    private void HandleNavigation(float vertical, float horizontal)
    {
        // Respetamos el tiempo de enfriamiento (cooldown)
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;

        Selectable current = GetCurrentSelectable();

        // --- NAVEGACIÓN VERTICAL POR FLANCO (debounce estricto) ---
        if (verticalReady && Mathf.Abs(vertical) >= axisThreshold && current != null)
        {
            if (vertical > 0)
            {
                Selectable next = current.FindSelectableOnUp();
                if (next == null && loopNavigation) next = FindEdgeSelectable(true);
                if (next != null) Select(next);
            }
            else
            {
                Selectable next = current.FindSelectableOnDown();
                if (next == null && loopNavigation) next = FindEdgeSelectable(false);
                if (next != null) Select(next);
            }
            lastMoveTime = Time.unscaledTime;
            verticalReady = false;
        }
        if (Mathf.Abs(vertical) < axisThreshold * 0.5f) verticalReady = true;

        // --- NAVEGACIÓN HORIZONTAL POR FLANCO (debounce estricto) ---
        if (horizontalReady && Mathf.Abs(horizontal) >= axisThreshold)
        {
            if (current is Slider slider)
            {
                slider.value += (horizontal > 0 ? 1 : -1) * slider.maxValue * 0.02f;
                slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
                slider.onValueChanged?.Invoke(slider.value);
            }
            else if (current is TMP_Dropdown dropdown)
            {
                int next = dropdown.value + (horizontal > 0 ? 1 : -1);
                if (next < 0) next = dropdown.options.Count - 1;
                if (next >= dropdown.options.Count) next = 0;
                dropdown.value = next;
                dropdown.RefreshShownValue();
                dropdown.onValueChanged?.Invoke(next);
            }
            else if (current != null)
            {
                var selectorHorizontal = current.GetComponent<SelectorHorizontalUI>();
                if (selectorHorizontal != null)
                {
                    if (horizontal > 0) selectorHorizontal.Siguiente();
                    else selectorHorizontal.Anterior();
                }
            }
            lastMoveTime = Time.unscaledTime;
            horizontalReady = false;
        }
        if (Mathf.Abs(horizontal) < axisThreshold * 0.5f) horizontalReady = true;
    }
    private void HandleCancel()
    {
        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (cancelTarget != null)
            {
                Selectable botonASeleccionar = cancelTarget.GetComponent<Selectable>();
                if (botonASeleccionar != null)
                {
                    Select(botonASeleccionar);
                }
            }
        }
    }

    private Selectable GetCurrentSelectable()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return null;
        return EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
    }

    private void Select(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null) return;

        if (lastSelected != null && lastSelected.gameObject != selectable.gameObject)
        {
            var oldEffect = lastSelected.GetComponent<BotonInteractivo>();
            if (oldEffect != null) oldEffect.OnDeselect(null);

            var oldSkill = lastSelected.GetComponent<SkillNode>();
            if (oldSkill != null) oldSkill.OnDeselect(null);

            var oldSelectable = lastSelected.GetComponent<Selectable>();
            if (oldSelectable != null)
            {
                oldSelectable.OnDeselect(new BaseEventData(EventSystem.current));
            }
        }

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        var newEffect = selectable.GetComponent<BotonInteractivo>();
        if (newEffect != null) newEffect.OnSelect(null);

        var newSkill = selectable.GetComponent<SkillNode>();
        if (newSkill != null) newSkill.OnSelect(null);

        if (selectable.transition == Selectable.Transition.SpriteSwap || selectable.transition == Selectable.Transition.ColorTint)
        {
            selectable.OnSelect(new BaseEventData(EventSystem.current));
        }

        lastSelected = selectable;
    }

    private Selectable FindEdgeSelectable(bool findTop)
    {
        Selectable best = null;
        foreach (var s in GetComponentsInChildren<Selectable>(true))
        {
            if (!s.interactable || !s.gameObject.activeInHierarchy) continue;
            if (best == null) best = s;
            else if (findTop && s.transform.position.y > best.transform.position.y) best = s;
            else if (!findTop && s.transform.position.y < best.transform.position.y) best = s;
        }
        return best;
    }
}