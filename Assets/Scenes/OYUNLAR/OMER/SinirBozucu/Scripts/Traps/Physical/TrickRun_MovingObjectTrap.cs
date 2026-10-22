using System.Collections;
using UnityEngine;

public class TrickRun_MovingObjectTrap : TrickRun_TrapBase
{
    [Header("Object Settings")]
    [SerializeField] private Transform movingObject;
    [SerializeField] private Transform targetPoint;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private bool carryPlayer;
    [SerializeField] private float carryHalfWidth = 3f;

    private Transform player;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool startingStateCaptured;

    // Hareket edecek nesnenin başlangıç konumunu kaydeder.
    private void Awake()
    {
        if (movingObject == null || targetPoint == null)
            return;

        startingPosition = movingObject.position;
        startingRotation = movingObject.rotation;
        startingStateCaptured = true;

        TrickRun_PlayerController controller = FindFirstObjectByType<TrickRun_PlayerController>();
        if (controller != null)
            player = controller.transform;
    }

    // TrickRun_TrapTrigger tarafından çalıştırıldığında hareketi başlatır.
    protected override void OnTrapActivated()
    {
        if (movingObject == null || !startingStateCaptured)
            return;

        StartCoroutine(MoveObject());
    }

    // Level yeniden başladığında nesneyi ilk konumuna döndürür.
    protected override void OnTrapReset()
    {
        if (movingObject == null || !startingStateCaptured)
            return;

        movingObject.SetPositionAndRotation(
            startingPosition,
            startingRotation
        );
    }

    // Nesneyi Transform üzerinden hedef noktaya taşır.
    private IEnumerator MoveObject()
    {
        while (Vector3.Distance(
            movingObject.position,
            targetPoint.position
        ) > 0.01f)
        {
            Vector3 previousPosition = movingObject.position;
            Vector3 nextPosition = Vector3.MoveTowards(
                movingObject.position,
                targetPoint.position,
                movementSpeed * Time.deltaTime
            );

            if (carryPlayer && PlayerIsStandingOnObject())
                player.position += nextPosition - previousPosition;

            movingObject.position = nextPosition;

            yield return null;
        }

        // Küçük konum farklarını ortadan kaldırır.
        movingObject.position = targetPoint.position;
    }

    private bool PlayerIsStandingOnObject()
    {
        if (player == null || movingObject == null)
            return false;

        Vector3 localPlayer = movingObject.InverseTransformPoint(player.position);
        return Mathf.Abs(localPlayer.x) <= carryHalfWidth &&
               localPlayer.y > 0.35f &&
               localPlayer.y < 1.8f;
    }
}
