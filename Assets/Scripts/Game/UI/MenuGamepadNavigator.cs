using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Navegación con mando para menús UI basados en Selectable (Button, Toggle, Slider, TMP_Dropdown).
/// - Vertical: navegación entre elementos
/// - Submit/A: activa el elemento seleccionado (AHORA LO GESTIONA UNITY POR DEFECTO)
/// - Cancel/B: cierra panel si se configura
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
    public GameObject cancelTarget; // panel o botón que se activa con B

    private float lastMoveTime;
    private float lastSubmitTime;
    private Selectable lastSelected;

    void OnEnable()
    {
        lastSelected = null;
        EnsureInitialSelection();
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EnsureInitialSelection();
            return;
        }

        HandleNavigation();

        // 🛑 APAGAMOS ESTO PARA EVITAR EL DOBLE CLIC 🛑
        // HandleSubmit(); 

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
            // Primer Selectable en el objeto y sus hijos
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
                if (next == null && loopNavigation)
                    next = FindEdgeSelectable(true);
                if (next != null)
                    Select(next);
            }
            else
            {
                Selectable next = current.FindSelectableOnDown();
                if (next == null && loopNavigation)
                    next = FindEdgeSelectable(false);
                if (next != null)
                    Select(next);
            }
            lastMoveTime = Time.unscaledTime;
            return;
        }

        if (Mathf.Abs(horizontal) >= axisThreshold)
        {
            // Ajustar sliders / dropdowns con stick
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

    private void HandleSubmit()
    {
        if (Time.unscaledTime - lastSubmitTime < moveCooldown) return; // Usar moveCooldown como cooldown para submit

        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            Selectable current = GetCurrentSelectable();
            if (current == null) return;

            if (current is Button btn)
            {
                btn.onClick?.Invoke();
            }
            else if (current is Toggle tog)
            {
                tog.isOn = !tog.isOn;
                tog.onValueChanged?.Invoke(tog.isOn);
            }
            else if (current is Slider)
            {
                // No action para slider, se controla con left/right
            }
            else if (current is TMP_Dropdown drop)
            {
                drop.Show();
            }
            else
            {
                ExecuteEvents.Execute(current.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }

            lastSubmitTime = Time.unscaledTime;
        }
    }

    private void HandleCancel()
    {
        // JoystickButton1 es el botón B (Círculo en PlayStation)
        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (cancelTarget != null)
            {
                // Buscamos si es un botón para seleccionarlo
                Selectable botonASeleccionar = cancelTarget.GetComponent<Selectable>();
                if (botonASeleccionar != null)
                {
                    Select(botonASeleccionar);

                    // Opcional: Si quieres que además de seleccionarse se PULSE automáticamente
                    // quita las dos barras (//) de la línea de abajo:
                    // cancelTarget.GetComponent<Button>()?.onClick.Invoke();
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

        // Desactivar el efecto del objeto anterior (si lo tiene)
        if (lastSelected != null && lastSelected.gameObject != selectable.gameObject)
        {
            var oldEffect = lastSelected.gameObject.GetComponent<ObjetoInteractivoCompleto>();
            if (oldEffect != null) oldEffect.DesactivarEfecto();

            var oldSelectable = lastSelected.GetComponent<Selectable>();
            if (oldSelectable != null)
            {
                oldSelectable.OnDeselect(new BaseEventData(EventSystem.current));
            }

            // Forzar pointer exit en EventTrigger de selección anterior
            SendPointerExit(lastSelected.gameObject);
        }

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        // Activa el efecto de la nueva selección si existe el componente
        var newEffect = selectable.gameObject.GetComponent<ObjetoInteractivoCompleto>();
        if (newEffect != null) newEffect.ActivarEfecto();

        // Forzar eventos de mouse equivalentes (pointer enter)
        SendPointerEnter(selectable.gameObject);

        // Forzar resaltado visual si es necesario
        if (selectable.transition == Selectable.Transition.SpriteSwap)
        {
            selectable.OnSelect(new BaseEventData(EventSystem.current));
        }
        else if (selectable.transition == Selectable.Transition.ColorTint)
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