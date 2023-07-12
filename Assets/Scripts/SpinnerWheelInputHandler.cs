using UnityEngine;
using UnityEngine.EventSystems;

public class SpinnerWheelInputHandler : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler, IPointerMoveHandler
{
    [SerializeField]
    SpinnerWheel spinnerWheel;

    bool _spinHasStarted = false;
    float initialAngle = 0f;
    float lastAngle = 0f;
    float strength = 0f;

    public void OnPointerDown(PointerEventData eventData)
    {   
        if (spinnerWheel.IsSpinning) return;
        _spinHasStarted = true;
        initialAngle = -(eventData.position - transform.position.ToVector2()).Angle() - transform.localEulerAngles.z;
        lastAngle = initialAngle;
        //Debug.Log($"Initial: {initialAngle}");
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if (spinnerWheel.IsSpinning) return;
        if (_spinHasStarted)
        {
            float currentAngle = -(eventData.position - transform.position.ToVector2()).Angle();
            strength = Mathf.DeltaAngle(lastAngle, currentAngle);
            lastAngle = currentAngle;
            var updatedAngle = currentAngle - initialAngle;
            transform.LocalEulerWith(z: updatedAngle);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("Pointer Exit");
        EndSpin();
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        //Debug.Log("Pointer Up");
        EndSpin();
    }

    private void EndSpin()
    {
        if (_spinHasStarted)
        {
            _spinHasStarted = false;
            //Debug.Log($"Strength: {strength}");
            if (strength < 0)
            {
                spinnerWheel.ContinueSpin(Mathf.Abs(strength));
            }
            strength = 0f;
        }
    }

    void Awake()
    {
        if (spinnerWheel == null)
        {
            spinnerWheel = GetComponentInParent<SpinnerWheel>();
        }
    }
}
