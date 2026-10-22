using System.Collections;
using UnityEngine;

public class TrickRun_FakeExitReturnSpikes : MonoBehaviour
{
    [SerializeField] private Transform[] spikes;
    [SerializeField] private Vector3[] hiddenLocalPositions;
    [SerializeField] private Vector3[] revealedLocalPositions;
    [SerializeField] private float riseSpeed = 10f;
    [SerializeField] private float firstSpikeDelay = 0.35f;
    [SerializeField] private float sequenceInterval = 0.12f;

    private bool activated;

    private void OnEnable()
    {
        ResetSpikes();
    }

    public void Configure(Transform[] sceneSpikes, Vector3[] hiddenPositions, Vector3[] revealedPositions)
    {
        spikes = sceneSpikes;
        hiddenLocalPositions = hiddenPositions;
        revealedLocalPositions = revealedPositions;
        ResetSpikes();
    }

    public void ActivateReturnSpikes()
    {
        if (activated || spikes == null || spikes.Length == 0)
            return;

        activated = true;
        StartCoroutine(RevealSequence());
    }

    private void ResetSpikes()
    {
        StopAllCoroutines();
        activated = false;

        if (spikes == null || hiddenLocalPositions == null)
            return;

        int count = Mathf.Min(spikes.Length, hiddenLocalPositions.Length);
        for (int i = 0; i < count; i++)
        {
            if (spikes[i] != null)
                spikes[i].localPosition = hiddenLocalPositions[i];
        }
    }

    private IEnumerator RevealSequence()
    {
        yield return new WaitForSeconds(firstSpikeDelay);

        // Dizide önce kapı çevresi, ardından dönüş yolu dikenleri bulunur.
        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
                StartCoroutine(RaiseSpike(i));

            if (sequenceInterval > 0f)
                yield return new WaitForSeconds(sequenceInterval);
        }
    }

    private IEnumerator RaiseSpike(int index)
    {
        if (index >= revealedLocalPositions.Length)
            yield break;

        Transform spike = spikes[index];
        Vector3 target = revealedLocalPositions[index];
        while (spike != null && Vector3.Distance(spike.localPosition, target) > 0.01f)
        {
            spike.localPosition = Vector3.MoveTowards(spike.localPosition, target, riseSpeed * Time.deltaTime);
            yield return null;
        }

        if (spike != null)
            spike.localPosition = target;
    }
}
