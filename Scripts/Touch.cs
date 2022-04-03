//#define MOBILE_3D

using UnityEngine;

namespace AInput
{
    public class Touch
    {
        private static int s_indexIncrement = 0;
        private int m_id = -1;

        // C# 9.0 prefers "new(constructor)" over "new Type(constructor)"
        //  but Unity hates that.
        public static readonly Vector2 kNullPosition = new Vector2(-1, -1);

        public bool isDown = false;
        public int team = 0;
        public int tapCount = 0;
        public float startTime = 0;
        public float endTime = 0;
        public TouchPhase phase = TouchPhase.Canceled;
        public TouchType type = TouchType.Direct;
        public Vector2 screenPosition = kNullPosition;
        public Vector2 deltaPosition = Vector2.zero;
        public Vector2 startToEndDelta = Vector2.zero;
        internal Vector2 _lastPosition = kNullPosition;
        internal Vector2 _startPosition = kNullPosition;
#if MOBILE_3D
        public Collider lastCollision = null;
#else
        public Collider2D lastCollision = null;
#endif

        public int id => m_id;

        public Touch()
        {
            m_id = s_indexIncrement++;
        }

        public static bool operator ==(Touch left, Touch right) { return (left is null && right is null || !(left is null || right is null) && left.m_id == right.m_id); }
        public static bool operator !=(Touch left, Touch right) { return !(left == right); }
        public override bool Equals(object obj) { return obj is Touch; }
        public override int GetHashCode() { return base.GetHashCode(); }
    }
}
