using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class CampusSceneGenerator
{
    const float F = 3.5f; // floor height
    const float Wall = 0.3f;

    // Landscape: bottom of the reference map (south, -Z) is the highest elevation; north (+Z) is lower.
    // Height grows in discrete steps; only stair ramps connect adjacent levels for walking.
    const float TerrainNorthZ = 90f;
    const float TerrainSouthZ = -55f;
    const int TerrainLevels = 6;
    const float StepHeight = 1.5f;
    const float PathLengthOverlap = 0.2f;
    /// <summary>North–south extent of each level-boundary plank (along Z).</summary>
    const float BoundaryPlankDepthZ = 3.2f;
    /// <summary>Thickness of stair / plank walk surfaces for NavMesh bake.</summary>
    const float PlankThickness = 0.14f;
    const float DoorwayWidth = 4f;

    static float TerrainBandWidth() => (TerrainNorthZ - TerrainSouthZ) / TerrainLevels;

    /// <summary>Which terrace (0 = north/low … south = higher index).</summary>
    static int TerrainLevelIndex(float z)
    {
        float band = TerrainBandWidth();
        float rel = TerrainNorthZ - z;
        int idx = Mathf.FloorToInt(rel / band);
        if (idx < 0) idx = 0;
        if (idx >= TerrainLevels) idx = TerrainLevels - 1;
        return idx;
    }

    /// <summary>Flat terrace height for this Z (piecewise constant).</summary>
    static float TerrainY(float z) => TerrainLevelIndex(z) * StepHeight;

    [MenuItem("CampusNav/Generate Full Campus Scene")]
    static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Generate Campus",
            "Создать полную 3D-сцену кампуса?\nСуществующий объект 'Campus' будет удален.", "Создать", "Отмена"))
            return;

        var old = GameObject.Find("Campus");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Campus");
        Undo.RegisterCreatedObjectUndo(root, "Generate Campus");

        var bldGroup = Child(root, "Buildings");
        var corGroup = Child(root, "Corridors");
        var mrkGroup = Child(root, "Markers");
        var entGroup = Child(mrkGroup, "Entrances");
        var elvGroup = Child(mrkGroup, "Elevators");
        var wcGroup = Child(mrkGroup, "WC");
        var rmpGroup = Child(mrkGroup, "Ramps");

        // ===== GROUND (flat terraces by Z band; south = higher Y; stairs connect levels) =====
        TieredGround(root, "Ground", 220f, 220f, segX: 48, segZPerBand: 12);

        // ===== OUTDOOR PATHS (walkable surfaces slightly above terrain) =====
        var paths = Child(root, "OutdoorPaths");
        // Main north road (Kelias link Vilniaus universiteto) — east-west, constant Z
        PathEW(paths, "Road_North", -10f, 85f, 130f, 8f);
        // Central north-south walkway
        PathNS(paths, "Walk_CentralNS", 5f, -50f, 90f, 8f);
        // South walkway
        PathEW(paths, "Walk_South", -5f, -45f, 120f, 8f);
        // West walkway
        PathNS(paths, "Walk_West", -70f, -40f, 80f, 8f);
        // East walkway
        PathNS(paths, "Walk_East", 60f, -15f, 85f, 8f);
        // Parking area approach
        PathEW(paths, "Walk_Parking", -40f, 40f, 30f, 8f);
        // Connector between buildings south area
        PathEW(paths, "Walk_SouthConnector", -20f, -35f, 60f, 8f);
        // Walk to SRA-II
        PathNS(paths, "Walk_toSRA2", 20f, -30f, 0f, 8f);

        // ===== LEVEL PLANKS (one sloped strip per terrace boundary — NavMesh walks across height steps) =====
        var stepLinks = Child(root, "LevelStepPlanks");
        AllTerraceBoundaryPlanks(stepLinks, halfX: 220f);

        // ==========================================================
        //  BUILDINGS - positioned to match the reference campus map
        //  Map: North = +Z, East = +X
        //  Reference: photo_2025-12-23 08.31.53.jpeg
        //  Ground floor Y follows terrain (south = higher).
        // ==========================================================

        Building(bldGroup, "SRK-I", V(-50, TerrainY(60), 60), 55, 18, 4);
        Building(bldGroup, "SRC", V(42, TerrainY(65), 65), 28, 28, 3);
        Building(bldGroup, "SRA-I", V(0, TerrainY(40), 40), 38, 22, 4);
        Building(bldGroup, "SRK-II", V(52, TerrainY(18), 18), 18, 45, 3);
        Building(bldGroup, "SRA-II", V(15, TerrainY(-12), -12), 32, 18, 3);
        Building(bldGroup, "SRL-I", V(5, TerrainY(-35), -35), 28, 18, 6);
        Building(bldGroup, "SRL-II", V(-45, TerrainY(-35), -35), 50, 18, 3);

        // ==========================================================
        //  CORRIDORS - connections between buildings
        // ==========================================================

        // SRK-I -> SRA-I (ground floor, runs south from SRK-I to north side of SRA-I)
        Corridor(corGroup, "Corridor_SRK1_SRA1",
            V(-22, TerrainY(51), 51), V(-10, TerrainY(51), 51), 5, F);

        // SRA-I -> SRC (3rd floor enclosed bridge, runs east)
        Corridor(corGroup, "Bridge_SRA1_SRC",
            V(19, TerrainY(52) + F * 2, 52), V(28, TerrainY(58) + F * 2, 58), 4, F);

        // SRA-I -> SRK-II (3rd floor enclosed bridge, runs east-southeast)
        Corridor(corGroup, "Bridge_SRA1_SRK2",
            V(19, TerrainY(35) + F * 2, 35), V(43, TerrainY(30) + F * 2, 30), 4, F);

        // SRA-I -> SRA-II / SRL-I (flat segments per terrace; level jumps only at stair objects)
        CorridorNSChain(corGroup, "Corridor_SRA1_South",
            V(5, TerrainY(29), 29), V(5, TerrainY(-3), -3), 5, F);

        // SRA-II -> SRL-I (short connection)
        Corridor(corGroup, "Corridor_SRA2_SRL1",
            V(10, TerrainY(-21), -21), V(10, TerrainY(-26), -26), 4, F);

        // SRL-I -> SRL-II (ground floor corridor west)
        Corridor(corGroup, "Corridor_SRL1_SRL2",
            V(-9, TerrainY(-35), -35), V(-20, TerrainY(-35), -35), 5, F);

        // ==========================================================
        //  MARKERS
        // ==========================================================

        // Entrances (green on map)
        Marker(entGroup, "Entrance_Main_SRK1", V(-50, TerrainY(69), 69), C.Entrance, 2f);
        Marker(entGroup, "Entrance_SRC_Main", V(42, TerrainY(79), 79), C.Entrance, 2f);
        Marker(entGroup, "Entrance_SRA1_Parking", V(-19, TerrainY(40), 40), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRL1_South", V(5, TerrainY(-44), -44), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRL2_West", V(-70, TerrainY(-35), -35), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRK2_East", V(61, TerrainY(18), 18), C.Entrance, 1.5f);

        // Elevators (marked with X-in-square on map)
        Marker(elvGroup, "Elevator_SRK1_Main", V(-40, TerrainY(60), 60), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRK1_Side", V(-30, TerrainY(60), 60), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRA1_Corridor", V(-15, TerrainY(51), 51), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRL1", V(5, TerrainY(-30), -30), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRC", V(42, TerrainY(70), 70), C.Elevator, 1.2f);

        // WC
        Marker(wcGroup, "WC_SRK1_F1", V(-55, TerrainY(60), 60), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRL1_F5", V(10, TerrainY(-35) + F * 4, -35), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRA2_F1", V(22, TerrainY(-12), -12), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRC_F2", V(50, TerrainY(65) + F, 65), C.WC, 0.8f);

        // Ramps
        RampMarker(rmpGroup, "Ramp_SRK1", V(-48, TerrainY(69), 69));
        RampMarker(rmpGroup, "Ramp_SRC", V(40, TerrainY(79), 79));

        // Smoking Areas (before entrances, outdoor)
        var smkGroup = Child(mrkGroup, "SmokingAreas");
        Marker(smkGroup, "Smoking_SRK1", V(-55, TerrainY(72), 72), C.Smoking, 1.2f);
        Marker(smkGroup, "Smoking_SRC", V(38, TerrainY(82), 82), C.Smoking, 1.2f);
        Marker(smkGroup, "Smoking_SRA1", V(-22, TerrainY(43), 43), C.Smoking, 1.2f);
        Marker(smkGroup, "Smoking_SRL1", V(10, TerrainY(-47), -47), C.Smoking, 1.2f);

        // Vending Machines (at entrances of SRA-I and SRA-II)
        var vndGroup = Child(mrkGroup, "VendingMachines");
        Marker(vndGroup, "Vending_SRA1", V(-5, TerrainY(40) + 0.1f, 40), C.Vending, 0.6f);
        Marker(vndGroup, "Vending_SRA2", V(15, TerrainY(-12) + 0.1f, -8), C.Vending, 0.6f);

        // Cafeteria (1st floor SRA-I)
        var cafGroup = Child(mrkGroup, "Cafeteria");
        Marker(cafGroup, "Cafeteria_SRA1", V(5, TerrainY(40) + 0.1f, 35), C.Cafeteria, 1.5f);

        // ==========================================================
        //  PLAYER
        // ==========================================================
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        // Position at the central walkway for better visibility
        player.transform.position = V(5, TerrainY(20) + 1.2f, 20); 
        player.tag = "Player";

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerModel";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = V(0, 1, 0);
        capsule.transform.localScale = V(0.8f, 1, 0.8f);
        SetMat(capsule, new Color(0.2f, 0.6f, 1f));
        capsule.tag = "Player";

        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.5f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // ==========================================================
        //  CAMERA with CampusCameraController
        // ==========================================================
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        var camCtrl = camObj.GetComponent<CampusCameraController>();
        if (camCtrl == null) camCtrl = camObj.AddComponent<CampusCameraController>();
        camCtrl.SwitchMode(CampusCameraController.CameraMode.Free);
        camCtrl.FocusOn(V(0, 5, 20));
        camCtrl.SetOrbit(45f, 60f, 60f);

        var so = new SerializedObject(camCtrl);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ==========================================================
        //  NAVIGATOR MANAGER
        // ==========================================================
        var navManager = new GameObject("NavigatorManager");
        navManager.transform.SetParent(root.transform);

        var navigator = navManager.AddComponent<CampusNavigator>();

        // LineRenderer for path visualization
        var lr = navManager.AddComponent<LineRenderer>();
        lr.startWidth = 0.4f;
        lr.endWidth = 0.4f;
        lr.positionCount = 0;
        var lrMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lrMat.color = new Color(0.1f, 0.7f, 1f);
        lr.sharedMaterial = lrMat;

        var arrowObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arrowObj.name = "PathArrow";
        arrowObj.transform.SetParent(navManager.transform);
        arrowObj.transform.localScale = V(0.6f, 0.6f, 0.6f);
        SetMat(arrowObj, new Color(1f, 0.4f, 0.1f));
        arrowObj.SetActive(false);

        // Wire navigator references via SerializedObject
        var navSO = new SerializedObject(navigator);
        navSO.FindProperty("agent").objectReferenceValue = agent;
        navSO.FindProperty("lineRenderer").objectReferenceValue = lr;
        navSO.FindProperty("arrow").objectReferenceValue = arrowObj.transform;
        navSO.ApplyModifiedPropertiesWithoutUndo();

        // ==========================================================
        //  GEO ANCHOR SYSTEM
        // ==========================================================
        var geoObj = new GameObject("GeoAnchorSystem");
        geoObj.transform.SetParent(root.transform);
        var geoAnchor = geoObj.AddComponent<GeoAnchorSystem>();

        // ==========================================================
        //  PLAYER GEO TRACKER
        // ==========================================================
        var geoTracker = player.AddComponent<PlayerGeoTracker>();
        var trackerSO = new SerializedObject(geoTracker);
        trackerSO.FindProperty("geoAnchor").objectReferenceValue = geoAnchor;
        trackerSO.FindProperty("agent").objectReferenceValue = agent;
        trackerSO.FindProperty("cameraController").objectReferenceValue = camCtrl;
        trackerSO.ApplyModifiedPropertiesWithoutUndo();

        // ==========================================================
        //  NAVIGATION PREFERENCES
        // ==========================================================
        var navPrefObj = new GameObject("NavigationPreferences");
        navPrefObj.transform.SetParent(root.transform);
        navPrefObj.AddComponent<NavigationPreferences>();

        // ==========================================================
        //  PATHFINDING BENCHMARK
        // ==========================================================
        var benchObj = new GameObject("PathfindingBenchmark");
        benchObj.transform.SetParent(root.transform);
        benchObj.AddComponent<PathfindingBenchmark>();

        // ==========================================================
        //  NAV MESH SURFACE
        // ==========================================================
        var navSurface = new GameObject("NavMeshSurface");
        navSurface.transform.SetParent(root.transform);
        var surface = navSurface.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        
        // Bake includes static geometry; interior floors are walkable where doorways exist (see Building).

        // ==========================================================
        //  UI & APP FLOW
        // ==========================================================
        var canvasObj = new GameObject("UI_Canvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem - ensure it exists and has a module
        var esObj = GameObject.Find("EventSystem");
        if (esObj == null) esObj = new GameObject("EventSystem");
        if (esObj.GetComponent<UnityEngine.EventSystems.EventSystem>() == null)
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        
        // Add both modules for compatibility, or the best one available
        if (esObj.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
        {
            // Try new input system first if available
            var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                var module = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                module.actionsAsset = actionsAsset;
            }
            else
            {
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        var flowObj = new GameObject("AppFlowManager");
        flowObj.transform.SetParent(root.transform);
        var flow = flowObj.AddComponent<AppFlowManager>();

        // Create Screens
        var mm = CreateScreen(canvasObj, "MainMenuScreen").AddComponent<MainMenuScreen>();
        var cs = CreateScreen(canvasObj, "CampusSelectScreen").AddComponent<CampusSelectScreen>();
        var ds = CreateScreen(canvasObj, "DestinationSelectScreen").AddComponent<DestinationSelectScreen>();
        var sp = CreateScreen(canvasObj, "StartPointScreen").AddComponent<StartPointScreen>();
        var ns = CreateScreen(canvasObj, "NavigationScreen").AddComponent<NavigationScreen>();

        // Link Flow Manager
        var flowSO = new SerializedObject(flow);
        flowSO.FindProperty("mainMenuScreen").objectReferenceValue = mm;
        flowSO.FindProperty("campusSelectScreen").objectReferenceValue = cs;
        flowSO.FindProperty("destinationSelectScreen").objectReferenceValue = ds;
        flowSO.FindProperty("startPointScreen").objectReferenceValue = sp;
        flowSO.FindProperty("navigationScreen").objectReferenceValue = ns;
        flowSO.FindProperty("navigator").objectReferenceValue = navigator;
        flowSO.FindProperty("geoAnchor").objectReferenceValue = geoAnchor;
        flowSO.FindProperty("playerTracker").objectReferenceValue = geoTracker;
        flowSO.FindProperty("cameraController").objectReferenceValue = camCtrl;
        flowSO.FindProperty("campusWorldRoot").objectReferenceValue = root;
        
        // Ensure we have a CampusData asset
        string cdPath = "Assets/Resources/MainCampusData.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var cd = AssetDatabase.LoadAssetAtPath<CampusData>(cdPath);
        if (cd == null)
        {
            cd = ScriptableObject.CreateInstance<CampusData>();
            cd.campusName = "Vilnius Tech - Saulėtekio Rūmai";
            cd.routeDatabase = AssetDatabase.LoadAssetAtPath<RouteDatabase>("Assets/RouteDatabase.asset");
            AssetDatabase.CreateAsset(cd, cdPath);
        }
        
        var campusArray = flowSO.FindProperty("availableCampuses");
        campusArray.arraySize = 1;
        campusArray.GetArrayElementAtIndex(0).objectReferenceValue = cd;
        
        flowSO.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("[CampusNav] Scene: tiered ground + boundary planks, buildings with north doorway for NavMesh, Bake NavMeshSurface when ready.");
    }

    static GameObject CreateScreen(GameObject canvas, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(canvas.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    // ======================== HELPERS ========================

    static Vector3 V(float x, float y, float z) => new(x, y, z);

    static GameObject Building(GameObject parent, string name, Vector3 pos, float w, float d, int floors)
    {
        var bld = new GameObject(name);
        bld.transform.SetParent(parent.transform);
        bld.transform.localPosition = pos;
        bld.isStatic = true;

        float h = floors * F;

        // Floor slab
        Box(bld, "Floor", V(0, 0.05f, 0), V(w, 0.1f, d), C.FloorSlab);

        // North wall (+Z) with centered doorway so NavMesh can reach ground-floor slab from outside
        float sideW = (w - DoorwayWidth) * 0.5f;
        if (sideW > 0.4f)
        {
            float halfW = w * 0.5f;
            float leftCx = -halfW + sideW * 0.5f;
            float rightCx = halfW - sideW * 0.5f;
            Box(bld, "Wall_N_L", V(leftCx, h / 2, d / 2), V(sideW, h, Wall), C.Wall);
            Box(bld, "Wall_N_R", V(rightCx, h / 2, d / 2), V(sideW, h, Wall), C.Wall);
        }
        else
            Box(bld, "Wall_N", V(0, h / 2, d / 2), V(w, h, Wall), C.Wall);

        Box(bld, "Wall_S", V(0, h / 2, -d / 2), V(w, h, Wall), C.Wall);
        Box(bld, "Wall_E", V(w / 2, h / 2, 0), V(Wall, h, d), C.Wall);
        Box(bld, "Wall_W", V(-w / 2, h / 2, 0), V(Wall, h, d), C.Wall);

        // Roof
        Box(bld, "Roof", V(0, h + 0.1f, 0), V(w + 0.4f, 0.2f, d + 0.4f), C.Roof);

        // Inter-floor slabs
        for (int i = 1; i < floors; i++)
            Box(bld, $"Slab_F{i}", V(0, i * F, 0), V(w - 0.2f, 0.12f, d - 0.2f), C.Slab);

        // Vertical connections (Elevators/Stairs)
        float swX = -w * 0.35f;
        float swZ = d * 0.35f;
        for (int i = 0; i < floors - 1; i++)
        {
            Vector3 bottom = V(swX, i * F + 0.1f, swZ);
            Vector3 top = V(swX, (i + 1) * F + 0.1f, swZ);
            AddNavMeshLink(bld, name, $"Link_F{i + 1}_F{i + 2}", bottom, top, i + 1, i + 2);
        }

        // Trigger collider for BuildingRevealer
        var col = bld.AddComponent<BoxCollider>();
        col.center = V(0, h / 2, 0);
        col.size = V(w + 6, h + 4, d + 6);
        col.isTrigger = true;

        bld.AddComponent<BuildingRevealer>();

        return bld;
    }

    static void Corridor(GameObject parent, string name, Vector3 a, Vector3 b, float w, float h)
    {
        var cor = new GameObject(name);
        cor.transform.SetParent(parent.transform);
        cor.isStatic = true;

        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) { len = w; dir = Vector3.forward; }

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        float lenFloor = len + w + PathLengthOverlap;

        // Floor (slight offset along corridor "up" so sloped corridors stay flush)
        var fl = Prim(cor, "Floor", PrimitiveType.Cube, mid + rot * Vector3.up * 0.05f, V(w, 0.1f, lenFloor), C.Corridor);
        fl.transform.rotation = rot;
        fl.isStatic = true;

        // Left wall
        var wl = Prim(cor, "WallL", PrimitiveType.Cube,
            mid + rot * V(-w / 2, h / 2, 0), V(Wall, h, lenFloor), C.Wall);
        wl.transform.rotation = rot;
        wl.isStatic = true;

        // Right wall
        var wr = Prim(cor, "WallR", PrimitiveType.Cube,
            mid + rot * V(w / 2, h / 2, 0), V(Wall, h, lenFloor), C.Wall);
        wr.transform.rotation = rot;
        wr.isStatic = true;

        // Ceiling (parallel to floor when corridor slopes)
        var ceil = Prim(cor, "Ceil", PrimitiveType.Cube,
            mid + rot * Vector3.up * h, V(w + Wall * 2, 0.15f, lenFloor), C.Roof);
        ceil.transform.rotation = rot;
        ceil.isStatic = true;
    }

    /// <summary>North–south corridor at fixed X, split into flat pieces per terrace (no sloped floor between levels).</summary>
    static void CorridorNSChain(GameObject parent, string name, Vector3 a, Vector3 b, float w, float h)
    {
        float x = a.x;
        float zMin = Mathf.Min(a.z, b.z);
        float zMax = Mathf.Max(a.z, b.z);
        var splits = new List<float> { zMin, zMax };
        float band = TerrainBandWidth();
        for (int k = 1; k < TerrainLevels; k++)
        {
            float bd = TerrainNorthZ - k * band;
            if (bd > zMin && bd < zMax) splits.Add(bd);
        }
        splits.Sort();
        for (int i = 0; i < splits.Count - 1; i++)
        {
            float za = splits[i];
            float zb = splits[i + 1];
            if (zb - za < 1e-4f) continue;
            float zm = (za + zb) * 0.5f;
            float y = TerrainY(zm);
            Corridor(parent, $"{name}_{i}", V(x, y, za), V(x, y, zb), w, h);
        }
    }

    static void Marker(GameObject parent, string name, Vector3 pos, Color col, float radius)
    {
        var m = new GameObject(name);
        m.transform.SetParent(parent.transform);
        m.transform.position = pos;

        var vis = Prim(m, "Visual", PrimitiveType.Cylinder,
            pos + Vector3.up * 0.5f, V(radius, 0.8f, radius), col);
        // Remove collider from marker visual so it doesn't interfere with NavMesh
        Object.DestroyImmediate(vis.GetComponent<Collider>());
    }

    static void RampMarker(GameObject parent, string name, Vector3 pos)
    {
        var m = new GameObject(name);
        m.transform.SetParent(parent.transform);
        m.transform.position = pos;

        var vis = Prim(m, "Visual", PrimitiveType.Cube,
            pos + V(0, 0.15f, 0), V(2.5f, 0.3f, 4f), C.Ramp);
        vis.transform.rotation = Quaternion.Euler(5, 0, 0);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
    }

    /// <summary>Outdoor path along Z — flat within each terrace; splits at level boundaries (no smooth ramp).</summary>
    static void PathNS(GameObject parent, string name, float x, float z0, float z1, float crossWidth)
    {
        float zMin = Mathf.Min(z0, z1);
        float zMax = Mathf.Max(z0, z1);
        var splits = new List<float> { zMin, zMax };
        float band = TerrainBandWidth();
        for (int k = 1; k < TerrainLevels; k++)
        {
            float b = TerrainNorthZ - k * band;
            if (b > zMin && b < zMax) splits.Add(b);
        }
        splits.Sort();
        for (int i = 0; i < splits.Count - 1; i++)
        {
            float za = splits[i];
            float zb = splits[i + 1];
            if (zb - za < 1e-4f) continue;
            float zm = (za + zb) * 0.5f;
            float y = TerrainY(zm);
            PathSegment(parent, $"{name}_{i}", V(x, y, za), V(x, y, zb), crossWidth);
        }
    }

    /// <summary>Outdoor path along X (east–west), constant Z.</summary>
    static void PathEW(GameObject parent, string name, float xCenter, float z, float lengthX, float crossWidth)
    {
        float half = lengthX * 0.5f;
        var a = V(xCenter - half, TerrainY(z), z);
        var b = V(xCenter + half, TerrainY(z), z);
        PathSegment(parent, name, a, b, crossWidth);
    }

    static void PathSegment(GameObject parent, string name, Vector3 a, Vector3 b, float crossWidth)
    {
        PathSegment(parent, name, a, b, crossWidth, C.Path, 0.08f);
    }

    static void PathSegment(GameObject parent, string name, Vector3 a, Vector3 b, float crossWidth, Color color)
    {
        PathSegment(parent, name, a, b, crossWidth, color, 0.08f);
    }

    static void PathSegment(GameObject parent, string name, Vector3 a, Vector3 b, float crossWidth, Color color, float thicknessY)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        // Narrow paths: extra length like crossWidth for joint overlap; wide boundary planks: no huge extension
        float lenExt = len + PathLengthOverlap + (crossWidth > 80f ? 0.25f : crossWidth);
        float halfT = thicknessY * 0.5f;
        var p = Prim(parent, name, PrimitiveType.Cube, mid + rot * Vector3.up * halfT, V(crossWidth, thicknessY, lenExt), color);
        p.transform.rotation = rot;
        p.isStatic = true;
    }

    /// <summary>One sloped plank per terrace boundary (full width), replaces scattered stair pieces.</summary>
    static void AllTerraceBoundaryPlanks(GameObject parent, float halfX)
    {
        float band = TerrainBandWidth();
        float xCenter = 0f;
        float zHalf = BoundaryPlankDepthZ * 0.5f;
        float wide = halfX * 2f + PathLengthOverlap;
        for (int k = 1; k < TerrainLevels; k++)
        {
            float zB = TerrainNorthZ - k * band;
            float yN = (k - 1) * StepHeight;
            float yS = k * StepHeight;
            var a = V(xCenter, yN, zB + zHalf);
            var b = V(xCenter, yS, zB - zHalf);
            PathSegment(parent, $"BoundaryPlank_{k}", a, b, wide, C.Stairs, PlankThickness);
        }
    }

    static void TieredGround(GameObject parent, string name, float halfX, float halfZ, int segX, int segZPerBand)
    {
        float band = TerrainBandWidth();
        float x0 = -halfX, x1 = halfX;
        var holder = new GameObject(name);
        holder.transform.SetParent(parent.transform);
        holder.transform.localPosition = Vector3.zero;

        for (int i = 0; i < TerrainLevels; i++)
        {
            float zSouth = TerrainNorthZ - (i + 1) * band;
            float zNorth = TerrainNorthZ - i * band;
            float zMin = Mathf.Max(-halfZ, zSouth);
            float zMax = Mathf.Min(halfZ, zNorth);
            if (zMax <= zMin + 1e-3f) continue;
            float y = i * StepHeight;
            FlatTerrainPatch(holder, $"Terrace_{i}", x0, x1, zMin, zMax, y, segX, segZPerBand);
        }
    }

    static void FlatTerrainPatch(GameObject parent, string name, float x0, float x1, float zMin, float zMax, float y, int segX, int segZ)
    {
        int vx = segX + 1, vz = segZ + 1;
        var verts = new Vector3[vx * vz];
        var uvs = new Vector2[vx * vz];

        for (int iz = 0; iz < vz; iz++)
        {
            float tz = iz / (float)segZ;
            float z = Mathf.Lerp(zMin, zMax, tz);
            for (int ix = 0; ix < vx; ix++)
            {
                float tx = ix / (float)segX;
                float x = Mathf.Lerp(x0, x1, tx);
                int i = iz * vx + ix;
                verts[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2(tx, tz);
            }
        }

        var tris = new List<int>(segX * segZ * 6);
        for (int iz = 0; iz < segZ; iz++)
        {
            for (int ix = 0; ix < segX; ix++)
            {
                int i00 = iz * vx + ix;
                int i10 = i00 + 1;
                int i01 = i00 + vx;
                int i11 = i01 + 1;
                tris.Add(i00);
                tris.Add(i01);
                tris.Add(i10);
                tris.Add(i10);
                tris.Add(i01);
                tris.Add(i11);
            }
        }

        var mesh = new Mesh { name = name + "_Mesh" };
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.isStatic = true;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", C.Ground);
        mat.SetFloat("_Smoothness", 0.1f);
        mr.sharedMaterial = mat;
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    static GameObject Box(GameObject parent, string name, Vector3 lp, Vector3 sc, Color col)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent.transform);
        b.transform.localPosition = lp;
        b.transform.localScale = sc;
        SetMat(b, col);
        b.isStatic = true;
        return b;
    }

    static void AddNavMeshLink(GameObject parent, string bldCode, string name, Vector3 start, Vector3 end, int f1, int f2)
    {
        var linkObj = new GameObject(name);
        linkObj.transform.SetParent(parent.transform);
        linkObj.transform.localPosition = start;

        var link = linkObj.AddComponent<NavMeshLink>();
        link.startPoint = Vector3.zero;
        link.endPoint = end - start;
        link.width = 3.0f;
        link.bidirectional = true;

        var el = linkObj.AddComponent<ElevatorLink>();
        el.Initialize(bldCode, f1, f2);
    }

    static GameObject Prim(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color col)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        SetMat(obj, col);
        return obj;
    }

    static GameObject Child(GameObject parent, string name)
    {
        var c = new GameObject(name);
        c.transform.SetParent(parent.transform);
        return c;
    }

    static void SetMat(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        
        // Try URP first, then Standard, then any fallback
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
        
        var m = new Material(shader);
        if (shader != null && shader.name.Contains("Universal Render Pipeline"))
            m.SetColor("_BaseColor", col);
        else if (m.HasProperty("_Color"))
            m.SetColor("_Color", col);
            
        r.sharedMaterial = m;
    }

    // Named colors for consistency
    static class C
    {
        public static readonly Color Wall = new(0.78f, 0.76f, 0.82f);
        public static readonly Color FloorSlab = new(0.92f, 0.91f, 0.87f);
        public static readonly Color Roof = new(0.52f, 0.44f, 0.38f);
        public static readonly Color Slab = new(0.86f, 0.85f, 0.82f);
        public static readonly Color Ground = new(0.38f, 0.52f, 0.32f);
        public static readonly Color Path = new(0.72f, 0.72f, 0.68f);
        public static readonly Color Corridor = new(0.88f, 0.86f, 0.78f);
        public static readonly Color Entrance = new(0.2f, 0.85f, 0.2f);
        public static readonly Color Elevator = new(0.9f, 0.25f, 0.9f);
        public static readonly Color WC = new(0.2f, 0.5f, 0.95f);
        public static readonly Color Ramp = new(0.95f, 0.6f, 0.15f);
        public static readonly Color Stairs = new(0.55f, 0.5f, 0.48f);
        public static readonly Color Smoking = new(0.6f, 0.6f, 0.1f);
        public static readonly Color Vending = new(0.1f, 0.8f, 0.6f);
        public static readonly Color Cafeteria = new(0.95f, 0.7f, 0.2f);
    }
}
