using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrickRun_ReverseControlBoundary : MonoBehaviour
{
    [SerializeField] private TrickRun_ReverseControlsTrap reverseControls;
    [SerializeField] private bool reverseOnContact;

    private Collider2D boundaryCollider;

    private void Awake()
    {
        boundaryCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (boundaryCollider == null)
            boundaryCollider = GetComponent<Collider2D>();

        boundaryCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || reverseControls == null)
            return;

        reverseControls.SetReversed(reverseOnContact);
        boundaryCollider.enabled = false;
    }

    public void Configure(TrickRun_ReverseControlsTrap trap, bool shouldReverse)
    {
        reverseControls = trap;
        reverseOnContact = shouldReverse;
    }
}
