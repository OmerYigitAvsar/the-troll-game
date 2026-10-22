using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrickRun_JumpControlBoundary : MonoBehaviour
{
    [SerializeField] private TrickRun_DisableJumpTrap jumpTrap;
    [SerializeField] private bool enableJumpOnContact;
    private Collider2D boundaryCollider;

    private void Awake() => boundaryCollider = GetComponent<Collider2D>();

    private void OnEnable()
    {
        if (boundaryCollider == null) boundaryCollider = GetComponent<Collider2D>();
        boundaryCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || jumpTrap == null) return;
        jumpTrap.SetJumpEnabled(enableJumpOnContact);
        boundaryCollider.enabled = false;
    }

    public void Configure(TrickRun_DisableJumpTrap trap, bool enableJump)
    {
        jumpTrap = trap;
        enableJumpOnContact = enableJump;
    }
}
