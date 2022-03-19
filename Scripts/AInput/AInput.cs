#if (UNITY_EDITOR || UNITY_STANDALONE)
#define MOUSE_CONTROLS
#endif
#if (UNITY_ANDROID || UNITY_IOS)
#define TOUCH_CONTROLS
#endif

//#define DEBUG_MODE
//#define MOBILE_3D

/*
============================= [ AInput ] =============================
                        by Michael McCluskey

This script package utilizes Unity's Event system, coroutines,
and a bit of magic to provide a more configurable option to
Unity's input system.

Each screen input has its own unique data, such as the last collider 
hit, the amount of consecutive taps, and how long the given input has
has been down.

=============================   USAGE   ==============================

* Static members:
    touches - A reference to the internal ATouch array
    touchCount - Amount of touches on screen
    lastTouch - The last touch that had an interaction
    tapCount - The tap count of the last touch

* Only 1 AInput component is expected in the scene
* AInput events use the function signature: 

    void Function(ATouch)

    for single input and:

    void Function(ATouch, ATouch)

    for double input.

* Write a function with this signature in your script, then add:
    
    AInput.{event}.AddListener(Function);

    to your scripts's Start function, replacing {event} with
    your chosen event. All events are listed below.

=============================   EVENTS   =============================

* Utility
    suspiciousBehavio(u)rEvent - This event will fire whenever suspicious behavior is found.
    * Currently, only a minimum tap time is checked.

* Single Input Events
    touchStartEvent - When a touch starts (useful for instant touch checks)
    touchEndEvent - When a touch ends (useful for custom checks)
    tapEvent - A single tap
    doubleTapEvent - 2 consecutive taps
    multitapEvent - 3+ taps. You can get the tap count from AInput.tapCount
    flickEvent - Lifting the touch has velocity. You can check input.deltaPosition for the last movement delta
    swipeEvent - A short, configured slide

    longTapEvent - A stationary touch that extends the maximum tap time
    longTapBeginEvent
    longTapEndEvent
    longTapCancelEvent

    slideEvent - Any touch motion produces this event (configurable)
    slideBeginEvent
    slideEndEvent
    slideCancelEvent

* Double Input Events
    pinchEvent - 2 touches are both sliding
     * Slides are disabled while a pinch event occurs
     
========================   CONFIGURATION   ===========================

This script offers more configuration than Unity provides. You can
generate a Configuration ScriptableObject from the asset menu:

    Right Click > ScriptableObjects > AInputConfiguration

You can then drop that object into the AInput component.

* See AInputConfiguration.cs for all configuration options.
      
======================   PLANNED FEATURES   ==========================

* 3D games (it may work currently, but it's untested)
* Assign touch to multiplayer (team)
* Multi-touch simulation for Mouse and Keyboard
* Better support and/or documentation for Unity UI
      
======================================================================
*/

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class AInput : MonoBehaviour
{
    enum MultiEvent
    {
        kBegin,
        kActive,
        kEnd,
        kCancel,
        
        kCount,
    }

    struct CoroutineStatus
    {
        public bool isRunning;
        public Coroutine routine;
    }

    // Configuration
    [SerializeField] private AInputConfiguration m_config = null;

    private static readonly GUIStyle s_debugGUIStyle = new GUIStyle();
    private static bool s_initialized = false;

#if MOUSE_CONTROLS
    static readonly int s_LMB = 0;
    static readonly int s_RMB = 1;
#endif

#if MOBILE_3D
    public Collider lastCollision => m_lastTouch.lastCollision;
#else
    public Collider2D lastCollision => m_lastTouch.lastCollision;
#endif

    private ATouch[] m_touches = null;
    private int m_touchCount = 0;
    private ATouch m_lastTouch = null;

    private CoroutineStatus[] m_tapRoutine = null;
    private CoroutineStatus[] m_longTapRoutine = null;
    private CoroutineStatus[] m_slideRoutine = null;
    private CoroutineStatus m_pinchRoutine = new CoroutineStatus(); // Because Pinch is a complex gesture, we only want 1 at any time

    private static UnityEvent<ATouch> s_suspiciousBehaviourEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_touchBeginEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_touchEndEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_tapEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_doubleTapEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_multiTapEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_flickEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch> s_swipeEvent = new UnityEvent<ATouch>();
    private static UnityEvent<ATouch>[] s_longTapEvent = { new UnityEvent<ATouch>(), new UnityEvent<ATouch>(), new UnityEvent<ATouch>(), new UnityEvent<ATouch>() };
    private static UnityEvent<ATouch>[] s_slideEvent = { new UnityEvent<ATouch>(), new UnityEvent<ATouch>(), new UnityEvent<ATouch>(), new UnityEvent<ATouch>() };
    private static UnityEvent<ATouch, ATouch>[] s_pinchEvent = { new UnityEvent<ATouch,ATouch>(), new UnityEvent<ATouch, ATouch>(), new UnityEvent<ATouch, ATouch>(), new UnityEvent<ATouch, ATouch>() };

    public ATouch[] touches => m_touches;
    public int touchCount => m_touchCount;
    public ATouch lastTouch => m_lastTouch;
    public int tapCount => m_lastTouch.tapCount;

    public static UnityEvent<ATouch> suspiciousBehaviourEvent => s_suspiciousBehaviourEvent;
    public static UnityEvent<ATouch> suspiciousBehaviorEvent => s_suspiciousBehaviourEvent;
    public static UnityEvent<ATouch> touchBeginEvent => s_touchBeginEvent;
    public static UnityEvent<ATouch> touchEndEvent => s_touchEndEvent;
    public static UnityEvent<ATouch> tapEvent => s_tapEvent;
    public static UnityEvent<ATouch> doubleTapEvent => s_doubleTapEvent;
    public static UnityEvent<ATouch> multiTapEvent => s_multiTapEvent;
    public static UnityEvent<ATouch> flickEvent => s_flickEvent;
    public static UnityEvent<ATouch> swipeEvent => s_swipeEvent;
    public static UnityEvent<ATouch> longTapEvent => s_longTapEvent[(int)MultiEvent.kActive];
    public static UnityEvent<ATouch> longTapBeginEvent => s_longTapEvent[(int)MultiEvent.kBegin];
    public static UnityEvent<ATouch> longTapEndEvent => s_longTapEvent[(int)MultiEvent.kEnd];
    public static UnityEvent<ATouch> longTapCancelEvent => s_longTapEvent[(int)MultiEvent.kCancel];
    public static UnityEvent<ATouch> slideEvent => s_slideEvent[(int)MultiEvent.kActive];
    public static UnityEvent<ATouch> slideBeginEvent => s_slideEvent[(int)MultiEvent.kBegin];
    public static UnityEvent<ATouch> slideEndEvent => s_slideEvent[(int)MultiEvent.kEnd];
    public static UnityEvent<ATouch> slideCancelEvent => s_slideEvent[(int)MultiEvent.kCancel];
    public static UnityEvent<ATouch, ATouch> pinchEvent => s_pinchEvent[(int)MultiEvent.kActive];
    public static UnityEvent<ATouch, ATouch> pinchBeginEvent => s_pinchEvent[(int)MultiEvent.kBegin];
    public static UnityEvent<ATouch, ATouch> pinchEndEvent => s_pinchEvent[(int)MultiEvent.kEnd];
    public static UnityEvent<ATouch, ATouch> pinchCancelEvent => s_pinchEvent[(int)MultiEvent.kCancel];

    private void Start()
    {
        if (!s_initialized)
            s_initialized = true;
        else
            Debug.LogError("Additional AInput component! This will break functionality, please keep only 1 AInput component in the scene.", this);

        // These all need to be in Start, and not the constructor.
        m_touches = new ATouch[m_config.maxTouches];

        m_tapRoutine = new CoroutineStatus[m_config.maxTouches];
        m_longTapRoutine = new CoroutineStatus[m_config.maxTouches];
        m_slideRoutine = new CoroutineStatus[m_config.maxTouches];

        FillArray(m_touches, m_config.maxTouches);

        s_debugGUIStyle.fontSize = 24;
    }

    private void Update()
    {
        // Filter out already finished touches
        for (int i = 0; i < m_config.maxTouches; ++i)
        {
            ATouch touch = m_touches[i];
            TryStopTouch(touch);

            // Increment new touches to the "already exists" phase
            if(touch.phase == TouchPhase.Began)
                touch.phase = TouchPhase.Stationary;
        }

#if TOUCH_CONTROLS
        // Convert touches to our ATouches
        for(int i = 0; i < Input.touchCount; ++i)
        {
            // We only read as many touches as we have configured.
            if (i >= m_config.maxTouches)
                break;

            Touch touch = Input.GetTouch(i);
            ATouch aTouch = m_touches[touch.fingerId];

            aTouch.type = touch.type;
            aTouch.screenPosition = touch.position;

            if(!aTouch.isDown)
            {
                aTouch.isDown = true;
                aTouch.startTime = Time.time;
                StartTouch(aTouch);
            }
            
            // We need to specify which phases to import, basically Begin
            //  and End since we can't really detect those otherwise.
            //  Everything else is handled by our script.
            switch(touch.phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Ended: aTouch.phase = touch.phase; break;
            }
            
            m_lastTouch = aTouch;
        }

#elif MOUSE_CONTROLS
        // If LMB event
        if(Input.GetMouseButton(s_LMB))
        {
            // If it just went down
            if(Input.GetMouseButtonDown(s_LMB))
            {
                // Add the new touch to our list
                ATouch touch = null;
                for(int i = 0; i < m_config.maxTouches; ++i)
                {
                    if(!m_touches[i].isDown)
                    {
                        touch = m_touches[i];
                        touch.isDown = true;
                        m_lastTouch = touch;
                        break;
                    }
                }

                if (touch == null)
                    return;

                touch.startTime = Time.time;
                touch.screenPosition = Input.mousePosition;
                touch.phase = TouchPhase.Began;
                touch.type = TouchType.Indirect;

                StartTouch(touch);
            }

            m_lastTouch.screenPosition = Input.mousePosition;
        }
        // Released LMB event
        else if (Input.GetMouseButtonUp(s_LMB))
        {
            Vector2 mousePosition = Input.mousePosition;

            // With mouse controls, we'll always be working with the most recent touch
            ATouch touch = m_lastTouch;

            if (touch == null)
                return;

            m_lastTouch.phase = TouchPhase.Ended;
        }
#endif // MOUSE_CONTROLS


        foreach(ATouch touch in m_touches)
        {
            // If the touch has ended, is not down, or the position hasn't updated, continue.
            if(touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended || touch.screenPosition == touch._lastPosition)
                continue;

            // Are we moving?
            // Make sure that the first placement doesn't count as a move.
            Vector2 deltaPos = touch.screenPosition - touch._lastPosition;
            if (touch.phase == TouchPhase.Began || Vector2.SqrMagnitude(deltaPos) <= m_config.maxTouchDistance * m_config.maxTouchDistance)
            {
                touch.phase = TouchPhase.Stationary;
                touch.deltaPosition = Vector2.zero;
            }
            else
            {
                touch.phase = TouchPhase.Moved;
                touch.deltaPosition = deltaPos;

                // If we have not invoked a slide, do so now
                if(!m_slideRoutine[touch.id].isRunning)
                {
                    StartSlide(touch);
                }
            }

            touch._lastPosition = touch.screenPosition;
        }

        // Try and grab last collision for all touches
        if(m_config.minimalRaycasting == false)
        { 
            for(int i = 0; i < m_config.maxTouches; ++i)
            {
                TryCast(m_touches[i]);
            }
        }
    }

#if DEBUG_MODE
    private void OnGUI()
    {
        if (m_lastTouch == null)
            return;

        GUILayout.Label("INPUT DEBUG:", s_debugGUIStyle);
        GUILayout.Label($"Last Touch ID: {m_lastTouch.id}", s_debugGUIStyle);
        GUILayout.Label($"Touches: {m_touchCount}", s_debugGUIStyle);
        GUILayout.Label($"Last Touch Phase: {m_lastTouch.phase}", s_debugGUIStyle);
        GUILayout.Label($"Last Touch Position: {m_lastTouch.screenPosition}", s_debugGUIStyle);
        GUILayout.Label($"Last Touch LastPosition: {m_lastTouch._lastPosition}", s_debugGUIStyle);
        GUILayout.Label($"Tap Count: {m_lastTouch.tapCount}", s_debugGUIStyle);
        GUILayout.Label($"slideEvent[0]: {m_slideRoutine[0].isRunning}", s_debugGUIStyle);
    }
#endif

    private void ResetCoroutineStatus(CoroutineStatus[] routines)
    {
        for(int i = 0; i < routines.Length; ++i)
        {
            routines[i].isRunning = false;
        }
    }

    private void TryCast(ATouch touch)
    {
        if (touch == null)
            return;
        
        if(!touch.isDown)
            touch.lastCollision = null;

        touch.lastCollision = RayCastToWorld(touch.screenPosition);
    }

    private void StartTouch(ATouch touch)
    {
        ++m_touchCount;

        touch._startPosition = touch.screenPosition;

        // We'll do an additional cast here because we need to know if something is right under the touch
        TryCast(touch);

        if (m_tapRoutine[touch.id].isRunning)
            StopCoroutine(m_tapRoutine[touch.id].routine);
        
        CancelEvent(s_longTapEvent, ref m_longTapRoutine[touch.id], touch);

        s_touchBeginEvent.Invoke(touch);
        m_tapRoutine[touch.id].routine = StartCoroutine(TapCheck(touch));

        // This routine starts now, but has a delay for when the event actually starts
        m_longTapRoutine[touch.id].routine = StartCoroutine(LongTapCheck(touch));
    }

    private void StartSlide(ATouch touch)
    {
        if (m_tapRoutine[touch.id].isRunning)
            StopCoroutine(m_tapRoutine[touch.id].routine);

        CancelEvent(s_longTapEvent, ref m_longTapRoutine[touch.id], touch);

        if(!m_pinchRoutine.isRunning)
        {
            // For each other touch, if it is sliding, cancel that slide and begin a pinch
            foreach(ATouch otherTouch in m_touches)
            {
                if (touch == otherTouch)
                    continue;

                if(m_slideRoutine[otherTouch.id].isRunning)
                {
                    CancelEvent(s_slideEvent, ref m_slideRoutine[otherTouch.id], otherTouch);
                    StartCoroutine(PinchCheck(otherTouch, touch));
                    return;
                }
            }

            // We've made a decision here that swipes cannot occur while a pinch is occuring.
            m_slideRoutine[touch.id].routine = StartCoroutine(SlideCheck(touch));
        }
    }

    private void CancelEvent(UnityEvent<ATouch>[] multitouchEvent, ref CoroutineStatus routineStatus, ATouch touch)
    {
        if(routineStatus.routine != null)
            StopCoroutine(routineStatus.routine);
        if(routineStatus.isRunning)
            multitouchEvent[(int)MultiEvent.kCancel].Invoke(touch);
        routineStatus.isRunning = false;
    }

    private void TryStopTouch(ATouch touch)
    {
        if (touch.phase == TouchPhase.Ended)
        {
            touch.endTime = Time.time;
            touch.phase = TouchPhase.Canceled;
            touch.startToEndDelta = touch.screenPosition - touch._startPosition;
            touch.isDown = false;
            --m_touchCount;
            
            TryCast(touch);

            s_touchEndEvent.Invoke(touch);

            if (touch.deltaPosition != Vector2.zero)
                s_flickEvent.Invoke(touch);
        }
    }

    private void FillArray<T>(T[] array, int length) where T : new()
    {
        for(int i = 0; i < length; ++i)
        {
            array[i] = new T();
        }
    }

    private IEnumerator TapCheck(ATouch touch)
    {
        m_tapRoutine[touch.id].isRunning = true;

        while (touch.isDown) 
        { 
            yield return null; 
        }

        float pressTime = touch.endTime - touch.startTime;
        // If the touch time is suspicious, report it, but 
        //  continue as normal as to not alert the user.
        if(pressTime < m_config.maxSuspiciousTime)
        {
            s_suspiciousBehaviourEvent.Invoke(touch);
        }
        
        // If the touch time is within limits, we tapped.
        if (pressTime < m_config.maxTapTime)
        {
            ++touch.tapCount;

            // Wait until the next possible tap (this coroutine will be killed if another starts)
            yield return new WaitForSeconds(m_config.maxConsecutiveTouchTime);

            // Send the tap event
            switch (touch.tapCount)
            {
                case 1: s_tapEvent.Invoke(touch); break;
                case 2: s_doubleTapEvent.Invoke(touch); break;
                default: s_multiTapEvent.Invoke(touch); break;
            }

            yield return null;

            // Reset tap count
            touch.tapCount = 0;
        }
        // Otherwise the tap failed, and we reset all taps for this touch
        else
        {
            touch.tapCount = 0;
        }

        m_tapRoutine[touch.id].isRunning = false;
    }

    private IEnumerator LongTapCheck(ATouch touch)
    {
        yield return new WaitForSeconds(m_config.maxTapTime);


        // If the touch is still down, begin a long press
        if (touch.isDown)
        {
            m_longTapRoutine[touch.id].isRunning = true;
            s_longTapEvent[(int)MultiEvent.kBegin].Invoke(touch);
            yield return null;
        }
        // otherwise, exit this routine
        else
        {
            yield break;
        }

        while(touch.isDown)
        {
            s_longTapEvent[(int)MultiEvent.kActive].Invoke(touch);
            yield return null;
        }

        s_longTapEvent[(int)MultiEvent.kEnd].Invoke(touch);

        m_longTapRoutine[touch.id].isRunning = false;
    }

    private IEnumerator SlideCheck(ATouch touch)
    {
        m_slideRoutine[touch.id].isRunning = true;

        s_slideEvent[(int)MultiEvent.kBegin].Invoke(touch);
        yield return null;

        while (touch.isDown)
        {
            s_slideEvent[(int)MultiEvent.kActive].Invoke(touch);
            yield return null;
        }

        s_slideEvent[(int)MultiEvent.kEnd].Invoke(touch);

        if(touch.endTime - touch.startTime < m_config.maxSwipeTime && Vector3.SqrMagnitude(touch.startToEndDelta) >= m_config.minSwipeDistance * m_config.minSwipeDistance)
        {
            s_swipeEvent.Invoke(touch);
        }

        m_slideRoutine[touch.id].isRunning = false;
    }

    private IEnumerator PinchCheck(ATouch touch1, ATouch touch2)
    {
        m_pinchRoutine.isRunning = true;

        s_pinchEvent[(int)MultiEvent.kBegin].Invoke(touch1, touch2);
        yield return null;

        while (touch1.isDown || touch2.isDown)
        {
            s_pinchEvent[(int)MultiEvent.kActive].Invoke(touch1, touch2);
            yield return null;
        }

        s_pinchEvent[(int)MultiEvent.kEnd].Invoke(touch1, touch2);

        m_pinchRoutine.isRunning = false;
    }

#if !MOBILE_3D
    // https://kylewbanks.com/blog/unity-2d-detecting-gameobject-clicks-using-raycasts
    private Collider2D RayCastToWorld(Vector2 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
        return Physics2D.OverlapPoint(worldPos2D);
    }
#else
    private Collider RayCastToWorld(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        Physics.Raycast(ray, out hit);
        return hit.collider;
    }
#endif
}
