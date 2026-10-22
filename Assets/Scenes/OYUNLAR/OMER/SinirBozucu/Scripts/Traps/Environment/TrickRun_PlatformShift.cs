using UnityEngine;

public class TrickRun_PlatformShift : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float movementSpeed = 9f;

    [Header("Hierarchy References")]
    [SerializeField] private Transform platform03;
    [SerializeField] private Transform platform08;
    [SerializeField] private Transform platform02;
    [SerializeField] private Collider2D rightWallCollider;
    [SerializeField] private TrickRun_ShiftTrigger leftTrigger;
    [SerializeField] private TrickRun_ShiftTrigger rightTrigger;

    private Rigidbody2D platform03Body;
    private Rigidbody2D platform08Body;
    private Collider2D platform03Collider;
    private Collider2D platform08Collider;
    private Collider2D platform02Collider;

    private Vector2 platform03Start;
    private Vector2 platform08Start;
    private Vector2 platform03Target;
    private Vector2 platform08Target;
    private float firstLayerY;
    private bool isBound;
    private bool isMoving;
    private bool hasActivated;

    private void Awake()
    {
        BindObjects();
    }

    private void OnEnable()
    {
        if (!isBound) BindObjects();
        ResetPlatforms();
    }

    private void OnDisable()
    {
        ResetPlatforms();
    }

    public void ActivateShift()
    {
        if (!isBound || isMoving || hasActivated)
            return;

        hasActivated = true;
        isMoving = true;
        SetTriggerCollidersEnabled(false);
    }

    private void FixedUpdate()
    {
        if (!isBound || !isMoving)
            return;

        Vector2 next03 = Vector2.MoveTowards(platform03Body.position, platform03Target, movementSpeed * Time.fixedDeltaTime);
        Vector2 next08 = Vector2.MoveTowards(platform08Body.position, platform08Target, movementSpeed * Time.fixedDeltaTime);
        platform03Body.MovePosition(next03);
        platform08Body.MovePosition(next08);

        if (Vector2.Distance(next03, platform03Target) <= 0.01f &&
            Vector2.Distance(next08, platform08Target) <= 0.01f)
        {
            platform03Body.position = platform03Target;
            platform08Body.position = platform08Target;
            isMoving = false;
        }
    }

    private void BindObjects()
    {
        Transform levelRoot = transform;
        while (levelRoot.parent != null && !levelRoot.name.StartsWith("Level_"))
            levelRoot = levelRoot.parent;

        Transform[] children = levelRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (platform03 == null && (child.name == "StaticPlatform_03" || child.name == "SabitPlatform_03")) platform03 = child;
            else if (platform08 == null && (child.name == "StaticPlatform_08" || child.name == "SabitPlatform_08")) platform08 = child;
            else if (platform02 == null && (child.name == "StaticPlatform_02" || child.name == "SabitPlatform_02")) platform02 = child;
            else if (rightWallCollider == null && (child.name == "RightWall" || child.name == "Sağ Duvar" || child.name == "Sag Duvar"))
                rightWallCollider = child.GetComponent<Collider2D>();
        }

        if (platform02 == null || platform03 == null || platform08 == null || rightWallCollider == null)
        {
            Debug.LogError("Seviye 37 platform hareketi için SabitPlatform_03, SabitPlatform_08 veya Sağ Duvar bulunamadı.", this);
            return;
        }

        platform03Collider = platform03.GetComponent<Collider2D>();
        platform08Collider = platform08.GetComponent<Collider2D>();
        platform02Collider = platform02.GetComponent<Collider2D>();
        if (platform02Collider == null || platform03Collider == null || platform08Collider == null)
        {
            Debug.LogError("Seviye 37 hareketli platform collider bağlantıları eksik.", this);
            return;
        }

        platform03Body = PrepareBody(platform03);
        platform08Body = PrepareBody(platform08);
        platform03Start = platform03.position;
        platform08Start = platform08.position;
        float platform03HalfWidth = platform03Collider.bounds.extents.x;
        float target03X = rightWallCollider.bounds.min.x - platform03HalfWidth;
        float rightTravel = Mathf.Max(0f, target03X - platform03Start.x);
        float platform08HalfWidth = platform08Collider.bounds.extents.x;
        float minimum08X = GetLeftWallInnerEdge(children) + platform08HalfWidth;

        platform03Target = new Vector2(target03X, platform03Start.y);
        platform08Target = new Vector2(Mathf.Max(minimum08X, platform08Start.x - rightTravel), platform08Start.y);

        Collider2D left = platform02Collider;
        Collider2D right = platform08Collider;
        firstLayerY = Mathf.Max(platform02Collider.bounds.max.y, platform08Collider.bounds.max.y);
        isBound = true;
        if (leftTrigger == null || rightTrigger == null)
            CreateShiftTriggers(left.bounds.max.x, right.bounds.min.x);
        else
        {
            leftTrigger.Configure(this);
            rightTrigger.Configure(this);
        }
    }

    private static Rigidbody2D PrepareBody(Transform platform)
    {
        Rigidbody2D body = platform.GetComponent<Rigidbody2D>();
        if (body == null) body = platform.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        return body;
    }

    private void CreateShiftTriggers(float gapLeftEdge, float gapRightEdge)
    {
        leftTrigger = CreateTrigger("PlatformShiftTrigger_Left", gapLeftEdge - 0.45f);
        rightTrigger = CreateTrigger("PlatformShiftTrigger_Right", gapRightEdge + 0.45f);
    }

    private TrickRun_ShiftTrigger CreateTrigger(string triggerName, float worldX)
    {
        Transform existing = transform.Find("Mechanics/Triggers/" + triggerName) ??
                             transform.Find("Mekanikler/Tetikleyiciler/" + triggerName);
        GameObject triggerObject;
        if (existing != null)
        {
            triggerObject = existing.gameObject;
        }
        else
        {
            Transform levelRoot = transform;
            while (levelRoot.parent != null && !levelRoot.name.StartsWith("Level_"))
                levelRoot = levelRoot.parent;
            Transform triggerParent = levelRoot.Find("Mechanics/Triggers") ??
                                      levelRoot.Find("Mekanikler/Tetikleyiciler");
            if (triggerParent == null) triggerParent = transform;
            triggerObject = new GameObject(triggerName);
            triggerObject.transform.SetParent(triggerParent, false);
            Vector3 worldPosition = new Vector3(worldX, firstLayerY + 1.25f, 0f);
            triggerObject.transform.position = worldPosition;
            BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.9f, 3f);
        }

        triggerObject.transform.position = new Vector3(worldX, firstLayerY + 1.25f, 0f);
        BoxCollider2D triggerCollider = triggerObject.GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(0.9f, 3f);
        }

        TrickRun_ShiftTrigger trigger = triggerObject.GetComponent<TrickRun_ShiftTrigger>();
        if (trigger == null) trigger = triggerObject.AddComponent<TrickRun_ShiftTrigger>();
        trigger.Configure(this);
        return trigger;
    }

    private void SetTriggerCollidersEnabled(bool enabled)
    {
        if (leftTrigger != null) leftTrigger.SetColliderEnabled(enabled);
        if (rightTrigger != null) rightTrigger.SetColliderEnabled(enabled);
    }

    private static float GetLeftWallInnerEdge(Transform[] children)
    {
        foreach (Transform child in children)
        {
            if (child.name != "LeftWall" && child.name != "Sol Duvar") continue;
            Collider2D wall = child.GetComponent<Collider2D>();
            if (wall != null) return wall.bounds.max.x;
        }
        return float.NegativeInfinity;
    }

    private void ResetPlatforms()
    {
        if (!isBound) return;
        isMoving = false;
        hasActivated = false;
        SetTriggerCollidersEnabled(true);
        platform03Body.position = platform03Start;
        platform08Body.position = platform08Start;
        platform03Body.linearVelocity = Vector2.zero;
        platform08Body.linearVelocity = Vector2.zero;
    }
}
