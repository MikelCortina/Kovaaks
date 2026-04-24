using UnityEngine;
using System.Collections;

public class TunnelEffectController : MonoBehaviour
{
    public InfiniteProceduralTunnel tunnel;

    [Header("Efecto Actual")]
    public TunnelEffectSO startingEffect;
    public float transitionDuration = 2f;

    private Coroutine currentTransition;
    private TunnelEffectSO activeEffect;

    void Start()
    {
        if (startingEffect != null)
        {
            ApplyEffectInstantly(startingEffect);
            activeEffect = startingEffect;
        }
    }

    void Update()
    {
        if (activeEffect is BreathingTunnelEffectSO breathingEffect)
        {
            float breathingOffset = Mathf.Sin(Time.time * breathingEffect.breathingSpeed) * breathingEffect.breathingAmplitude;
            tunnel.baseRadius = breathingEffect.baseRadius + breathingOffset;
        }
    }

    public void ChangeEffect(TunnelEffectSO newEffect)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionToEffect(newEffect, transitionDuration));
    }

    private void ApplyEffectInstantly(TunnelEffectSO effect)
    {
        tunnel.baseRadius = effect.baseRadius;
        tunnel.speed = effect.speed;
        tunnel.noiseScale = effect.noiseScale;
        tunnel.noiseStrength = effect.noiseStrength;
        tunnel.curveX = effect.curveX;
        tunnel.curveY = effect.curveY;
        tunnel.tunnelColors = effect.tunnelColors;

        // Comprobamos si el efecto inicial es un giro
        if (effect is TurningTunnelEffectSO turnEffect)
        {
            tunnel.turnStrengthX = turnEffect.turnStrengthX;
            tunnel.turnStrengthY = turnEffect.turnStrengthY;
            tunnel.curveStartDistance = turnEffect.curveStartDistance;
        }
        else
        {
            tunnel.turnStrengthX = 0f;
            tunnel.turnStrengthY = 0f;
        }
    }

    private IEnumerator TransitionToEffect(TunnelEffectSO target, float duration)
    {
        activeEffect = null;

        float t = 0;

        // Guardar valores iniciales
        float startRadius = tunnel.baseRadius;
        float startSpeed = tunnel.speed;
        float startNoiseScale = tunnel.noiseScale;
        float startNoiseStrength = tunnel.noiseStrength;
        float startCurveX = tunnel.curveX;
        float startCurveY = tunnel.curveY;

        float startTurnX = tunnel.turnStrengthX;
        float startTurnY = tunnel.turnStrengthY;
        float startCurveDist = tunnel.curveStartDistance;

        // Determinar valores objetivo del giro (0 si no es un efecto de giro)
        float targetTurnX = 0f;
        float targetTurnY = 0f;
        float targetCurveDist = startCurveDist; // Mantiene la distancia si volvemos a 0

        if (target is TurningTunnelEffectSO turnSO)
        {
            targetTurnX = turnSO.turnStrengthX;
            targetTurnY = turnSO.turnStrengthY;
            targetCurveDist = turnSO.curveStartDistance;
        }

        tunnel.tunnelColors = target.tunnelColors;

        while (t < duration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0, 1, t / duration);

            tunnel.baseRadius = Mathf.Lerp(startRadius, target.baseRadius, pct);
            tunnel.speed = Mathf.Lerp(startSpeed, target.speed, pct);
            tunnel.noiseScale = Mathf.Lerp(startNoiseScale, target.noiseScale, pct);
            tunnel.noiseStrength = Mathf.Lerp(startNoiseStrength, target.noiseStrength, pct);
            tunnel.curveX = Mathf.Lerp(startCurveX, target.curveX, pct);
            tunnel.curveY = Mathf.Lerp(startCurveY, target.curveY, pct);

            // Interpolamos el giro
            tunnel.turnStrengthX = Mathf.Lerp(startTurnX, targetTurnX, pct);
            tunnel.turnStrengthY = Mathf.Lerp(startTurnY, targetTurnY, pct);
            tunnel.curveStartDistance = Mathf.Lerp(startCurveDist, targetCurveDist, pct);

            yield return null;
        }

        ApplyEffectInstantly(target);
        activeEffect = target;
        currentTransition = null;
    }
}