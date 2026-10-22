using UnityEngine;

public class TrickRun_CameraFollower : MonoBehaviour
{
    [Header("Horizontal Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 7f;
    [SerializeField] private float lookAhead = 2f;
    [SerializeField] private float fixedY;
    [SerializeField] private float gameplayVerticalOffset = -0.75f;
    [Range(0f, 0.4f)]
    [SerializeField] private float mobileControlsViewportHeight = 0.2f;
    [SerializeField] private float levelEndPadding = 2.5f;
    [SerializeField] private float wallScreenPadding = 0.05f;

    private Camera cameraComponent;
    private TrickRun_LevelCameraBounds levelBounds;
    private Collider2D leftWallCollider;
    private Collider2D rightWallCollider;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        ApplyMobileViewport();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        RefreshVerticalBoundsFromPlayerPosition();

        float targetX = ClampToLevel(target.position.x + lookAhead);
        float targetY = fixedY;
        if (levelBounds != null && levelBounds.FollowPlayerVertically && cameraComponent != null)
        {
            float halfHeight = cameraComponent.orthographicSize;
            float minY = levelBounds.BottomEdge + halfHeight;
            float maxY = levelBounds.TopEdge - halfHeight;
            targetY = minY <= maxY
                ? Mathf.Clamp(target.position.y - 1.25f, minY, maxY)
                : (levelBounds.BottomEdge + levelBounds.TopEdge) * 0.5f;
        }
        Vector3 targetPosition = new Vector3(targetX, targetY, -10f);
        float smoothing = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing);
    }

    // Test sirasinda level Hierarchy'den elle acildiginda LevelManager eski level
    // sinirini tutabilir. Oyuncunun icinde bulundugu aktif dikey leveli bulup
    // kameranin Y takibini dogru sinira baglar.
    private void RefreshVerticalBoundsFromPlayerPosition()
    {
        if (levelBounds != null && levelBounds.FollowPlayerVertically)
            return;

        if (Mathf.Abs(target.position.y - fixedY) < 4f)
            return;

        TrickRun_LevelCameraBounds[] activeBounds = Object.FindObjectsByType<TrickRun_LevelCameraBounds>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Vector3 playerPosition = target.position;
        for (int i = 0; i < activeBounds.Length; i++)
        {
            TrickRun_LevelCameraBounds candidate = activeBounds[i];
            if (candidate == null || !candidate.FollowPlayerVertically)
                continue;

            bool insideHorizontal = playerPosition.x >= candidate.LeftEdge && playerPosition.x <= candidate.RightEdge;
            bool insideVertical = playerPosition.y >= candidate.BottomEdge && playerPosition.y <= candidate.TopEdge;
            if (!insideHorizontal || !insideVertical || levelBounds == candidate)
                continue;

            levelBounds = candidate;
            fixedY = candidate.CameraY + gameplayVerticalOffset;
            FindRoomWalls();
            break;
        }
    }

    public void ConfigureForLevel(Transform player, TrickRun_LevelCameraBounds bounds)
    {
        target = player;
        levelBounds = bounds;
        FindRoomWalls();

        if (levelBounds != null)
            fixedY = levelBounds.CameraY + gameplayVerticalOffset;

        SnapToTarget();
    }

    // Oda siniri oyun sirasinda degistiginde kamerayi aniden isinlamadan duvarlari yeniler.
    public void RefreshLevelBounds(Transform player, TrickRun_LevelCameraBounds bounds)
    {
        target = player;
        levelBounds = bounds;
        FindRoomWalls();

        if (levelBounds != null)
            fixedY = levelBounds.CameraY + gameplayVerticalOffset;
    }

    public void ConfigureSettings(Transform player, float speed, float ahead, float y)
    {
        target = player;
        followSpeed = speed;
        lookAhead = ahead;
        fixedY = y + gameplayVerticalOffset;
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        float snapY = fixedY;
        if (levelBounds != null && levelBounds.FollowPlayerVertically && cameraComponent != null)
        {
            float minY = levelBounds.BottomEdge + cameraComponent.orthographicSize;
            float maxY = levelBounds.TopEdge - cameraComponent.orthographicSize;
            snapY = minY <= maxY ? Mathf.Clamp(target.position.y - 1.25f, minY, maxY) : fixedY;
        }
        transform.position = new Vector3(ClampToLevel(target.position.x + lookAhead), snapY, -10f);
    }

    private float ClampToLevel(float desiredX)
    {
        if (levelBounds == null || cameraComponent == null)
            return desiredX;

        float halfWidth = cameraComponent.orthographicSize * cameraComponent.aspect;
        float minimum;
        float maximum;

        if (leftWallCollider != null && rightWallCollider != null)
        {
            // Kameranin gorunen alani duvarlarin ic yuzlerinde biter.
            // Kucuk padding, sprite kenarinin ekranda belirmesini engeller.
            minimum = leftWallCollider.bounds.max.x + halfWidth + wallScreenPadding;
            maximum = rightWallCollider.bounds.min.x - halfWidth - wallScreenPadding;
        }
        else
        {
            minimum = levelBounds.LeftEdge + halfWidth;
            maximum = levelBounds.RightEdge - halfWidth + levelEndPadding;
        }

        if (minimum > maximum)
        {
            if (leftWallCollider != null && rightWallCollider != null)
                return (leftWallCollider.bounds.max.x + rightWallCollider.bounds.min.x) * 0.5f;

            return (levelBounds.LeftEdge + levelBounds.RightEdge) * 0.5f;
        }

        return Mathf.Clamp(desiredX, minimum, maximum);
    }

    private void FindRoomWalls()
    {
        leftWallCollider = null;
        rightWallCollider = null;

        if (levelBounds == null)
            return;

        Transform room = levelBounds.transform.Find("Oda Sınırları") ??
                         levelBounds.transform.Find("RoomBounds");
        if (room == null)
            return;

        Transform leftWall = room.Find("Sol Duvar") ?? room.Find("LeftWall");
        Transform rightWall = room.Find("Sağ Duvar") ?? room.Find("RightWall");

        if (leftWall != null)
            leftWallCollider = leftWall.GetComponent<Collider2D>();

        if (rightWall != null)
            rightWallCollider = rightWall.GetComponent<Collider2D>();
    }

    private void ApplyMobileViewport()
    {
        if (cameraComponent == null)
            return;

        float controlsHeight = Mathf.Clamp(mobileControlsViewportHeight, 0f, 0.4f);
        cameraComponent.rect = new Rect(0f, controlsHeight, 1f, 1f - controlsHeight);
    }
}
