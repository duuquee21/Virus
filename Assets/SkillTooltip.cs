using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip instance;

    [Header("References")]
    public RectTransform rect;
    public Vector2 offset = new Vector2(0, 120f);

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Swing Animation (Juice)")]
    public float initialAmplitude = 15f;
    public float swingFrequency = 10f;
    public float dampingSpeed = 4f;
    private float currentAmplitude = 0f;
    private float swingTimer = 0f;

    [Header("Scale Animation (Pop/Shrink)")]
    public float scaleSpeed = 15f;
    private float currentScale = 0f;
    private float targetScale = 0f;

    private string nombreTabla = "TextosJuego";
    private CanvasGroup canvasGroup;
    void Awake()
    {
        instance = this;
        ForceHide();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave;
        return op;
    }

    public void Show(string translatedTitle, string translatedDescription, int cost, RectTransform target)
    {
        bool veniaDeFuera = !gameObject.activeSelf;

        if (veniaDeFuera)
        {
            // 2. MAGIA: Lo hacemos invisible AHORA MISMO, antes de que Unity intente dibujarlo mal
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            currentScale = 0.05f;
            if (rect != null) rect.localScale = new Vector3(0.05f, 0.05f, 1f);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (rect != null) rect.localScale = Vector3.one;

        titleText.text = translatedTitle;
        descriptionText.text = translatedDescription;
        string textoCoste = GetTexto("txt_coste");
        costText.text = $"{textoCoste}: {cost} ";

        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        if (rect != null && target != null)
        {
            rect.position = target.position;
            rect.anchoredPosition += offset;
        }

        if (veniaDeFuera)
        {
            if (rect != null) rect.localScale = new Vector3(0.05f, 0.05f, 1f);
        }

        targetScale = 1f;
        swingTimer = 0f;
        currentAmplitude = initialAmplitude;
    }

    void Update()
    {
        if (rect == null) return;

        // 3. REVELACIÓN: En cuanto empieza a crecer y Unity ya pasó ese primer frame, le quitamos la invisibilidad.
        if (canvasGroup != null && canvasGroup.alpha == 0f && currentScale > 0.4f)
        {
            canvasGroup.alpha = 1f;
        }

        currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        rect.localScale = new Vector3(currentScale, currentScale, 1f);

        if (currentAmplitude > 0.01f)
        {
            swingTimer += Time.unscaledDeltaTime;
            currentAmplitude = Mathf.Lerp(currentAmplitude, 0f, Time.unscaledDeltaTime * dampingSpeed);
            float angle = Mathf.Sin(swingTimer * swingFrequency) * currentAmplitude;
            rect.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            rect.rotation = Quaternion.identity;
        }

        if (targetScale == 0f && currentScale < 0.4f)
        {
            currentScale = 0.4f; // Mantenemos la escala en el mismo punto de corte
            rect.localScale = new Vector3(0.4f, 0.4f, 1f);
            gameObject.SetActive(false);
        }
    }

    public void Hide(bool forceRigid = false)
    {
        targetScale = 0f;

        // Si forzamos la rigidez (porque compramos) O si el balanceo ya era pequeño
        if (forceRigid || currentAmplitude < 3f)
        {
            currentAmplitude = 0f;

            // Forzamos la rotación a 0 en este mismo frame para evitar que gire ni un milímetro más
            if (rect != null) rect.rotation = Quaternion.identity;
        }
    }

    public void ForceHide()
    {
        targetScale = 0f;
        currentScale = 0.05f; // Nunca 0
        currentAmplitude = 0f;

        if (rect != null)
        {
            rect.localScale = new Vector3(0.05f, 0.05f, 1f); // Nunca Vector3.zero
            rect.rotation = Quaternion.identity;
        }
        gameObject.SetActive(false);
    }
}