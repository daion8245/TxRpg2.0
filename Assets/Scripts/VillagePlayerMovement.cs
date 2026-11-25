using UnityEngine;
using UnityEngine.InputSystem;

public class VillagePlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.22f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Characters character = Characters.Knight;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private float _moveInput;
    private bool _usingActionInput;
    private bool _isGrounded;

    public int FacingDir { get; private set; } = 1;
    public Vector2 BodyCenter => _rb != null ? _rb.position : (Vector2)transform.position;

    private void Awake()
    {
        _rb = GetComponentInChildren<Rigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_rb == null) return;

        float horizontal = _moveInput;
        var kb = Keyboard.current;
        if (!_usingActionInput && kb != null)
        {
            horizontal = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
        }

        _rb.linearVelocity = new Vector2(horizontal * speed, _rb.linearVelocity.y);

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            FacingDir = horizontal < 0f ? -1 : 1;
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = horizontal < 0f;
        }

        Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : _rb.position + Vector2.down * 0.65f;
        _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        if (!_usingActionInput && kb != null && kb.spaceKey.wasPressedThisFrame && _isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }

        AnimationSet(horizontal);
    }

    private void AnimationSet(float horizontal)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        AnimationStates state;
        if (!_isGrounded) state = AnimationStates.Jump;
        else if (Mathf.Abs(horizontal) > 0.01f) state = AnimationStates.Walk;
        else state = AnimationStates.Idle;

        _animator.Play(AnimationManager.GetAnimation(character, state));
    }

    public void TeleportTo(Vector2 worldPos)
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.position = worldPos;
            _rb.transform.position = worldPos;
        }
        else
        {
            transform.position = worldPos;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _usingActionInput = true;
        _moveInput = context.ReadValue<Vector2>().x;
        if (context.canceled) _usingActionInput = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded && _rb != null)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }

    // PlayerInput SendMessages 모드 호환 오버로드
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>().x;
        _usingActionInput = Mathf.Abs(_moveInput) > 0.01f;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && _isGrounded && _rb != null)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }
}
