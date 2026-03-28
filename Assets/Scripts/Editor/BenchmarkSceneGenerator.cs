using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public static class BenchmarkSceneGenerator
{
    const float F = 3.5f;
    const float Wall = 0.25f;

    [MenuItem("CampusNav/Generate Benchmark Scene")]
    static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Benchmark Scene",
            "Сгенерировать тестовую сцену для сравнения алгоритмов?\nОбъект 'BenchmarkWorld' будет пересоздан.",
            "Создать", "Отмена"))
            return;

        var old = GameObject.Find("BenchmarkWorld");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("BenchmarkWorld");
        Undo.RegisterCreatedObjectUndo(root, "Generate Benchmark Scene");

        // ===== TERRAIN (multi-level) =====
        // Level 0 (north, z>20): y=0
        // Level 1 (center, -10<z<20): y=2
        // Level 2 (south, z<-10): y=4
        float[] terrainY = { 0f, 2f, 4f };
        float[] terrainZ = { 20f, -10f }; // boundaries between levels

        // North plateau (y=0)
        var gndN = Prim(root, "Ground_North", PrimitiveType.Cube,
            V(0, -0.05f, 50), V(160, 0.1f, 80), new Color(0.35f, 0.50f, 0.30f));
        gndN.isStatic = true;
        // Center plateau (y=2)
        var gndC = Prim(root, "Ground_Center", PrimitiveType.Cube,
            V(0, 2f - 0.05f, 5), V(160, 0.1f, 30), new Color(0.32f, 0.48f, 0.28f));
        gndC.isStatic = true;
        // South plateau (y=4)
        var gndS = Prim(root, "Ground_South", PrimitiveType.Cube,
            V(0, 4f - 0.05f, -40), V(160, 0.1f, 60), new Color(0.30f, 0.45f, 0.26f));
        gndS.isStatic = true;

        // Ramps between levels (walkable slopes for NavMesh)
        var ramps = Child(root, "TerrainRamps");
        Color rampCol = new(0.60f, 0.58f, 0.52f);
        // Ramp: North→Center (z=20, y: 0→2) — wide slope
        Ramp(ramps, "Ramp_N_to_C", V(0, 0, 23), V(0, 2, 17), 30f, rampCol);
        // Ramp: Center→South (z=-10, y: 2→4)
        Ramp(ramps, "Ramp_C_to_S", V(0, 2, -7), V(0, 4, -13), 30f, rampCol);
        // Side ramp west (narrow, alternative route)
        Ramp(ramps, "Ramp_N_to_C_West", V(-50, 0, 23), V(-50, 2, 17), 6f, rampCol);
        Ramp(ramps, "Ramp_C_to_S_West", V(-50, 2, -7), V(-50, 4, -13), 6f, rampCol);
        // Side ramp east
        Ramp(ramps, "Ramp_N_to_C_East", V(50, 0, 23), V(50, 2, 17), 6f, rampCol);
        Ramp(ramps, "Ramp_C_to_S_East", V(50, 2, -7), V(50, 4, -13), 6f, rampCol);
        // Stairs (narrow steep ramp, shorter but steeper)
        Ramp(ramps, "Stairs_N_C_center", V(25, 0, 22), V(25, 2, 19), 3f, new Color(0.55f, 0.50f, 0.48f));
        Ramp(ramps, "Stairs_C_S_center", V(-25, 2, -8), V(-25, 4, -11), 3f, new Color(0.55f, 0.50f, 0.48f));

        // ===== MAZE-LIKE COMPLEX LAYOUT =====
        var walls = new GameObject("Walls");
        walls.transform.SetParent(root.transform);

        Color wallCol = new(0.75f, 0.73f, 0.78f);
        Color floorCol = new(0.90f, 0.88f, 0.85f);

        // --- Building A (northwest, on north plateau y=0) ---
        var bldA = Child(root, "Building_A");
        bldA.transform.position = V(-40, 0, 40);
        bldA.isStatic = true;
        // Main wing
        FloorSlab(bldA, "FloorA1", V(0, 0.05f, 0), 30, 20, floorCol);
        WallBox(bldA, "WallA_N", V(0, F * 1.5f, 10), 30, F * 3, Wall, wallCol);
        WallBox(bldA, "WallA_S", V(0, F * 1.5f, -10), 30, F * 3, Wall, wallCol);
        WallBox(bldA, "WallA_W", V(-15, F * 1.5f, 0), Wall, F * 3, 20, wallCol);
        // East wall with doorway
        WallBox(bldA, "WallA_E_top", V(15, F * 1.5f, 5), Wall, F * 3, 10, wallCol);
        WallBox(bldA, "WallA_E_bot", V(15, F * 1.5f, -7), Wall, F * 3, 6, wallCol);
        // Extension wing (south)
        FloorSlab(bldA, "FloorA2", V(5, 0.05f, -22), 20, 14, floorCol);
        WallBox(bldA, "WallA2_E", V(15, F * 1.5f, -22), Wall, F * 3, 14, wallCol);
        WallBox(bldA, "WallA2_W", V(-5, F * 1.5f, -22), Wall, F * 3, 14, wallCol);
        WallBox(bldA, "WallA2_S", V(5, F * 1.5f, -29), 20, F * 3, Wall, wallCol);
        // Upper floors
        FloorSlab(bldA, "SlabA_F2", V(0, F, 0), 30, 20, new Color(0.85f, 0.83f, 0.80f));
        FloorSlab(bldA, "SlabA_F3", V(0, F * 2, 0), 30, 20, new Color(0.85f, 0.83f, 0.80f));
        // Roof
        FloorSlab(bldA, "RoofA", V(0, F * 3 + 0.1f, 0), 31, 21, new Color(0.50f, 0.43f, 0.36f));
        bldA.AddComponent<BuildingRevealer>();

        // --- Building B (east, on north plateau y=0) ---
        var bldB = Child(root, "Building_B");
        bldB.transform.position = V(40, 0, 25);
        bldB.isStatic = true;
        FloorSlab(bldB, "FloorB", V(0, 0.05f, 0), 25, 35, floorCol);
        WallBox(bldB, "WallB_N", V(0, F, 17.5f), 25, F * 2, Wall, wallCol);
        WallBox(bldB, "WallB_S", V(0, F, -17.5f), 25, F * 2, Wall, wallCol);
        WallBox(bldB, "WallB_E", V(12.5f, F, 0), Wall, F * 2, 35, wallCol);
        // West wall with doorway
        WallBox(bldB, "WallB_W_top", V(-12.5f, F, 12), Wall, F * 2, 11, wallCol);
        WallBox(bldB, "WallB_W_bot", V(-12.5f, F, -12), Wall, F * 2, 11, wallCol);
        FloorSlab(bldB, "SlabB_F2", V(0, F, 0), 25, 35, new Color(0.85f, 0.83f, 0.80f));
        FloorSlab(bldB, "RoofB", V(0, F * 2 + 0.1f, 0), 26, 36, new Color(0.50f, 0.43f, 0.36f));
        bldB.AddComponent<BuildingRevealer>();

        // --- Building C (south, on south plateau y=4) ---
        var bldC = Child(root, "Building_C");
        bldC.transform.position = V(0, 4, -40);
        bldC.isStatic = true;
        FloorSlab(bldC, "FloorC", V(0, 0.05f, 0), 50, 15, floorCol);
        WallBox(bldC, "WallC_N_L", V(-18, F * 2, 7.5f), 14, F * 4, Wall, wallCol);
        WallBox(bldC, "WallC_N_R", V(18, F * 2, 7.5f), 14, F * 4, Wall, wallCol);
        WallBox(bldC, "WallC_S", V(0, F * 2, -7.5f), 50, F * 4, Wall, wallCol);
        WallBox(bldC, "WallC_E", V(25, F * 2, 0), Wall, F * 4, 15, wallCol);
        WallBox(bldC, "WallC_W", V(-25, F * 2, 0), Wall, F * 4, 15, wallCol);
        for (int i = 1; i <= 3; i++)
            FloorSlab(bldC, $"SlabC_F{i + 1}", V(0, F * i, 0), 50, 15, new Color(0.85f, 0.83f, 0.80f));
        FloorSlab(bldC, "RoofC", V(0, F * 4 + 0.1f, 0), 51, 16, new Color(0.50f, 0.43f, 0.36f));
        bldC.AddComponent<BuildingRevealer>();

        // ===== OUTDOOR PATHS (respect terrain heights) =====
        var paths = Child(root, "Paths");
        Color pathCol = new(0.70f, 0.70f, 0.66f);

        // North level (y=0) paths
        PathBox(paths, "Path_A_B", V(5, 0.04f, 35), 60, 5, pathCol);
        PathBox(paths, "Path_North_NS", V(-20, 0.04f, 35), 5, 30, pathCol);
        PathBox(paths, "Path_Outer_N", V(0, 0.04f, 60), 120, 4, pathCol);

        // Center level (y=2) paths
        PathBox(paths, "Plaza", V(5, 2.04f, 5), 30, 20, new Color(0.72f, 0.68f, 0.62f));
        PathBox(paths, "Path_Center_W", V(-30, 2.04f, 5), 5, 20, pathCol);
        PathBox(paths, "Path_Center_E", V(35, 2.04f, 5), 5, 20, pathCol);
        PathBox(paths, "Path_Zigzag1", V(-10, 2.04f, 12), 25, 3, pathCol);
        PathBox(paths, "Path_Zigzag2", V(15, 2.04f, 0), 3, 15, pathCol);

        // South level (y=4) paths
        PathBox(paths, "Path_South_EW", V(0, 4.04f, -30), 80, 5, pathCol);
        PathBox(paths, "Path_toC", V(0, 4.04f, -35), 5, 15, pathCol);
        PathBox(paths, "Path_Outer_S", V(0, 4.04f, -60), 120, 4, pathCol);

        // Side paths along terrain edges
        PathBox(paths, "Path_Outer_W_N", V(-60, 0.04f, 40), 4, 40, pathCol);
        PathBox(paths, "Path_Outer_W_C", V(-60, 2.04f, 5), 4, 20, pathCol);
        PathBox(paths, "Path_Outer_W_S", V(-60, 4.04f, -35), 4, 40, pathCol);
        PathBox(paths, "Path_Outer_E_N", V(60, 0.04f, 40), 4, 40, pathCol);
        PathBox(paths, "Path_Outer_E_C", V(60, 2.04f, 5), 4, 20, pathCol);
        PathBox(paths, "Path_Outer_E_S", V(60, 4.04f, -35), 4, 40, pathCol);

        // ===== OBSTACLES (force non-trivial paths at each level) =====
        var obstacles = Child(root, "Obstacles");
        Color obstCol = new(0.55f, 0.45f, 0.40f);
        // North level obstacles
        WallBox(obstacles, "Wall_N1", V(-5, 0.8f, 30), 18, 1.6f, 0.5f, obstCol);
        WallBox(obstacles, "Wall_N2", V(20, 0.8f, 45), 0.5f, 1.6f, 12, obstCol);
        // Center level obstacles
        WallBox(obstacles, "Wall_C1", V(-8, 2 + 0.8f, 8), 14, 1.6f, 0.5f, obstCol);
        WallBox(obstacles, "Wall_C2", V(18, 2 + 0.8f, 2), 0.5f, 1.6f, 16, obstCol);
        WallBox(obstacles, "Wall_C3", V(-5, 2 + 0.8f, -2), 8, 1.6f, 0.5f, obstCol);
        // South level obstacles
        WallBox(obstacles, "Wall_S1", V(-15, 4 + 0.8f, -30), 20, 1.6f, 0.5f, obstCol);
        WallBox(obstacles, "Wall_S2", V(10, 4 + 0.8f, -35), 0.5f, 1.6f, 15, obstCol);

        // ===== RETAINING WALLS (visual, terrain edges) =====
        Color retainCol = new(0.45f, 0.42f, 0.38f);
        WallBox(obstacles, "Retain_NC", V(0, 1f, 20), 160, 2f, 0.3f, retainCol);
        WallBox(obstacles, "Retain_CS", V(0, 3f, -10), 160, 2f, 0.3f, retainCol);

        // ===== DESTINATION MARKERS (ground floor of each building) =====
        var markers = Child(root, "Markers");

        // Point 1: Building A (north plateau, y=0)
        MarkerCylinder(markers, "Dest_A_Inside",
            V(-40, 0.5f, 40), new Color(1f, 0.3f, 0.3f), 1.5f);
        // Point 2: Building B (north plateau, y=0)
        MarkerCylinder(markers, "Dest_B_Inside",
            V(40, 0.5f, 25), new Color(0.3f, 1f, 0.3f), 1.5f);
        // Point 3: Building C (south plateau, y=4)
        MarkerCylinder(markers, "Dest_C_Inside",
            V(0, 4.5f, -40), new Color(0.3f, 0.3f, 1f), 1.5f);

        // Start marker (center plateau, y=2)
        MarkerCylinder(markers, "Start_Plaza",
            V(0, 2.5f, 5), new Color(1f, 0.9f, 0.2f), 2f);

        // ===== PLAYER =====
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = V(0, 2.1f, 5);
        player.tag = "Player";

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerModel";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = V(0, 1, 0);
        capsule.transform.localScale = V(0.8f, 1, 0.8f);
        SetMat(capsule, new Color(0.2f, 0.6f, 1f));

        var agent = player.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 5f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.angularSpeed = 360f;

        // ===== CAMERA =====
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        camObj.transform.position = V(0, 80, -60);
        camObj.transform.rotation = Quaternion.Euler(55, 0, 0);

        var camCtrl = camObj.GetComponent<CampusCameraController>();
        if (camCtrl == null) camCtrl = camObj.AddComponent<CampusCameraController>();

        var camSO = new SerializedObject(camCtrl);
        camSO.FindProperty("target").objectReferenceValue = player.transform;
        camSO.ApplyModifiedPropertiesWithoutUndo();

        // ===== NAVMESH SURFACE =====
        var navSurface = new GameObject("BenchmarkNavMesh");
        navSurface.transform.SetParent(root.transform);
        var surface = navSurface.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;

        // ===== BENCHMARK SYSTEM =====
        var benchObj = new GameObject("BenchmarkSystem");
        benchObj.transform.SetParent(root.transform);
        var benchmark = benchObj.AddComponent<PathfindingBenchmark>();

        // ===== ROUTE POINTS (for graph-based pathfinding) =====
        // Create inline route points for the benchmark
        var routeDb = ScriptableObject.CreateInstance<RouteDatabase>();
        routeDb.allPoints = new System.Collections.Generic.List<RoutePoint>();

        // Key navigation nodes
        // Nodes at correct terrain heights:
        // North (z>20): y=0, Center (-10<z<20): y=2, South (z<-10): y=4
        string[] nodeNames = {
            "Start_Plaza", "Path_North", "Path_South", "Path_East", "Path_West",
            "Building_A_Entrance", "Building_A_Inside", "Ramp_N_to_C",
            "Building_B_Entrance", "Building_B_Inside",
            "Building_C_Entrance", "Building_C_Inside", "Ramp_C_to_S",
            "Plaza_NE", "Plaza_NW", "Plaza_SE", "Plaza_SW",
            "Outer_NW", "Outer_NE", "Outer_SW", "Outer_SE",
            "Center_West", "Center_East", "Path_AB_Mid", "South_West"
        };
        Vector3[] nodePositions = {
            V(0, 2.1f, 5),       // Start (center plateau)
            V(0, 0.1f, 35),      // Path_North (north plateau)
            V(0, 4.1f, -30),     // Path_South (south plateau)
            V(35, 2.1f, 5),      // Path_East (center)
            V(-30, 2.1f, 5),     // Path_West (center)
            V(-25, 0.1f, 40),    // Building A entrance (north)
            V(-40, 0.1f, 40),    // Building A inside (north)
            V(0, 1.0f, 20),      // Ramp N→C
            V(27, 0.1f, 25),     // Building B entrance (north)
            V(40, 0.1f, 25),     // Building B inside (north)
            V(0, 4.1f, -33),     // Building C entrance (south)
            V(0, 4.1f, -40),     // Building C inside (south)
            V(0, 3.0f, -10),     // Ramp C→S
            V(15, 2.1f, 12),     // Plaza NE
            V(-15, 2.1f, 12),    // Plaza NW
            V(15, 2.1f, -2),     // Plaza SE
            V(-15, 2.1f, -2),    // Plaza SW
            V(-55, 0.1f, 50),    // Outer NW
            V(55, 0.1f, 50),     // Outer NE
            V(-55, 4.1f, -50),   // Outer SW
            V(55, 4.1f, -50),    // Outer SE
            V(-30, 2.1f, 5),     // Center West
            V(35, 2.1f, 5),      // Center East
            V(5, 0.1f, 35),      // Path AB Mid (north)
            V(-30, 4.1f, -30)    // South West
        };

        if (!AssetDatabase.IsValidFolder("Assets/BenchmarkData"))
            AssetDatabase.CreateFolder("Assets", "BenchmarkData");

        for (int i = 0; i < nodeNames.Length; i++)
        {
            var rp = ScriptableObject.CreateInstance<RoutePoint>();
            rp.pointName = nodeNames[i];
            rp.worldPosition = nodePositions[i];
            rp.floor = nodePositions[i].y > 1f ? Mathf.FloorToInt(nodePositions[i].y / F) + 1 : 1;
            rp.buildingCode = nodeNames[i].StartsWith("Building_A") ? "A" :
                              nodeNames[i].StartsWith("Building_B") ? "B" :
                              nodeNames[i].StartsWith("Building_C") ? "C" : "";
            rp.category = nodeNames[i].Contains("Floor") ? RoutePointCategory.Classroom :
                          nodeNames[i].Contains("Entrance") ? RoutePointCategory.Entrance :
                          RoutePointCategory.Corridor;
            rp.isAccessible = true;

            string safeName = nodeNames[i].Replace(" ", "_");
            string path = $"Assets/BenchmarkData/BM_{safeName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoutePoint>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(rp, existing);
                Object.DestroyImmediate(rp);
                rp = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(rp, path);
            }
            routeDb.allPoints.Add(rp);
        }

        string dbPath = "Assets/BenchmarkData/BenchmarkRouteDB.asset";
        var existingDb = AssetDatabase.LoadAssetAtPath<RouteDatabase>(dbPath);
        if (existingDb != null)
        {
            existingDb.allPoints = routeDb.allPoints;
            EditorUtility.SetDirty(existingDb);
            Object.DestroyImmediate(routeDb);
            routeDb = existingDb;
        }
        else
        {
            AssetDatabase.CreateAsset(routeDb, dbPath);
        }

        // Wire benchmark
        var benchSO = new SerializedObject(benchmark);
        benchSO.FindProperty("routeDatabase").objectReferenceValue = routeDb;
        benchSO.ApplyModifiedPropertiesWithoutUndo();

        // ===== BENCHMARK UI =====
        // Destinations: 6=A_Inside(north,y=0), 9=B_Inside(north,y=0), 11=C_Inside(south,y=4)
        CreateBenchmarkUI(root, agent, benchmark, routeDb,
            new Vector3[] { nodePositions[6], nodePositions[9], nodePositions[11] },
            new string[] { "Building A (north)", "Building B (north)", "Building C (south, uphill)" });

        // ===== LIGHTING =====
        var light = GameObject.Find("Directional Light");
        if (light == null)
        {
            light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            l.shadows = LightShadows.Soft;
        }
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[BenchmarkScene] Generated! Now:\n" +
            "1. Select BenchmarkNavMesh > Inspector > Bake\n" +
            "2. Press Play\n" +
            "3. Use the UI to select destinations and algorithms");
    }

    static void CreateBenchmarkUI(GameObject root,
        UnityEngine.AI.NavMeshAgent agent,
        PathfindingBenchmark benchmark,
        RouteDatabase routeDb,
        Vector3[] destinations, string[] destNames)
    {
        // Create Canvas
        var canvasObj = new GameObject("BenchmarkCanvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem — must exist at root level with InputActions assigned
        var existingES = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingES != null) Undo.DestroyObjectImmediate(existingES.gameObject);

        var esObj = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Assign the project's InputActionAsset so UI input actually works
        var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        if (actionsAsset != null)
        {
            var moduleSO = new SerializedObject(inputModule);
            moduleSO.FindProperty("m_ActionsAsset").objectReferenceValue = actionsAsset;
            moduleSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Add BenchmarkUI component
        var benchUI = canvasObj.AddComponent<BenchmarkUI>();
        var uiSO = new SerializedObject(benchUI);
        uiSO.FindProperty("benchmark").objectReferenceValue = benchmark;
        uiSO.FindProperty("playerAgent").objectReferenceValue = agent;
        uiSO.FindProperty("routeDatabase").objectReferenceValue = routeDb;

        // Set destination data via arrays
        var destPos = uiSO.FindProperty("destinationPositions");
        destPos.arraySize = destinations.Length;
        for (int i = 0; i < destinations.Length; i++)
            destPos.GetArrayElementAtIndex(i).vector3Value = destinations[i];

        var destNm = uiSO.FindProperty("destinationNames");
        destNm.arraySize = destNames.Length;
        for (int i = 0; i < destNames.Length; i++)
            destNm.GetArrayElementAtIndex(i).stringValue = destNames[i];

        uiSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ===== HELPERS =====

    static Vector3 V(float x, float y, float z) => new(x, y, z);

    static GameObject Child(GameObject parent, string name)
    {
        var c = new GameObject(name);
        c.transform.SetParent(parent.transform);
        return c;
    }

    static void FloorSlab(GameObject parent, string name, Vector3 lp, float w, float d, Color col)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent.transform);
        b.transform.localPosition = lp;
        b.transform.localScale = V(w, 0.12f, d);
        SetMat(b, col);
        b.isStatic = true;
    }

    static void WallBox(GameObject parent, string name, Vector3 lp, float w, float h, float d, Color col)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent.transform);
        b.transform.localPosition = lp;
        b.transform.localScale = V(w, h, d);
        SetMat(b, col);
        b.isStatic = true;
    }

    static void PathBox(GameObject parent, string name, Vector3 pos, float w, float d, Color col)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent.transform);
        b.transform.position = pos;
        b.transform.localScale = V(w, 0.08f, d);
        SetMat(b, col);
        b.isStatic = true;
    }

    static void Ramp(GameObject parent, string name, Vector3 top, Vector3 bottom, float width, Color col)
    {
        Vector3 mid = (top + bottom) * 0.5f;
        Vector3 dir = bottom - top;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = mid;
        obj.transform.localScale = V(width, 0.12f, len + 1f);
        obj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // Tilt to match slope
        float angle = Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.LookRotation(
            new Vector3(dir.x, 0, dir.z).normalized, Vector3.up)
            * Quaternion.Euler(-angle, 0, 0);
        SetMat(obj, col);
        obj.isStatic = true;
    }

    static GameObject MarkerCylinder(GameObject parent, string name, Vector3 pos, Color col, float radius)
    {
        var m = new GameObject(name);
        m.transform.SetParent(parent.transform);
        m.transform.position = pos;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Visual";
        vis.transform.SetParent(m.transform);
        vis.transform.position = pos;
        vis.transform.localScale = V(radius, 0.5f, radius);
        SetMat(vis, col);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        return m;
    }

    static GameObject Prim(GameObject parent, string name, PrimitiveType type,
        Vector3 pos, Vector3 scale, Color col)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        SetMat(obj, col);
        return obj;
    }

    static void SetMat(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", col);
        r.sharedMaterial = m;
    }
}
