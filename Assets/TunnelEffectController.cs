using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TunnelEffectController : MonoBehaviour
{
    public InfiniteProceduralTunnel tunnel;

    [Header("Efectos Iniciales Combinados")]
    public List<TunnelEffectSO> startingEffects = new List<TunnelEffectSO>();
    public float transitionDuration = 2f;

    private Coroutine currentTransition;
    private List<TunnelEffectSO> activeEffects = new List<TunnelEffectSO>();

    private float currentBaseRadius;
    private float currentSpeed;
    private float currentNoiseScale;
    private float currentNoiseStrength;
    private float currentCurveX;
    private float currentCurveY;
    private float currentTurnX;
    private float currentTurnY;
    private float currentCurveDist;
    private float currentColorSpeed;
    private Color currentHighlightColor;
    private float currentTwistDegrees;
    private float currentGlobalRotationSpeed;

    private float currentWobbleSpeed;
    private float currentWobbleStrength;
    private float currentWobbleNoiseScale; // NUEVO

    void Start()
    {
        if (startingEffects.Count > 0)
        {
            ApplyEffectsInstantly(startingEffects);
        }
    }

    void Update()
    {
        float breathingOffset = 0f;
        foreach (var effect in activeEffects)
        {
            if (effect is BreathingTunnelEffectSO breath)
            {
                breathingOffset += Mathf.Sin(Time.time * breath.breathingSpeed) * breath.breathingAmplitude;
            }
        }

        tunnel.baseRadius = currentBaseRadius + breathingOffset;
        tunnel.speed = currentSpeed;
        tunnel.noiseScale = currentNoiseScale;
        tunnel.noiseStrength = currentNoiseStrength;
        tunnel.curveX = currentCurveX;
        tunnel.curveY = currentCurveY;

        tunnel.turnStrengthX = currentTurnX;
        tunnel.turnStrengthY = currentTurnY;
        tunnel.curveStartDistance = currentCurveDist;

        tunnel.colorCycleSpeed = currentColorSpeed;
        tunnel.highlightColor = currentHighlightColor;
        tunnel.twistDegreesPerMeter = currentTwistDegrees;
        tunnel.globalRotationSpeed = currentGlobalRotationSpeed;

        tunnel.wobbleSpeed = currentWobbleSpeed;
        tunnel.wobbleStrength = currentWobbleStrength;
        tunnel.wobbleNoiseScale = currentWobbleNoiseScale; // NUEVO
    }

    public void ChangeEffects(List<TunnelEffectSO> newEffects)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionToEffects(newEffects, transitionDuration));
    }

    public void AddEffect(TunnelEffectSO newEffect)
    {
        List<TunnelEffectSO> newList = new List<TunnelEffectSO>(activeEffects);
        if (!newList.Contains(newEffect)) newList.Add(newEffect);
        ChangeEffects(newList);
    }

    public void RemoveEffect(TunnelEffectSO effectToRemove)
    {
        List<TunnelEffectSO> newList = new List<TunnelEffectSO>(activeEffects);
        if (newList.Contains(effectToRemove)) newList.Remove(effectToRemove);
        ChangeEffects(newList);
    }

    private void EvaluateTargetState(List<TunnelEffectSO> effects,
        out float tRadius, out float tSpeed, out float tNoiseScale, out float tNoiseStrength,
        out float tCurveX, out float tCurveY, out Gradient tColors,
        out float tTurnX, out float tTurnY, out float tCurveDist, out float tColorSpeed,
        out bool tUseHighlight, out Color tHighlightColor, out int tHighlightStep, out HighlightTunnelEffectSO.HighlightMode tHighlightMode,
        out float tTwist, out float tGlobalRot,
        out float tWobbleSpeed, out float tWobbleStrength, out float tWobbleNoiseScale) // NUEVO
    {
        tRadius = 5f; tSpeed = 10f; tNoiseScale = 1.5f; tNoiseStrength = 2f;
        tColors = new Gradient(); tColors.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) });

        tCurveX = 0f; tCurveY = 0f;
        tTurnX = 0f; tTurnY = 0f; tCurveDist = 0f; tColorSpeed = 0f;
        tUseHighlight = false; tHighlightColor = Color.clear; tHighlightStep = 0;
        tHighlightMode = HighlightTunnelEffectSO.HighlightMode.Longitudinal;
        tTwist = 0f;
        tGlobalRot = 0f;
        tWobbleSpeed = 0f; tWobbleStrength = 0f; tWobbleNoiseScale = 2f; // NUEVO

        if (effects.Count > 0 && effects[0] != null)
        {
            tRadius = effects[0].baseRadius;
            tSpeed = effects[0].speed;
            tNoiseScale = effects[0].noiseScale;
            tNoiseStrength = effects[0].noiseStrength;
            tColors = effects[0].tunnelColors;
        }

        foreach (var e in effects)
        {
            if (e == null) continue;

            tCurveX += e.curveX;
            tCurveY += e.curveY;

            if (e is TurningTunnelEffectSO turn)
            {
                tTurnX += turn.turnStrengthX;
                tTurnY += turn.turnStrengthY;
                tCurveDist = Mathf.Max(tCurveDist, turn.curveStartDistance);
            }
            if (e is ColorCycleTunnelEffectSO color)
            {
                tColorSpeed += color.colorCycleSpeed;
                tColors = color.tunnelColors;
            }
            if (e is HighlightTunnelEffectSO hl)
            {
                tUseHighlight = hl.useHighlight;
                tHighlightColor = hl.highlightColor;
                tHighlightStep = hl.highlightStep;
                tHighlightMode = hl.highlightMode;
            }
            if (e is TwistTunnelEffectSO twistSO)
            {
                tTwist += twistSO.twistDegreesPerMeter;
            }
            if (e is GlobalRotationTunnelEffectSO rotSO)
            {
                tGlobalRot += rotSO.globalRotationSpeed;
            }
            if (e is VertexWobbleTunnelEffectSO wobbleSO)
            {
                tWobbleSpeed += wobbleSO.wobbleSpeed;
                tWobbleStrength += wobbleSO.wobbleStrength;
                tWobbleNoiseScale = wobbleSO.wobbleNoiseScale; // La escala de ruido no se suma, se sobreescribe con el último efecto encontrado
            }
        }
    }

    private void ApplyEffectsInstantly(List<TunnelEffectSO> targetEffects)
    {
        EvaluateTargetState(targetEffects,
            out currentBaseRadius, out currentSpeed, out currentNoiseScale, out currentNoiseStrength,
            out currentCurveX, out currentCurveY, out Gradient tColors,
            out currentTurnX, out currentTurnY, out currentCurveDist, out currentColorSpeed,
            out bool tUseHighlight, out currentHighlightColor, out int tHighlightStep, out HighlightTunnelEffectSO.HighlightMode tHighlightMode,
            out currentTwistDegrees, out currentGlobalRotationSpeed,
            out currentWobbleSpeed, out currentWobbleStrength, out currentWobbleNoiseScale);

        tunnel.tunnelColors = tColors;
        tunnel.useHighlight = tUseHighlight;
        tunnel.highlightStep = tHighlightStep;
        tunnel.highlightMode = tHighlightMode;

        activeEffects = new List<TunnelEffectSO>(targetEffects);
    }

    private IEnumerator TransitionToEffects(List<TunnelEffectSO> targetEffects, float duration)
    {
        EvaluateTargetState(targetEffects,
            out float tRadius, out float tSpeed, out float tNoiseScale, out float tNoiseStrength,
            out float tCurveX, out float tCurveY, out Gradient tColors,
            out float tTurnX, out float tTurnY, out float tCurveDist, out float tColorSpeed,
            out bool tUseHighlight, out Color tHighlightColor, out int tHighlightStep, out HighlightTunnelEffectSO.HighlightMode tHighlightMode,
            out float tTwist, out float tGlobalRot,
            out float tWobbleSpeed, out float tWobbleStrength, out float tWobbleNoiseScale);

        float sRadius = currentBaseRadius;
        float sSpeed = currentSpeed;
        float sNoiseScale = currentNoiseScale;
        float sNoiseStrength = currentNoiseStrength;
        float sCurveX = currentCurveX;
        float sCurveY = currentCurveY;
        float sTurnX = currentTurnX;
        float sTurnY = currentTurnY;
        float sCurveDist = currentCurveDist;
        float sColorSpeed = currentColorSpeed;
        Color sHighlightColor = currentHighlightColor;
        float sTwist = currentTwistDegrees;
        float sGlobalRot = currentGlobalRotationSpeed;
        float sWobbleSpeed = currentWobbleSpeed;
        float sWobbleStrength = currentWobbleStrength;
        float sWobbleNoiseScale = currentWobbleNoiseScale; // NUEVO

        tunnel.tunnelColors = tColors;
        if (tUseHighlight)
        {
            tunnel.useHighlight = true;
            tunnel.highlightStep = tHighlightStep;
            tunnel.highlightMode = tHighlightMode;
        }
        else if (tunnel.useHighlight)
        {
            tHighlightColor = new Color(sHighlightColor.r, sHighlightColor.g, sHighlightColor.b, 0f);
        }

        activeEffects = new List<TunnelEffectSO>(targetEffects);

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0, 1, t / duration);

            currentBaseRadius = Mathf.Lerp(sRadius, tRadius, pct);
            currentSpeed = Mathf.Lerp(sSpeed, tSpeed, pct);
            currentNoiseScale = Mathf.Lerp(sNoiseScale, tNoiseScale, pct);
            currentNoiseStrength = Mathf.Lerp(sNoiseStrength, tNoiseStrength, pct);
            currentCurveX = Mathf.Lerp(sCurveX, tCurveX, pct);
            currentCurveY = Mathf.Lerp(sCurveY, tCurveY, pct);
            currentTurnX = Mathf.Lerp(sTurnX, tTurnX, pct);
            currentTurnY = Mathf.Lerp(sTurnY, tTurnY, pct);
            currentCurveDist = Mathf.Lerp(sCurveDist, tCurveDist, pct);
            currentColorSpeed = Mathf.Lerp(sColorSpeed, tColorSpeed, pct);
            currentHighlightColor = Color.Lerp(sHighlightColor, tHighlightColor, pct);
            currentTwistDegrees = Mathf.Lerp(sTwist, tTwist, pct);
            currentGlobalRotationSpeed = Mathf.Lerp(sGlobalRot, tGlobalRot, pct);
            currentWobbleSpeed = Mathf.Lerp(sWobbleSpeed, tWobbleSpeed, pct);
            currentWobbleStrength = Mathf.Lerp(sWobbleStrength, tWobbleStrength, pct);
            currentWobbleNoiseScale = Mathf.Lerp(sWobbleNoiseScale, tWobbleNoiseScale, pct); // NUEVO

            yield return null;
        }

        ApplyEffectsInstantly(targetEffects);
        if (!tUseHighlight) tunnel.useHighlight = false;

        currentTransition = null;
    }
}