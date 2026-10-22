using UnityEngine;

public class TrickRun_FakeExit : MonoBehaviour
{
    [SerializeField] private GameObject exitToReveal;

    private SpriteRenderer doorRenderer;
    private Collider2D doorCollider;
    private TrickRun_FakeExitReturnSpikes returnSpikes;
    private bool hasRevealedExit;

    private void Awake()
    {
        doorRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<Collider2D>();
        returnSpikes = GetComponentInParent<TrickRun_FakeExitReturnSpikes>(true);

        // Level 27'nin dönüş fazı sahte kapı tarafından başlatılır.
        if (returnSpikes == null)
        {
            Transform current = transform;
            while (current != null && current.name != "Level_027_FakeDoors")
                current = current.parent;

            if (current != null)
                returnSpikes = current.gameObject.AddComponent<TrickRun_FakeExitReturnSpikes>();
        }
    }

    private void OnEnable()
    {
        if (doorRenderer != null) doorRenderer.enabled = true;
        if (doorCollider != null) doorCollider.enabled = true;
        if (exitToReveal != null) exitToReveal.SetActive(false);
        hasRevealedExit = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || hasRevealedExit)
            return;

        hasRevealedExit = true;
        if (exitToReveal != null)
            exitToReveal.SetActive(true);
        if (returnSpikes != null)
            returnSpikes.ActivateReturnSpikes();
        if (doorRenderer != null)
            doorRenderer.enabled = false;
        if (doorCollider != null)
            doorCollider.enabled = false;
    }
}
