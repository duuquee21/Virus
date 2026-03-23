using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float lifetime = 1f;
    public float speed = 2f;

    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    // Se ejecuta cada vez que el pool activa el objeto
    void OnEnable()
    {
        timer = 0f;
        if (textMesh != null)
        {
            textColor = textMesh.color;
            textColor.a = 1f; // Resetear opacidad
            textMesh.color = textColor;
        }

        // Variación horizontal inicial
        transform.position += new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);
    }
    // Añade esto dentro de tu clase FloatingText
    private void OnDisable()
    {
        // Detenemos cualquier proceso pendiente
        CancelInvoke();
        StopAllCoroutines();

        // Opcional: Resetear el alfa a 0 para que no parpadee al reactivarse
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 0f;
            textMesh.color = c;
        }
    }
    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        timer += Time.deltaTime;

        // Fade out
        textColor.a = Mathf.Lerp(1f, 0f, timer / lifetime);
        textMesh.color = textColor;

        // En lugar de Destroy, lo desactivamos al terminar el tiempo
        if (timer >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetText(string text)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        textMesh.text = text;
    }
}