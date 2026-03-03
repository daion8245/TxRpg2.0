using UnityEngine;
using UnityEngine.InputSystem;

public class VillagePlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Characters character = Characters.Knight;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private float _moveInput;
    private Animator _animator;
    private bool _isGrounded;

    private void Awake()
    {
        _rb = GetComponentInChildren<Rigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        _rb.linearVelocity = new Vector2(_moveInput * speed, _rb.linearVelocity.y);

        if (_moveInput != 0)
        {
            _spriteRenderer.flipX = _moveInput < 0;
        }

        // 매 프레임 바닥 체크 갱신
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        AnimationSet();
    }

    private void AnimationSet()
    {
        AnimationStates state;

        if (!_isGrounded)
        {
            // 공중에 있으면 무조건 Jump
            state = AnimationStates.Jump;
        }
        else if (Mathf.Abs(_moveInput) > 0.01f)
        {
            // 바닥 + 입력 있으면 Walk
            state = AnimationStates.Walk;
        }
        else
        {
            // 바닥 + 입력 없으면 Idle
            state = AnimationStates.Idle;
        }

        _animator.Play(AnimationManager.GetAnimation(character, state));
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer))
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }
}
