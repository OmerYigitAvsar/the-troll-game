using UnityEngine;
using UnityEngine.Serialization;

public class TrickRun_ReverseControlsTrap : TrickRun_TrapBase
{
    [Header("Oyuncu Referansı")]
    [SerializeField] private TrickRun_PlayerController playerController;

    [Header("Ters Kontrol Sınırları")]
    [SerializeField] private Transform startBoundary;
    [SerializeField] private Transform endBoundary;

    // Eski sahnelerde ikinci sınırın yerini korumak için bırakıldı.
    [FormerlySerializedAs("effectDuration")]
    [SerializeField, Min(0.1f)] private float boundaryDistance = 5f;

    protected override void OnTrapActivated()
    {
        SetReversed(true);
    }

    public void SetReversed(bool reversed)
    {
        if (playerController != null)
        {
            playerController.SetControlsReversed(reversed);
        }
    }

    protected override void OnTrapReset()
    {
        SetReversed(false);
    }
}
