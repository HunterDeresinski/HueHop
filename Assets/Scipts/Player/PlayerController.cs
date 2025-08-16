using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Asset")]
    public InputActionAsset InputActions;

    [Header("Launch Settings")]
    public float launchPower = 2f;

    [Header("Trajectory Prediction")]
    public TrajectoryPredictor trajectoryPredictor;

    [Header("Grab Settings")]
    [SerializeField] private float _grabRadius = 1.5f;

    [Header("Custom Gravity Settings")]
    [SerializeField] private float _gravityAcceleration = 25f;
    [SerializeField] private float _maxFallSpeed = 15f;
    [SerializeField] private bool _useCustomGravity = true;

    [Header("State Machine")]
    [SerializeField] private PlayerStateMachine _stateMachine;

    [Header("Sprite / Animator")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [Tooltip("Animator Controllers per color (Green, Blue, Pink, Red). Optional; leave null to use sprite/tint fallback.")]
    [SerializeField] private RuntimeAnimatorController[] _animatorByColor = new RuntimeAnimatorController[4];
    [Tooltip("Optional fallback sprites per color (used only if no controller set).")]
    [SerializeField] private Sprite[] _spriteByColor = new Sprite[4];
    [Tooltip("Optional tint if no controller/sprite is assigned for a color.")]
    [SerializeField] private Color[] _tintByColor = new Color[4]
    {
        new Color(0.55f, 1f, 0.55f), // green-ish
        new Color(0.55f, 0.75f, 1f), // blue-ish
        new Color(1f, 0.55f, 0.85f), // pink-ish
        new Color(1f, 0.55f, 0.55f), // red-ish
    };

    [Header("Player Color")]
    [SerializeField] private PlayerColor _currentColor = PlayerColor.Green;
    public enum PlayerColor { Green, Blue, Pink, Red }

    [Tooltip("Layers must exist: Player_Green, Player_Blue, Player_Pink, Player_Red")]
    [SerializeField] private string[] _playerLayerByColor = new string[] { "Player_Green", "Player_Blue", "Player_Pink", "Player_Red" };

    [Tooltip("Ground masks that match each color's tilemap layers")]
    [SerializeField] private LayerMask[] _groundMaskByColor = new LayerMask[4];

    [Tooltip("Grabbable masks that match each color")]
    [SerializeField] private LayerMask[] _grabbableMaskByColor = new LayerMask[4];

    [Header("Pickup Tags (create these in Tags list)")]
    [SerializeField] private string _pickupGreenTag = "Pickup_Green";
    [SerializeField] private string _pickupBlueTag  = "Pickup_Blue";
    [SerializeField] private string _pickupPinkTag  = "Pickup_Pink";
    [SerializeField] private string _pickupRedTag   = "Pickup_Red";

    private InputAction _clickAction;
    private InputAction _positionAction;
    private InputAction _grabAction;

    private Vector2 _startScreenPos;
    private bool _isDragging = false;
    private bool _isGrabbing = false;
    private float _currentFallSpeed = 0f;

    private Rigidbody2D _rigidbody;
    private Camera _camera;
    private Animator _animator;

    public bool IsGrabbing => _isGrabbing;
    public bool IsDragging => _isDragging;
    public PlayerColor CurrentColor => _currentColor;

    private void OnClickStarted(InputAction.CallbackContext ctx) => StartDrag();
    private void OnClickCanceled(InputAction.CallbackContext ctx) => EndDrag();
    private void OnGrabStarted(InputAction.CallbackContext ctx) => TryGrab();
    private void OnGrabCanceled(InputAction.CallbackContext ctx) => ReleaseGrab();

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator  = GetComponent<Animator>();
        _camera = Camera.main;

        if (_stateMachine == null)
            _stateMachine = GetComponent<PlayerStateMachine>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        var playerMap = InputActions.FindActionMap("Player");
        _clickAction = playerMap.FindAction("Click");
        _positionAction = playerMap.FindAction("Position");
        _grabAction = playerMap.FindAction("Grab");

        // Prevent double gravity – we use custom gravity only
        _rigidbody.gravityScale = 0f;

        // Apply starting color
        SetPlayerColor(_currentColor);

        // Sync predictor with controller physics
        if (trajectoryPredictor != null)
        {
            trajectoryPredictor.useCustomGravity = _useCustomGravity;
            trajectoryPredictor.gravityAcceleration = _gravityAcceleration;
            trajectoryPredictor.maxFallSpeed = _maxFallSpeed;
            trajectoryPredictor.ascentGravityMultiplier = 0.3f;
            trajectoryPredictor.fallingStartEpsilon = 0.1f;

            // DAMPING: use the new linearDamping property
            trajectoryPredictor.useLinearDamping = true;
            trajectoryPredictor.linearDamping   = _rigidbody.linearDamping;

            // Avoid self-hits while standing
            trajectoryPredictor.useShapeCast = false;

            // give it the collider so it can offset the start above the feet
            if (trajectoryPredictor.playerCollider == null)
                trajectoryPredictor.playerCollider = GetComponent<Collider2D>();
        }
    }

    void OnEnable()
    {
        var map = InputActions.FindActionMap("Player");
        map.Enable();

        _clickAction.started += OnClickStarted;
        _clickAction.canceled += OnClickCanceled;
        _grabAction.started += OnGrabStarted;
        _grabAction.canceled += OnGrabCanceled;
    }

    void OnDisable()
    {
        _clickAction.started -= OnClickStarted;
        _clickAction.canceled -= OnClickCanceled;
        _grabAction.started -= OnGrabStarted;
        _grabAction.canceled -= OnGrabCanceled;

        InputActions.FindActionMap("Player").Disable();
    }

    void Update()
    {
        if (_isDragging && trajectoryPredictor != null)
        {
            Vector2 currentScreenPos = _positionAction.ReadValue<Vector2>();
            Vector2 dragVector = _startScreenPos - currentScreenPos;
            Vector2 launchVelocity = dragVector * launchPower * Time.fixedDeltaTime;
            trajectoryPredictor.DrawArc(_rigidbody.position, launchVelocity);
        }

        UpdateSpriteFlip();
    }

    void FixedUpdate()
    {
        if (_useCustomGravity && !_isGrabbing)
            ApplyCustomGravity();
    }

    void UpdateSpriteFlip()
    {
        float horizontalVelocity = _rigidbody.linearVelocity.x;

        if (horizontalVelocity > 0.05f)
            _spriteRenderer.flipX = false; // right
        else if (horizontalVelocity < -0.05f)
            _spriteRenderer.flipX = true;  // left
    }

    // === COLOR SWITCHING (public so pickups can call it) ===
    public void SetPlayerColor(PlayerColor color)
    {
        _currentColor = color;
        int i = (int)color;

        // 1) Switch layer (do it recursively so children follow)
        string layerName = (_playerLayerByColor != null && _playerLayerByColor.Length > i) ? _playerLayerByColor[i] : null;
        int layer = (!string.IsNullOrEmpty(layerName)) ? LayerMask.NameToLayer(layerName) : -1;
        if (layer == -1)
        {
            Debug.LogError($"Layer not found for color {color}. Check _playerLayerByColor.");
        }
        else
        {
            SetLayerRecursively(gameObject, layer);
        }

        // 2) Ground mask -> state machine
        if (_stateMachine != null && _groundMaskByColor != null && _groundMaskByColor.Length > i)
            _stateMachine.SetGroundMask(_groundMaskByColor[i]);

        // 3) Visual swap: Animator controller > Sprite > Tint fallback
        bool appliedVisual = false;

        if (_animator != null && _animatorByColor != null && _animatorByColor.Length > i && _animatorByColor[i] != null)
        {
            _animator.runtimeAnimatorController = _animatorByColor[i];
            appliedVisual = true;
            if (_spriteRenderer) _spriteRenderer.color = Color.white;
        }
        else if (_spriteRenderer != null && _spriteByColor != null && _spriteByColor.Length > i && _spriteByColor[i] != null)
        {
            _spriteRenderer.sprite = _spriteByColor[i];
            appliedVisual = true;
            _spriteRenderer.color = Color.white;
        }

        if (!appliedVisual && _spriteRenderer != null && _tintByColor != null && _tintByColor.Length > i)
        {
            _spriteRenderer.color = _tintByColor[i];
        }
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    void ApplyCustomGravity()
    {
        Vector2 v = _rigidbody.linearVelocity;

        if (v.y <= 0.1f)
        {
            _currentFallSpeed += _gravityAcceleration * Time.fixedDeltaTime;
            _currentFallSpeed = Mathf.Min(_currentFallSpeed, _maxFallSpeed);
            v.y = -_currentFallSpeed;
            _rigidbody.linearVelocity = v;
        }
        else
        {
            v.y -= _gravityAcceleration * 0.3f * Time.fixedDeltaTime;
            _rigidbody.linearVelocity = v;
            _currentFallSpeed = 0f;
        }
    }

    void StartDrag()
    {
        _startScreenPos = _positionAction.ReadValue<Vector2>();
        _isDragging = true;
        trajectoryPredictor?.DrawArc(_rigidbody.position, Vector2.zero);
    }

    void EndDrag()
    {
        Vector2 endScreenPos = _positionAction.ReadValue<Vector2>();
        Vector2 dragVector = _startScreenPos - endScreenPos;
        _isDragging = false;

        if (_isGrabbing)
            ReleaseGrab();

        _rigidbody.linearVelocity = dragVector * launchPower * Time.fixedDeltaTime;
        _currentFallSpeed = 0f;
        trajectoryPredictor?.HideArc();
    }

    void TryGrab()
    {
        int idx = (int)_currentColor;
        LayerMask grabMask = (_grabbableMaskByColor != null && _grabbableMaskByColor.Length > idx)
            ? _grabbableMaskByColor[idx] : ~0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_rigidbody.position, _grabRadius, grabMask);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Grabbable"))
            {
                _isGrabbing = true;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.gravityScale = 0;
                _currentFallSpeed = 0f;

                _stateMachine?.SetGrabbing(true);
                break;
            }
        }
    }

    void ReleaseGrab()
    {
        if (_isGrabbing)
        {
            _isGrabbing = false;
            _rigidbody.gravityScale = 0;
            _currentFallSpeed = 0f;
            _stateMachine?.SetGrabbing(false);
        }
    }

    // --- PICKUPS ---
    // Add these tags to your Project Settings > Tags and layer your pickups accordingly.
    // A pickup should have: Collider2D (isTrigger = true), one of the *_pickup tags below.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (TryHandleColorPickup(other.gameObject))
        {
            Destroy(other.gameObject);
        }
    }

    // In case your pickups use non-trigger colliders
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHandleColorPickup(collision.gameObject))
        {
            Destroy(collision.gameObject);
        }
    }

    private bool TryHandleColorPickup(GameObject go)
    {
        string tag = go.tag;
        if (string.IsNullOrEmpty(tag)) return false;

        if (tag == _pickupGreenTag)
        {
            SetPlayerColor(PlayerColor.Green);
            return true;
        }
        if (tag == _pickupBlueTag)
        {
            SetPlayerColor(PlayerColor.Blue);
            return true;
        }
        if (tag == _pickupPinkTag)
        {
            SetPlayerColor(PlayerColor.Pink);
            return true;
        }
        if (tag == _pickupRedTag)
        {
            SetPlayerColor(PlayerColor.Red);
            return true;
        }

        return false;
    }

    void OnDrawGizmos()
    {
        if (_rigidbody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_rigidbody.position, _grabRadius);
        }
    }
}
