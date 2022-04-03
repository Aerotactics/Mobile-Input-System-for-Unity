//#define DEBUG_MODE
//#define MOBILE_3D

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AInput
{
    public sealed class Input : MonoBehaviour
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
        [SerializeField] private InputConfiguration m_config = null;

        private static readonly GUIStyle s_debugGUIStyle = new GUIStyle();
        private static bool s_initialized = false;
#if MOBILE_3D
        private static Collider s_lastCollision = null;
#else
        private static Collider2D s_lastCollision = null;
#endif

        static readonly int s_LMB = 0;
        static readonly int s_RMB = 1;

        private static Touch[] s_touches = null;
        private static int s_touchCount = 0;
        private static Touch s_lastTouch = null;

        private CoroutineStatus[] m_tapRoutine = null;
        private CoroutineStatus[] m_longTapRoutine = null;
        private CoroutineStatus[] m_slideRoutine = null;
        private CoroutineStatus m_pinchRoutine = new CoroutineStatus(); // Because Pinch is a complex gesture, we only want 1 at any time

        private static UnityEvent<Touch> s_suspiciousBehaviourEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch> s_tapEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch> s_doubleTapEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch> s_multiTapEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch> s_flickEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch> s_swipeEvent = new UnityEvent<Touch>();
        private static UnityEvent<Touch>[] s_touchEvent = { new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>() };
        private static UnityEvent<Touch>[] s_longTapEvent = { new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>() };
        private static UnityEvent<Touch>[] s_slideEvent = { new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>(), new UnityEvent<Touch>() };
        private static UnityEvent<Touch, Touch>[] s_pinchEvent = { new UnityEvent<Touch, Touch>(), new UnityEvent<Touch, Touch>(), new UnityEvent<Touch, Touch>(), new UnityEvent<Touch, Touch>() };

        public static Touch[] touches => s_touches;
        public static int touchCount => s_touchCount;
        public static Touch lastTouch => s_lastTouch;
        public static int tapCount => s_lastTouch.tapCount;

#if MOBILE_3D
        public static Collider lastCollision => s_lastCollision;
#else
        public static Collider2D lastCollision => s_lastCollision;
#endif

        public static UnityEvent<Touch> suspiciousBehaviourEvent => s_suspiciousBehaviourEvent;
        public static UnityEvent<Touch> suspiciousBehaviorEvent => s_suspiciousBehaviourEvent;
        public static UnityEvent<Touch> touchEvent => s_touchEvent[(int)MultiEvent.kActive];
        public static UnityEvent<Touch> touchBeginEvent => s_touchEvent[(int)MultiEvent.kBegin];
        public static UnityEvent<Touch> touchEndEvent => s_touchEvent[(int)MultiEvent.kEnd];
        public static UnityEvent<Touch> tapEvent => s_tapEvent;
        public static UnityEvent<Touch> doubleTapEvent => s_doubleTapEvent;
        public static UnityEvent<Touch> multiTapEvent => s_multiTapEvent;
        public static UnityEvent<Touch> flickEvent => s_flickEvent;
        public static UnityEvent<Touch> swipeEvent => s_swipeEvent;
        public static UnityEvent<Touch> longTapEvent => s_longTapEvent[(int)MultiEvent.kActive];
        public static UnityEvent<Touch> longTapBeginEvent => s_longTapEvent[(int)MultiEvent.kBegin];
        public static UnityEvent<Touch> longTapEndEvent => s_longTapEvent[(int)MultiEvent.kEnd];
        public static UnityEvent<Touch> longTapCancelEvent => s_longTapEvent[(int)MultiEvent.kCancel];
        public static UnityEvent<Touch> slideEvent => s_slideEvent[(int)MultiEvent.kActive];
        public static UnityEvent<Touch> slideBeginEvent => s_slideEvent[(int)MultiEvent.kBegin];
        public static UnityEvent<Touch> slideEndEvent => s_slideEvent[(int)MultiEvent.kEnd];
        public static UnityEvent<Touch> slideCancelEvent => s_slideEvent[(int)MultiEvent.kCancel];
        public static UnityEvent<Touch, Touch> pinchEvent => s_pinchEvent[(int)MultiEvent.kActive];
        public static UnityEvent<Touch, Touch> pinchBeginEvent => s_pinchEvent[(int)MultiEvent.kBegin];
        public static UnityEvent<Touch, Touch> pinchEndEvent => s_pinchEvent[(int)MultiEvent.kEnd];
        public static UnityEvent<Touch, Touch> pinchCancelEvent => s_pinchEvent[(int)MultiEvent.kCancel];

        private void Start()
        {
            if (!s_initialized)
                s_initialized = true;
            else
                Debug.LogError("Additional AInput InputComponent! This will break functionality, please keep only 1 AInput InputComponent in the scene.", this);

            // These all need to be in Start, and not the constructor.
            s_touches = new Touch[m_config.maxTouches];

            m_tapRoutine = new CoroutineStatus[m_config.maxTouches];
            m_longTapRoutine = new CoroutineStatus[m_config.maxTouches];
            m_slideRoutine = new CoroutineStatus[m_config.maxTouches];

            FillArray(s_touches, m_config.maxTouches);

            s_debugGUIStyle.fontSize = 24;
        }

        private void Update()
        {
            // Filter out already finished touches
            for (int i = 0; i < m_config.maxTouches; ++i)
            {
                Touch touch = s_touches[i];
                TryStopTouch(touch);

                // Increment new touches to the "already exists" phase
                if (touch.phase == TouchPhase.Began)
                    touch.phase = TouchPhase.Stationary;
            }

            if (m_config.useMouseForInputs == false)
            {
                // Convert touches to our ATouches
                for (int i = 0; i < UnityEngine.Input.touchCount; ++i)
                {
                    // We only read as many touches as we have configured.
                    if (i >= m_config.maxTouches)
                        break;

                    UnityEngine.Touch uTouch = UnityEngine.Input.GetTouch(i);

                    if (uTouch.fingerId >= m_config.maxTouches)
                        continue;

                    Touch aTouch = s_touches[uTouch.fingerId];

                    aTouch.type = uTouch.type;
                    aTouch.screenPosition = uTouch.position;

                    if (!aTouch.isDown)
                    {
                        aTouch.isDown = true;
                        aTouch.startTime = Time.time;
                        StartTouch(aTouch);
                    }

                    // We need to specify which phases to import, basically Begin
                    //  and End since we can't really detect those otherwise.
                    //  Everything else is handled by our script.
                    switch (uTouch.phase)
                    {
                        case TouchPhase.Began:
                        case TouchPhase.Ended: aTouch.phase = uTouch.phase; break;
                    }

                    s_lastTouch = aTouch;
                }
            }
            else
            {
                // If LMB event
                if (UnityEngine.Input.GetMouseButton(s_LMB))
                {
                    // If it just went down
                    if (UnityEngine.Input.GetMouseButtonDown(s_LMB))
                    {
                        // Add the new touch to our list
                        Touch touch = null;
                        for (int i = 0; i < m_config.maxTouches; ++i)
                        {
                            if (!s_touches[i].isDown)
                            {
                                touch = s_touches[i];
                                touch.isDown = true;
                                s_lastTouch = touch;
                                break;
                            }
                        }

                        if (touch == null)
                            return;

                        touch.startTime = Time.time;
                        touch.screenPosition = UnityEngine.Input.mousePosition;
                        touch.phase = TouchPhase.Began;
                        touch.type = TouchType.Indirect;

                        StartTouch(touch);
                    }

                    s_lastTouch.screenPosition = UnityEngine.Input.mousePosition;
                }
                // Released LMB event
                else if (UnityEngine.Input.GetMouseButtonUp(s_LMB))
                {
                    Vector2 mousePosition = UnityEngine.Input.mousePosition;

                    // With mouse controls, we'll always be working with the most recent touch
                    Touch touch = s_lastTouch;

                    if (touch == null)
                        return;

                    s_lastTouch.phase = TouchPhase.Ended;
                }
            }

            foreach (Touch touch in s_touches)
            {
                // If the touch has ended, is not down, or the position hasn't updated, continue.
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended || touch.screenPosition == touch._lastPosition)
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
                    if (!m_slideRoutine[touch.id].isRunning)
                    {
                        StartSlide(touch);
                    }
                }

                touch._lastPosition = touch.screenPosition;

                s_touchEvent[(int)MultiEvent.kActive].Invoke(touch);
            }

            // Try and grab last collision for all touches
            if (m_config.minimalRaycasting == false)
            {
                for (int i = 0; i < m_config.maxTouches; ++i)
                {
                    if (TryCast(s_touches[i]))
                        s_lastCollision = s_touches[i].lastCollision;
                }
            }
        }
        private void OnGUI()
        {
            if (m_config.showDebugUI)
            {
                if (s_lastTouch == null)
                    return;

                GUILayout.Label("INPUT DEBUG:", s_debugGUIStyle);
                GUILayout.Label($"Last Touch ID: {s_lastTouch.id}", s_debugGUIStyle);
                GUILayout.Label($"Touches: {s_touchCount}", s_debugGUIStyle);
                GUILayout.Label($"Last Touch Phase: {s_lastTouch.phase}", s_debugGUIStyle);
                GUILayout.Label($"Last Touch Position: {s_lastTouch.screenPosition}", s_debugGUIStyle);
                GUILayout.Label($"Last Touch LastPosition: {s_lastTouch._lastPosition}", s_debugGUIStyle);
                GUILayout.Label($"Tap Count: {s_lastTouch.tapCount}", s_debugGUIStyle);
                GUILayout.Label($"slideEvent[0]: {m_slideRoutine[0].isRunning}", s_debugGUIStyle);
            }
        }

        private void ResetCoroutineStatus(CoroutineStatus[] routines)
        {
            for (int i = 0; i < routines.Length; ++i)
            {
                routines[i].isRunning = false;
            }
        }

        private bool TryCast(Touch touch)
        {
            if (touch == null)
                return false;

            if (!touch.isDown)
                touch.lastCollision = null;

            touch.lastCollision = RayCastToWorld(touch.screenPosition);
            if (touch.lastCollision != null)
                return true;
            return false;
        }

        private void StartTouch(Touch touch)
        {
            ++s_touchCount;

            touch._startPosition = touch.screenPosition;

            // We'll do an additional cast here because we need to know if something is right under the touch
            if(TryCast(touch))
                s_lastCollision = touch.lastCollision;

            if (m_tapRoutine[touch.id].isRunning)
                StopCoroutine(m_tapRoutine[touch.id].routine);

            CancelEvent(s_longTapEvent, ref m_longTapRoutine[touch.id], touch);

            s_touchEvent[(int)MultiEvent.kBegin].Invoke(touch);
            m_tapRoutine[touch.id].routine = StartCoroutine(TapCheck(touch));

            // This routine starts now, but has a delay for when the event actually starts
            m_longTapRoutine[touch.id].routine = StartCoroutine(LongTapCheck(touch));
        }

        private void StartSlide(Touch touch)
        {
            if (m_tapRoutine[touch.id].isRunning)
                StopCoroutine(m_tapRoutine[touch.id].routine);

            CancelEvent(s_longTapEvent, ref m_longTapRoutine[touch.id], touch);

            if (!m_pinchRoutine.isRunning)
            {
                // For each other touch, if it is sliding, cancel that slide and begin a pinch
                foreach (Touch otherTouch in s_touches)
                {
                    if (touch == otherTouch)
                        continue;

                    if (m_slideRoutine[otherTouch.id].isRunning)
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

        private void CancelEvent(UnityEvent<Touch>[] multitouchEvent, ref CoroutineStatus routineStatus, Touch touch)
        {
            if (routineStatus.routine != null)
                StopCoroutine(routineStatus.routine);
            if (routineStatus.isRunning)
                multitouchEvent[(int)MultiEvent.kCancel].Invoke(touch);
            routineStatus.isRunning = false;
        }

        private void TryStopTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Ended)
            {
                touch.endTime = Time.time;
                touch.phase = TouchPhase.Canceled;
                touch.startToEndDelta = touch.screenPosition - touch._startPosition;
                touch.isDown = false;
                --s_touchCount;

                if (TryCast(touch))
                    s_lastCollision = touch.lastCollision;

                s_touchEvent[(int)MultiEvent.kEnd].Invoke(touch);

                if (touch.deltaPosition != Vector2.zero)
                    s_flickEvent.Invoke(touch);
            }
        }

        private void FillArray<T>(T[] array, int length) where T : new()
        {
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }
        }

        private IEnumerator TapCheck(Touch touch)
        {
            m_tapRoutine[touch.id].isRunning = true;

            while (touch.isDown)
            {
                yield return null;
            }

            float pressTime = touch.endTime - touch.startTime;
            // If the touch time is suspicious, report it, but 
            //  continue as normal as to not alert the user.
            if (pressTime < m_config.maxSuspiciousTime)
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

        private IEnumerator LongTapCheck(Touch touch)
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

            while (touch.isDown)
            {
                s_longTapEvent[(int)MultiEvent.kActive].Invoke(touch);
                yield return null;
            }

            s_longTapEvent[(int)MultiEvent.kEnd].Invoke(touch);

            m_longTapRoutine[touch.id].isRunning = false;
        }

        private IEnumerator SlideCheck(Touch touch)
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

            if (touch.endTime - touch.startTime < m_config.maxSwipeTime && Vector3.SqrMagnitude(touch.startToEndDelta) >= m_config.minSwipeDistance * m_config.minSwipeDistance)
            {
                s_swipeEvent.Invoke(touch);
            }

            m_slideRoutine[touch.id].isRunning = false;
        }

        private IEnumerator PinchCheck(Touch touch1, Touch touch2)
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
}
