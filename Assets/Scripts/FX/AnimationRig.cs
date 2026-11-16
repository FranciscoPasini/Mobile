using UnityEngine;

/// <summary>
/// Puts the Animator on the skinned mesh (the FBX), not the empty wrapper cube.
/// Copies the controller if it was assigned on the parent.
/// </summary>
public static class AnimationRig
{
    public static bool IsReady(Animator animator)
    {
        return animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
    }

    public static Animator BindAnimator(GameObject root, Animator current, RuntimeAnimatorController fallback = null)
    {
        Transform model = FindModelRoot(root.transform);
        RuntimeAnimatorController controller = current != null ? current.runtimeAnimatorController : null;
        Avatar avatar = current != null ? current.avatar : null;

        if (controller == null)
        {
            Animator[] all = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].runtimeAnimatorController != null)
                {
                    controller = all[i].runtimeAnimatorController;
                    if (avatar == null) avatar = all[i].avatar;
                    break;
                }
            }
        }
        if (controller == null) controller = fallback;

        if (model == null) return current;

        Animator modelAnimator = model.GetComponent<Animator>();
        if (modelAnimator == null) modelAnimator = model.gameObject.AddComponent<Animator>();

        if (modelAnimator.runtimeAnimatorController == null && controller != null)
        {
            modelAnimator.runtimeAnimatorController = controller;
        }
        if (modelAnimator.avatar == null && avatar != null)
        {
            modelAnimator.avatar = avatar;
        }
        modelAnimator.applyRootMotion = false;

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != modelAnimator) animators[i].enabled = false;
        }

        return modelAnimator;
    }

    private static Transform FindModelRoot(Transform root)
    {
        SkinnedMeshRenderer skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skin == null) return null;

        Transform model = skin.transform;
        while (model.parent != null && model.parent != root)
        {
            model = model.parent;
        }
        return model;
    }
}
