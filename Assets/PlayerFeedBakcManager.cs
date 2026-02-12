using UnityEngine;

public class PlayerFeedBakcManager : MonoBehaviour
{
    private float proximoFeedbackPermitido;

    public void OnFragmentReached(Vector3 position)
    {
        if (InfectionFeedback.instance != null)
        {
            // Sonido y partículas
            InfectionFeedback.instance.PlayBasicImpactEffect(position, Color.white, true);

            // Prueba con un incremento más pequeño (0.02f o 0.03f)
            // para que no llegue a escala 1 tan pronto.
            InfectionFeedback.instance.PlayHitFeedback(this.gameObject, 0.03f);
        }
    }
}