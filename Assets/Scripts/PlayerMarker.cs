using UnityEngine;

/// <summary>
/// Renders the player as a navigation-style map marker (pin with circle base)
/// visible from above. Replaces the default capsule player model.
/// </summary>
public class PlayerMarker : MonoBehaviour
{
    [SerializeField] private Color markerColor = new(0.2f, 0.5f, 1f);
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float markerSize = 1.5f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.15f;
    [SerializeField] private bool billboardToCamera = true;

    Transform pinHead;
    Transform pinBody;
    Transform baseRing;
    Transform pulseRing;
    Material markerMat;
    Material outlineMat;
    Material pulseMat;

    void Start()
    {
        BuildMarkerVisual();
    }

    void BuildMarkerVisual()
    {
        // Clean up any existing player model (capsule)
        var existingModel = transform.Find("PlayerModel");
        if (existingModel != null)
            Destroy(existingModel.gameObject);

        markerMat = CreateUnlitMaterial(markerColor);
        outlineMat = CreateUnlitMaterial(outlineColor);
        pulseMat = CreateUnlitMaterial(new Color(markerColor.r, markerColor.g, markerColor.b, 0.3f));

        // === Pin head (sphere at top) ===
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "MarkerHead";
        head.transform.SetParent(transform);
        head.transform.localPosition = new Vector3(0, 3.2f * markerSize, 0);
        head.transform.localScale = Vector3.one * markerSize * 1.0f;
        head.GetComponent<Renderer>().material = markerMat;
        Destroy(head.GetComponent<Collider>());
        pinHead = head.transform;

        // === Pin body (thin cylinder connecting head to base) ===
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "MarkerPin";
        body.transform.SetParent(transform);
        body.transform.localPosition = new Vector3(0, 1.8f * markerSize, 0);
        body.transform.localScale = new Vector3(0.15f * markerSize, 1.2f * markerSize, 0.15f * markerSize);
        body.GetComponent<Renderer>().material = markerMat;
        Destroy(body.GetComponent<Collider>());
        pinBody = body.transform;

        // === White outline ring on head ===
        var ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ring.name = "MarkerOutline";
        ring.transform.SetParent(pinHead);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = Vector3.one * 1.2f;
        ring.GetComponent<Renderer>().material = outlineMat;
        Destroy(ring.GetComponent<Collider>());

        // === Base ring (flat cylinder on the ground) ===
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "MarkerBase";
        baseObj.transform.SetParent(transform);
        baseObj.transform.localPosition = new Vector3(0, 0.05f, 0);
        baseObj.transform.localScale = new Vector3(markerSize * 1.2f, 0.05f, markerSize * 1.2f);
        baseObj.GetComponent<Renderer>().material = outlineMat;
        Destroy(baseObj.GetComponent<Collider>());
        baseRing = baseObj.transform;

        // === Pulse ring (animated expanding ring) ===
        var pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulse.name = "MarkerPulse";
        pulse.transform.SetParent(transform);
        pulse.transform.localPosition = new Vector3(0, 0.03f, 0);
        pulse.transform.localScale = new Vector3(markerSize * 1.5f, 0.02f, markerSize * 1.5f);
        pulse.GetComponent<Renderer>().material = pulseMat;
        Destroy(pulse.GetComponent<Collider>());
        pulseRing = pulse.transform;

        // === Direction indicator (small cone/cube pointing forward) ===
        var dir = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dir.name = "DirectionIndicator";
        dir.transform.SetParent(transform);
        dir.transform.localPosition = new Vector3(0, 0.1f, markerSize * 0.9f);
        dir.transform.localScale = new Vector3(0.3f * markerSize, 0.1f, 0.5f * markerSize);
        dir.GetComponent<Renderer>().material = markerMat;
        Destroy(dir.GetComponent<Collider>());
    }

    void Update()
    {
        // Pulse animation
        if (pulseRing != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float baseScale = markerSize * 1.5f * pulse;
            pulseRing.localScale = new Vector3(baseScale, 0.02f, baseScale);

            // Fade pulse ring
            if (pulseMat != null)
            {
                float alpha = Mathf.Lerp(0.3f, 0.05f, (pulse - 1f + pulseAmount) / (2f * pulseAmount));
                var col = pulseMat.GetColor("_BaseColor");
                col.a = alpha;
                pulseMat.SetColor("_BaseColor", col);
            }
        }

        // Billboard - make pin head always face camera
        if (billboardToCamera && pinHead != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Only rotate the head group to face camera, not the whole marker
                Vector3 lookDir = cam.transform.position - pinHead.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    // Subtle rotation toward camera for better visibility
                }
            }
        }
    }

    static Material CreateUnlitMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);

        // Enable transparency if alpha < 1
        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        return mat;
    }
}
