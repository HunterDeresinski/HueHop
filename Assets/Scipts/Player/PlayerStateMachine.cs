using UnityEngine;

public enum PlayerState
{
    Idle            = 0,  // Idle state
    WindUp          = 1,  // Starting drag
    Jumping         = 2,  // Launching (up)
    JumpingForward  = 3,  // Launching with horizontal movement
    Falling         = 4,  // Falling down (vertical)
    Landing         = 5,  // Landing on ground
    Spitting        = 6,  // Spitting attack
    SplatWall       = 7,  // Grabbing walls
    Injured         = 8,  // Taking damage
    Recovering      = 9,  // Recovering from damage
    Dead            = 10, // Dead
    Spiking         = 11, // Spiking attack
    FallingForward  = 12  // Falling down with horizontal movement
}

[RequireComponent(typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField] private PlayerState _currentState = PlayerState.Idle;
    [SerializeField] private PlayerState _previousState = PlayerState.Idle;

    [Header("Animation Parameters")]
    [SerializeField] private string _stateParameter = "State";
    [SerializeField] private string _isGroundedParameter = "IsGrounded";
    [SerializeField] private string _isGrabbingParameter = "IsGrabbing";
    [SerializeField] private string _isDraggingParameter = "IsDragging";
    [SerializeField] private string _velocityXParameter = "VelocityX";
    [SerializeField] private string _velocityYParameter = "VelocityY";

    [Header("State Settings")]
    [SerializeField] private float _fallingThreshold = -0.1f;
    [SerializeField] private float _jumpingThreshold = 0.1f;
    [SerializeField] private float _groundedThreshold = 0.1f;
    [SerializeField] private float _landingDuration = 0.5f;

    [Header("Airborne Variant Thresholds")]
    [Tooltip("If abs(Vx) >= this when we start falling, we use FallForward.")]
    [SerializeField] private float _forwardEnterVX = 0.20f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask _groundMask;                        // set in Inspector (Tiles/Platforms), exclude Player
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private float _groundCheckOffsetY = -0.5f;

    [Header("Ground Debounce")]
    [SerializeField] private float _groundEnterDebounce = 0.02f;           // time grounded before we accept "grounded"
    [SerializeField] private float _groundExitDebounce  = 0.05f;           // time ungrounded before we accept "airborne"

    [Header("Landing Detection")]
    [SerializeField] private float _landingVelocityThreshold = -0.05f;     // must be moving down (<=) to count as landed

    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private PlayerController _playerController;

    // State tracking
    private bool _isGrounded = false;
    private bool _isGrabbing = false;
    private bool _isDragging = false;
    private bool _wasGrounded = false;
    private bool _justLanded = false;

    private float _stateTimer = 0f;
    private float _stateDuration = 0f;
    private float _lastStateChangeTime = 0f;
    private float _stateChangeCooldown = 0.05f;

    // debounce accumulators
    private float _groundedAccum = 0f;
    private float _ungroundedAccum = 0f;

    // ---- Fall variant lock (prevents Fall <-> FallForward toggling) ----
    private bool _fallVariantLocked = false;
    private bool _fallVariantIsForward = false;

    // Events
    public System.Action<PlayerState> OnStateChanged;
    public System.Action<PlayerState, PlayerState> OnStateTransition;

    public PlayerState CurrentState => _currentState;
    public PlayerState PreviousState => _previousState;
    public bool IsGrounded => _isGrounded;
    public bool IsGrabbing => _isGrabbing;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        UpdateAnimatorParameters();
        ChangeState(PlayerState.Idle);
    }

    void Update()
    {
        UpdateStateLogic();
        UpdateAnimatorParameters();
    }

    void UpdateStateLogic()
    {
        _stateTimer += Time.deltaTime;

        if (Time.time - _lastStateChangeTime >= _stateChangeCooldown)
        {
            PlayerState newState = DetermineNewState();

            if (newState != _currentState)
            {
                ChangeState(newState);
                _lastStateChangeTime = Time.time;
            }
        }
    }

    PlayerState DetermineNewState()
    {
        Vector2 velocity = _rigidbody.linearVelocity;

        // Update grounded with debounce
        _wasGrounded = _isGrounded;
        _isGrounded = CheckIfGrounded();

        // "Just Landed" edge detection (air -> ground and moving downward)
        _justLanded = (!_wasGrounded && _isGrounded && velocity.y <= _landingVelocityThreshold);

        if (_playerController != null)
        {
            _isGrabbing = _playerController.IsGrabbing;
            _isDragging = _playerController.IsDragging;
        }

        // Lock timed states
        if (_currentState == PlayerState.Landing ||
            _currentState == PlayerState.Injured ||
            _currentState == PlayerState.Recovering)
        {
            if (_stateTimer < _stateDuration) return _currentState;
        }

        float absVx = Mathf.Abs(velocity.x);

        // Global landing gate: enter Landing exactly once when touching down
        if (_justLanded && _currentState != PlayerState.Landing)
            return PlayerState.Landing;

        switch (_currentState)
        {
            case PlayerState.Idle:
                if (_isDragging) return PlayerState.WindUp;
                if (!_isGrounded)
                {
                    if (velocity.y > _jumpingThreshold)
                        return absVx >= _forwardEnterVX ? PlayerState.JumpingForward : PlayerState.Jumping;
                    else
                        return absVx >= _forwardEnterVX ? PlayerState.FallingForward : PlayerState.Falling;
                }
                if (_isGrabbing) return PlayerState.SplatWall;
                break;

            case PlayerState.WindUp:
                if (!_isDragging) return PlayerState.Jumping; // launch begins as Jumping (up)
                break;

            case PlayerState.Jumping:
                if (_isGrounded) return PlayerState.Landing;
                if (velocity.y <= _fallingThreshold)
                {
                    // Pick fall variant ONCE when we first start falling
                    return absVx >= _forwardEnterVX ? PlayerState.FallingForward : PlayerState.Falling;
                }
                if (absVx >= _forwardEnterVX) return PlayerState.JumpingForward;
                break;

            case PlayerState.JumpingForward:
                if (_isGrounded) return PlayerState.Landing;
                if (velocity.y <= _fallingThreshold)
                {
                    // Pick fall variant ONCE when we first start falling
                    return absVx >= _forwardEnterVX ? PlayerState.FallingForward : PlayerState.Falling;
                }
                if (absVx < _forwardEnterVX * 0.5f) return PlayerState.Jumping; // optional: drop back if nearly zero
                break;

            case PlayerState.Falling:
                // While falling, DO NOT switch to FallForward; wait for landing or jump re-entry
                if (_isGrounded) return PlayerState.Landing;
                if (velocity.y > _jumpingThreshold)
                    return absVx >= _forwardEnterVX ? PlayerState.JumpingForward : PlayerState.Jumping;
                break;

            case PlayerState.FallingForward:
                // While forward-falling, DO NOT switch back to Fall
                if (_isGrounded) return PlayerState.Landing;
                if (velocity.y > _jumpingThreshold)
                    return absVx >= _forwardEnterVX ? PlayerState.JumpingForward : PlayerState.Jumping;
                break;

            case PlayerState.Landing:
                // Stay in Landing until the animation finishes. No early escape on brief ungrounded flicker.
                if (_stateTimer >= _stateDuration) return PlayerState.Idle;
                return _currentState;

            case PlayerState.SplatWall:
                if (!_isGrabbing)
                {
                    if (_isGrounded) return PlayerState.Landing;

                    if (velocity.y > _jumpingThreshold)
                        return absVx >= _forwardEnterVX ? PlayerState.JumpingForward : PlayerState.Jumping;
                    else
                        return absVx >= _forwardEnterVX ? PlayerState.FallingForward : PlayerState.Falling;
                }
                break;

            case PlayerState.Injured:
                if (_stateTimer >= _stateDuration) return PlayerState.Recovering;
                break;

            case PlayerState.Recovering:
                if (_stateTimer >= _stateDuration) return PlayerState.Idle;
                break;

            case PlayerState.Dead:
                return PlayerState.Dead;
        }

        return _currentState;
    }

    bool CheckIfGrounded()
    {
        // Overlap box just under the feet (debounced)
        Vector2 center = (Vector2)transform.position + new Vector2(0f, _groundCheckOffsetY);
        bool rawGrounded = Physics2D.OverlapBox(center, _groundCheckSize, 0f, _groundMask) != null;

        if (rawGrounded)
        {
            _groundedAccum += Time.deltaTime;
            _ungroundedAccum = 0f;

            if (!_isGrounded && _groundedAccum >= _groundEnterDebounce)
                _isGrounded = true;
        }
        else
        {
            _ungroundedAccum += Time.deltaTime;
            _groundedAccum = 0f;

            if (_isGrounded && _ungroundedAccum >= _groundExitDebounce)
                _isGrounded = false;
        }

        // Also prevent “grounded” while clearly moving upward fast
        float groundedVelocityThreshold = _groundedThreshold + 0.05f;
        if (_isGrounded && _rigidbody.linearVelocity.y > groundedVelocityThreshold)
            _isGrounded = false;

        return _isGrounded;
    }

    // --- Animator control helpers ---

    private string GetAnimatorStateName(PlayerState s)
    {
        // Map enum to Animator state names (match your controller)
        switch (s)
        {
            case PlayerState.Idle:            return "Idle";
            case PlayerState.WindUp:          return "WindUp";
            case PlayerState.Jumping:         return "Jump";
            case PlayerState.JumpingForward:  return "JumpForward";
            case PlayerState.Falling:         return "Fall";
            case PlayerState.FallingForward:  return "FallForward";
            case PlayerState.Landing:         return "Land";
            case PlayerState.Spitting:        return "Spit";
            case PlayerState.SplatWall:       return "SplatWall";
            case PlayerState.Injured:         return "Injured";
            case PlayerState.Recovering:      return "Recover";
            case PlayerState.Dead:            return "Death";
            case PlayerState.Spiking:         return "Spike";
            default:                          return "Idle";
        }
    }

    public void ChangeState(PlayerState newState)
    {
        if (newState == _currentState) return;

        _previousState = _currentState;
        _currentState = newState;
        _stateTimer = 0f;

        // Lock/unlock fall variant at state boundaries
        if (newState == PlayerState.Falling || newState == PlayerState.FallingForward)
        {
            if (!_fallVariantLocked)
            {
                _fallVariantLocked = true;
                _fallVariantIsForward = Mathf.Abs(_rigidbody.linearVelocity.x) >= _forwardEnterVX;
            }
        }
        else if (newState == PlayerState.Landing || newState == PlayerState.Idle)
        {
            _fallVariantLocked = false; // release lock when we touch down / settle
        }

        switch (newState)
        {
            case PlayerState.Injured:
                _stateDuration = 1.0f;
                break;
            case PlayerState.Recovering:
                _stateDuration = 0.5f;
                break;
            case PlayerState.Landing:
                _stateDuration = _landingDuration;
                // kill residual downward motion to avoid immediate pop back to Falling
                if (_rigidbody.linearVelocity.y < 0f)
                    _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, 0f);
                break;
            default:
                _stateDuration = 0f;
                break;
        }

        // Hard-drive the Animator to this state immediately
        if (_animator)
        {
            _animator.SetInteger(_stateParameter, (int)_currentState);
            _animator.CrossFade(GetAnimatorStateName(_currentState), 0.06f, 0, 0f);
        }

        OnStateChanged?.Invoke(newState);
        OnStateTransition?.Invoke(_previousState, newState);
        //Debug.Log($"Player State: {_previousState} -> {newState}");
    }

    void UpdateAnimatorState()
    {
        if (_animator == null)
        {
            Debug.LogWarning("Animator component not found on PlayerStateMachine!");
            return;
        }

        // Keep parameter in sync every frame
        int currentStateValue = _animator.GetInteger(_stateParameter);
        int newStateValue = (int)_currentState;
        if (currentStateValue != newStateValue)
            _animator.SetInteger(_stateParameter, newStateValue);

        if (Debug.isDebugBuild)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            string currentStateName =
                info.IsName("Idle")         ? "Idle" :
                info.IsName("WindUp")       ? "WindUp" :
                info.IsName("Jump")         ? "Jump" :
                info.IsName("JumpForward")  ? "JumpForward" :
                info.IsName("Fall")         ? "Fall" :
                info.IsName("FallForward")  ? "FallForward" :
                info.IsName("Land")         ? "Land" :
                info.IsName("Spit")         ? "Spit" :
                info.IsName("SplatWall")    ? "SplatWall" :
                info.IsName("Injured")      ? "Injured" :
                info.IsName("Recover")      ? "Recover" :
                info.IsName("Death")        ? "Death" :
                info.IsName("Spike")        ? "Spike" :
                "Other/Unknown";

            // Debug.Log($"Animator Current State: {currentStateName}");
        }
    }

    void UpdateAnimatorParameters()
    {
        if (_animator == null)
        {
            Debug.LogWarning("Animator component not found on PlayerStateMachine!");
            return;
        }

        Vector2 velocity = _rigidbody.linearVelocity;

        bool currentGrounded = _animator.GetBool(_isGroundedParameter);
        bool currentGrabbing = _animator.GetBool(_isGrabbingParameter);
        bool currentDragging = _animator.GetBool(_isDraggingParameter);
        float currentVelocityX = _animator.GetFloat(_velocityXParameter);
        float currentVelocityY = _animator.GetFloat(_velocityYParameter);

        if (currentGrounded != _isGrounded)
        {
            _animator.SetBool(_isGroundedParameter, _isGrounded);
        }

        if (currentGrabbing != _isGrabbing)
            _animator.SetBool(_isGrabbingParameter, _isGrabbing);

        if (currentDragging != _isDragging)
            _animator.SetBool(_isDraggingParameter, _isDragging);

        if (Mathf.Abs(currentVelocityX - velocity.x) > 0.01f)
        {
            _animator.SetFloat(_velocityXParameter, velocity.x);
        }

        if (Mathf.Abs(currentVelocityY - velocity.y) > 0.01f)
        {
            _animator.SetFloat(_velocityYParameter, velocity.y);
        }
    }

    public void SetGrabbing(bool isGrabbing) => _isGrabbing = isGrabbing;

    public void TriggerSpit()
    {
        if (_currentState == PlayerState.Idle || _currentState == PlayerState.Jumping)
            ChangeState(PlayerState.Spitting);
    }

    public void TriggerSpike()
    {
        if (_currentState == PlayerState.Idle || _currentState == PlayerState.Jumping)
            ChangeState(PlayerState.Spiking);
    }

    public void TakeDamage()
    {
        if (_currentState != PlayerState.Dead && _currentState != PlayerState.Injured)
            ChangeState(PlayerState.Injured);
    }

    public void Die() => ChangeState(PlayerState.Dead);
    public void Respawn() => ChangeState(PlayerState.Idle);

    void OnDrawGizmos()
    {
        if (_rigidbody != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, Vector2.down * 0.6f);
        }
    }

    // visualize the overlap box in editor when selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = (Vector2)transform.position + new Vector2(0f, _groundCheckOffsetY);
        Gizmos.DrawWireCube(center, new Vector3(_groundCheckSize.x, _groundCheckSize.y, 0f));
    }

    public void SetGroundMask(LayerMask newMask)
    {
        _groundMask = newMask;
    }
}