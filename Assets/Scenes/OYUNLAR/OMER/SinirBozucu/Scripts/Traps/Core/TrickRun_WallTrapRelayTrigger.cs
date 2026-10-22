using UnityEngine;

public class TrickRun_WallTrapRelayTrigger : MonoBehaviour
{
    [SerializeField] private Transform activatingWall;
    [SerializeField] private TrickRun_TrapBase nextWallTrap;

    private bool hasTriggered;

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || activatingWall == null || nextWallTrap == null)
            return;

        Transform entered = other.transform;
        if (entered != activatingWall && !entered.IsChildOf(activatingWall))
            return;

        hasTriggered = true;
        nextWallTrap.ActivateTrap();
    }
}
