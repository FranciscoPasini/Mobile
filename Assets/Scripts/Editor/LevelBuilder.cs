using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>
/// Builds the Game scene map around the camp at the origin (camera looks north, +Z).
///
/// Layout: a walled plaza holds the camp, ringed by turrets that keep it in range. Three corridors
/// lead in from the north, west and east, each fed by its own spawn field, so every enemy follows one
/// known lane. Nothing spawns from the south because the camera can't see it. Buyable walls zigzag
/// each lane to keep enemies longer under turrets and shock zones, and buying a piece unlocks the
/// next one further out.
///
/// The old map is kept in a disabled "OldLevel" object, so it can be restored or deleted by hand.
/// Ids like "N1" are only used to wire unlocks; objects get plain names.
/// </summary>
public static class LevelBuilder
{
    // Top of the floor, matching where the existing prefabs were placed.
    private const float Ground = 0.566f;
    private const float WallHeight = 2f;
    private const float WallZoneScale = 2f;
    // Buy zones are spheres; squashing them leaves a flat circle on the ground.
    private const float ZoneFlatHeight = 0.05f;

    private enum TurretType { Basic, Rapid, Grenade, Rail }

    private static Transform groundRoot;
    private static Transform buildRoot;
    private static Material grass;
    private static Material concrete;
    private static Material ghost;
    private static Material buyZone;
    private static GameObject turretPrefab;
    private static GameObject shockPrefab;
    private static GameObject magnetPrefab;
    private static GameObject areaPrefab;
    private static Object rapidData;
    private static Object grenadeData;
    private static Object railData;
    private static Object grenadeBullet;
    private static Object railBullet;
    private static Object cheapWallData;
    private static Object wallData;
    private static readonly Dictionary<string, PurchaseArea> areas = new Dictionary<string, PurchaseArea>();

    [MenuItem("Tools/Game/Build Level")]
    public static void Build()
    {
        if (Object.FindFirstObjectByType<TownManager>() == null)
        {
            Debug.LogError("Open the Game scene first.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Build Level",
            "Rebuilds the map around the camp. The current map is moved into a disabled 'OldLevel' object.", "Build", "Cancel"))
        {
            return;
        }

        if (!LoadAssets()) return;

        Undo.SetCurrentGroupName("Build Level");
        int undoGroup = Undo.GetCurrentGroup();
        areas.Clear();

        StoreOldLevel();

        GameObject level = GameObject.Find("Level");
        if (level != null) Undo.DestroyObjectImmediate(level);
        level = new GameObject("Level");
        Undo.RegisterCreatedObjectUndo(level, "Create Level");

        groundRoot = new GameObject("Ground").transform;
        groundRoot.SetParent(level.transform, false);
        buildRoot = new GameObject("Buildings").transform;
        buildRoot.SetParent(level.transform, false);
        Transform spawnRoot = new GameObject("Spawns").transform;
        spawnRoot.SetParent(level.transform, false);

        BuildGround(level.transform);
        BuildCamp();
        BuildNorth();
        BuildSide(-1f);
        BuildSide(1f);
        WireUnlocks();
        SetSpawns(spawnRoot);
        MoveCampAndPlayer();
        BakeNavMesh();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(level.scene);
        Debug.Log("Level built. Check the NavMesh in the Scene view, then save the scene.", level);
    }

    #region Layout

    private static void BuildGround(Transform level)
    {
        // Only Ground's children are baked, so buildings, buy zones and labels never cut holes in the NavMesh.
        Block("Floor", groundRoot, -59f, 59f, -19f, 73f, 1f, grass).transform.position = new Vector3(0f, Ground - 0.5f, 27f);

        // Endless-looking grass around the map. Not baked and no collider.
        GameObject outer = Block("Outer Grass", level, -220f, 220f, -220f, 220f, 1f, grass, false);
        outer.transform.position = new Vector3(0f, Ground - 0.53f, 0f);

        Block("Border West", groundRoot, -59f, -58f, -19f, 73f, 3f, concrete);
        Block("Border East", groundRoot, 58f, 59f, -19f, 73f, 3f, concrete);
        Block("Border North", groundRoot, -59f, 59f, 72f, 73f, 3f, concrete);
        Block("Border South", groundRoot, -59f, 59f, -19f, -18f, 3f, concrete);

        // Raised grass blocks that split the map into a north, west and east sector.
        Block("Cliff North West", groundRoot, -58f, -9f, 16f, 48f, 2f, grass);
        Block("Cliff North East", groundRoot, 9f, 58f, 16f, 48f, 2f, grass);
        Block("Cliff West Top", groundRoot, -44f, -16f, 8f, 16f, 2f, grass);
        Block("Cliff West Bottom", groundRoot, -44f, -16f, -18f, -8f, 2f, grass);
        Block("Cliff East Top", groundRoot, 16f, 44f, 8f, 16f, 2f, grass);
        Block("Cliff East Bottom", groundRoot, 16f, 44f, -18f, -8f, 2f, grass);

        // Rocks in the north field so the three north spawns don't walk in a single line.
        Block("Rock", groundRoot, -3f, 3f, 56f, 59f, 2f, grass);
        Block("Rock", groundRoot, -30f, -23f, 57f, 62f, 2f, grass);
        Block("Rock", groundRoot, 23f, 30f, 57f, 62f, 2f, grass);
    }

