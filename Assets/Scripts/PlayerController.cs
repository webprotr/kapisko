using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Multiplayer / Local Player Settings")]
    public bool isLocalPlayer = true;

    [Header("Health & Stats Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Look / Camera Settings")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private Transform cameraSocket;
    [SerializeField] private float topClamp = 85.0f;
    [SerializeField] private float bottomClamp = -85.0f;

    [Header("Interaction & Throw Settings")]
    [SerializeField] private Transform holdSocket;          
    [SerializeField] private float pickupRange = 3.0f;        
    [SerializeField] private float minThrowForce = 12f;       
    [SerializeField] private float maxThrowForce = 35f;       
    [SerializeField] private float maxChargeTime = 1.2f;      
    [SerializeField] private float attackRange = 2.5f;        

    [Header("UI & Visual References")]
    [SerializeField] private GameObject crosshairUI;
    [SerializeField] private GameObject chargeBarUI;
    [SerializeField] private Image chargeBarFillImage;
    [SerializeField] private LineRenderer trajectoryLine; // Atış Yörünge Çizgisi

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject localCameraObject;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch = 0.0f;

    private InteractableObject currentHeldItem;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;
    private float jumpAnimTimer = 0f;

    // Animator Hash (Performans için hafızada tutuyoruz)
    private static readonly int GetHitHash = Animator.StringToHash("GetHit");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            if (localCameraObject != null) localCameraObject.SetActive(false);
            return;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (chargeBarUI != null) chargeBarUI.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleGrounded();
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleInteraction();
        HandleCombatAndThrow();
        ApplyGravity();
    }

    // --- HASAR VE DARBE ALMA MEKANİĞİ ---
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Combat] {gameObject.name} {damage} hasar aldı! Kalan Can: {currentHealth}");

        // 1. Darbe Alma Animasyonunu Tetikle
        if (animator != null)
        {
            animator.SetTrigger(GetHitHash);
        }

        // 2. Darbe yediği için elindeki eşya otomatik yere düşer
        DropItem();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[Combat] {gameObject.name} Öldü!");
        // Ölüm durumunda yapılacaklar (Respawn / Ragdoll vs.) buraya eklenebilir
    }

    private void HandleGrounded()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, bottomClamp, topClamp);

        if (cameraSocket != null)
        {
            cameraSocket.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ).normalized;
        bool isMoving = inputDir.magnitude > 0.1f;

        if (isMoving)
        {
            Vector3 moveDirection = (transform.forward * inputZ + transform.right * inputX).normalized;
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            float targetSpeed = isRunning ? 2.0f : 1.0f;
            if (animator != null) animator.SetFloat("Speed", targetSpeed);
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0.0f);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpAnimTimer = 0.5f;
        }

        if (animator != null)
        {
            if (jumpAnimTimer > 0)
            {
                jumpAnimTimer -= Time.deltaTime;
                animator.SetFloat("Jump", 1.0f); 
            }
            else
            {
                animator.SetFloat("Jump", 0.0f); 
            }
        }
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentHeldItem != null)
            {
                DropItem();
            }
            else
            {
                TryPickupItem();
            }
        }
    }

    private void TryPickupItem()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            InteractableObject item = hit.collider.GetComponent<InteractableObject>();
            if (item != null && !item.isHeld)
            {
                currentHeldItem = item;
                currentHeldItem.OnPickedUp(holdSocket, this);
            }
        }
    }

    public void DropItem()
    {
        if (currentHeldItem != null)
        {
            currentHeldItem.OnDropped();
            currentHeldItem = null;
        }
    }

    private void HandleCombatAndThrow()
    {
        if (currentHeldItem == null)
        {
            if (isChargingThrow) ResetChargeUI();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isChargingThrow = true;
            throwChargeTimer = 0f;

            // Tıklandığı an barı hemen 0'a çekiyoruz
            if (chargeBarFillImage != null) chargeBarFillImage.fillAmount = 0f;

            if (crosshairUI != null) crosshairUI.SetActive(true);
            if (chargeBarUI != null) chargeBarUI.SetActive(true);
            if (trajectoryLine != null) trajectoryLine.enabled = true;
        }

        if (Input.GetMouseButton(0) && isChargingThrow)
        {
            throwChargeTimer += Time.deltaTime;
            float chargeRatio = Mathf.Clamp01(throwChargeTimer / maxChargeTime);

            if (chargeBarFillImage != null)
            {
                chargeBarFillImage.fillAmount = chargeRatio;
            }

            // Atış yörüngesini ekranda çiz
            DrawTrajectory(chargeRatio);
        }

        if (Input.GetMouseButtonUp(0) && isChargingThrow)
        {
            isChargingThrow = false;

            if (throwChargeTimer < 0.2f)
            {
                PerformMeleeAttack();
            }
            else
            {
                PerformThrow();
            }

            ResetChargeUI();
        }
    }

    private void DrawTrajectory(float chargeRatio)
    {
        if (trajectoryLine == null) return;

        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
        Vector3 startPos = cameraTransform.position + cameraTransform.forward * 1.0f;
        Vector3 startVelocity = cameraTransform.forward * throwForce;

        int pointsCount = 30;
        trajectoryLine.positionCount = pointsCount;

        for (int i = 0; i < pointsCount; i++)
        {
            float time = i * 0.05f;
            // Fizik formülü: Pos = StartPos + Velocity * t + 1/2 * Gravity * t^2
            Vector3 point = startPos + startVelocity * time + 0.5f * Physics.gravity * time * time;
            trajectoryLine.SetPosition(i, point);
        }
    }

    private void ResetChargeUI()
    {
        isChargingThrow = false;
        throwChargeTimer = 0f;

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (chargeBarUI != null) chargeBarUI.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (chargeBarFillImage != null) chargeBarFillImage.fillAmount = 0f;
    }

    private void PerformMeleeAttack()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                // Yakın dövüş hasarı ver (Can düşürür, animasyonu tetikler ve elindeki eşyayı düşürür)
                targetPlayer.TakeDamage(15f);
            }
        }
    }

    private void PerformThrow()
    {
        float chargeRatio = Mathf.Clamp01(throwChargeTimer / maxChargeTime);
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

        Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * 1.0f;
        Vector3 throwDirection = cameraTransform.forward;

        InteractableObject itemToThrow = currentHeldItem;
        currentHeldItem = null;

        itemToThrow.Throw(spawnPosition, throwDirection * throwForce, controller);
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}