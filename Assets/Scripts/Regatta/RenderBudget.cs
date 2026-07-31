// Copyright (c) 2026 Cyril Moron — EPL-2.0
// Runtime render-budget controls, for a machine where the renderer competes
// with the physics co-sim (Docker/Rosetta) for CPU. Three independent knobs,
// each persisted in PlayerPrefs and reapplied on Start. Attach next to
// ManualHelm/RegattaHud on the camera rig -- draws its own HUD line, same
// top-left stacking convention RegattaCameraRig and ManualHelm already use.
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class RenderBudget : MonoBehaviour
{
    // 30 -> 60 -> uncapped (-1) -> 30 ...
    static readonly int[] FrameCaps = { 30, 60, -1 };
    const int DefaultCapIndex = 1;  // FrameCaps[1] == 60

    // 100% -> 75% -> 50% -> 100% ...
    static readonly float[] RenderScales = { 1f, 0.75f, 0.5f };
    const int DefaultScaleIndex = 0;  // RenderScales[0] == 100%

    const KeyCode CapKey = KeyCode.K;
    const KeyCode ScaleKey = KeyCode.L;
    const KeyCode FxKey = KeyCode.O;

    const string PrefCap = "regatta.renderBudget.capIndex";
    const string PrefScale = "regatta.renderBudget.scaleIndex";
    const string PrefFx = "regatta.renderBudget.fxEnabled";

    // Consulted every frame by ActuatorAnimator (sail blend shapes) and once
    // at spawn by NativeFoamWakeController (wake/foam/bow-wave). Defaults
    // open: a boat spawning before this component's Start has run would see
    // effects on regardless of the saved pref -- in practice it never does,
    // Addressables instantiation is async and lands well after scene Start.
    public static bool EffectsEnabled { get; private set; } = true;

    int _capIndex, _scaleIndex;

    // Duration-based EMA, same idea as RegattaHud.SpeedSmoothingSeconds: a
    // fixed per-frame factor reads differently at 5 fps than at 60 fps, a
    // fixed duration does not.
    const float FpsSmoothingSeconds = 0.33f;
    float _fps;

    void Start()
    {
        _capIndex = PlayerPrefs.GetInt(PrefCap, DefaultCapIndex);
        _scaleIndex = PlayerPrefs.GetInt(PrefScale, DefaultScaleIndex);
        EffectsEnabled = PlayerPrefs.GetInt(PrefFx, 1) != 0;
        ApplyCap();
        ApplyScale();
        ApplyEffects();
    }

    void Update()
    {
        if (Input.GetKeyDown(CapKey))
        {
            _capIndex = NextCycleIndex(_capIndex, FrameCaps.Length);
            PlayerPrefs.SetInt(PrefCap, _capIndex);
            PlayerPrefs.Save();
            ApplyCap();
        }
        if (Input.GetKeyDown(ScaleKey))
        {
            _scaleIndex = NextCycleIndex(_scaleIndex, RenderScales.Length);
            PlayerPrefs.SetInt(PrefScale, _scaleIndex);
            PlayerPrefs.Save();
            ApplyScale();
        }
        if (Input.GetKeyDown(FxKey))
        {
            EffectsEnabled = !EffectsEnabled;
            PlayerPrefs.SetInt(PrefFx, EffectsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyEffects();
        }

        if (Time.deltaTime > 0f)
            _fps = Mathf.Lerp(_fps, 1f / Time.deltaTime,
                Mathf.Clamp01(Time.deltaTime / FpsSmoothingSeconds));
    }

    void ApplyCap()
    {
        QualitySettings.vSyncCount = 0;  // vSync overrides targetFrameRate otherwise
        Application.targetFrameRate = FrameCaps[_capIndex];
    }

    void ApplyScale()
    {
        float scale = RenderScales[_scaleIndex];
        // From the DISPLAY's native size, not the current Screen size: cycling
        // back to 100% must restore native, not re-scale an already-scaled size.
        int w = ScaledDimension(Display.main.systemWidth, scale);
        int h = ScaledDimension(Display.main.systemHeight, scale);
        Screen.SetResolution(w, h, Screen.fullScreenMode);
    }

    void ApplyEffects()
    {
        var water = FindFirstObjectByType<WaterSurface>();
        if (water != null)
        {
            water.foam = EffectsEnabled;
            water.deformation = EffectsEnabled;
        }
        // Include inactive: fx-off already disabled these; turning fx back on
        // must find them too, not just the ones still active.
        foreach (var wake in FindObjectsByType<NativeFoamWakeController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            wake.gameObject.SetActive(EffectsEnabled);
        // Sail blend shapes: ActuatorAnimator reads EffectsEnabled itself
        // every frame -- nothing to push here.
    }

    // ---- pure logic, covered by RenderBudgetTests -------------------------
    static int NextCycleIndex(int current, int length) => (current + 1) % length;

    static int ScaledDimension(int nativeSize, float scale) =>
        Mathf.RoundToInt(nativeSize * scale);

    void OnGUI()
    {
        // y=118: one row below ManualHelm's helm-state line (y=94), same
        // top-left column, same stacking the camera line and helm line use.
        GUI.Label(new Rect(10, 118, 760, 24),
            $"cap {(FrameCaps[_capIndex] < 0 ? "uncapped" : FrameCaps[_capIndex].ToString())}"
            + $" | scale {RenderScales[_scaleIndex] * 100f:0}%"
            + $" | fx {(EffectsEnabled ? "on" : "off")}"
            + $" -- [{CapKey}] cap [{ScaleKey}] scale [{FxKey}] fx  {_fps:0} fps");
    }
}
