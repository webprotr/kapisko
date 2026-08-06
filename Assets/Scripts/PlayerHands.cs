using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHands : MonoBehaviour
{
    [Header("Arm References")]
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;

    [Header("Reach Positions")]
    [SerializeField] private Vector3 leftReachPosition = new Vector3(-0.25f, -0.12f, 1.15f);
    [SerializeField] private Vector3 rightReachPosition = new Vector3(0.25f, -0.12f, 1.15f);

    [Header("Settings")]
    [SerializeField] private float handMoveSpeed = 12f;

    [Header("Hand Points")]
    [SerializeField] private Transform holdPoint;

    private Vector3 leftRestPosition;
    private Vector3 rightRestPosition;

    private Renderer[] leftArmRenderers;
    private Renderer[] rightArmRenderers;

    // Şu an elde tutulan objenin referansı (Kameranın kopyalanıp kopmasını engellemek için)
    private Transform currentGrabbedTransform;

    private void Awake()
    {
        if (leftArm != null)
        {
            leftRestPosition = leftArm.localPosition;
            leftArmRenderers = leftArm.GetComponentsInChildren<Renderer>(true);
        }

        if (rightArm != null)
        {
            rightRestPosition = rightArm.localPosition;
            rightArmRenderers = rightArm.GetComponentsInChildren<Renderer>(true);
        }

        SetLeftArmVisible(false);
        SetRightArmVisible(false);
    }

    private void Update()
    {
        bool leftHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool rightHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;

        Vector3 leftTarget = leftHeld ? leftReachPosition : leftRestPosition;
        Vector3 rightTarget = rightHeld ? rightReachPosition : rightRestPosition;

        MoveArm(leftArm, leftTarget);
        MoveArm(rightArm, rightTarget);

        // Eğer elinde tuttuğun bir obje varsa kolları görünür kılabilirsin (İsteğe bağlı)
        SetLeftArmVisible(leftHeld);
        SetRightArmVisible(rightHeld);
    }

    private void MoveArm(Transform arm, Vector3 targetPosition)
    {
        if (arm == null) return;

        arm.localPosition = Vector3.Lerp(
            arm.localPosition,
            targetPosition,
            handMoveSpeed * Time.deltaTime
        );
    }

    public void Grab(Transform objectToGrab)
    {
        currentGrabbedTransform = objectToGrab;

        Transform targetParent = holdPoint != null ? holdPoint : this.transform;

        objectToGrab.SetParent(targetParent);
        objectToGrab.localPosition = Vector3.zero;
        objectToGrab.localRotation = Quaternion.identity;
    }

    public void Release()
    {
        // GÜVENLİ BIRAKMA: Tüm alt objeleri sökmek yerine SADECE eldeki nesneyi serbest bırakır.
        // Bu sayede kamera veya kollar asla kopmaz/kilitlenmez.
        if (currentGrabbedTransform != null)
        {
            Transform targetParent = holdPoint != null ? holdPoint : this.transform;

            // Eğer obje hâlâ bizim elimize bağlıysa ebeveynliğini sök
            if (currentGrabbedTransform.parent == targetParent)
            {
                currentGrabbedTransform.SetParent(null);
            }

            currentGrabbedTransform = null;
        }
    }

    public void SetLeftArmVisible(bool visible)
    {
        if (leftArmRenderers == null) return;

        foreach (Renderer armRenderer in leftArmRenderers)
        {
            armRenderer.enabled = visible;
        }
    }

    public void SetRightArmVisible(bool visible)
    {
        if (rightArmRenderers == null) return;

        foreach (Renderer armRenderer in rightArmRenderers)
        {
            armRenderer.enabled = visible;
        }
    }

    public void ShowBothArms()
    {
        SetLeftArmVisible(true);
        SetRightArmVisible(true);
    }

    public void HideBothArms()
    {
        SetLeftArmVisible(false);
        SetRightArmVisible(false);
    }
}