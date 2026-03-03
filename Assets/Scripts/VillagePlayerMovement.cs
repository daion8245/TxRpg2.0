using UnityEngine;
using UnityEngine.InputSystem;

public class VillagePlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private float _moveInput;
    private Animator _animator;

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
