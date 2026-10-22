using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrickRun_PlayerController : MonoBehaviour
{
    public event Action<bool> ControlsReversedChanged;
    public event Action<bool> JumpingEnabledChanged;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [Range(0.5f, 1f)]
    [SerializeField] private float invertedJumpMultiplier = 0.8f;

    [Header("Gravity")]
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float fallingGravity = 5f;
    [SerializeField] private float maximumFallSpeed = 18f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private float groundCheckWidthMultiplier = 0.75f;

    [Header("Slippery Movement")]
    [SerializeField] private float slipperyAcceleration = 12f;
    [SerializeField] private float slipperyDeceleration = 3f;

    private float activeSlipperyAcceleration;
    private float activeSlipperyDeceleration;
    private float activeSlipperySpeedMultiplier = 1f;

    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;

    private float horizontalInput;
    private bool mobileLeftHeld;
    private bool mobileRightHeld;
    private bool mobileJumpPressed;
    private bool mobileJumpReleased;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isGrounded;

    private bool isSlippery;
    
    private bool controlsReversed;

    private bool jumpingEnabled = true;

    private float movementSpeedMultiplier = 1f;

    private float gravityMultiplier = 1f;

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private SpriteRenderer characterRenderer;
    private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    private void LateUpdate()
    {
        if (characterAnimator == null || rb == null) return;
        characterAnimator.SetBool(GroundedParameter, isGrounded && rb.linearVelocity.y * gravityMultiplier <= 0.1f);
        characterAnimator.SetFloat(SpeedParameter, Mathf.Abs(rb.linearVelocity.x));
        if (characterRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
            characterRenderer.flipX = transform.InverseTransformDirection(new Vector3(rb.linearVelocity.x, 0f, 0f)).x < 0f;
    }


    // Player üzerindeki gerekli bileşenleri alır.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        activeSlipperyAcceleration = slipperyAcceleration;
        activeSlipperyDeceleration = slipperyDeceleration;

        if (GetComponent<TrickRun_MobileControlsAnimator>() == null)
            gameObject.AddComponent<TrickRun_MobileControlsAnimator>();

        // Duvar kenarlarinda surtunmeden dolayi asili kalmayi engeller.
        PhysicsMaterial2D frictionlessMaterial = new PhysicsMaterial2D("Player_Frictionless")
        {
            friction = 0f,
            bounciness = 0f
        };
        playerCollider.sharedMaterial = frictionlessMaterial;
    }

    // Klavye girişlerini ve zemin kontrolünü her karede işler.
    private void Update()
    {
        ReadMovementInput();
        CheckGround();
        UpdateJumpTimers();
        ReadJumpInput();
    }

    // Fizik işlemlerini sabit zaman aralıklarında uygular.
    private void FixedUpdate()
    {
        MovePlayer();
        ApplyBetterGravity();
    }

    // Klavyeden sağ ve sol hareket girişini okur.
    private void ReadMovementInput()
    {
        bool movingLeft = mobileLeftHeld;
        bool movingRight = mobileRightHeld;

        if (Keyboard.current != null)
        {
            movingLeft |= Keyboard.current.aKey.isPressed ||
                          Keyboard.current.leftArrowKey.isPressed;
            movingRight |= Keyboard.current.dKey.isPressed ||
                           Keyboard.current.rightArrowKey.isPressed;
        }

        if (movingLeft)
            horizontalInput = -1f;
        else if (movingRight)
            horizontalInput = 1f;
        else
            horizontalInput = 0f;

        // Ters kontrol etkisi açıksa yatay hareket yönünü değiştirir.
        if (controlsReversed)
        {
            horizontalInput *= -1f;
        }   

    }

    // Başka scriptlerin ters kontrol durumunu değiştirmesini sağlar.
    public void SetControlsReversed(bool isReversed)
    {
        if (controlsReversed == isReversed)
            return;

        controlsReversed = isReversed;
        ControlsReversedChanged?.Invoke(isReversed);
    }

    public void SetMobileLeftHeld(bool isHeld) => mobileLeftHeld = isHeld;
    public void SetMobileRightHeld(bool isHeld) => mobileRightHeld = isHeld;

    public void PressMobileJump()
    {
        mobileJumpPressed = true;
        mobileJumpReleased = false;
    }

    public void ReleaseMobileJump()
    {
        mobileJumpReleased = true;
    }

    // Player Collider'ının hemen altındaki alanı kontrol eder.
    private void CheckGround()
    {
        Bounds bounds = playerCollider.bounds;

        bool invertedGravity = gravityMultiplier < 0f;

        // Normal yer çekiminde ayakları, ters yer çekiminde başın üzerini kontrol eder.
        Vector2 checkPosition = new Vector2(
            bounds.center.x,
            invertedGravity
                ? bounds.max.y + groundCheckHeight / 2f
                : bounds.min.y - groundCheckHeight / 2f
        );

        // Kenarlardaki duvarların zemin sayılmasını önlemek için kutuyu daraltır.
        Vector2 checkSize = new Vector2(
            bounds.size.x * groundCheckWidthMultiplier,
            groundCheckHeight
        );

        isGrounded = Physics2D.OverlapBox(
            checkPosition,
            checkSize,
            0f,
            groundLayer
        );
    }

    // Coyote Time ve Jump Buffer sürelerini günceller.
    private void UpdateJumpTimers()
    {
        // Player yerdeyken kısa zıplama hakkını sürekli yeniler.
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // Önceden basılmış zıplama girişinin süresini azaltır.
        jumpBufferTimer -= Time.deltaTime;
    }

    // Zıplama tuşuna basılmasını ve bırakılmasını kontrol eder.
    private void ReadJumpInput()
    {

        // Zıplama kapalıysa girişleri okumaz.
        if (!jumpingEnabled)
            return;

        bool jumpPressed = mobileJumpPressed;
        bool jumpReleased = mobileJumpReleased;

        if (Keyboard.current != null)
        {
            jumpPressed |= Keyboard.current.spaceKey.wasPressedThisFrame ||
                           Keyboard.current.upArrowKey.wasPressedThisFrame;
            jumpReleased |= Keyboard.current.spaceKey.wasReleasedThisFrame ||
                            Keyboard.current.upArrowKey.wasReleasedThisFrame;
        }

        mobileJumpPressed = false;
        mobileJumpReleased = false;

        // Zıplama basıldığında girişi kısa süreliğine hafızada tutar.
        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        // Kayıtlı zıplama girişi ve zıplama hakkı varsa zıplar.
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump();
        }

        // Tuş erken bırakılırsa yükselme hızını azaltarak kısa zıplama yapar.
        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }


    // Başka scriptlerin Player'ın zıplama iznini değiştirmesini sağlar.
    public void SetJumpingEnabled(bool isEnabled)
    {
        if (jumpingEnabled == isEnabled)
            return;

        jumpingEnabled = isEnabled;
        JumpingEnabledChanged?.Invoke(isEnabled);

        // Zıplama kapatıldığında kayıtlı zıplama girişini temizler.
        if (!jumpingEnabled)
        {
            jumpBufferTimer = 0f;
            mobileJumpPressed = false;
            mobileJumpReleased = false;
        }
    } 

    // Player'ı yatay yönde hareket ettirir.
    private void MovePlayer()
    {
        // Player'ın ulaşmak istediği yatay hızı hesaplar.
        float targetHorizontalVelocity =
            horizontalInput * moveSpeed * movementSpeedMultiplier;

        if (isSlippery)
            targetHorizontalVelocity *= activeSlipperySpeedMultiplier;

        // Kayganlık etkisi kapalıysa normal hareket uygular.
        if (!isSlippery)
        {
            rb.linearVelocity = new Vector2(
                targetHorizontalVelocity,
                rb.linearVelocity.y
            );

            return;
        }

        // Tuşa basılıyken hızlanma, bırakıldığında yavaşlama değerini seçer.
        float movementRate =
            Mathf.Abs(horizontalInput) > 0.01f
                ? activeSlipperyAcceleration
                : activeSlipperyDeceleration;

        // Mevcut hızı hedef hıza yavaşça yaklaştırır.
        float newHorizontalVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetHorizontalVelocity,
            movementRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(
            newHorizontalVelocity,
            rb.linearVelocity.y
        );
    }

    // Başka scriptlerin Player'ın hareket hızını değiştirmesini sağlar.
    public void SetMovementSpeedMultiplier(float multiplier)
    {
        // Hız çarpanının negatif olmasını engeller.
        movementSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    // Başka scriptlerin kaygan hareket durumunu değiştirmesini sağlar.
    public void SetSlipperyMovement(bool slippery)
    {
        isSlippery = slippery;

        if (!slippery)
        {
            activeSlipperyAcceleration = slipperyAcceleration;
            activeSlipperyDeceleration = slipperyDeceleration;
            activeSlipperySpeedMultiplier = 1f;
        }
    }

    public void SetSlipperyMovement(bool slippery, float acceleration, float deceleration, float speedMultiplier)
    {
        isSlippery = slippery;
        activeSlipperyAcceleration = Mathf.Max(0.1f, acceleration);
        activeSlipperyDeceleration = Mathf.Max(0.05f, deceleration);
        activeSlipperySpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.25f, 2f);

        if (!slippery)
            SetSlipperyMovement(false);
    }

    // Player'a yukarı yönlü zıplama hızı verir.
    private void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            gravityMultiplier < 0f
                ? -jumpForce * invertedJumpMultiplier
                : jumpForce
        );

        // Aynı girişle tekrar zıplanmasını engeller.
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    // Yükselirken ve düşerken farklı yer çekimi uygular.
    private void ApplyBetterGravity()
    {
        bool movingWithGravity = gravityMultiplier < 0f
            ? rb.linearVelocity.y > 0f
            : rb.linearVelocity.y < 0f;
        float selectedGravity =
            movingWithGravity
                ? fallingGravity
                : normalGravity;

        // Trap tarafından belirlenen çarpanla yer çekimini değiştirir.
        rb.gravityScale = selectedGravity * gravityMultiplier;

        // Player'ın aşırı yüksek düşme hızına ulaşmasını engeller.
        if (rb.linearVelocity.y < -maximumFallSpeed)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                -maximumFallSpeed
            );
        }
        else if (rb.linearVelocity.y > maximumFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maximumFallSpeed);
        }
    }

    // Başka scriptlerin Player'ın yer çekimi çarpanını değiştirmesini sağlar.
    public void SetGravityMultiplier(float multiplier)
    {
        gravityMultiplier = Mathf.Approximately(multiplier, 0f) ? 1f : multiplier;
    }

    // Zemin kontrol alanını Scene ekranında gösterir.
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D colliderToDraw = GetComponent<BoxCollider2D>();

        if (colliderToDraw == null)
            return;

        Bounds bounds = colliderToDraw.bounds;

        Vector2 checkPosition = new Vector2(
            bounds.center.x,
            bounds.min.y - groundCheckHeight / 2f
        );

        Vector2 checkSize = new Vector2(
            bounds.size.x * groundCheckWidthMultiplier,
            groundCheckHeight
        );

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(checkPosition, checkSize);
    }
}
