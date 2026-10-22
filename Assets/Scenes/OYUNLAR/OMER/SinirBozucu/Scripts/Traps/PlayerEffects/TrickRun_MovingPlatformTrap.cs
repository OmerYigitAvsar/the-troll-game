using System.Collections;
using UnityEngine;

public class TrickRun_MovingPlatformTrap : TrickRun_TrapBase
{
    [Header("Platform Settings")]
    [SerializeField] private Rigidbody2D platformRigidbody;

    [Header("Movement Points")]
    [SerializeField] private Transform[] movementPoints;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float waitTimeAtPoint = 0.2f;
    [SerializeField] private bool pingPongMovement = true;

    private Vector2 startingPosition;
    private float startingRotation;

    private int currentPointIndex;
    private int movementDirection = 1;

    // Platformun başlangıç konumunu ve dönüşünü kaydeder.
    private void Awake()
    {
        startingPosition = platformRigidbody.position;
        startingRotation = platformRigidbody.rotation;
    }

    // Trap çalıştırıldığında platformun sürekli hareketini başlatır.
    protected override void OnTrapActivated()
    {
        if (movementPoints.Length == 0)
            return;

        StartCoroutine(MoveBetweenPoints());
    }

    // Level yeniden başladığında platformu ilk durumuna döndürür.
    protected override void OnTrapReset()
    {
        if (platformRigidbody == null)
            return;

        // Önceki hareketten kalan fizik hızlarını temizler.
        platformRigidbody.linearVelocity = Vector2.zero;
        platformRigidbody.angularVelocity = 0f;

        // Platformu başlangıç konumuna geri getirir.
        platformRigidbody.position = startingPosition;
        platformRigidbody.rotation = startingRotation;

        // Hareket sırasını başa alır.
        currentPointIndex = 0;
        movementDirection = 1;
    }

    // Platformu hareket noktaları arasında sürekli taşır.
    private IEnumerator MoveBetweenPoints()
    {
        while (true)
        {
            Transform targetPoint = movementPoints[currentPointIndex];

            // Platform hedef noktaya ulaşana kadar hareket eder.
            while (Vector2.Distance(
                platformRigidbody.position,
                targetPoint.position
            ) > 0.01f)
            {
                yield return new WaitForFixedUpdate();

                Vector2 nextPosition = Vector2.MoveTowards(
                    platformRigidbody.position,
                    targetPoint.position,
                    movementSpeed * Time.fixedDeltaTime
                );

                platformRigidbody.MovePosition(nextPosition);
            }

            // Küçük konum farklarını ortadan kaldırır.
            platformRigidbody.position = targetPoint.position;

            // Tek hedefli, ileri yönlü platform hedefe ulaştığında hareketi bitirir.
            // Burada durmazsa döngü hiç yield vermeden Unity ana thread'ini kilitler.
            if (!pingPongMovement && movementPoints.Length == 1)
                yield break;

            // Hedef noktada belirlenen süre kadar bekler.
            if (waitTimeAtPoint > 0f)
            {
                yield return new WaitForSeconds(waitTimeAtPoint);
            }

            SelectNextPoint();

            // Yanlış veya aynı konuma bakan hedeflerde bile kare başına bir kez ilerle.
            yield return new WaitForFixedUpdate();
        }
    }

    // Platformun sıradaki hedef noktasını belirler.
    private void SelectNextPoint()
    {
        // Ping Pong kapalıysa listenin sonundan tekrar başa döner.
        if (!pingPongMovement)
        {
            currentPointIndex++;

            if (currentPointIndex >= movementPoints.Length)
            {
                currentPointIndex = 0;
            }

            return;
        }

        // Ping Pong açıksa listenin başı ve sonu arasında gidip gelir.
        currentPointIndex += movementDirection;

        if (currentPointIndex >= movementPoints.Length)
        {
            movementDirection = -1;
            currentPointIndex = movementPoints.Length - 2;
        }
        else if (currentPointIndex < 0)
        {
            movementDirection = 1;
            currentPointIndex = 1;
        }

        // Yalnızca bir nokta varsa dizi sınırında kalmasını sağlar.
        currentPointIndex = Mathf.Clamp(
            currentPointIndex,
            0,
            movementPoints.Length - 1
        );
    }
}
