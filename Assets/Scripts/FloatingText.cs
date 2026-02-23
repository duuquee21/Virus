using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float destroyTime = 1f;
    public float speed = 2f;

    private TextMeshPro textMesh;
    private Color textColor;

    void Start()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;

        Destroy(gameObject, destroyTime);

        // Pequeña variación horizontal para evitar solapamiento perfecto
        transform.position += new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);
    }

    void Update()
    {
        // 1. Movimiento hacia arriba
        transform.position += Vector3.up * speed * Time.deltaTime;

        // 2. Reducción del Alfa
        // Restamos el alfa proporcionalmente al tiempo de vida total
        textColor.a -= (1f / destroyTime) * Time.deltaTime;
        textMesh.color = textColor;
    }

    public void SetText(string text)
    {
        // Aseguramos que tenemos la referencia si se llama antes del Start
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        textMesh.text = text;
    }
}