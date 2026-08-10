using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Unity.Cinemachine; // Unity 6 Cinemachine kütüphanesi

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Health & Stats Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SyncVar(hook = nameof(OnHealthChanged))] private float currentHealth;

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
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch = 0.0f;

    private InteractableObject currentHeldItem;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;
    private float jumpAnimTimer = 0f;
    private bool cameraBound = false;

    // --- AĞ SENKRONİZASYONU İÇİN EKLENEN ANİMASYON AĞ DEĞİŞKENLERİ ---
    [SyncVar(hook = nameof(OnSpeedChanged))] private float networkSpeed = 0f;
    [SyncVar(hook = nameof(OnJumpChanged))] private float networkJump = 0f;

    private static readonly int GetHitHash = Animator.StringToHash("GetHit");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        TryBindCamera();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        // Otomatik UI Bulma Koruması
        if (crosshairUI == null) crosshairUI = GameObject.Find("CrosshairUI");
        if (chargeBarUI == null) chargeBarUI = GameObject.Find("ChargeBarUI");
        if (chargeBarUI != null && chargeBarFillImage == null)
        {
            chargeBarFillImage = chargeBarUI.GetComponentInChildren<Image>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (chargeBarUI != null) chargeBarUI.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (!cameraBound)
        {
            TryBindCamera();
        }

        HandleGrounded();
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleInteraction();
        HandleCombatAndThrow();
        ApplyGravity();
    }

    private void TryBindCamera()
    {
        if (cameraSocket == null) return;

        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();

        if (vcam != null)
        {
            vcam.Target.TrackingTarget = cameraSocket;
            vcam.Target.LookAtTarget = cameraSocket;

            // Kamera Clipping engelleme ayarı
            vcam.Lens.NearClipPlane = 0.01f;

            cameraTransform = vcam.transform;
            cameraBound = true;
            Debug.Log("[Cinemachine] Unity 6 Kamerası başarıyla kilitlendi!");
        }
    }

    // --- CAN VE HASAR SENKRONİZASYONU ---
    public void TakeDamage(float damage)
    {
        if (!isServer) return; // Hasar hesabını sunucu kontrol eder

        currentHealth -= damage;
        RpcOnGetHit();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [ClientRpc]
    private void RpcOnGetHit()
    {
        if (animator != null)
        {
            animator.SetTrigger(GetHitHash);
        }
        DropItem();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"[Combat] {gameObject.name} Kalan Can: {newHealth}");
    }

    private void Die()
    {
        Debug.Log($"[Combat] {gameObject.name} Öldü!");
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
        float targetSpeed = 0.0f;

        if (isMoving)
        {
            Vector3 moveDirection = (transform.forward * inputZ + transform.right * inputX).normalized;
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            targetSpeed = isRunning ? 2.0f : 1.0f;
        }

        // Hız değiştiyse sunucuya ilet
        if (Mathf.Abs(networkSpeed - targetSpeed) > 0.05f)
        {
            CmdUpdateSpeed(targetSpeed);
        }
    }

    [Command]
    private void CmdUpdateSpeed(float newSpeed)
    {
        networkSpeed = newSpeed;
    }

    private void OnSpeedChanged(float oldSpeed, float newSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", newSpeed);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpAnimTimer = 0.5f;
        }

        float jumpVal = 0.0f;
        if (jumpAnimTimer > 0)
        {
            jumpAnimTimer -= Time.deltaTime;
            jumpVal = 1.0f;
        }

        if (Mathf.Abs(networkJump - jumpVal) > 0.05f)
        {
            CmdUpdateJump(jumpVal);
        }
    }

    [Command]
    private void CmdUpdateJump(float newJump)
    {
        networkJump = newJump;
    }

    private void OnJumpChanged(float oldJump, float newJump)
    {
        if (animator != null)
        {
            animator.SetFloat("Jump", newJump);
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
        if (cameraTransform == null) return;

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
        if (trajectoryLine == null || cameraTransform == null) return;

        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
        Vector3 startPos = cameraTransform.position + cameraTransform.forward * 1.0f;
        Vector3 startVelocity = cameraTransform.forward * throwForce;

        int pointsCount = 30;
        trajectoryLine.positionCount = pointsCount;

        for (int i = 0; i < pointsCount; i++)
        {
            float time = i * 0.05f;
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
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                targetPlayer.TakeDamage(15f);
            }
        }
    }

    private void PerformThrow()
    {
        if (cameraTransform == null) return;

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