using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

// Custom Button Behavior.
// Hold Button, fill representing "strength" animates.
// Release button to invoke with current "strength."
public class HoldButtonStrength : MonoBehaviour
{
    [SerializeField]
    Button button;
    [SerializeField]
    SpinnerWheel spinner;
    [SerializeField]
    Image fillImage;
    [SerializeField]
    AnimationCurve fillCurve;
    [SerializeField]
    [NaughtyAttributes.MinMaxSlider(1f, 20f)]
    Vector2 strengthRange = new Vector2(5f, 10f);
    [SerializeField]
    float duration = 5f;
    float _strength = 0f;
    Coroutine _coroutine;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponentInChildren<Button>();
        }
    }
    void Start()
    {
        EventTrigger eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry mouseDownEvent = new EventTrigger.Entry();
        mouseDownEvent.eventID = EventTriggerType.PointerDown;
        EventTrigger.Entry mouseUpEvent = new EventTrigger.Entry();
        mouseUpEvent.eventID = EventTriggerType.PointerUp;
        mouseDownEvent.callback.AddListener((x) => OnMouseDown());
        mouseUpEvent.callback.AddListener((x) => OnMouseUp());
        eventTrigger.triggers.Add(mouseUpEvent);
        eventTrigger.triggers.Add(mouseDownEvent);
        fillImage.fillAmount = 0f;
        spinner.OnSpin.AddListener(() => StopButtonAnimation());
    }

    private void OnMouseDown()
    {
        if (!button.interactable) return;
        SetStrengthFromCurve(0f);
        _coroutine = StartCoroutine(FillStrengthRoutine());
    }

    private void OnMouseUp()
    {
        if (!button.interactable) return;
        StopButtonAnimation();
        spinner.Spin(_strength);
    }

    private void StopButtonAnimation()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private void SetStrengthFromCurve(float t)
    {
        float curveVal = fillCurve.Evaluate(t);
        fillImage.fillAmount = curveVal;
        _strength = Mathf.Lerp(strengthRange.x, strengthRange.y, curveVal);
    }
    private IEnumerator FillStrengthRoutine()
    {
        while (true)
        {
            float dur = duration;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float scrub = Mathf.InverseLerp(0f, dur, elapsed);
                SetStrengthFromCurve(scrub);
                yield return new WaitForEndOfFrame();
                if (spinner.IsSpinning) yield break;
            }
        }
    }

}
