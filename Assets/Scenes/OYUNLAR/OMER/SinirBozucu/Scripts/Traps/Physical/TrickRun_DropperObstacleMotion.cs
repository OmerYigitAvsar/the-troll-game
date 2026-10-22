using UnityEngine;

public class TrickRun_DropperObstacleMotion : MonoBehaviour
{
    [SerializeField] private Transform[] movingObstacles;
    [SerializeField] private float horizontalDistance = 4.5f;
    [SerializeField] private float movementSpeed = 1.6f;
    [SerializeField] private float phaseDifference = 1.8f;

    private Vector3[] startPositions;

    private void Awake()
    {
        startPositions = new Vector3[movingObstacles.Length];
        for (int i = 0; i < movingObstacles.Length; i++)
        {
            if (movingObstacles[i] != null)
                startPositions[i] = movingObstacles[i].localPosition;
        }
    }

    private void Update()
    {
        for (int i = 0; i < movingObstacles.Length; i++)
        {
            Transform obstacle = movingObstacles[i];
            if (obstacle == null)
                continue;

            Vector3 position = startPositions[i];
            position.x += Mathf.Sin(Time.time * movementSpeed + i * phaseDifference)
                          * horizontalDistance;
            obstacle.localPosition = position;
        }
    }

    private void OnDisable()
    {
        if (startPositions == null)
            return;

        for (int i = 0; i < movingObstacles.Length; i++)
        {
            if (movingObstacles[i] != null)
                movingObstacles[i].localPosition = startPositions[i];
        }
    }
}
