using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrickRun_ShiftTrigger : MonoBehaviour
{
    [SerializeField] private TrickRun_PlatformShift platformShift;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || platformShift == null)
            return;

        platformShift.ActivateShift();
    }

    public void Configure(TrickRun_PlatformShift shift)
    {
        platformShift = shift;
        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
    }

    public void SetColliderEnabled(bool enabled)
    {
        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null) triggerCollider.enabled = enabled;
    }
}
