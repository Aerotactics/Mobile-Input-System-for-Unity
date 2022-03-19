using UnityEngine;

public class AInputDebug : MonoBehaviour
{
    private void Start()
    {
        AInput.suspiciousBehaviourEvent.AddListener(OnSuspiciousBehaviour);
        AInput.tapEvent.AddListener(OnTapTest);
        AInput.doubleTapEvent.AddListener(OnTapTest);
        AInput.multiTapEvent.AddListener(OnTapTest);
        AInput.flickEvent.AddListener(OnFlickTest);
        AInput.swipeEvent.AddListener(OnSwipeTest);
        AInput.longTapBeginEvent.AddListener(OnLongTapBeginTest);
        AInput.longTapEndEvent.AddListener(OnLongTapEndTest);
        AInput.longTapCancelEvent.AddListener(OnLongTapCancelTest);
        AInput.slideBeginEvent.AddListener(OnSlideBeginTest);
        AInput.slideEndEvent.AddListener(OnSlideEndTest);
        AInput.slideCancelEvent.AddListener(OnSlideCancelTest);
        AInput.pinchBeginEvent.AddListener(OnPinchBeginTest);
        AInput.pinchEndEvent.AddListener(OnPinchEndTest);
    }

    public void OnSuspiciousBehaviour(ATouch touch)
    {
        Debug.Log($"Suspicious behaviour alert! Tap Time: {touch.endTime - touch.startTime}");
    }
    public void OnTapTest(ATouch touch)
    {
        Debug.Log($"Tapped! Tap Count: {touch.tapCount}");
    }

    public void OnFlickTest(ATouch touch)
    {
        Debug.Log($"Flicked! Delta: {touch.deltaPosition}");
    }

    public void OnSwipeTest(ATouch touch)
    {
        Debug.Log($"Swiped! Delta: {touch.startToEndDelta}");
    }

    public void OnLongTapBeginTest(ATouch touch)
    {
        Debug.Log($"LongTap Begin: {touch.startTime}");
    }

    public void OnLongTapEndTest(ATouch touch)
    {
        Debug.Log($"LongTap End: {touch.endTime}");
    }

    public void OnLongTapCancelTest(ATouch touch)
    {
        Debug.Log($"LongTap Cancelled.");
    }

    public void OnSlideBeginTest(ATouch touch)
    {
        Debug.Log($"Slide Started! Delta: {touch.deltaPosition}");
    }

    public void OnSlideEndTest(ATouch touch)
    {
        Debug.Log($"Slide Ended! Delta: {touch.deltaPosition}");
    }

    public void OnSlideCancelTest(ATouch touch)
    {
        Debug.Log($"Slide canceled.");
    }

    public void OnPinchBeginTest(ATouch touch1, ATouch touch2)
    {
        Debug.Log($"Pinch began!");
    }

    public void OnPinchEndTest(ATouch touch1, ATouch touch2)
    {
        Debug.Log($"Pinch ended!");
    }
}
