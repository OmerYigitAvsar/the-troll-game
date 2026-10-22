using System.Collections;
using UnityEngine;

public class TrickRun_ShiftingExitTrap : TrickRun_TrapBase
{
    [SerializeField] private Transform exitDoor;
    [SerializeField] private Transform[] destinations;
    [SerializeField] private float movementSpeed = 12f;
    [SerializeField] private bool teleportInstantly;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private int nextDestination;
    private bool isMoving;

    public int CompletedShiftCount => nextDestination;

    private void Awake()
    {
        if (exitDoor != null)
        {
            startingPosition = exitDoor.position;
            startingRotation = exitDoor.rotation;
        }
    }

    protected override void OnTrapActivated()
    {
        TryActivateExitMovement();
    }

    private void TryActivateExitMovement()
    {
        if (exitDoor == null || destinations == null || nextDestination >= destinations.Length || isMoving)
            return;

        // Ortak kapı tetikleyicilerinden yalnızca oyuncunun mevcut kapıya yakın
        // olduğu taraftaki çalışır. Böylece dönüş tetikleyicisi ilk geçişte
        // kapıyı erkenden oyuncunun yanına ışınlayamaz.
        if (teleportInstantly)
        {
            Transform destination = destinations[nextDestination++];
            if (destination != null)
                exitDoor.position = destination.position;
            return;
        }

        StartCoroutine(MoveToNextDestination());
    }

    private IEnumerator MoveToNextDestination()
    {
        isMoving = true;
        Transform destination = destinations[nextDestination++];
        while (destination != null && Vector3.Distance(exitDoor.position, destination.position) > 0.01f)
        {
            exitDoor.position = Vector3.MoveTowards(
                exitDoor.position,
                destination.position,
                movementSpeed * Time.deltaTime);
            yield return null;
        }

        if (destination != null)
            exitDoor.position = destination.position;
        isMoving = false;
    }

    protected override void OnTrapReset()
    {
        isMoving = false;
        nextDestination = 0;
        if (exitDoor != null)
            exitDoor.SetPositionAndRotation(startingPosition, startingRotation);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isMoving = false;
        nextDestination = 0;
        if (exitDoor != null)
            exitDoor.SetPositionAndRotation(startingPosition, startingRotation);
    }
}
