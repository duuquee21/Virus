using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Navegación inteligente que alterna entre Ratón y Mando automáticamente.
/// </summary>
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
    private float lastSubmitTime;
    private Selectable lastSelected;

    // 🖱️ NUEVAS VARIABLES: Detectores de dispositivo
    private Vector3 lastMousePosition;
    public static bool usandoRaton = false;

    void OnEnable()
    {
        lastSelected = null;
        lastMousePosition = Input.mousePosition;
        EnsureInitialSelection();
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // 1. 🖱️ DETECTAR RATÓN: Si se mueve o hace clic
        if (Input.mousePosition != lastMousePosition || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            usandoRaton = true;
            lastMousePosition = Input.mousePosition;

            // Vaciamos la selección visual para dejar al jugador libre
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // 2. 🎮 DETECTAR MANDO: Si toca los joysticks o los botones
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        bool tocandoMando = Mathf.Abs(v) >= axisThreshold || Mathf.Abs(h) >= axisThreshold ||
                            Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel") ||
                            Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1);

        if (tocandoMando)
        {
            usandoRaton = false;
        }

        // 3. 🧠 LÓGICA INTELIGENTE DE SELECCIÓN
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            // Si agarramos el mando y no hay nada seleccionado, recuperamos el foco
            if (!usandoRaton && tocandoMando)
            {
                // Volvemos al último botón que tocamos, o al primero por defecto
                if (lastSelected != null && lastSelected.gameObject.activeInHierarchy)
                    Select(lastSelected);
                else
                    EnsureInitialSelection();
            }

            // Aunque el ratón nos haya quitado la selección, el mando debe poder pulsar "Atrás" (B)
            if (!usandoRaton) HandleCancel();

            return;
        }

        // 4. Si el mando tiene el control, permitimos navegar normalmente
        if (!usandoRaton)
        {
            HandleNavigation();
        }

        HandleCancel();
    }

    private void EnsureInitialSelection()
    {
        if (EventSystem.current == null) return;
        if (firstSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
        else if (preferEventSystemFirst)
        {
            var s = GetComponentInChildren<Selectable>();
            if (s != null)
                EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }

    private void HandleNavigation()
    {
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        Selectable current = GetCurrentSelectable();

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

        if (Mathf.Abs(horizontal) >= axisThreshold)
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

        if (lastSelected != null && lastSelected.gameObject != selectable.gameObject)
        {
            // Quitamos el efecto del botón anterior si lo tuviera
            var oldEffect = lastSelected.gameObject.GetComponent<ObjetoInteractivoCompleto>();
            if (oldEffect != null) oldEffect.DesactivarEfecto();

            var oldSelectable = lastSelected.GetComponent<Selectable>();
            if (oldSelectable != null)
            {
                oldSelectable.OnDeselect(new BaseEventData(EventSystem.current));
            }

            SendPointerExit(lastSelected.gameObject);
        }

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        // Activamos el efecto del nuevo botón
        var newEffect = selectable.gameObject.GetComponent<ObjetoInteractivoCompleto>();
        if (newEffect != null) newEffect.ActivarEfecto();

        SendPointerEnter(selectable.gameObject);

        if (selectable.transition == Selectable.Transition.SpriteSwap || selectable.transition == Selectable.Transition.ColorTint)
        {
            selectable.OnSelect(new BaseEventData(EventSystem.current));
        }
        else if (selectable.transition == Selectable.Transition.Animation)
        {
            var animator = selectable.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Selected");
                animator.SetBool("Highlighted", true);
            }
        }

        lastSelected = selectable;
    }

    private void SendPointerEnter(GameObject target)
    {
        if (EventSystem.current == null || target == null) return;
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) return;

        var eventData = new PointerEventData(EventSystem.current);
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
                entry.callback?.Invoke(eventData);
        }
    }

    private void SendPointerExit(GameObject target)
    {
        if (EventSystem.current == null || target == null) return;
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) return;

        var eventData = new PointerEventData(EventSystem.current);
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerExit)
                entry.callback?.Invoke(eventData);
        }
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