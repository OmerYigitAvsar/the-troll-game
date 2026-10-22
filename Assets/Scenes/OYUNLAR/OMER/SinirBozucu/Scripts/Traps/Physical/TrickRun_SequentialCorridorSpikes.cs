using System.Collections;
using UnityEngine;

public class TrickRun_SequentialCorridorSpikes : TrickRun_TrapBase
{
    [Header("Platform Takibi")]
    [SerializeField] private Transform movingPlatform;

    [Header("Dikenler - Sağdan Sola Sıralı")]
    [SerializeField] private Transform[] spikes;
    [SerializeField] private float riseDistance = 0.9f;
    [SerializeField] private float riseSpeed = 12f;
    [SerializeField] private float delayBetweenSpikes = 0.18f;

    private Vector3 platformOffset;
    private Vector3[] hiddenLocalPositions;
    private Vector3[] revealedLocalPositions;

    private void Awake()
    {
        // Prefablar kendi TrickRun_RisingSpikeTrap bileşenleriyle geliyor. Bu grupta
        // konumu yalnızca bu kontrolcü yönetmeli; aksi halde ölüm sonrası iki
        // farklı sıfırlama dünya/yerel konumlarını birbirine karıştırıyor.
        TrickRun_RisingSpikeTrap[] individualControllers =
            GetComponentsInChildren<TrickRun_RisingSpikeTrap>(true);
        foreach (TrickRun_RisingSpikeTrap controller in individualControllers)
            controller.enabled = false;

        // Inspector listesine sonradan eklenen dikenlerin sırası önemli değil;
        // aktivasyon her zaman koridorun sağından soluna doğru ilerler.
        System.Array.Sort(spikes, (left, right) =>
        {
            if (left == null) return 1;
            if (right == null) return -1;
            return right.localPosition.x.CompareTo(left.localPosition.x);
        });

        platformOffset = movingPlatform != null
            ? transform.position - movingPlatform.position
            : Vector3.zero;

        hiddenLocalPositions = new Vector3[spikes.Length];
        revealedLocalPositions = new Vector3[spikes.Length];

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] == null)
                continue;

            hiddenLocalPositions[i] = spikes[i].localPosition;
            revealedLocalPositions[i] = hiddenLocalPositions[i] + Vector3.up * riseDistance;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(ResetAfterChildrenEnabled());
    }

    private void LateUpdate()
    {
        if (movingPlatform != null)
            transform.position = movingPlatform.position + platformOffset;
    }

    protected override void OnTrapActivated()
    {
        StartCoroutine(RaiseSequentially());
    }

    protected override void OnTrapReset()
    {
        if (hiddenLocalPositions == null)
            return;

        if (movingPlatform != null)
            transform.position = movingPlatform.position + platformOffset;

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
                spikes[i].localPosition = hiddenLocalPositions[i];
        }
    }

    private IEnumerator ResetAfterChildrenEnabled()
    {
        // Bütün alt nesnelerin OnEnable çağrıları bittikten sonra son sözü grup
        // kontrolcüsü söyler. Böylece her ölümde aynı yerel konumlara dönülür.
        yield return null;
        OnTrapReset();
    }

    private IEnumerator RaiseSequentially()
    {
        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
                StartCoroutine(RaiseOne(spikes[i], revealedLocalPositions[i]));

            if (i < spikes.Length - 1)
                yield return new WaitForSeconds(delayBetweenSpikes);
        }
    }

    private IEnumerator RaiseOne(Transform spike, Vector3 target)
    {
        while (Vector3.Distance(spike.localPosition, target) > 0.01f)
        {
            spike.localPosition = Vector3.MoveTowards(
                spike.localPosition,
                target,
                riseSpeed * Time.deltaTime);
            yield return null;
        }

        spike.localPosition = target;
    }
}
