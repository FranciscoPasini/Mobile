using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds Animator Controllers from the MC and Enemy FBX takes and wires them onto the prefabs.
/// Player uses two layers: legs (Rifle Walking) + upper body (Rifle Aiming Idle / Reloading).
/// </summary>
public static class CharacterAnimationSetup
{
    private const string PlayerFbx = "Assets/3DModels/MC.fbx";
    private const string EnemyFbx = "Assets/3DModels/Enemy.fbx";
    private const string PlayerControllerPath = "Assets/Animations/Player.controller";
    private const string EnemyControllerPath = "Assets/Animations/Enemy.controller";
    private const string UpperMaskPath = "Assets/Animations/PlayerUpperBody.mask";

    [MenuItem("Tools/Game/Rebind Character Animations")]
    public static void RebindControllers()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        AnimationClip[] playerClips = LoadClips(PlayerFbx);
        AnimationClip[] enemyClips = LoadClips(EnemyFbx);
        if (playerClips.Length == 0 && enemyClips.Length == 0) return;

        bool dirty = false;
        dirty |= BindPlayerMotions(playerClips);
        dirty |= BindEnemyMotions(enemyClips);
        if (dirty) AssetDatabase.SaveAssets();

        Debug.Log(
            (dirty ? "Rebound character clips (were broken). " : "Character clips already bound. ") +
            "MC: " + string.Join(", ", playerClips.Select(c => c.name)) +
            ". Enemy: " + string.Join(", ", enemyClips.Select(c => c.name)));
    }

    [MenuItem("Tools/Game/Setup Character Animations")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        AnimationClip[] playerClips = LoadClips(PlayerFbx);
        AnimationClip[] enemyClips = LoadClips(EnemyFbx);

        Debug.Log("MC clips: " + string.Join(", ", playerClips.Select(c => c.name)));

        AnimatorController playerController = BuildPlayerController(playerClips);
        AnimatorController enemyController = BuildEnemyController(enemyClips);

        WirePlayer(playerController);
        WireEnemies(enemyController);

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"Character animations set up. Player clips: {string.Join(", ", playerClips.Select(c => c.name))}. " +
            $"Enemy clips: {string.Join(", ", enemyClips.Select(c => c.name))}.");
    }

    private static bool BindPlayerMotions(AnimationClip[] clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
        if (controller == null) return false;

        AnimationClip walk = FindClip(clips, "Rifle Walking", "Walking", "Walk");
        AnimationClip aim = FindClip(clips, "Rifle Aiming Idle", "Aiming", "Aim");
        AnimationClip reload = FindClip(clips, "Reloading", "Reload");
        return AssignMotions(controller, new[]
        {
            ("Idle", aim ?? walk),
            ("RifleWalking", walk),
            ("Aiming", aim),
            ("Reloading", reload)
        });
    }

    private static bool BindEnemyMotions(AnimationClip[] clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyControllerPath);
        if (controller == null) return false;

        return AssignMotions(controller, new[]
        {
            ("Running", FindClip(clips, "Running", "Run", "Walk")),
            ("Hitting", FindClip(clips, "Hitting", "Hit", "Attack"))
        });
    }

    private static bool AssignMotions(AnimatorController controller, (string state, AnimationClip clip)[] assignments)
    {
        bool dirty = false;
        AnimatorControllerLayer[] layers = controller.layers;
        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            ChildAnimatorState[] states = layers[layerIndex].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                for (int a = 0; a < assignments.Length; a++)
                {
                    if (state.name != assignments[a].state) continue;
                    if (assignments[a].clip == null) continue;
                    if (state.motion == assignments[a].clip) continue;
                    state.motion = assignments[a].clip;
                    dirty = true;
                }
            }
        }

        if (dirty) EditorUtility.SetDirty(controller);
        return dirty;
    }

    private static AnimationClip[] LoadClips(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview"))
            .ToArray();
    }

    private static AnimationClip FindClip(AnimationClip[] clips, params string[] names)
    {
        foreach (string name in names)
        {
            AnimationClip exact = clips.FirstOrDefault(c => c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
        }

        foreach (string name in names)
        {
            AnimationClip contains = clips.FirstOrDefault(c => c.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (contains != null) return contains;
        }

        return null;
    }

    private static AnimatorController BuildPlayerController(AnimationClip[] clips)
    {
        if (File.Exists(PlayerControllerPath)) AssetDatabase.DeleteAsset(PlayerControllerPath);

        AnimationClip walk = FindClip(clips, "Rifle Walking", "Walking", "Walk");
        AnimationClip aim = FindClip(clips, "Rifle Aiming Idle", "Aiming", "Aim");
        AnimationClip reload = FindClip(clips, "Reloading", "Reload");
        AnimationClip idle = FindClip(clips, "Idle") ?? aim ?? walk;

        AvatarMask upperMask = CreateUpperBodyMask();

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine baseRoot = controller.layers[0].stateMachine;
        AnimatorState idleState = AddLoopState(baseRoot, "Idle", idle);
        AnimatorState walkState = AddLoopState(baseRoot, "RifleWalking", walk);
        baseRoot.defaultState = idleState;
        Instant(idleState, walkState, AnimatorConditionMode.Greater, 0.1f, "Speed");
        Instant(walkState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");

        controller.AddLayer("UpperBody");
        AnimatorControllerLayer[] layers = controller.layers;
        layers[1].defaultWeight = 0f;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        layers[1].avatarMask = upperMask;
        controller.layers = layers;

        AnimatorStateMachine upper = controller.layers[1].stateMachine;
        AnimatorState aimState = AddLoopState(upper, "Aiming", aim);
        AnimatorState reloadState = AddState(upper, "Reloading", reload);
        upper.defaultState = aimState;
        AnyTrigger(upper, reloadState, "Reload");
        ExitTo(reloadState, aimState);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AvatarMask CreateUpperBodyMask()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, UpperMaskPath);
        }

        if (model != null)
        {
            mask.AddTransformPath(model.transform, true);
            for (int i = 0; i < mask.transformCount; i++)
            {
                mask.SetTransformActive(i, IsUpperBodyPath(mask.GetTransformPath(i)));
            }
        }

        EditorUtility.SetDirty(mask);
        return mask;
    }

    private static bool IsUpperBodyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        int slash = path.LastIndexOf('/');
        string leaf = (slash >= 0 ? path.Substring(slash + 1) : path).ToLowerInvariant();

        if (leaf.Contains("armature") || leaf.Contains("hips") || leaf.Contains("upleg")
            || leaf.Contains("leg") || leaf.Contains("foot") || leaf.Contains("toe") || leaf.Contains("sphere"))
        {
            return false;
        }

        return leaf.Contains("spine")
            || leaf.Contains("neck")
            || leaf.Contains("head")
            || leaf.Contains("shoulder")
            || leaf.Contains("arm")
            || leaf.Contains("hand")
            || leaf.Contains("rifle")
            || leaf.Contains("weapon");
    }

    private static AnimatorController BuildEnemyController(AnimationClip[] clips)
    {
        if (File.Exists(EnemyControllerPath)) AssetDatabase.DeleteAsset(EnemyControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(EnemyControllerPath);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState run = AddLoopState(root, "Running", FindClip(clips, "Running", "Run", "Walk"));
        AnimatorState hit = AddState(root, "Hitting", FindClip(clips, "Hitting", "Hit", "Attack"));
        root.defaultState = run;

        AnyTrigger(root, hit, "Hit");
        ExitTo(hit, run);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddLoopState(AnimatorStateMachine root, string name, AnimationClip clip)
    {
        return AddState(root, name, clip);
    }

    private static AnimatorState AddState(AnimatorStateMachine root, string name, AnimationClip clip)
    {
        AnimatorState state = root.AddState(name);
        if (clip != null)
        {
            state.motion = clip;
        }
        else
        {
            Debug.LogWarning($"CharacterAnimationSetup: no clip found for state '{name}'.");
        }
        return state;
    }

    private static void Instant(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string parameter)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.12f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AnyTrigger(AnimatorStateMachine root, AnimatorState to, string parameter)
    {
        AnimatorStateTransition transition = root.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void ExitTo(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.9f;
        transition.duration = 0.1f;
    }

    private static void WirePlayer(AnimatorController controller)
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (player == null)
        {
            Debug.LogError("Player prefab not found.");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(player);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        PlayerAnimation driver = root.GetComponent<PlayerAnimation>();
        if (driver == null) driver = root.AddComponent<PlayerAnimation>();

        Animator animator = BindModelAnimator(root, controller, PlayerFbx);

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(PlayerFbx).OfType<Avatar>().FirstOrDefault();
        if (avatar != null && animator != null) animator.avatar = avatar;

        var so = new SerializedObject(driver);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("weapons").objectReferenceValue = root.GetComponent<PlayerWeaponSystem>();
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void WireEnemies(AnimatorController controller)
    {
        string[] paths =
        {
            "Assets/Prefabs/Enemy.prefab",
            "Assets/Prefabs/Enemy_Fast.prefab",
            "Assets/Prefabs/Enemy_Brute.prefab"
        };

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(EnemyFbx).OfType<Avatar>().FirstOrDefault();

        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            EnemyAnimation driver = root.GetComponent<EnemyAnimation>();
            if (driver == null) driver = root.AddComponent<EnemyAnimation>();

            Animator animator = BindModelAnimator(root, controller, EnemyFbx);
            if (avatar != null && animator != null) animator.avatar = avatar;

            var so = new SerializedObject(driver);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Animator BindModelAnimator(GameObject root, AnimatorController controller, string fbxPath)
    {
        Transform model = null;
        SkinnedMeshRenderer skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skin != null)
        {
            model = skin.transform;
            while (model.parent != null && model.parent != root.transform) model = model.parent;
        }
        if (model == null) model = root.transform;

        Animator[] existing = root.GetComponentsInChildren<Animator>(true);
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.enabled = true;

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
        if (avatar != null) animator.avatar = avatar;

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null && existing[i] != animator) Object.DestroyImmediate(existing[i]);
        }

        return animator;
    }
}
