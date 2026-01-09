using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float waitTime = 1f; //tiempo quieto antes de moverse

    // limitar movimiento al mapa

    public Vector2 minPosition = new Vector2(-8, -4);
    public Vector2 maxPosition = new Vector2(8, 4);

    private Vector2 targetPosition;
    private float waitCounter;
    private bool isWalking;


    void Start()
    {
        PickNewTarget();
    }

    // Update is called once per frame
    void Update()
    {
        if (isWalking)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            //si llegamos, el contador svolvera a 0
            if(Vector2.Distance(transform.position,targetPosition) < 0.1f)
            {
                isWalking = false;
                waitCounter = waitTime;
            }
        }

        else
        {
            //tiempo de espera

            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                PickNewTarget();
            }
        }
    }

    void PickNewTarget()
    {
        //elegir coordenadas aleatorias

        float randomX = Random.Range(minPosition.x, maxPosition.y);
        float randomY = Random.Range(maxPosition.x, minPosition.y);

        targetPosition = new Vector2(randomX, randomY);
        isWalking = true;

    }

}
