using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class RoutePointGenerator
{
    const float F = 3.5f; // floor height — must match CampusSceneGenerator

    // Building definitions: code, position, width, depth, floors
    struct BuildingDef
    {
        public string Code;
        public Vector3 Pos;
        public float W, D;
        public int Floors;
        public BuildingDef(string code, Vector3 pos, float w, float d, int floors)
        { Code = code; Pos = pos; W = w; D = d; Floors = floors; }
    }

    // Flat terrain — everything at Y=0
    static float TerrainY(float z) => 0f;

    [MenuItem("CampusNav/Generate All Route Points")]
    static void GenerateAll()
    {
        if (!EditorUtility.DisplayDialog("Generate Route Points",
            "Создать все RoutePoint ассеты и заполнить RouteDatabase?\nСуществующие ассеты в Assets/RoutePoints/ будут перезаписаны.",
            "Создать", "Отмена"))
            return;

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/RoutePoints"))
            AssetDatabase.CreateFolder("Assets", "RoutePoints");

        var allPoints = new List<RoutePoint>();

        // Building definitions matching CampusYardSceneGenerator (real campus layout)
        var buildings = new BuildingDef[]
        {
            new("SRC",    new Vector3(-15, TerrainY(-25), -25),  38, 35, 3),  // S1
            new("SRA-I",  new Vector3(25, TerrainY(15), 15),     28, 30, 4),  // S2
            new("SRK-I",  new Vector3(35, TerrainY(-25), -25),   55, 18, 4),  // S3
            new("SRA-II", new Vector3(18, TerrainY(50), 50),     25, 22, 3),  // S4
            new("SRK-II", new Vector3(-10, TerrainY(20), 20),    20, 30, 3),  // S5
            new("SRL-II", new Vector3(28, TerrainY(72), 72),     22, 20, 3),  // S6
            new("SRL-I",  new Vector3(60, TerrainY(72), 72),     40, 18, 6),  // S7
        };

        // ===== ENTRANCES (matching real campus map) =====
        allPoints.Add(Create("Entrance_SRC", "SRC", 0, "E01",
            new Vector3(-15, TerrainY(-43), -43), RoutePointCategory.Entrance, "S1 - Centriniai rūmai entrance"));
        allPoints.Add(Create("Entrance_SRA1", "SRA-I", 0, "E02",
            new Vector3(39, TerrainY(15), 15), RoutePointCategory.Entrance, "S2 - Auditorinis korpusas entrance"));
        allPoints.Add(Create("Entrance_SRK1", "SRK-I", 0, "E03",
            new Vector3(35, TerrainY(-34), -34), RoutePointCategory.Entrance, "S3 - Mokomasis korpusas entrance"));
        allPoints.Add(Create("Entrance_SRA2", "SRA-II", 0, "E04",
            new Vector3(18, TerrainY(39), 39), RoutePointCategory.Entrance, "S4 - Auditorinis korpusas II entrance"));
        allPoints.Add(Create("Entrance_SRK2", "SRK-II", 0, "E05",
            new Vector3(-20, TerrainY(20), 20), RoutePointCategory.Entrance, "S5 - Mokomasis korpusas II entrance"));
        allPoints.Add(Create("Entrance_SRL2", "SRL-II", 0, "E06",
            new Vector3(28, TerrainY(62), 62), RoutePointCategory.Entrance, "S6 - Laboratorinis korpusas II entrance"));
        allPoints.Add(Create("Entrance_SRL1", "SRL-I", 0, "E07",
            new Vector3(60, TerrainY(63), 63), RoutePointCategory.Entrance, "S7 - Laboratorinis korpusas I entrance"));

        // ===== ELEVATORS =====
        allPoints.Add(Create("Elevator_SRC", "SRC", 0, "LF01",
            new Vector3(-10, TerrainY(-25), -25), RoutePointCategory.Elevator, "SRC elevator"));
        allPoints.Add(Create("Elevator_SRA1", "SRA-I", 0, "LF02",
            new Vector3(20, TerrainY(15), 15), RoutePointCategory.Elevator, "SRA-I elevator"));
        allPoints.Add(Create("Elevator_SRK1", "SRK-I", 0, "LF03",
            new Vector3(30, TerrainY(-25), -25), RoutePointCategory.Elevator, "SRK-I elevator"));
        allPoints.Add(Create("Elevator_SRL1", "SRL-I", 0, "LF04",
            new Vector3(55, TerrainY(72), 72), RoutePointCategory.Elevator, "SRL-I elevator"));

        // ===== CAFETERIA =====
        allPoints.Add(Create("Cafeteria_SRC", "SRC", 1, "CF01",
            new Vector3(-15, TerrainY(-25) + 0.1f, -20), RoutePointCategory.Cafeteria, "Cafeteria in SRC"));

        // ===== VENDING MACHINES =====
        allPoints.Add(Create("Vending_SRA1", "SRA-I", 1, "VM01",
            new Vector3(30, TerrainY(15) + 0.1f, 15), RoutePointCategory.VendingMachine, "Vending machine at SRA-I"));
        allPoints.Add(Create("Vending_SRK1", "SRK-I", 1, "VM02",
            new Vector3(35, TerrainY(-25) + 0.1f, -22), RoutePointCategory.VendingMachine, "Vending machine at SRK-I"));

        // ===== SMOKING AREAS =====
        allPoints.Add(Create("Smoking_SRC", "SRC", 0, "SM01",
            new Vector3(-18, TerrainY(-46), -46), RoutePointCategory.SmokingArea, "Smoking area near SRC entrance"));
        allPoints.Add(Create("Smoking_SRA1", "SRA-I", 0, "SM02",
            new Vector3(42, TerrainY(13), 13), RoutePointCategory.SmokingArea, "Smoking area near SRA-I entrance"));
        allPoints.Add(Create("Smoking_SRK1", "SRK-I", 0, "SM03",
            new Vector3(38, TerrainY(-37), -37), RoutePointCategory.SmokingArea, "Smoking area near SRK-I entrance"));

        // ===== WC (at least one per building, per floor where applicable) =====
        foreach (var bld in buildings)
        {
            for (int floor = 1; floor <= bld.Floors; floor++)
            {
                float y = bld.Pos.y + (floor - 1) * F + 0.1f;
                // WC typically near the entrance side (north)
                Vector3 wcPos = new(bld.Pos.x + bld.W * 0.35f, y, bld.Pos.z + bld.D * 0.35f);
                string code = bld.Code.Replace("-", "");
                allPoints.Add(Create($"WC_{code}_F{floor}", bld.Code, floor, $"WC{floor}",
                    wcPos, RoutePointCategory.WC, $"Restroom {bld.Code} floor {floor}"));
            }
        }

        // ===== STAIRS (one stairwell per building) =====
        foreach (var bld in buildings)
        {
            if (bld.Floors > 1)
            {
                Vector3 stairsPos = new(bld.Pos.x - bld.W * 0.3f, bld.Pos.y + 0.1f, bld.Pos.z + bld.D * 0.3f);
                string code = bld.Code.Replace("-", "");
                allPoints.Add(Create($"Stairs_{code}", bld.Code, 1, "ST01",
                    stairsPos, RoutePointCategory.Stairs, $"Main stairwell {bld.Code}"));
            }
        }

        // ===== CLASSROOMS / AUDITORIUMS / LABS / OFFICES =====
        // Generate rooms for each building, each floor
        // Room numbering: BuildingFloor-RoomNumber (e.g., SRK-I 2-01)
        foreach (var bld in buildings)
        {
            int roomsPerFloor = GetRoomsPerFloor(bld.Code);
            for (int floor = 1; floor <= bld.Floors; floor++)
            {
                float y = bld.Pos.y + (floor - 1) * F + 0.1f;
                for (int room = 1; room <= roomsPerFloor; room++)
                {
                    // Distribute rooms along the building width
                    float t = (float)room / (roomsPerFloor + 1);
                    float x = bld.Pos.x + Mathf.Lerp(-bld.W * 0.4f, bld.W * 0.4f, t);
                    // Alternate rooms on north/south side
                    float zOffset = (room % 2 == 0) ? bld.D * 0.25f : -bld.D * 0.25f;
                    float z = bld.Pos.z + zOffset;

                    Vector3 pos = new(x, y, z);
                    string roomNum = $"{floor}{room:D2}";
                    string roomName = $"{bld.Code} {roomNum}";
                    var category = GetRoomCategory(bld.Code, floor, room);

                    allPoints.Add(Create($"Room_{bld.Code.Replace("-", "")}_{roomNum}",
                        bld.Code, floor, roomNum, pos, category,
                        $"{category} in {bld.Code}, floor {floor}"));
                }
            }
        }

        // ===== CORRIDORS =====
        allPoints.Add(Create("Corridor_S1_S3", "", 1, "CR01",
            new Vector3(6, TerrainY(-25) + 0.1f, -25), RoutePointCategory.Corridor, "Corridor S1(SRC) to S3(SRK-I)"));
        allPoints.Add(Create("Corridor_S5_S2", "", 1, "CR02",
            new Vector3(5, TerrainY(18) + 0.1f, 18), RoutePointCategory.Corridor, "Corridor S5(SRK-II) to S2(SRA-I)"));
        allPoints.Add(Create("Corridor_S2_S3", "", 1, "CR03",
            new Vector3(28, TerrainY(-8) + 0.1f, -8), RoutePointCategory.Corridor, "Corridor S2(SRA-I) to S3(SRK-I)"));
        allPoints.Add(Create("Corridor_S2_S4", "", 1, "CR04",
            new Vector3(22, TerrainY(35) + 0.1f, 35), RoutePointCategory.Corridor, "Corridor S2(SRA-I) to S4(SRA-II)"));
        allPoints.Add(Create("Corridor_S4_S6", "", 1, "CR05",
            new Vector3(25, TerrainY(62) + 0.1f, 62), RoutePointCategory.Corridor, "Corridor S4(SRA-II) to S6(SRL-II)"));
        allPoints.Add(Create("Corridor_S6_S7", "", 1, "CR06",
            new Vector3(40, TerrainY(72) + 0.1f, 72), RoutePointCategory.Corridor, "Corridor S6(SRL-II) to S7(SRL-I)"));

        // ===== RAMPS =====
        allPoints.Add(Create("Ramp_SRC", "SRC", 0, "RM01",
            new Vector3(-18, TerrainY(-43), -43), RoutePointCategory.Ramp, "Wheelchair ramp SRC"));
        allPoints.Add(Create("Ramp_SRA1", "SRA-I", 0, "RM02",
            new Vector3(37, TerrainY(13), 13), RoutePointCategory.Ramp, "Wheelchair ramp SRA-I"));

        // ===== PARKING =====
        allPoints.Add(Create("Parking_West", "", 0, "PK01",
            new Vector3(-38, TerrainY(0), 0), RoutePointCategory.Parking, "West parking lot"));
        allPoints.Add(Create("Parking_South", "", 0, "PK02",
            new Vector3(20, TerrainY(-52), -52), RoutePointCategory.Parking, "South parking lot"));

        // ===== LIBRARY =====
        allPoints.Add(Create("Library_SRC", "SRC", 2, "LB01",
            new Vector3(-15, TerrainY(-25) + F + 0.1f, -25), RoutePointCategory.Library, "Library in SRC, 2nd floor"));

        // ===== NAVIGATION WAYPOINTS (Outdoor grid covering campus area) =====
        for (float x = -60; x <= 90; x += 20)
        {
            for (float z = -65; z <= 80; z += 20)
            {
                float ox = x + 3, oz = z + 3;
                allPoints.Add(Create($"WP_{ox}_{oz}", "", 0, "WP",
                    new Vector3(ox, TerrainY(oz) + 0.1f, oz), RoutePointCategory.Other, "Navigation waypoint"));
            }
        }

        // ===== Save all to database =====
        var dbGuids = AssetDatabase.FindAssets("t:RouteDatabase");
        RouteDatabase db = null;
        if (dbGuids.Length > 0)
        {
            db = AssetDatabase.LoadAssetAtPath<RouteDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));
        }
        else
        {
            db = ScriptableObject.CreateInstance<RouteDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/RouteDatabase.asset");
        }

        db.allPoints.Clear();
        db.allPoints.AddRange(allPoints);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RoutePointGenerator] Created {allPoints.Count} route points and populated RouteDatabase.");
    }

    static RoutePoint Create(string name, string buildingCode, int floor, string roomNumber,
        Vector3 worldPos, RoutePointCategory category, string description)
    {
        string safeName = name.Replace(" ", "_").Replace("-", "_");
        string path = $"Assets/RoutePoints/{safeName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<RoutePoint>(path);
        if (existing != null)
        {
            existing.pointName = name;
            existing.buildingCode = buildingCode;
            existing.floor = floor;
            existing.roomNumber = roomNumber;
            existing.worldPosition = worldPos;
            existing.category = category;
            existing.description = description;
            existing.isAccessible = true;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var rp = ScriptableObject.CreateInstance<RoutePoint>();
        rp.pointName = name;
        rp.buildingCode = buildingCode;
        rp.floor = floor;
        rp.roomNumber = roomNumber;
        rp.worldPosition = worldPos;
        rp.category = category;
        rp.description = description;
        rp.isAccessible = true;

        AssetDatabase.CreateAsset(rp, path);
        return rp;
    }

    static int GetRoomsPerFloor(string buildingCode)
    {
        return buildingCode switch
        {
            "SRK-I" => 8,   // Large building
            "SRK-II" => 6,
            "SRA-I" => 7,
            "SRA-II" => 5,
            "SRC" => 5,
            "SRL-I" => 6,
            "SRL-II" => 8,  // Long building
            _ => 5
        };
    }

    static RoutePointCategory GetRoomCategory(string building, int floor, int room)
    {
        // Distribute room types realistically
        // Floor 1: mostly offices/admin, some classrooms
        // Floor 2-3: classrooms and auditoriums
        // Floor 4+: labs and offices

        if (floor == 1)
        {
            if (room <= 2) return RoutePointCategory.Office;
            return RoutePointCategory.Classroom;
        }
        if (floor <= 3)
        {
            if (room == 1) return RoutePointCategory.Auditorium;
            if (room % 3 == 0) return RoutePointCategory.Lab;
            return RoutePointCategory.Classroom;
        }
        // Upper floors
        if (room % 2 == 0) return RoutePointCategory.Lab;
        return RoutePointCategory.Office;
    }
}
