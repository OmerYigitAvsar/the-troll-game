using UnityEngine;
using UnityEngine.EventSystems;

public class TrickRun_MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ControlAction { MoveLeft, MoveRight, Jump }

    [SerializeField] private ControlAction action;
    [SerializeField] private TrickRun_PlayerController playerController;
    private readonly System.Collections.Generic.HashSet<int> heldPointers = new System.Collections.Generic.HashSet<int>();

    public bool IsDirectionalButton =>
        action == ControlAction.MoveLeft || action == ControlAction.MoveRight;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<TrickRun_PlayerController>();
    }

    public void Configure(ControlAction selectedAction, TrickRun_PlayerController player)
    {
        action = selectedAction;
        playerController = player;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<TrickRun_PlayerController>();

        if (playerController == null) return;
        if (!heldPointers.Add(eventData.pointerId) || heldPointers.Count > 1) return;
        if (action == ControlAction.MoveLeft) playerController.SetMobileLeftHeld(true);
        else if (action == ControlAction.MoveRight) playerController.SetMobileRightHeld(true);
        else playerController.PressMobileJump();
    }

    public void OnPointerUp(PointerEventData eventData) => ReleasePointer(eventData.pointerId);
    public void OnPointerExit(PointerEventData eventData) => ReleasePointer(eventData.pointerId);
    private void ReleasePointer(int id)
    {
        if (heldPointers.Remove(id) && heldPointers.Count == 0) ReleaseControl();
    }
    private void CancelTouches() { heldPointers.Clear(); ReleaseControl(); }
    private void OnDisable() => CancelTouches();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelTouches(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelTouches(); }

    private void ReleaseControl()
    {
        if (playerController == null) return;
        if (action == ControlAction.MoveLeft) playerController.SetMobileLeftHeld(false);
        else if (action == ControlAction.MoveRight) playerController.SetMobileRightHeld(false);
        else playerController.ReleaseMobileJump();
    }
}
