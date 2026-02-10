using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingScoreUI : MonoBehaviour
{
    public TextMeshProUGUI textoTMP;
    public CanvasGroup canvasGroup;
    private RectTransform miRect;

    [Header("Configuración Visual")]
    public float duracionViaje = 0.8f;
    public float dispersion = 30f; 
    
    [Header("Curvas de Animación (Juice)")]
    
    public AnimationCurve curvaAltura; 
    public float alturaArco = 100f; 
    
   
    public AnimationCurve curvaEscala; 

    public void IniciarViaje(int puntos, Vector3 posicionMundoPersona, RectTransform destinoFinal, Canvas canvasPadre)
    {
        textoTMP = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        miRect = GetComponent<RectTransform>();
        
        textoTMP.text = "+" + puntos;
        
        // aleatorio la aparicion
        if (Camera.main != null)
        {
            Vector2 posicionPantalla = Camera.main.WorldToScreenPoint(posicionMundoPersona);
            Vector2 posicionLocalCanvas;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasPadre.transform as RectTransform, 
                posicionPantalla, 
                canvasPadre.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, 
                out posicionLocalCanvas
            );

            // Añadimos un poco de ruido aleatorio para que se vea orgánico
            float ruidoX = Random.Range(-dispersion, dispersion);
            float ruidoY = Random.Range(-dispersion, dispersion);
            
            miRect.anchoredPosition = posicionLocalCanvas + new Vector2(ruidoX, ruidoY);
        }

        StartCoroutine(VolarConEstilo(destinoFinal));
    }

    IEnumerator VolarConEstilo(RectTransform destino)
    {
        float tiempoPasado = 0f;
        Vector2 posInicial = miRect.anchoredPosition;
        Vector2 posFinal = Vector2.zero;

        // Calculamos destino final una vez
        if (destino != null)
        {
             Vector2 screenPointDestino = RectTransformUtility.WorldToScreenPoint(null, destino.position);
             RectTransformUtility.ScreenPointToLocalPointInRectangle(
                 transform.parent as RectTransform,
                 screenPointDestino,
                 null,
                 out posFinal
             );
        }

        while (tiempoPasado < duracionViaje)
        {
            tiempoPasado += Time.deltaTime;
            float t = tiempoPasado / duracionViaje; // Va de 0 a 1

            // lerp y curva
            Vector2 posicionBase = Vector2.Lerp(posInicial, posFinal, t);
            
            // Le sumamos una altura extra basada en la curva (El arco)
            // Si la curva es una montaña, subirá y bajará durante el trayecto
            float alturaExtra = curvaAltura.Evaluate(t) * alturaArco;
            
            miRect.anchoredPosition = new Vector2(posicionBase.x, posicionBase.y + alturaExtra);

            // escala
            float escala = curvaEscala.Evaluate(t);
            transform.localScale = Vector3.one * escala;

            // transparencia
            if (t > 0.8f) 
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}