    private static void BuildCamp()
    {
        // Low walls around the camp with a 6 m gate to each corridor.
        Block("Plaza Wall", groundRoot, -9f, -3f, 15.5f, 16.5f, 1.6f, concrete);
        Block("Plaza Wall", groundRoot, 3f, 9f, 15.5f, 16.5f, 1.6f, concrete);
        Block("Plaza Wall", groundRoot, -16.5f, -15.5f, 3f, 8f, 1.6f, concrete);
        Block("Plaza Wall", groundRoot, -16.5f, -15.5f, -8f, -3f, 1.6f, concrete);
        Block("Plaza Wall", groundRoot, 15.5f, 16.5f, 3f, 8f, 1.6f, concrete);
        Block("Plaza Wall", groundRoot, 15.5f, 16.5f, -8f, -3f, 1.6f, concrete);

        // A ring about 6 m from the camp, beside the three lanes that lead to it, so every
        // turret here can hit enemies attacking the camp. Basic ones are available from the start.
        Turret("Camp N", TurretType.Basic, 2.8f, 5.2f, Vector2.right);
        Turret("Camp W", TurretType.Basic, -5.2f, -2.8f, Vector2.down);
        Turret("Camp E", TurretType.Basic, 5.2f, -2.8f, Vector2.down);
        Turret("Camp Rapid", TurretType.Rapid, -2.8f, 5.2f, Vector2.left);
        Turret("Camp Grenade", TurretType.Grenade, -5.6f, 2.6f, Vector2.left);
        Turret("Camp Rail", TurretType.Rail, 5.6f, 2.6f, Vector2.right);

        // Behind the camp: collects coins from enemies the camp turrets kill while the player is away.
        Place("Camp Magnet", magnetPrefab, "Coin Magnet", 0f, -6f);
    }

    private static void BuildNorth()
    {
        // Corridor x -9..9, z 16..48. Walls leave a 5.5 m gap on alternating sides.
        Wall("Wall N1", -9f, 3.5f, 21.5f, 22.5f, new Vector2(-1.5f, 20.2f), true);
        Wall("Wall N2", -3.5f, 9f, 28.5f, 29.5f, new Vector2(5.5f, 30.8f), false);
        Wall("Wall N3", -9f, 3.5f, 35.5f, 36.5f, new Vector2(-5.5f, 37.8f), false);
        Wall("Wall N4", -3.5f, 9f, 42.5f, 43.5f, new Vector2(5.5f, 44.8f), false);

        // Turrets sit in the corners the zigzag never crosses.
        Turret("N1", TurretType.Basic, -7.4f, 18.5f, Vector2.right);
        Turret("N2", TurretType.Rapid, 7.4f, 27f, Vector2.left);
        Turret("N3", TurretType.Grenade, -7.4f, 34f, Vector2.right);
        Turret("N4", TurretType.Rail, 7.4f, 41f, Vector2.left);

        // Shock zones fill the middle of the zigzag, where enemies walk the longest.
        Place("Shock N1", shockPrefab, "Shock Zone", 0f, 32.5f);
        Place("Shock N2", shockPrefab, "Shock Zone", 0f, 25.5f);
        Place("Magnet N", magnetPrefab, "Coin Magnet", -6.2f, 29f);
    }

