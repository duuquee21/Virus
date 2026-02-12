using System.Collections;
using UnityEngine;

public class AnimacionFinalNivel : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefab1;
    public GameObject prefab2;
    public GameObject prefab3;

    [Header("Escalas")]
    public float escalaP1 = 3f;
    public float escalaP2y3 = 6f;

    [Header("Tiempos Fase 1 (Círculo Bicolor)")]
    public float t1_CrecerBlanco = 0.5f;
    public float t1_EncogerNegro = 0.5f;

    [Header("Tiempos Fase 2 (Segundo Círculo)")]
    public float t2_Crecer = 1.2f;

    [Header("Tiempos Fase 3 (Tercer Círculo)")]
    public float t3_Encoger = 0.8f;

    [ContextMenu("Ejecutar Secuencia")]
    public void Ejecutar()
    {
        StopAllCoroutines();
        StartCoroutine(RutinaDetallada());
    }

    private IEnumerator RutinaDetallada()
    {
        // --- BUSCAR POSICIÓN DEL VIRUS ---
        Vector3 posicionInstancia = transform.position; // Backup por si no encuentra al Virus
        GameObject jugadorVirus = GameObject.FindGameObjectWithTag("Virus");

        if (jugadorVirus != null)
        {
            posicionInstancia = jugadorVirus.transform.position;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto con el tag 'Virus'. Usando posición del objeto actual.");
        }

        Vector3 vEscalaP1 = Vector3.one * escalaP1;
        Vector3 vEscalaP2y3 = Vector3.one * escalaP2y3;

        // --- FASE 1: Prefab 1 (Crece Blanco, Encoge Negro) ---
        // Usamos posicionInstancia en lugar de transform.position
        GameObject c1 = Instantiate(prefab1, posicionInstancia, Quaternion.identity);
        SpriteRenderer rend1 = c1.GetComponent<SpriteRenderer>();

        rend1.color = Color.white;
        yield return StartCoroutine(LerpEscala(c1.transform, Vector3.zero, vEscalaP1, t1_CrecerBlanco));

        rend1.color = Color.black;
        yield return StartCoroutine(LerpEscala(c1.transform, vEscalaP1, Vector3.zero, t1_EncogerNegro));
        Destroy(c1);

        // --- FASE 2: Prefab 2 (Solo Crece) ---
        GameObject c2 = Instantiate(prefab2, posicionInstancia, Quaternion.identity);
        yield return StartCoroutine(LerpEscala(c2.transform, Vector3.zero, vEscalaP2y3, t2_Crecer));

        // --- FASE 3: Prefab 3 (Solo Encoge) ---
        GameObject c3 = Instantiate(prefab3, posicionInstancia, Quaternion.identity);
        c3.transform.localScale = vEscalaP2y3;

        Destroy(c2);
        yield return StartCoroutine(LerpEscala(c3.transform, vEscalaP2y3, Vector3.zero, t3_Encoger));
        Destroy(c3);
    }

    private IEnumerator LerpEscala(Transform target, Vector3 inicio, Vector3 fin, float tiempo)
    {
        if (tiempo <= 0)
        {
            target.localScale = fin;
            yield break;
        }

        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / tiempo;
            float suave = Mathf.SmoothStep(0, 1, t);
            target.localScale = Vector3.Lerp(inicio, fin, suave);
            yield return null;
        }
        target.localScale = fin;
    }
}