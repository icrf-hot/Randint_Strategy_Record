using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Omnidirectional star background, one capped combat particle system, monitor volume and combat feedback.</summary>
public sealed class TerminalWorld : MonoBehaviour
{
    public bool perspectiveFloor;
    private static TerminalWorld current;
    private ParticleSystem particles;
    private Volume monitor;
    private Camera view;
    private float shakeRemaining;
    private Vector3 offset;
    private Material sky, previousSky;
    private Skybox cameraSkybox;
    private Material previousCameraSky;
    private bool previousCameraSkyEnabled;
    private CameraClearFlags previousClearFlags;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private VolumeProfile profile;
    private void Start()
    {
        current = this; view = Camera.main;
        if (view == null) view = FindFirstObjectByType<Camera>();
        if (view != null) {
            previousClearFlags = view.clearFlags;
            view.backgroundColor = UITheme.BgBase;
            sky = Material("Randint/TerminalStarfield");
            sky.SetColor("_BaseColor", UITheme.BgBase);
            sky.SetColor("_StarColor", UITheme.TextMain);
            sky.SetColor("_AccentColor", UITheme.Accent);
            previousSky = RenderSettings.skybox; RenderSettings.skybox = sky;
            if (view.TryGetComponent<Skybox>(out cameraSkybox)) {
                previousCameraSky = cameraSkybox.material; previousCameraSkyEnabled = cameraSkybox.enabled;
                cameraSkybox.material = sky; cameraSkybox.enabled = true;
            }
            view.clearFlags = CameraClearFlags.Skybox;
        }
        BuildParticles();
        // No camera-facing floor or horizon: the map supports a full 360-degree orbit.
        var volumeObject = new GameObject("TerminalMonitorVolume"); volumeObject.transform.SetParent(transform);
        monitor = volumeObject.AddComponent<Volume>(); monitor.isGlobal = true; monitor.priority = 50;
        profile = ScriptableObject.CreateInstance<VolumeProfile>(); monitor.sharedProfile = profile;
        var vignette = profile.Add<Vignette>(); vignette.intensity.Override(.16f); vignette.smoothness.Override(.45f); vignette.color.Override(UITheme.BgBase);
        var chromatic = profile.Add<ChromaticAberration>(); chromatic.intensity.Override(.025f);
        if (view != null && view.TryGetComponent<UniversalAdditionalCameraData>(out var data)) data.renderPostProcessing = true;
    }
    private Material Material(string shader)
    {
        Shader asset = Shader.Find(shader);
        if (asset == null) asset = Shader.Find("Universal Render Pipeline/Unlit");
        var result = new Material(asset); ownedMaterials.Add(result); return result;
    }
    private void BuildParticles()
    {
        // Background stars live in the skybox. This single system is reserved for combat fragments.
        var go = new GameObject("DataFragments"); go.transform.SetParent(transform);
        particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main; main.maxParticles = UITheme.ParticleLimit; main.startLifetime = .45f;
        main.startSpeed = 0; main.startSize = .06f; main.startColor = UITheme.Accent;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission; emission.rateOverTime = 0;
        var shape = particles.shape; shape.enabled = false;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Material("Randint/TerminalParticle");
        particles.Play();
    }
    public static void Hit(Vector3 position, bool destroyed)
    {
        if (current == null) return;
        current.shakeRemaining = UITheme.ShakeTime;
        if (!destroyed || current.particles == null || !TerminalPreferences.Motion) return;
        // Bursts share the ambient system and its hard maxParticles limit.
        for (int i = 0; i < 20; i++) {
            var emit = new ParticleSystem.EmitParams { position = position, velocity = Random.insideUnitSphere * 1.5f,
                startLifetime = .45f, startSize = .06f, startColor = UITheme.Accent };
            current.particles.Emit(emit, 1);
        }
    }
    private void LateUpdate()
    {
        if (sky != null) sky.SetFloat("_MotionTime", TerminalPreferences.Motion ? Time.unscaledTime : 0);
        if (monitor != null) monitor.weight = TerminalPreferences.Monitor ? 1 : 0;
        if (particles != null) {
            var emission = particles.emission; emission.enabled = TerminalPreferences.Motion;
        }
        if (view == null) return;
        view.transform.localPosition -= offset; offset = Vector3.zero;
        if (shakeRemaining > 0 && TerminalPreferences.Motion && !TransitionManager.IsBusy) {
            shakeRemaining -= Time.unscaledDeltaTime;
            offset = Random.insideUnitSphere * (.035f * Mathf.Clamp01(shakeRemaining / UITheme.ShakeTime));
            view.transform.localPosition += offset;
        }
    }
    private void OnDestroy()
    {
        if (view != null) view.transform.localPosition -= offset;
        if (current == this) current = null;
        if (RenderSettings.skybox == sky) RenderSettings.skybox = previousSky;
        if (cameraSkybox != null && cameraSkybox.material == sky) {
            cameraSkybox.material = previousCameraSky; cameraSkybox.enabled = previousCameraSkyEnabled;
        }
        if (view != null) view.clearFlags = previousClearFlags;
        if (profile != null) { foreach (var component in profile.components) Destroy(component); Destroy(profile); }
        foreach (var material in ownedMaterials) Destroy(material);
    }
}
