using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingCellMovement : MonoBehaviour
{
    public float velocidadBase = 5f;
    public float fuerzaEmpuje = 10f; // Ajusta esto para que el Virus lo sienta
    private Vector2 direccion;
    private Rigidbody2D rb;
    private Material mat;
    private Coroutine jellyAnim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Importante: Usamos .material para crear una instancia única por objeto
        mat = GetComponent<SpriteRenderer>().material;
        direccion = new Vector2(1, 1).normalized;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + direccion * velocidadBase * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Vector2 puntoImpacto = otro.ClosestPoint(transform.position);

        // Obtenemos el punto más cercano del choque
        Vector2 puntoGlobal = otro.ClosestPoint(transform.position);

        // Lo transformamos a coordenadas locales del círculo
        Vector3 puntoLocal = transform.InverseTransformPoint(puntoGlobal);

        // Lanzamos la animación
        if (jellyAnim != null) StopCoroutine(jellyAnim);
        jellyAnim = StartCoroutine(DoJelly(puntoLocal));

        if (otro.CompareTag("Pared"))
        {
           
            Vector2 normal = ((Vector2)transform.position - puntoImpacto).normalized;
            direccion = Vector2.Reflect(direccion, normal).normalized;
        }
        // 2. Si choca con cualquier otra cosa -> EMPUJA
        else
        {
            Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();
            if (rbOtro != null)
            {
                // Calculamos dirección desde el centro de la bola hacia el objeto
                Vector2 direccionEmpuje = (otro.transform.position - transform.position).normalized;

                // Aplicamos impulso instantáneo
                rbOtro.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
            }
        }

    }

  


    // Añade esto dentro de FloatingCellMovement.cs
    public void CambiarDireccion(Vector2 nuevaDireccion)
    {
        direccion = nuevaDireccion.normalized;
    }

    public Vector2 GetDireccion()
    {
        return direccion;
    }

    IEnumerator DoJelly(Vector3 localPos)
    {
        mat.SetVector("_ImpactPos", localPos);
        float t = 0;

        // 1. Aumentamos la duración (antes era 0.5s, ahora 2.0s para que dure el rebote)
        float duracion = 2.0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime / duracion;

            // 2. Oscilación con decaimiento:
            // - Sin(t * PI * 10): El '10' determina cuántas veces rebota (frecuencia).
            // - (1.0f - t): Hace que la fuerza baje linealmente hasta cero.
            // - El 0.5f final es la fuerza inicial del impacto.
            float decaimiento = Mathf.Exp(-t * 3.0f);
            float deformacion = Mathf.Sin(t * Mathf.PI * 12.0f) * decaimiento * 0.6f;

            mat.SetFloat("_Deform", deformacion);
            yield return null;
        }

        mat.SetFloat("_Deform", 0);
    }
}