    /// <param name="side">-1 builds the west corridor, 1 the mirrored east one.</param>
    private static void BuildSide(float side)
    {
        string s = side < 0f ? "W" : "E";

        // Corridor |x| 16..44, z -8..8.
        Wall($"Wall {s}1", X(side, -22.5f, -21.5f), -8f, 2.5f, new Vector2(side * 20.2f, -1f), true);
        Wall($"Wall {s}2", X(side, -30.5f, -29.5f), -2.5f, 8f, new Vector2(side * 31.8f, -6.2f), false);
        Wall($"Wall {s}3", X(side, -38.5f, -37.5f), -8f, 2.5f, new Vector2(side * 39.8f, -2f), false);

        Turret($"{s}1", TurretType.Basic, side * 19f, -6.4f, Vector2.up);
        Turret($"{s}2", TurretType.Rapid, side * 34f, 6.6f, Vector2.down);
        Turret($"{s}3", TurretType.Grenade, side * 28f, 6.6f, Vector2.down);

        Place($"Shock {s}1", shockPrefab, "Shock Zone", side * 26f, 0f);
        Place($"Shock {s}2", shockPrefab, "Shock Zone", side * 34f, 0f);
        Place($"Magnet {s}", magnetPrefab, "Coin Magnet", side * 19.3f, 5f);
    }

    private static void WireUnlocks()
    {
        // North: the main lane, so it has the longest chain.
        Unlocks("Camp N", "Camp Rapid", "N1", "Wall N1");
        Unlocks("N1", "N2", "Shock N1");
        Unlocks("N2", "N3");
        Unlocks("N3", "N4");
        Unlocks("Wall N1", "Wall N2");
        Unlocks("Wall N2", "Wall N3");
        Unlocks("Wall N3", "Wall N4");
        Unlocks("Shock N1", "Magnet N", "Shock N2");

        Unlocks("Camp W", "Camp Grenade", "W1", "Wall W1");
        Unlocks("Camp E", "Camp Rail", "E1", "Wall E1");
        foreach (string s in new[] { "W", "E" })
        {
            Unlocks($"{s}1", $"{s}2", $"Shock {s}1");
            Unlocks($"{s}2", $"{s}3");
            Unlocks($"Wall {s}1", $"Wall {s}2");
            Unlocks($"Wall {s}2", $"Wall {s}3");
            Unlocks($"Shock {s}1", $"Magnet {s}", $"Shock {s}2");
        }
    }

    private static void SetSpawns(Transform parent)
    {
        // Three in the north field (the main lane), one each in the west and east fields.
        var spawns = new List<Collider>
        {
            SpawnZone("Spawn North West", parent, -50f, 65f, 6f, 6f),
            SpawnZone("Spawn North", parent, 0f, 67f, 10f, 4f),
            SpawnZone("Spawn North East", parent, 50f, 65f, 6f, 6f),
            SpawnZone("Spawn West", parent, -53f, -1f, 4f, 14f),
            SpawnZone("Spawn East", parent, 53f, -1f, 4f, 14f),
        };

        EnemyPool pool = Object.FindFirstObjectByType<EnemyPool>();
        if (pool == null)
        {
            Debug.LogWarning("No EnemyPool found, spawn areas not assigned.");
            return;
        }

        var so = new SerializedObject(pool);
        SetList(so.FindProperty("spawnAreas"), spawns);
        so.ApplyModifiedProperties();
    }

    private static void MoveCampAndPlayer()
    {
        Building_Town town = Object.FindFirstObjectByType<Building_Town>();
        if (town != null) MoveTo(town.transform, new Vector3(0f, town.transform.position.y, 0f));

        PlayerWeaponSystem player = Object.FindFirstObjectByType<PlayerWeaponSystem>();
        // South of the camp magnet so the player doesn't start on its buy zone.
        if (player != null) MoveTo(player.transform, new Vector3(0f, player.transform.position.y, -11f));

        // Same offset the cameras had from the player, so the first frame matches.
        Vector3 cameraPosition = new Vector3(0f, 18.83f, -24f);
        if (Camera.main != null) MoveTo(Camera.main.transform, cameraPosition);
        GameObject vcam = GameObject.Find("CinemachineCamera");
        if (vcam != null) MoveTo(vcam.transform, cameraPosition);
    }

    #endregion

    #region Pieces

