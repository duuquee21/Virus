using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

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
    public float initialAmplitude = 15f; // Fuerza del balanceo inicial (más "gordo")
    public float swingFrequency = 10f;  // Velocidad de la oscilación
    public float dampingSpeed = 4f;     // Qué tan rápido se detiene (más alto = se para antes)
    private float currentAmplitude = 0f;
    private float timer = 0f;
    private bool isAnimating = false;




    // 1. ¡Actualizado al nombre de tu nueva tabla!
    private string nombreTabla = "TextosJuego";

    void Awake()
    {
        instance = this;
       
        Hide();
    }

    // Función auxiliar para traducir solo el Coste y el ADN
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTabla, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si falla, devuelve la clave para que veas el error
        return op;
    }

    // 2. Modificado para recibir los textos ya traducidos desde SkillNode
    public void Show(string translatedTitle, string translatedDescription, int cost, RectTransform target)
    {
        gameObject.SetActive(true); // Activar antes de configurar
    transform.SetAsLastSibling();
    
   

        titleText.text = translatedTitle;
        descriptionText.text = translatedDescription;

        string textoCoste = GetTexto("txt_coste");
        costText.text = $"{textoCoste}: {cost} ";

        if (rect != null && target != null)
        {
            rect.position = target.position;
            rect.anchoredPosition += offset;
        }

        gameObject.SetActive(true);
        StartSwingAnimation();
    }

    private void StartSwingAnimation()
    {
        timer = 0f;
        currentAmplitude = initialAmplitude;
        isAnimating = true;
        if (rect != null) rect.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (!isAnimating || rect == null) return;

        timer += Time.unscaledDeltaTime;

        // 1. Reducimos la amplitud con el tiempo (Damping)
        currentAmplitude = Mathf.Lerp(currentAmplitude, 0f, Time.unscaledDeltaTime * dampingSpeed);

        // 2. Calculamos el ángulo usando Seno
        float angle = Mathf.Sin(timer * swingFrequency) * currentAmplitude;
        rect.rotation = Quaternion.Euler(0f, 0f, angle);

        // 3. Si el movimiento es ya imperceptible, lo paramos del todo
        if (currentAmplitude < 0.1f)
        {
            rect.rotation = Quaternion.identity;
            isAnimating = false;
        }
    }

    public void Hide()
    {
        isAnimating = false;
        if (rect != null) rect.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

 
}