using UnityEngine;

public class ImpactReceiver : MonoBehaviour
{
    [Header("Ajustes Individuales")]
    [Tooltip("Permite que unos objetos se sacudan más que otros (1 = normal)")]
    public float multiplicadorMasa = 1f;

    private float fuerzaActual = 0f;
    private float velocidadRetorno = 20f;
    private Vector3 offsetActual;

    void OnEnable()
    {
        LevelTransitioner.OnImpactShake += StartShake;
    }

    void OnDisable()
    {
        LevelTransitioner.OnImpactShake -= StartShake;
        // Limpiamos el offset al desactivar para no dejar el objeto movido
        ResetPosition();
    }

    private void StartShake(float intensidad)
    {
        // Aplicamos la intensidad del evento multiplicada por la masa del objeto
        fuerzaActual = intensidad * multiplicadorMasa;
    }

    void LateUpdate() // Usamos LateUpdate para que ocurra DESPUÉS del movimiento normal
    {
        if (fuerzaActual > 0.01f)
        {
            // 1. Calculamos el nuevo desplazamiento aleatorio
            float x = UnityEngine.Random.Range(-1f, 1f) * fuerzaActual;
            float y = UnityEngine.Random.Range(-1f, 1f) * fuerzaActual;

            // 2. Restamos el offset anterior y sumamos el nuevo
            // Esto permite que el objeto siga su ruta original pero con la vibración encima
            transform.localPosition -= offsetActual;
            offsetActual = new Vector3(x, y, 0);
            transform.localPosition += offsetActual;

            // 3. Reducimos la fuerza del impacto
            fuerzaActual = Mathf.Lerp(fuerzaActual, 0, Time.deltaTime * velocidadRetorno);
        }
        else if (offsetActual != Vector3.zero)
        {
            ResetPosition();
        }
    }

    private void ResetPosition()
    {
        transform.localPosition -= offsetActual;
        offsetActual = Vector3.zero;
        fuerzaActual = 0;
    }
}