using UnityEngine;

public class StaticBumper : MonoBehaviour
{
    [Header("Configuración de Fuerzas")]
    public float fuerzaReboteBola = 1.0f; // Multiplicador para el rebote de la bola
    public float fuerzaEmpujeObjetos = 15f; // Fuerza de empuje para el Virus/Enemigos

    private void OnTriggerEnter2D(Collider2D otro)
    {
        // 1. Intentamos obtener el Rigidbody2D del objeto que entró
        Rigidbody2D rbOtro = otro.GetComponent<Rigidbody2D>();

        if (rbOtro != null)
        {
            // Calculamos la dirección desde el centro de este objeto hacia el otro
            Vector2 direccionHaciaAfuera = (otro.transform.position - transform.position).normalized;

            // 2. CASO ESPECIAL: LA BOLA
            // Verificamos si es la bola para que cambie su vector de dirección
            FloatingCellMovement movBola = otro.GetComponent<FloatingCellMovement>();

            if (movBola != null)
            {
                // Calculamos la normal del rebote basada en la posición
                Vector2 puntoImpacto = otro.ClosestPoint(transform.position);
                Vector2 normal = ((Vector2)otro.transform.position - puntoImpacto).normalized;

                // Reflejamos su dirección actual
                Vector2 nuevaDireccion = Vector2.Reflect(movBola.GetDireccion(), normal);
                movBola.CambiarDireccion(nuevaDireccion);

                // Opcional: Le damos un pequeño impulso extra para que no se pegue
                rbOtro.AddForce(direccionHaciaAfuera * fuerzaReboteBola, ForceMode2D.Impulse);
            }
            // 3. RESTO DE OBJETOS (Virus, Enemigos, etc.)
            else
            {
                // Aplicamos el empujón físico a cualquier cosa que tenga Rigidbody
                rbOtro.AddForce(direccionHaciaAfuera * fuerzaEmpujeObjetos, ForceMode2D.Impulse);
            }
        }
    }
}