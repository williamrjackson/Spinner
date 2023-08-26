using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SpinnerWheel : MonoBehaviour
{
    [SerializeField]
    int segments = 8;
    [SerializeField]
    float needleThresholdDegrees = 5f;
    [SerializeField]
    AnimationCurve curve;
    [SerializeField]
    AnimationCurve continueCurve;
    [SerializeField]
    [Range(5f, 10f)]
    float duration = 5f;
    [SerializeField]
    [Range(0f, 1f)]
    float durationRandomness = .5f;
    [SerializeField]
    Image wedge;
    [SerializeField]
    TextMeshProUGUI wedgeLabel;
    [SerializeField]
    [Range(0f, .01f)]
    float border;
    [SerializeField]
    [Range(0f, 1f)]
    float ColorSat = 1f;
    [SerializeField]
    [Range(0f, 1f)]
    float ColorVal = .5f;
    public UnityEvent OnSpin;
    public UnityEvent<int> OnResult;
    
    [NaughtyAttributes.Button("Draw")]
    void DrawButtonAction()
    {
        Draw();
    }
    [NaughtyAttributes.Button("Spin!")]
    void SpinButtonAction()
    {
        Spin(10f);
    }

    private bool _isSpinning = false;
    public bool IsSpinning => _isSpinning;

    private SpinnerWheelInputHandler _spinCollider;
    private Canvas _rootCanvas;
    private Canvas RootCanvas
    {
        get
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            }
            return _rootCanvas;
        }
    }
    public int Segments
    {
        get => segments;
        set
        {
            if (value == segments) return;
            segments = value;
            Draw();
        }
    }

    private void Start() 
    {
        Draw();    
    }
    private SpinnerWheelInputHandler spinCollider
    {
        get
        {
            if (_spinCollider == null)
            {
                _spinCollider = GetComponentInChildren<SpinnerWheelInputHandler>();
            }
            return _spinCollider;
        }
    }
    private float offset => Mathf.DeltaAngle(0f, 360f / segments) / 2f;
    public void Draw()
    {
        wedge.gameObject.SetActive(false);
        wedgeLabel.gameObject.SetActive(false);
        spinCollider.transform.localEulerAngles = Vector3.zero;
        for (int i = wedge.transform.parent.childCount - 1; i >= 0; i--)
        {
            Transform child = wedge.transform.parent.GetChild(i);
            if (child.gameObject != wedge.gameObject && child.gameObject != wedgeLabel.gameObject)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
        for (int i = 0; i < segments; i++)
        {
            float borderAngle = Mathf.Lerp(0f, 180f, border);
            float angle = ((360f / segments) * i) + borderAngle;
            Color color = Color.HSVToRGB(Mathf.InverseLerp(0, segments, i), ColorSat, ColorVal);
            Image thisWedge = Instantiate(wedge, wedge.transform.parent);
            TextMeshProUGUI thisLabel = Instantiate(wedgeLabel, wedgeLabel.transform.parent);
            thisLabel.transform.position = wedgeLabel.transform.position;
            thisWedge.transform.LocalEulerWith(z: offset - borderAngle);
            thisWedge.color = color;
            thisWedge.fillAmount = (1f / segments) - border;
            thisWedge.transform.localPosition = wedge.transform.localPosition;
            thisWedge.gameObject.SetActive(true);
            thisLabel.SetText((i + 1).ToString());
            thisLabel.transform.SetParent(thisWedge.transform);
            thisLabel.gameObject.SetActive(true);
            thisWedge.transform.LocalEulerWith(z: offset - angle);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(thisWedge.gameObject, "Create Spinner Graphic");
#endif
        }
    }

    private float radius
    {
        get
        {
            RectTransform spinner = spinCollider.GetComponent<RectTransform>();
            return (spinner.rect.width * .5f) * RootCanvas.scaleFactor;
        }
    }
    public void Spin(float spinStrength)
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine(spinStrength));
    }
    public void ContinueSpin(float spinStrength)
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine(spinStrength, true));
    }
#if UNITY_EDITOR
    Vector3 resultPos = Vector3.zero;
#endif
    private IEnumerator SpinRoutine(float strength, bool startWithStrength = false)
    {
        _isSpinning = true;
        if (OnSpin != null) OnSpin.Invoke();
        float posRandom = Mathf.Abs(durationRandomness);
        float duration = this.duration + Random.Range(posRandom * -1f, posRandom);
        float elapsed = 0f;
        Transform spinner = spinCollider.transform;
        while (elapsed < duration)
        {
            float currentZ = spinner.localEulerAngles.z;
            float curveVal = (startWithStrength) ?
                continueCurve.Evaluate(Mathf.InverseLerp(0f, duration, elapsed)) :
                curve.Evaluate(Mathf.InverseLerp(0f, duration, elapsed));

            spinner.LocalEulerWith(z: currentZ - (curveVal * strength));
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Vector3[] positions = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i - spinner.localEulerAngles.z + offset;
            positions[i] = spinner.position + new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0f).normalized * radius;
        }
        Vector3 top = spinner.position + Vector3.up * radius;
        int nearestIndex = GetNearestPoint(top, positions);
        int result = nearestIndex;
#if UNITY_EDITOR
        resultPos = positions[nearestIndex];
#endif
        if (top.x > positions[nearestIndex].x)
        {
            result = (result + 1) % segments;
            float distanceFromNeedle = Vector3.SignedAngle(Vector3.up, spinner.position - positions[nearestIndex], Vector3.back);
            if (distanceFromNeedle > needleThresholdDegrees)
            {
                float adjustmentTime = .5f;
                float adjustmentElapsed = 0f;
                float startAngle = spinner.localEulerAngles.z;
                float targetAngle = Mathf.DeltaAngle(0f, spinner.localEulerAngles.z + needleThresholdDegrees);
                while (adjustmentElapsed < adjustmentTime)
                {
                    adjustmentElapsed += Time.deltaTime;
                    float scrub = Mathf.InverseLerp(0f, adjustmentTime, adjustmentElapsed);
                    float thisAngle = Mathf.LerpAngle(startAngle, targetAngle, scrub);
                    spinner.LocalEulerWith(z: thisAngle);
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        if (OnResult != null)
        {
            OnResult.Invoke(result);
        }
        _isSpinning = false;
    }
    
    private int GetNearestPoint(Vector3 reference, Vector3[] targets)
    {
        int nearestIndex = -1;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < targets.Length; i++)
        {
            float dist = Vector3.Distance(reference, targets[i]);
            if (dist < minDist)
            {
                nearestIndex = i;
                minDist = dist;
            }
        }
        return nearestIndex;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform spinTransform = spinCollider.transform;
        Gizmos.DrawCube(resultPos, Vector3.one * 30f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spinTransform.position, radius);
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i - spinTransform.localEulerAngles.z + offset;
         
            Gizmos.color = Color.HSVToRGB(Mathf.InverseLerp(0, segments, i), 1f, 1f);
            Vector3 dir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0f);
            Vector3 p1 = spinTransform.position;
            Vector3 p2 = spinTransform.position + (dir * radius);
            Gizmos.DrawLine(p1, p2);
        }
    }
#endif
}
