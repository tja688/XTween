using System.Collections;
using SevenStrikeModules.XTween;
using UnityEngine;

public class XTweenDelayPlayOrderRepro : MonoBehaviour
{
    [Header("Experiment Settings")]
    public float startX = 0f;
    public float endX = 200f;
    public float duration = 1f;
    public float delay = 0.5f;
    public EaseMode easeMode = EaseMode.OutBack;
    public float preDelayMoveThreshold = 0.001f;

    private Transform _targetA;
    private Transform _targetB;
    private XTween_Interface _tweenA;
    private XTween_Interface _tweenB;

    private float _runStartTime;
    private int _delayCallbackCountA;
    private int _delayCallbackCountB;
    private float _maxPreDelayDeltaA;
    private float _maxPreDelayDeltaB;

    private IEnumerator Start()
    {
        SetupTargets();
        yield return null;
        yield return RunExperiment();
    }

    private void SetupTargets()
    {
        CleanupChildIfExists("Repro_A_PlayThenSetDelay");
        CleanupChildIfExists("Repro_B_SetDelayThenPlay");

        _targetA = CreateTarget("Repro_A_PlayThenSetDelay", new Vector3(0f, 0f, 0f));
        _targetB = CreateTarget("Repro_B_SetDelayThenPlay", new Vector3(0f, 0f, 2f));
    }

    private Transform CreateTarget(string objectName, Vector3 localPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        return go.transform;
    }

    private void CleanupChildIfExists(string childName)
    {
        var child = transform.Find(childName);
        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator RunExperiment()
    {
        _delayCallbackCountA = 0;
        _delayCallbackCountB = 0;
        _maxPreDelayDeltaA = 0f;
        _maxPreDelayDeltaB = 0f;

        SetX(_targetA, startX);
        SetX(_targetB, startX);

        _tweenA = BuildTween(
            "A",
            _targetA,
            () => _delayCallbackCountA++,
            d => _maxPreDelayDeltaA = Mathf.Max(_maxPreDelayDeltaA, d));

        _tweenB = BuildTween(
            "B",
            _targetB,
            () => _delayCallbackCountB++,
            d => _maxPreDelayDeltaB = Mathf.Max(_maxPreDelayDeltaB, d));

        _runStartTime = Time.time;
        Debug.Log($"[Repro] Start | delay={delay:F3}, duration={duration:F3}, ease={easeMode}");

        // A组（问题组）: Play -> SetDelay
        _tweenA.Play();
        _tweenA.SetDelay(delay);
        Debug.Log("[Repro] Group A sequence: Play() then SetDelay(0.5)");

        // B组（对照组）: SetDelay -> Play
        _tweenB.SetDelay(delay);
        _tweenB.Play();
        Debug.Log("[Repro] Group B sequence: SetDelay(0.5) then Play()");

        yield return StartCoroutine(SamplePreDelayWindow());

        yield return new WaitForSeconds(duration + 0.3f);

        float endXA = GetX(_targetA);
        float endXB = GetX(_targetB);
        bool movedBeforeDelayA = _maxPreDelayDeltaA > preDelayMoveThreshold;
        bool movedBeforeDelayB = _maxPreDelayDeltaB > preDelayMoveThreshold;

        Debug.Log(
            "[Repro][Summary] " +
            $"A.preDelayMaxDelta={_maxPreDelayDeltaA:F4}, A.delayCb={_delayCallbackCountA}, A.movedBeforeDelay={movedBeforeDelayA}; " +
            $"B.preDelayMaxDelta={_maxPreDelayDeltaB:F4}, B.delayCb={_delayCallbackCountB}, B.movedBeforeDelay={movedBeforeDelayB}; " +
            $"A.endX={endXA:F3}, B.endX={endXB:F3}");
    }

    private IEnumerator SamplePreDelayWindow()
    {
        while (true)
        {
            float elapsed = Time.time - _runStartTime;
            if (elapsed >= delay)
            {
                yield break;
            }

            float xA = GetX(_targetA);
            float xB = GetX(_targetB);
            float dA = Mathf.Abs(xA - startX);
            float dB = Mathf.Abs(xB - startX);
            _maxPreDelayDeltaA = Mathf.Max(_maxPreDelayDeltaA, dA);
            _maxPreDelayDeltaB = Mathf.Max(_maxPreDelayDeltaB, dB);

            Debug.Log(
                $"[Repro][Sample] t={elapsed:F3} " +
                $"A.x={xA:F4} A.delayCb={_delayCallbackCountA} | " +
                $"B.x={xB:F4} B.delayCb={_delayCallbackCountB}");

            yield return null;
        }
    }

    private XTween_Interface BuildTween(string tag, Transform target, System.Action onDelayCb, System.Action<float> onPreDelayDelta)
    {
        return XTween.To(
                getter: () => GetX(target),
                setter: x => SetX(target, x),
                endValue: endX,
                duration: duration,
                autokill: false)
            .SetEase(easeMode)
            .OnStart(() => Debug.Log($"[Repro][{tag}] OnStart at t={Time.time - _runStartTime:F3}"))
            .OnDelayUpdate(progress =>
            {
                onDelayCb();
                float x = GetX(target);
                float elapsed = Time.time - _runStartTime;
                if (elapsed < delay)
                {
                    onPreDelayDelta(Mathf.Abs(x - startX));
                }
                Debug.Log($"[Repro][{tag}] OnDelayUpdate t={elapsed:F3} progress={progress:F3} x={x:F4}");
            })
            .OnUpdate<float>((value, linearProgress, elapsedTime) =>
            {
                float elapsed = Time.time - _runStartTime;
                if (elapsed < delay)
                {
                    onPreDelayDelta(Mathf.Abs(value - startX));
                    Debug.Log($"[Repro][{tag}] OnUpdate(pre-delay-window) t={elapsed:F3} value={value:F4} linear={linearProgress:F3} elapsed={elapsedTime:F3}");
                }
            })
            .OnComplete(total => Debug.Log($"[Repro][{tag}] OnComplete total={total:F3} finalX={GetX(target):F4}"));
    }

    private static float GetX(Transform t)
    {
        return t.position.x;
    }

    private static void SetX(Transform t, float x)
    {
        Vector3 p = t.position;
        p.x = x;
        t.position = p;
    }
}
