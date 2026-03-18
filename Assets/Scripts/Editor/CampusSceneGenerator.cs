using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class CampusSceneGenerator
{
    const float F = 3.5f; // floor height
    const float Wall = 0.3f;

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

        // ===== GROUND =====
        var ground = Prim(root, "Ground", PrimitiveType.Cube,
            new Vector3(0, -0.5f, 0), new Vector3(250, 1, 250), C.Ground);
        ground.isStatic = true;

        // ===== OUTDOOR PATHS (walkable surfaces slightly above ground) =====
        var paths = Child(root, "OutdoorPaths");
        // Main north road (Kelias link Vilniaus universiteto)
        Path(paths, "Road_North", V(-10, 0, 85), V(130, 0.08f, 8));
        // Central north-south walkway
        Path(paths, "Walk_CentralNS", V(5, 0, 20), V(8, 0.08f, 140));
        // South walkway
        Path(paths, "Walk_South", V(-5, 0, -45), V(120, 0.08f, 8));
        // West walkway
        Path(paths, "Walk_West", V(-70, 0, 20), V(8, 0.08f, 120));
        // East walkway
        Path(paths, "Walk_East", V(60, 0, 35), V(8, 0.08f, 100));
        // Parking area approach
        Path(paths, "Walk_Parking", V(-40, 0, 40), V(30, 0.08f, 8));
        // Connector between buildings south area
        Path(paths, "Walk_SouthConnector", V(-20, 0, -35), V(60, 0.08f, 8));
        // Walk to SRA-II
        Path(paths, "Walk_toSRA2", V(20, 0, -15), V(8, 0.08f, 30));

        // ==========================================================
        //  BUILDINGS - positioned to match the reference campus map
        //  Map: North = +Z, East = +X
        //  Reference: photo_2025-12-23 08.31.53.jpeg
        // ==========================================================

        Building(bldGroup, "SRK-I", V(-50, 0, 60), 55, 18, 4);
        Building(bldGroup, "SRC", V(42, 0, 65), 28, 28, 3);
        Building(bldGroup, "SRA-I", V(0, 0, 40), 38, 22, 4);
        Building(bldGroup, "SRK-II", V(52, 0, 18), 18, 45, 3);
        Building(bldGroup, "SRA-II", V(15, 0, -12), 32, 18, 3);
        Building(bldGroup, "SRL-I", V(5, 0, -35), 28, 18, 6);
        Building(bldGroup, "SRL-II", V(-45, 0, -35), 50, 18, 3);

        // ==========================================================
        //  CORRIDORS - connections between buildings
        // ==========================================================

        // SRK-I -> SRA-I (ground floor, runs south from SRK-I to north side of SRA-I)
        Corridor(corGroup, "Corridor_SRK1_SRA1",
            V(-22, 0, 51), V(-10, 0, 51), 5, F);

        // SRA-I -> SRC (3rd floor enclosed bridge, runs east)
        Corridor(corGroup, "Bridge_SRA1_SRC",
            V(19, F * 2, 52), V(28, F * 2, 58), 4, F);

        // SRA-I -> SRK-II (3rd floor enclosed bridge, runs east-southeast)
        Corridor(corGroup, "Bridge_SRA1_SRK2",
            V(19, F * 2, 35), V(43, F * 2, 30), 4, F);

        // SRA-I -> SRA-II / SRL-I area (ground floor long corridor south)
        Corridor(corGroup, "Corridor_SRA1_South",
            V(5, 0, 29), V(5, 0, -3), 5, F);

        // SRA-II -> SRL-I (short connection)
        Corridor(corGroup, "Corridor_SRA2_SRL1",
            V(10, 0, -21), V(10, 0, -26), 4, F);

        // SRL-I -> SRL-II (ground floor corridor west)
        Corridor(corGroup, "Corridor_SRL1_SRL2",
            V(-9, 0, -35), V(-20, 0, -35), 5, F);

        // ==========================================================
        //  MARKERS
        // ==========================================================

        // Entrances (green on map)
        Marker(entGroup, "Entrance_Main_SRK1", V(-50, 0, 69), C.Entrance, 2f);
        Marker(entGroup, "Entrance_SRC_Main", V(42, 0, 79), C.Entrance, 2f);
        Marker(entGroup, "Entrance_SRA1_Parking", V(-19, 0, 40), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRL1_South", V(5, 0, -44), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRL2_West", V(-70, 0, -35), C.Entrance, 1.5f);
        Marker(entGroup, "Entrance_SRK2_East", V(61, 0, 18), C.Entrance, 1.5f);

        // Elevators (marked with X-in-square on map)
        Marker(elvGroup, "Elevator_SRK1_Main", V(-40, 0, 60), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRK1_Side", V(-30, 0, 60), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRA1_Corridor", V(-15, 0, 51), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRL1", V(5, 0, -30), C.Elevator, 1.2f);
        Marker(elvGroup, "Elevator_SRC", V(42, 0, 70), C.Elevator, 1.2f);

        // WC
        Marker(wcGroup, "WC_SRK1_F1", V(-55, 0, 60), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRL1_F5", V(10, F * 4, -35), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRA2_F1", V(22, 0, -12), C.WC, 0.8f);
        Marker(wcGroup, "WC_SRC_F2", V(50, F, 65), C.WC, 0.8f);

        // Ramps
        RampMarker(rmpGroup, "Ramp_SRK1", V(-48, 0, 69));
        RampMarker(rmpGroup, "Ramp_SRC", V(40, 0, 79));

        // ==========================================================
        //  PLAYER
        // ==========================================================
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = V(-50, 0.1f, 69); // at main entrance
        player.tag = "Player";

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerModel";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = V(0, 1, 0);
        capsule.transform.localScale = V(0.8f, 1, 0.8f);
        SetMat(capsule, new Color(0.2f, 0.6f, 1f));

        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.angularSpeed = 360f;

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
        camObj.transform.position = V(0, 60, -30);
        camObj.transform.rotation = Quaternion.Euler(65, 0, 0);

        var camCtrl = camObj.GetComponent<CampusCameraController>();
        if (camCtrl == null) camCtrl = camObj.AddComponent<CampusCameraController>();

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
        geoObj.AddComponent<GeoAnchorSystem>();

        // ==========================================================
        //  NAVIGATION PREFERENCES
        // ==========================================================
        var navPrefObj = new GameObject("NavigationPreferences");
        navPrefObj.transform.SetParent(root.transform);
        navPrefObj.AddComponent<NavigationPreferences>();

        // ==========================================================
        //  NAV MESH SURFACE
        // ==========================================================
        var navSurface = new GameObject("NavMeshSurface");
        navSurface.transform.SetParent(root.transform);
        var surface = navSurface.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;

        // ==========================================================
        //  ADD BuildingRevealer TO EACH BUILDING
        // ==========================================================
        foreach (Transform child in bldGroup.transform)
            child.gameObject.AddComponent<BuildingRevealer>();

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("[CampusNav] Full campus scene generated: 7 buildings, 6 corridors, player, camera, navigator.");
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

        // 4 walls
        Box(bld, "Wall_N", V(0, h / 2, d / 2), V(w, h, Wall), C.Wall);
        Box(bld, "Wall_S", V(0, h / 2, -d / 2), V(w, h, Wall), C.Wall);
        Box(bld, "Wall_E", V(w / 2, h / 2, 0), V(Wall, h, d), C.Wall);
        Box(bld, "Wall_W", V(-w / 2, h / 2, 0), V(Wall, h, d), C.Wall);

        // Roof
        Box(bld, "Roof", V(0, h + 0.1f, 0), V(w + 0.4f, 0.2f, d + 0.4f), C.Roof);

        // Inter-floor slabs
        for (int i = 1; i < floors; i++)
            Box(bld, $"Slab_F{i}", V(0, i * F, 0), V(w - 0.2f, 0.12f, d - 0.2f), C.Slab);

        // Trigger collider for BuildingRevealer
        var col = bld.AddComponent<BoxCollider>();
        col.center = V(0, h / 2, 0);
        col.size = V(w + 6, h + 4, d + 6);
        col.isTrigger = true;

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

        // Floor
        var fl = Prim(cor, "Floor", PrimitiveType.Cube, mid + Vector3.up * 0.05f, V(w, 0.1f, len + w), C.Corridor);
        fl.transform.rotation = rot;
        fl.isStatic = true;

        // Left wall
        var wl = Prim(cor, "WallL", PrimitiveType.Cube,
            mid + rot * V(-w / 2, h / 2, 0), V(Wall, h, len + w), C.Wall);
        wl.transform.rotation = rot;
        wl.isStatic = true;

        // Right wall
        var wr = Prim(cor, "WallR", PrimitiveType.Cube,
            mid + rot * V(w / 2, h / 2, 0), V(Wall, h, len + w), C.Wall);
        wr.transform.rotation = rot;
        wr.isStatic = true;

        // Ceiling
        var ceil = Prim(cor, "Ceil", PrimitiveType.Cube,
            mid + Vector3.up * h, V(w + Wall * 2, 0.15f, len + w), C.Roof);
        ceil.transform.rotation = rot;
        ceil.isStatic = true;
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

    static void Path(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        var p = Prim(parent, name, PrimitiveType.Cube, pos, scale, C.Path);
        p.isStatic = true;
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
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", col);
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
    }
}
