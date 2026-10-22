using System.Collections;
using UnityEngine;

public class TrickRun_RisingSpikeTrap : TrickRun_TrapBase
{
    [Header("Spike Settings")]
    [SerializeField] private Transform[] spikes;
    [SerializeField] private float riseDistance = 1.5f;
    [SerializeField] private float riseSpeed = 12f;
    [SerializeField] private Vector3 riseDirection = Vector3.up;

    private Vector3[] hiddenPositions;
    private Vector3[] revealedPositions;
    private bool waitingForLevel30Phase;
    private bool risingStarted;

    // Dikenlerin başlangıç ve hedef konumlarını hazırlar.
    private void Awake()
    {
        if (spikes == null || spikes.Length == 0)
            spikes = new[] { transform };

        hiddenPositions = new Vector3[spikes.Length];
        revealedPositions = new Vector3[spikes.Length];

        for (int i = 0; i < spikes.Length; i++)
        {
            // Boş bırakılmış bir liste elemanı varsa onu atlar.
            if (spikes[i] == null)
                continue;

            // Dikenin başlangıçtaki gizli konumunu kaydeder.
            hiddenPositions[i] = spikes[i].position;

            // Dikenin çıkacağı hedef konumu hesaplar.
            Vector3 direction = riseDirection.sqrMagnitude > 0.001f
                ? riseDirection.normalized
                : Vector3.up;
            revealedPositions[i] = hiddenPositions[i] + direction * riseDistance;
        }
    }

    // TrickRun_TrapTrigger, ActivateTrap çağırdığında çalışır.
    protected override void OnTrapActivated()
    {
        if (IsInsideLevel30() && !CanActivateInCurrentLevel30Phase())
        {
            waitingForLevel30Phase = true;
            return;
        }

        risingStarted = true;
        StartCoroutine(RaiseSpikes());
    }

    private void Update()
    {
        if (!waitingForLevel30Phase || risingStarted || !CanActivateInCurrentLevel30Phase())
            return;

        waitingForLevel30Phase = false;
        risingStarted = true;
        StartCoroutine(RaiseSpikes());
    }

    // Level yeniden başladığında dikenleri gizli konumlarına döndürür.
    protected override void OnTrapReset()
    {
        waitingForLevel30Phase = false;
        risingStarted = false;

        // Awake henüz konumları hazırlamadıysa işlem yapmaz.
        if (hiddenPositions == null)
            return;

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
            {
                spikes[i].position = hiddenPositions[i];
            }
        }
    }

    private bool IsInsideLevel30()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("Seviye_030_") || current.name.StartsWith("Level_030_"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private bool CanActivateInCurrentLevel30Phase()
    {
        Transform level = transform;
        while (level != null && level.GetComponent<TrickRun_LevelCameraBounds>() == null)
            level = level.parent;
        if (level == null) return true;

        TrickRun_ShiftingExitTrap door = level.GetComponentInChildren<TrickRun_ShiftingExitTrap>(true);
        TrickRun_PlayerController player = FindFirstObjectByType<TrickRun_PlayerController>();
        Rigidbody2D body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (door == null || body == null) return false;

        bool isSecondSpike = gameObject.name.EndsWith("_02");
        int requiredPhase = isSecondSpike ? 2 : 1;
        bool correctDirection = isSecondSpike ? body.linearVelocity.x > 0.1f : body.linearVelocity.x < -0.1f;
        if (door.CompletedShiftCount < requiredPhase || !correctDirection)
            return false;

        if (spikes == null || spikes.Length == 0 || spikes[0] == null)
            return true;
        return Mathf.Abs(player.transform.position.x - spikes[0].position.x) <= 2.5f;
    }

    // Bütün dikenleri aynı anda hedef konumlarına taşır.
    private IEnumerator RaiseSpikes()
    {
        bool allSpikesReachedTarget = false;

        // Bütün dikenler hedefe ulaşana kadar hareket devam eder.
        while (!allSpikesReachedTarget)
        {
            allSpikesReachedTarget = true;

            for (int i = 0; i < spikes.Length; i++)
            {
                if (spikes[i] == null)
                    continue;

                // Dikeni hedef konumuna doğru hareket ettirir.
                spikes[i].position = Vector3.MoveTowards(
                    spikes[i].position,
                    revealedPositions[i],
                    riseSpeed * Time.deltaTime
                );

                // Dikenlerden biri hâlâ hareket ediyorsa döngünün sürmesini sağlar.
                if (Vector3.Distance(
                    spikes[i].position,
                    revealedPositions[i]
                ) > 0.01f)
                {
                    allSpikesReachedTarget = false;
                }
            }

            yield return null;
        }

        // Küçük konum farklarını ortadan kaldırır.
        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
            {
                spikes[i].position = revealedPositions[i];
            }
        }
    }
}
