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

    public static Vector3 lastMousePosition;
    public static bool usandoRaton = true;

    void OnEnable()
    {
        lastMousePosition = Input.mousePosition;
        lastSelected = null;

        // 🛑 NUEVO: Si no estamos usando el ratón, forzamos la selección inmediata.
        // Usamos una pequeña espera (un frame) para que a Unity le dé tiempo a activar todo.
        if (!usandoRaton)
        {
            StartCoroutine(ForzarSeleccionInicialAlActivar());
        }
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

        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        bool ratonMovido = mouseDelta.sqrMagnitude > 2.0f;
        bool clicRaton = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);

        if (ratonMovido || clicRaton)
        {
            if (!usandoRaton)
            {
                usandoRaton = true;

                // 🛑 AHORA TAMBIÉN APAGA LOS NODOS DEL ÁRBOL 🛑
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
            lastMousePosition = Input.mousePosition;
        }

        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        bool tocandoMando = Mathf.Abs(v) >= axisThreshold || Mathf.Abs(h) >= axisThreshold ||
                            Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton1);

        if (tocandoMando && usandoRaton)
        {
            usandoRaton = false;

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