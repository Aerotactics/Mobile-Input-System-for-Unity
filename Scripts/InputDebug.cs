using UnityEngine;

using Input = AInput.Input;
using Touch = AInput.Touch;

public class InputDebug : MonoBehaviour
{
    private void Start()
    {
        Input.suspiciousBehaviourEvent.AddListener(OnSuspiciousBehaviour);
        Input.tapEvent.AddListener(OnTapTest);
        Input.doubleTapEvent.AddListener(OnTapTest);
        Input.multiTapEvent.AddListener(OnTapTest);
        Input.flickEvent.AddListener(OnFlickTest);
        Input.swipeEvent.AddListener(OnSwipeTest);
        Input.longTapBeginEvent.AddListener(OnLongTapBeginTest);
        Input.longTapEndEvent.AddListener(OnLongTapEndTest);
        Input.longTapCancelEvent.AddListener(OnLongTapCancelTest);
        Input.slideBeginEvent.AddListener(OnSlideBeginTest);
        Input.slideEndEvent.AddListener(OnSlideEndTest);
        Input.slideCancelEvent.AddListener(OnSlideCancelTest);
        Input.pinchBeginEvent.AddListener(OnPinchBeginTest);
        Input.pinchEndEvent.AddListener(OnPinchEndTest);
    }

    public void OnSuspiciousBehaviour(AInput.Touch touch)
    {
        Debug.Log($"Suspicious behaviour alert! Tap Time: {touch.endTime - touch.startTime}");
    }
    public void OnTapTest(Touch touch)
    {
        Debug.Log($"Tapped! Tap Count: {touch.tapCount}");
    }

    public void OnFlickTest(Touch touch)
    {
        Debug.Log($"Flicked! Delta: {touch.deltaPosition}");
    }

    public void OnSwipeTest(Touch touch)
    {
        Debug.Log($"Swiped! Delta: {touch.startToEndDelta}");
    }

    public void OnLongTapBeginTest(Touch touch)
    {
        Debug.Log($"LongTap Begin: {touch.startTime}");
    }

    public void OnLongTapEndTest(Touch touch)
    {
        Debug.Log($"LongTap End: {touch.endTime}");
    }

    public void OnLongTapCancelTest(Touch touch)
    {
        Debug.Log($"LongTap Cancelled.");
    }

    public void OnSlideBeginTest(Touch touch)
    {
        Debug.Log($"Slide Started! Delta: {touch.deltaPosition}");
    }

    public void OnSlideEndTest(Touch touch)
    {
        Debug.Log($"Slide Ended! Delta: {touch.deltaPosition}");
    }

    public void OnSlideCancelTest(Touch touch)
    {
        Debug.Log($"Slide canceled.");
    }

    public void OnPinchBeginTest(Touch touch1, Touch touch2)
    {
        Debug.Log($"Pinch began!");
    }

    public void OnPinchEndTest(Touch touch1, Touch touch2)
    {
        Debug.Log($"Pinch ended!");
    }
}
