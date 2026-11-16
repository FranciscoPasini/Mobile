using UnityEngine;

/// <summary>
/// Legs play Rifle Walking on the base layer and face the movement direction.
/// Arms and spine play aiming / reload on an upper-body overlay, and the spine
/// twists toward the current fire target.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerWeaponSystem weapons;
    [SerializeField] private RuntimeAnimatorController controller;

    [Header("Aim")]
    [Tooltip("How far the torso can twist from the legs, in degrees.")]
    [SerializeField] private float maxTwist = 90f;
    [Tooltip("Corrects the aim clip if the rifle isn't pointing straight ahead in it.")]
    [SerializeField] private float aimYawOffset = 0f;
    [SerializeField] private float twistSpeed = 540f;
    [Tooltip("Degrees per second the whole body turns toward the target while standing still.")]
    [SerializeField] private float idleTurnSpeed = 360f;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Aiming = Animator.StringToHash("Aiming");
    private static readonly int Reload = Animator.StringToHash("Reload");

    private const int UpperLayer = 1;
    private const float MovingThreshold = 0.1f;

    private PlayerController moveSource;
    private Transform[] spineBones;
    private Quaternion[] lastWritten;
    private Quaternion[] lastAnimated;
    private float currentTwist;

    private void Awake()
    {
        if (weapons == null) weapons = GetComponent<PlayerWeaponSystem>();
        animator = AnimationRig.BindAnimator(gameObject, animator, controller);
        CacheSpine();
    }

    private void OnEnable()
    {
        if (weapons != null) weapons.OnWeaponPickedUp += HandleWeaponPickedUp;
    }

    private void OnDisable()
    {
        if (weapons != null) weapons.OnWeaponPickedUp -= HandleWeaponPickedUp;
    }

    private void Update()
    {
        if (!AnimationRig.IsReady(animator)) return;

        bool hasTarget = HasFireTarget();
        animator.SetFloat(Speed, ReadMoveAmount());
        animator.SetBool(Aiming, hasTarget);

        if (animator.layerCount > UpperLayer)
        {
            float targetWeight = hasTarget ? 1f : 0f;
            float current = animator.GetLayerWeight(UpperLayer);
            animator.SetLayerWeight(UpperLayer, Mathf.MoveTowards(current, targetWeight, Time.deltaTime * 8f));
        }
    }

    private void LateUpdate()
    {
        float targetTwist = 0f;

        if (HasFireTarget())
        {
            Vector3 toTarget = weapons.GetActiveWeapon().GetTargetPosition() - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                if (ReadMoveAmount() < MovingThreshold)
                {
                    Quaternion look = Quaternion.LookRotation(toTarget, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, idleTurnSpeed * Time.deltaTime);
                }

                float yaw = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up) + aimYawOffset;
                targetTwist = Mathf.Clamp(yaw, -maxTwist, maxTwist);
            }
        }

        currentTwist = Mathf.MoveTowards(currentTwist, targetTwist, twistSpeed * Time.deltaTime);
        ApplyTwist(currentTwist);
    }

    private void ApplyTwist(float twist)
    {
        if (spineBones == null) return;

        float perBone = twist / spineBones.Length;
        Quaternion step = Quaternion.AngleAxis(perBone, Vector3.up);

        for (int i = 0; i < spineBones.Length; i++)
        {
            Transform bone = spineBones[i];

            // If the animator didn't rewrite the bone this frame, start from the last animated pose so the twist never stacks.
            if (bone.localRotation == lastWritten[i]) bone.localRotation = lastAnimated[i];
            lastAnimated[i] = bone.localRotation;

            bone.rotation = step * bone.rotation;
            lastWritten[i] = bone.localRotation;
        }
    }

    private void CacheSpine()
    {
        Transform root = animator != null ? animator.transform : transform;
        Transform spine = FindBone(root, "Spine");
        Transform spine1 = FindBone(root, "Spine1");
        Transform spine2 = FindBone(root, "Spine2");

        int count = (spine != null ? 1 : 0) + (spine1 != null ? 1 : 0) + (spine2 != null ? 1 : 0);
        if (count == 0) return;

        spineBones = new Transform[count];
        int index = 0;
        if (spine != null) spineBones[index++] = spine;
        if (spine1 != null) spineBones[index++] = spine1;
        if (spine2 != null) spineBones[index++] = spine2;

        lastWritten = new Quaternion[count];
        lastAnimated = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            lastAnimated[i] = spineBones[i].localRotation;
            lastWritten[i] = new Quaternion(0f, 0f, 0f, 0f);
        }
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string name = t.name;
            int colon = name.LastIndexOf(':');
            if (colon >= 0) name = name.Substring(colon + 1);
            if (name == boneName) return t;
        }
        return null;
    }

    private void HandleWeaponPickedUp(Base_Weapon weapon)
    {
        if (AnimationRig.IsReady(animator)) animator.SetTrigger(Reload);
    }

    private bool HasFireTarget()
    {
        Base_Weapon weapon = weapons != null ? weapons.GetActiveWeapon() : null;
        return weapon != null && weapon.GetTargetObject() != null;
    }

    private float ReadMoveAmount()
    {
        if (moveSource == null || !moveSource.enabled)
        {
            moveSource = null;
            PlayerController[] controllers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i].enabled)
                {
                    moveSource = controllers[i];
                    break;
                }
            }
        }

        return moveSource != null ? moveSource.MoveAmount : 0f;
    }
}
