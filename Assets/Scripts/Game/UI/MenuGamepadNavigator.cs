using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuGamepadNavigator : MonoBehaviour
{
    [Header("Navegación")]
    public Selectable firstSelectable;
    public float moveCooldown = 0.18f;
    public float axisThreshold = 0.5f;

    [Header("Opciones")]
    public bool loopNavigation = true;
    public bool preferEventSystemFirst = true;

    [Header("Cancel (opcional)")]
    public GameObject cancelTarget;

    private float lastMoveTime;
    private Selectable lastSelected;

    // Memoria global
    public static Vector3 lastMousePosition;
    public static bool usandoRaton = true;

    void OnEnable()
    {
        lastMousePosition = Input.mousePosition;

        // Solo si sabemos con certeza que usa mando, seleccionamos algo al abrir
        if (!usandoRaton)
        {
            EnsureInitialSelection();
        }
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // 1. 🖱️ DETECTAR RATÓN (Con pequeño margen para evitar vibraciones de la mesa)
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        bool ratonMovido = mouseDelta.sqrMagnitude > 2.0f;
        bool clicRaton = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);

        if (ratonMovido || clicRaton)
        {
            if (!usandoRaton)
            {
                usandoRaton = true; // Volvemos a modo ratón

                // 🛑 APAGADO FORZOSO DEL BOTÓN
                if (lastSelected != null)
                {
                    // ¡AQUÍ ESTABA EL ERROR! Ahora sí busca tu BotonInteractivo
                    var botonScript = lastSelected.GetComponent<BotonInteractivo>();
                    if (botonScript != null) botonScript.OnDeselect(null);
                }

                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
            lastMousePosition = Input.mousePosition;
        }

        // 2. 🎮 DETECTAR MANDO
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        bool tocandoMando = Mathf.Abs(v) >= axisThreshold || Mathf.Abs(h) >= axisThreshold ||
                            Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel") ||
                            Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1);

        if (tocandoMando && usandoRaton)
        {
            usandoRaton = false; // Pasamos a modo mando

            // 💡 ENCENDIDO FORZOSO DEL BOTÓN
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

        // 3. LÓGICA INTELIGENTE DE SELECCIÓN
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (!usandoRaton) HandleCancel();
            return;
        }

        if (!usandoRaton)
        {
            HandleNavigation();
        }

        HandleCancel();
    }

    private void EnsureInitialSelection()
    {
        if (EventSystem.current == null) return;

        // ¡SEGUNDO ERROR ARREGLADO! Ahora usamos nuestro propio Select para que se guarde en lastSelected
        if (firstSelectable != null)
        {
            Select(firstSelectable);
        }
        else if (preferEventSystemFirst)
        {
            var s = GetComponentInChildren<Selectable>();
            if (s != null) Select(s);
        }
    }

    private void HandleNavigation()
    {
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        Selectable current = GetCurrentSelectable();

        // ⬆️⬇️ NAVEGACIÓN VERTICAL (Arriba y Abajo)
        if (Mathf.Abs(vertical) >= axisThreshold && current != null)
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
            return;
        }

        // ⬅️➡️ NAVEGACIÓN HORIZONTAL (Izquierda y Derecha)
        if (Mathf.Abs(horizontal) >= axisThreshold)
        {
            if (current is Slider slider)
            {
                slider.value += (horizontal > 0 ? 1 : -1) * slider.maxValue * 0.02f;
                slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
                slider.onValueChanged?.Invoke(slider.value);
            }
            else if (current is TMP_Dropdown dropdown) // (Por si te queda algún dropdown viejo)
            {
                int next = dropdown.value + (horizontal > 0 ? 1 : -1);
                if (next < 0) next = dropdown.options.Count - 1;
                if (next >= dropdown.options.Count) next = 0;
                dropdown.value = next;
                dropdown.RefreshShownValue();
                dropdown.onValueChanged?.Invoke(next);
            }
            // 🌟 AQUI ESTÁ LA MAGIA PARA TU NUEVA RULETA 🌟
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
        }
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

        // Si venimos de otro botón, lo apagamos
        if (lastSelected != null && lastSelected.gameObject != selectable.gameObject)
        {
            var oldEffect = lastSelected.GetComponent<BotonInteractivo>();
            if (oldEffect != null) oldEffect.OnDeselect(null);

            var oldSelectable = lastSelected.GetComponent<Selectable>();
            if (oldSelectable != null)
            {
                oldSelectable.OnDeselect(new BaseEventData(EventSystem.current));
            }
        }

        // Seleccionamos el nuevo
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        // Encendemos el nuevo
        var newEffect = selectable.GetComponent<BotonInteractivo>();
        if (newEffect != null) newEffect.OnSelect(null);

        if (selectable.transition == Selectable.Transition.SpriteSwap || selectable.transition == Selectable.Transition.ColorTint)
        {
            selectable.OnSelect(new BaseEventData(EventSystem.current));
        }

        // GUARDAMOS EN MEMORIA
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