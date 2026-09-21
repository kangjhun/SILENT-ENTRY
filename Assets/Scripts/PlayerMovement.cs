using UnityEngine;

[RequireComponent (typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Transform _camerTransform;

    [SerializeField] float _walkSpeed = 3f;
    [SerializeField] float _runSpeed = 6f;
    [SerializeField] float _rotationSpeed = 540f;

    [SerializeField] float _gravity = -20f;

    CharacterController _controller;
    float _verticalSpeed;

    public float MoveSpeed { get; private set; }
    public bool IsRunning {  get; private set; }
    public Vector2 AnimationMove {  get; private set; }
     void Awake()
    {
        _controller = GetComponent<CharacterController> ();
    }
    void Update()
    {
        if (_camerTransform == null) return;

        Vector2 input = Vector2.zero;

        bool runPressed = false;

        if(Cursor.lockState == CursorLockMode.Locked)
        {
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            runPressed = Input.GetKey(KeyCode.LeftShift);
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = Vector3.ProjectOnPlane(_camerTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up,forward);

        Vector3 moveDirection = forward * input.y + right * input.x;

        bool hasInput = input.sqrMagnitude > 0.001f;

        bool wantsRun = runPressed && input.y > 0f && Mathf.Abs(input.x) < 0.01f;
        float speed = runPressed ? _runSpeed : _walkSpeed;

        if (Cursor.lockState == CursorLockMode.Locked && forward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        if(_controller.isGrounded && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        _verticalSpeed += _gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed;

        velocity.y = _verticalSpeed;

        Vector3 beforePosition = transform.position;

        CollisionFlags flags = _controller.Move(velocity * Time.deltaTime);

        if((flags & CollisionFlags.Below) !=0 && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        Vector3 moved = transform.position - beforePosition;
        moved.y = 0f;

        MoveSpeed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;

        IsRunning = runPressed && hasInput && MoveSpeed > 0.1f;
        Vector3 worldVelocity = Time.deltaTime > 0f ? moved / Time.deltaTime : Vector3.zero;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        AnimationMove = Vector2.ClampMagnitude(new Vector2(localVelocity.x, localVelocity.z) / Mathf.Max(_walkSpeed, 0.01f), 1f);

    }
}