    private static GameObject Block(string name, Transform parent, float x0, float x1, float z0, float z1, float height, Material material, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, Ground + height * 0.5f, (z0 + z1) * 0.5f);
        go.transform.localScale = new Vector3(x1 - x0, height, z1 - z0);
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    private static GameObject Spawn(GameObject prefab, string name, float x, float y, float z)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, buildRoot);
        go.name = name;
        go.transform.position = new Vector3(x, y, z);
        return go;
    }

    /// <param name="zoneDirection">Side of the turret its buy circle faces (x, z).</param>
    private static void Turret(string id, TurretType type, float x, float z, Vector2 zoneDirection)
    {
        string displayName = type switch
        {
            TurretType.Rapid => "Rapid Turret",
            TurretType.Grenade => "Grenade Turret",
            TurretType.Rail => "Rail Turret",
            _ => "Turret",
        };

        GameObject go = Spawn(turretPrefab, displayName, x, Ground - 0.026f, z);
        // The buy circle sits on the turret's local -Z.
        go.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-zoneDirection.x, -zoneDirection.y) * Mathf.Rad2Deg, 0f);

        var so = new SerializedObject(go.GetComponent<Building_Turret>());
        so.FindProperty("buildingName").stringValue = displayName;
        if (type == TurretType.Rapid) so.FindProperty("turretData").objectReferenceValue = rapidData;
        if (type == TurretType.Grenade)
        {
            so.FindProperty("turretData").objectReferenceValue = grenadeData;
            so.FindProperty("bulletData").objectReferenceValue = grenadeBullet;
        }
        if (type == TurretType.Rail)
        {
            so.FindProperty("turretData").objectReferenceValue = railData;
            so.FindProperty("bulletData").objectReferenceValue = railBullet;
        }
        so.ApplyModifiedProperties();

        Register(id, go);
    }

    private static void Place(string id, GameObject prefab, string displayName, float x, float z)
    {
        GameObject go = Spawn(prefab, displayName, x, Ground, z);
        var so = new SerializedObject(go.GetComponent<Base_PurchaseableBuilding>());
        so.FindProperty("buildingName").stringValue = displayName;
        so.ApplyModifiedProperties();
        Register(id, go);
    }

    /// <summary>
    /// Buyable wall: a faint slab on the ground until bought, then a solid block that carves the NavMesh.
    /// </summary>
    private static void Wall(string id, Vector2 xRange, float z0, float z1, Vector2 buySpot, bool cheap)
    {
        Wall(id, xRange.x, xRange.y, z0, z1, buySpot, cheap);
    }

    private static void Wall(string id, float x0, float x1, float z0, float z1, Vector2 buySpot, bool cheap)
    {
        var root = new GameObject("Wall");
        root.transform.SetParent(buildRoot, false);
        root.transform.position = new Vector3((x0 + x1) * 0.5f, Ground, (z0 + z1) * 0.5f);

        GameObject built = Block("Built", root.transform, x0, x1, z0, z1, WallHeight, concrete);
        GameObjectUtility.SetStaticEditorFlags(built, (StaticEditorFlags)0);
        var obstacle = built.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = Vector3.one;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;

        GameObject preview = Block("Unbuilt", root.transform, x0, x1, z0, z1, 0.3f, ghost, false);
        GameObjectUtility.SetStaticEditorFlags(preview, (StaticEditorFlags)0);

        var wall = root.AddComponent<Building_Obstacle>();
        var so = new SerializedObject(wall);
        so.FindProperty("purchaseData").objectReferenceValue = cheap ? cheapWallData : wallData;
        so.FindProperty("buildingName").stringValue = "Wall";
        so.FindProperty("builtVisual").objectReferenceValue = built;
        so.FindProperty("unbuiltVisual").objectReferenceValue = preview;
        SetList(so.FindProperty("objectsToEnable"), new List<Object> { built });
        SetList(so.FindProperty("obstacles"), new List<Object> { obstacle });
        so.ApplyModifiedProperties();
        built.SetActive(false);

        GameObject zone = Spawn(areaPrefab, "Buy Zone", buySpot.x, Ground, buySpot.y);
        zone.transform.SetParent(root.transform, true);
        zone.transform.localScale = Vector3.one * WallZoneScale;
        var zoneSo = new SerializedObject(zone.GetComponent<PurchaseArea>());
        zoneSo.FindProperty("purchasable").objectReferenceValue = wall;
        zoneSo.ApplyModifiedProperties();

        Register(id, zone);
    }

    private static Collider SpawnZone(string name, Transform parent, float x, float z, float sizeX, float sizeZ)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, Ground + 1.5f, z);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(sizeX, 4f, sizeZ);
        return box;
    }

    private static void Register(string id, GameObject go)
    {
        PurchaseArea area = go.GetComponentInChildren<PurchaseArea>(true);
        if (area == null)
        {
            Debug.LogError($"{id} has no PurchaseArea.", go);
            return;
        }

        // Prefabs float the buy zone; sit it on the ground and flatten it into a circle.
        Transform t = area.transform;
        if (area.gameObject != go) t.localPosition = new Vector3(t.localPosition.x, 0f, t.localPosition.z);
        t.localScale = new Vector3(t.localScale.x, ZoneFlatHeight, t.localScale.z);

        if (area.TryGetComponent(out MeshRenderer zoneRenderer))
        {
            zoneRenderer.sharedMaterial = buyZone;
            zoneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        areas[id] = area;
    }

    /// <summary>
    /// <paramref name="next"/> stay hidden until <paramref name="id"/> is bought.
    /// </summary>
    private static void Unlocks(string id, params string[] next)
    {
        var list = new List<Object>();
        foreach (string n in next)
        {
            list.Add(areas[n]);
            var lockedSo = new SerializedObject(areas[n]);
            lockedSo.FindProperty("hideOnSpawn").boolValue = true;
            lockedSo.ApplyModifiedProperties();
        }

        var so = new SerializedObject(areas[id]);
        SetList(so.FindProperty("areasToUnlockOnPurchase"), list);
        so.ApplyModifiedProperties();
    }

    #endregion

    #region Helpers

    /// <summary>West-side x range, mirrored to the east when side is 1.</summary>
    private static Vector2 X(float side, float westMin, float westMax)
    {
        return side < 0f ? new Vector2(westMin, westMax) : new Vector2(-westMax, -westMin);
    }

    private static bool LoadAssets()
    {
        grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GrassGround.mat");
        concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Concrete.mat");
        buyZone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BuyZone.mat");
        ghost = GetGhostMaterial();
        turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Turret.prefab");
        shockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/ElectricZone.prefab");
        magnetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/CoinMagnet.prefab");
        areaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PurchaseArea.prefab");
        rapidData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/Town/Data/SMG_Turret.asset");
        grenadeData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/Town/Data/Grenade_Turret.asset");
        railData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/Town/Data/Rail_Turret.asset");
        grenadeBullet = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/Bullets/Grenade.asset");
        railBullet = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/Bullets/RailBeam.asset");
        cheapWallData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/Buildings/Obstacle.asset");
        wallData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/Buildings/ObstacleMidPrice.asset");

        var required = new Object[]
        {
            grass, concrete, buyZone, ghost, turretPrefab, shockPrefab, magnetPrefab, areaPrefab,
            rapidData, grenadeData, railData, grenadeBullet, railBullet, cheapWallData, wallData,
        };
        foreach (Object asset in required)
        {
            if (asset != null) continue;
            Debug.LogError("Build Level: a material, prefab or data asset is missing. Check the paths in LevelBuilder.LoadAssets.");
            return false;
        }
        return true;
    }

    private static Material GetGhostMaterial()
    {
        const string path = "Assets/Materials/WallGhost.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// Moves every root object that isn't core gameplay (camera, UI, managers, camp, player) into a disabled OldLevel.
    /// </summary>
    private static void StoreOldLevel()
    {
        GameObject old = GameObject.Find("OldLevel");
        if (old != null) return;

        old = new GameObject("OldLevel");
        Undo.RegisterCreatedObjectUndo(old, "Create OldLevel");

        foreach (GameObject root in old.scene.GetRootGameObjects())
        {
            if (root == old || root.name == "Level" || IsCore(root)) continue;
            Undo.SetTransformParent(root.transform, old.transform, "Store old level");
        }

        old.SetActive(false);
    }

    private static bool IsCore(GameObject root)
    {
        return root.GetComponent<Camera>() || root.GetComponent<Light>() || root.GetComponent<Canvas>()
            || root.GetComponent<EventSystem>() || root.GetComponent<TownManager>() || root.GetComponent<Building_Town>()
            || root.GetComponent<PlayerWeaponSystem>() || root.GetComponent<AudioSource>()
            || root.name.Contains("Cinemachine");
    }

    private static void MoveTo(Transform target, Vector3 position)
    {
        Undo.RecordObject(target, "Move");
        target.position = position;
    }

    private static void BakeNavMesh()
    {
        var surface = groundRoot.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        const string path = "Assets/Scenes/Game/LevelNavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);
        EditorUtility.SetDirty(surface);
    }

    private static void SetList<T>(SerializedProperty property, List<T> items) where T : Object
    {
        property.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    #endregion
}
