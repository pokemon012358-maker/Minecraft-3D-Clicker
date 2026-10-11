using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    InputSystem_Actions _input;
    Vector2 dir;

    public CharacterController cc;
    public Transform cam;

    public float gravity = -9.81f;
    private float velocityY;
    private Animator animator;

    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpHeight = 2f;
    public GameObject TiaSang;
    public GameObject Color;
    public GameObject Push;

    public string isWalkingParam = "IsWalking";
    public string isRunningParam = "IsRunning";
    public string speedParam = "Speed";



    private bool isJumping;

    private void OnEnable()
    {
        _input = new InputSystem_Actions();
        _input.Enable();
        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;
        _input.Player.Jump.performed += OnJump;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;
        _input.Player.Jump.performed -= OnJump;
        _input.Disable();
    }

    void OnMove(InputAction.CallbackContext context)
    {
        dir = context.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (!cc.isGrounded) return;

        velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
        isJumping = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Power"))
        {
            StartCoroutine(TiaSangRoutine());
        }
        if (other.CompareTag("Color"))
        {
            StartCoroutine(ColorRoutine());
        }
    }

    IEnumerator TiaSangRoutine()
    {
        TiaSang.SetActive(true);

        yield return new WaitForSeconds(2f);

        TiaSang.SetActive(false);
    }

    IEnumerator ColorRoutine()
    {
        Color.SetActive(true);

        yield return new WaitForSeconds(2f);

        Color.SetActive(false);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            Vector3 direction =
                transform.position - hit.transform.position;

            direction.y = 0;

            if (direction == Vector3.zero)
                direction = transform.forward;

            direction.Normalize();

            velocityY = 8f;
            cc.Move(direction * 8f * Time.deltaTime);
            Debug.Log("Hit Obstacle");

            StartCoroutine(Bounce());

            IEnumerator Bounce()
            {
                Push.SetActive(true);

                yield return new WaitForSeconds(1f);

                Push.SetActive(false);
            }
        }
    }


    void Update()
    {
        Vector3 camF = cam.forward;
        Vector3 camR = cam.right;
        camF.y = 0;
        camR.y = 0;
        camF.Normalize();
        camR.Normalize();

        Vector3 moveDirection = (camF * dir.y + camR * dir.x);
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        bool isRunning = _input.Player.Sprint.IsPressed();
        float currentSpeed = 0f;

        if (dir.magnitude > 0)
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
            

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        else
        {
            currentSpeed = 0f;
            moveDirection = Vector3.zero;

        }


        Vector3 velocity = moveDirection * currentSpeed;

        bool isMoving = dir.magnitude > 0;
        bool isWalkingNow = isMoving && !isRunning;
        bool isRunningNow = isMoving && isRunning;

        if (animator != null)
        {
            animator.SetBool(isWalkingParam, isWalkingNow);
            animator.SetBool(isRunningParam, isRunningNow);
            animator.SetFloat(speedParam, currentSpeed);
        }

        if (cc.isGrounded && velocityY < 0)
        {
            velocityY = -2f;
            isJumping = false;
        }
        else
        {
            velocityY += gravity * Time.deltaTime;
        }

        velocity.y = velocityY;
        cc.Move(velocity * Time.deltaTime);
    }
}