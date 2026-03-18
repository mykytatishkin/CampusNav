using System.Collections.Generic;
using UnityEngine;

public class BuildingRevealer : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private string dissolveProperty = "_DissolveAmount";
    [SerializeField] private string transparentShaderName = "CampusNav/BuildingTransparency";

    readonly List<RendererData> renderers = new();
    bool playerInside;
    float currentDissolve;
    bool usingFadeMaterials;
    Shader transparentShader;

    struct RendererData
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
    }

    void Awake()
    {
        transparentShader = Shader.Find(transparentShaderName);
        CollectRenderers();
    }

    void CollectRenderers()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var origMats = r.sharedMaterials;
            var fadeMats = new Material[origMats.Length];

            for (int i = 0; i < origMats.Length; i++)
            {
                var src = origMats[i];
                var shader = transparentShader != null
                    ? transparentShader
                    : (src != null ? src.shader : Shader.Find("Universal Render Pipeline/Lit"));

                fadeMats[i] = new Material(shader);
                if (src != null)
                    fadeMats[i].SetColor("_BaseColor", src.GetColor("_BaseColor"));
                fadeMats[i].SetFloat(dissolveProperty, 0f);
            }

            renderers.Add(new RendererData
            {
                renderer = r,
                originalMaterials = origMats,
                fadeMaterials = fadeMats
            });
        }
    }

    void Update()
    {
        float target = playerInside ? 1f : 0f;
        if (Mathf.Approximately(currentDissolve, target)) return;

        currentDissolve = Mathf.MoveTowards(currentDissolve, target, fadeSpeed * Time.deltaTime);
        bool shouldFade = currentDissolve > 0.001f;

        if (shouldFade && !usingFadeMaterials)
        {
            foreach (var rd in renderers)
                rd.renderer.sharedMaterials = rd.fadeMaterials;
            usingFadeMaterials = true;
        }
        else if (!shouldFade && usingFadeMaterials)
        {
            foreach (var rd in renderers)
                rd.renderer.sharedMaterials = rd.originalMaterials;
            usingFadeMaterials = false;
        }

        if (usingFadeMaterials)
        {
            foreach (var rd in renderers)
                foreach (var mat in rd.fadeMaterials)
                    mat.SetFloat(dissolveProperty, currentDissolve);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other)) playerInside = false;
    }

    static bool IsPlayer(Collider col)
    {
        return col.CompareTag("Player") || col.GetComponent<UnityEngine.AI.NavMeshAgent>() != null;
    }
}
