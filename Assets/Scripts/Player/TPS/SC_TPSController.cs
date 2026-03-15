using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SC_TPSController : MonoBehaviour
{

    [Space(20)]
    public float damage = 3;
    [SerializeField] private float hp = 25;

    [Space(20)]
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float airControlPercent = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Attack")]
    [SerializeField] float attackDelay = 0.4f;
    [SerializeField] float attackCooldown = 1.2f;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private LayerMask targetLayer;

    [Header("Camera")]
    [SerializeField] private Transform cameraParent;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 60f;

    private bool canAttack = true;

    private Animator anim;
    private CharacterController controller;
    private Vector3 velocity;
    private float verticalVelocity;
    private float currentYRotation;
    private float rotationVelocity;
    private float cameraXRotation;
    private bool isAttack = false;
    private bool getAttack = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCamera();
        HandleMovement();
        DealDamage();

        if (Input.GetButton("Fire1"))
            HandleAttack();

        if (Input.GetButton("Fire2"))
            isAttack = true;

        if (Input.GetButtonUp("Fire2"))
            isAttack = false;
    }

    private void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentYRotation += mouseX;

        cameraXRotation -= mouseY;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -verticalLookLimit, verticalLookLimit);

        cameraParent.localRotation = Quaternion.Euler(cameraXRotation, currentYRotation, 0f);
    }

    private void HandleAttack()
    {
        if (!canAttack)
            return;

        StartCoroutine(AttackRoutine());
    }

    private void HandleMovement()
    {
        float dt = Time.unscaledDeltaTime;

        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude > 0)
        {
            anim.SetBool("IsRunning", true);
            anim.SetFloat("Speed", isRunning ? 1f : 0.8f);
        }
        else
        {
            anim.SetBool("IsRunning", false);
        }

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (!isAttack)
        {
            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + currentYRotation;

                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref rotationVelocity,
                    rotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                float control = isGrounded ? 1f : airControlPercent;

                velocity.x = moveDir.x * targetSpeed * control;
                velocity.z = moveDir.z * targetSpeed * control;
            }
            else
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + currentYRotation;

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                float control = isGrounded ? 1f : airControlPercent;

                velocity.x = moveDir.x * targetSpeed * control;
                velocity.z = moveDir.z * targetSpeed * control;
            }
            else
            {

                velocity.x = 0f;
                velocity.z = 0f;
            }
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
            verticalVelocity = jumpForce;

        verticalVelocity -= gravity * dt;
        velocity.y = verticalVelocity;

        controller.Move(velocity * dt);
    }

    private void DealDamage()
    {
        if (getAttack)
        {
            Ray ray = new Ray(hitPoint.position, hitPoint.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 0.3f, targetLayer))
            {
                hit.collider.gameObject.SetActive(false);
                getAttack = false;
                return;
            }

            Debug.DrawRay(hitPoint.position, hitPoint.forward * 0.3f, Color.green);
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        int j = Random.Range(0, 2);

        if (j == 0)
            anim.Play("Attacke1");
        else
            anim.Play("Attacke2");

        yield return new WaitForSecondsRealtime(attackDelay);

        getAttack = true;

        yield return new WaitForSecondsRealtime(attackCooldown);

        canAttack = true;
    }
}