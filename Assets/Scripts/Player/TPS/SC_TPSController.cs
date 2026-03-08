using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SC_TPSController : MonoBehaviour
{
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
    private ModelParent modelParent;
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
        modelParent = GetComponentInChildren<ModelParent>();

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
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

         
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude > 0)
        {
            for (int i = 0; i < modelParent.childModel.Count; i++)
                if (modelParent.childModel[i].name == "LegsR" || modelParent.childModel[i].name == "LegsL")
                {
                    modelParent.childModel[i].GetComponentInChildren<Animator>().Play("Run");
                    modelParent.childModel[i].GetComponentInChildren<Animator>().SetFloat("Speed", isRunning ? 1.2f : 0.8f);
                }
        }
        else
        {
            for (int i = 0; i < modelParent.childModel.Count; i++)
                if (modelParent.childModel[i].name == "LegsR" || modelParent.childModel[i].name == "LegsL")
                    modelParent.childModel[i].GetComponentInChildren<Animator>().Play("Idle");
        }

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + currentYRotation;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                rotationSmoothTime
            );
            if(!isAttack)
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

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = jumpForce;
        }

        verticalVelocity -= gravity * Time.deltaTime;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
    private void DealDamage()
    {
        if (getAttack == true)
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
    private Animator GetHandAnimator(string handName)
    {
        for (int i = 0; i < modelParent.childModel.Count; i++)
        {
            if (modelParent.childModel[i].name == handName)
            {
                return modelParent.childModel[i]
                    .GetComponentInChildren<Animator>();
            }
        }
        return null;
    }
    private IEnumerator RotateToCamera()
    {
        isAttack = true;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, currentYRotation, 0f);

        float time = 0f;
        float duration = 0.15f; // скорость поворота

        while (time < duration)
        {
            time += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, time / duration);
            yield return null;
        }

        transform.rotation = targetRot;
        yield return new WaitForSeconds(0.5f);
        isAttack = false;
    }
    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        int j = Random.Range(0, 2);

        Animator targetAnimator = null;

        if (j == 0)
        {
            targetAnimator = GetHandAnimator("HandL");
            if (targetAnimator != null)
                targetAnimator.Play("Attack1");
        }
        else
        {
            string randomHandR = "HandR";
            targetAnimator = GetHandAnimator(randomHandR);

            if (targetAnimator != null)
                targetAnimator.Play("Attack2");
            string randomHandL = "HandL";
            targetAnimator = GetHandAnimator(randomHandL);

            if (targetAnimator != null)
                targetAnimator.Play("Attack2");
        }
        StartCoroutine(RotateToCamera());
        // Задержка перед нанесением урона
        yield return new WaitForSeconds(attackDelay);

        getAttack = true;

        // Кулдаун
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

}