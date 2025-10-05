using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using System.Collections.Generic;

/// <summary>
/// AR Gesture Controller that integrates with Unity's XR Input system
/// Provides enhanced gesture recognition for mobile AR applications
/// Based on Unity AR Template patterns
/// </summary>
public class ARGestureController : MonoBehaviour
{
    [Header("Input Readers - Unity AR Template Style")]
    [SerializeField] private XRInputValueReader<Vector2> m_TapStartPositionInput = new XRInputValueReader<Vector2>("Tap Start Position");
    [SerializeField] private XRInputValueReader<Vector2> m_DragCurrentPositionInput = new XRInputValueReader<Vector2>("Drag Current Position");
    [SerializeField] private XRInputValueReader<Vector2> m_PinchStartPositionInput = new XRInputValueReader<Vector2>("Pinch Start Position");
    [SerializeField] private XRInputValueReader<float> m_PinchGapInput = new XRInputValueReader<float>("Pinch Gap");
    [SerializeField] private XRInputValueReader<float> m_TwistRotationInput = new XRInputValueReader<float>("Twist Rotation");

    [Header("Gesture Settings")]
    [SerializeField] private float pinchThreshold = 10f;
    [SerializeField] private float twistThreshold = 5f;
    [SerializeField] private float dragThreshold = 5f;
    [SerializeField] private float tapTimeThreshold = 0.3f;
    [SerializeField] private float doubleTapTimeThreshold = 0.5f;

    [Header("Sensitivity Settings")]
    [SerializeField] private float pinchSensitivity = 1f;
    [SerializeField] private float twistSensitivity = 1f;
    [SerializeField] private float dragSensitivity = 1f;

    // Events
    public System.Action<Vector2> OnTap;
    public System.Action<Vector2> OnDoubleTap;
    public System.Action<Vector2> OnDragStart;
    public System.Action<Vector2> OnDragUpdate;
    public System.Action<Vector2> OnDragEnd;
    public System.Action<Vector2, float> OnPinchStart;
    public System.Action<Vector2, float> OnPinchUpdate;
    public System.Action<Vector2, float> OnPinchEnd;
    public System.Action<Vector2, float> OnTwistStart;
    public System.Action<Vector2, float> OnTwistUpdate;
    public System.Action<Vector2, float> OnTwistEnd;

    // Private state
    private GestureState currentGestureState = GestureState.None;
    private Vector2 lastTouchPosition;
    private float lastTapTime;
    private float gestureStartTime;
    private float initialPinchDistance;
    private float initialTwistAngle;
    private bool isGestureActive;

    // Touch data tracking
    private struct TouchData
    {
        public Vector2 position;
        public Vector2 deltaPosition;
        public float time;
        public int fingerId;
    }

    private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
    private List<TouchData> touchHistory = new List<TouchData>();

    public enum GestureState
    {
        None,
        Tap,
        Drag,
        Pinch,
        Twist,
        DoubleTap
    }

    public GestureState CurrentGestureState => currentGestureState;
    public bool IsGestureActive => isGestureActive;

    void OnEnable()
    {
        // Initialize XR Input Readers
        m_TapStartPositionInput?.EnableDirectActionIfModeUsed();
        m_DragCurrentPositionInput?.EnableDirectActionIfModeUsed();
        m_PinchStartPositionInput?.EnableDirectActionIfModeUsed();
        m_PinchGapInput?.EnableDirectActionIfModeUsed();
        m_TwistRotationInput?.EnableDirectActionIfModeUsed();
    }

    void OnDisable() 
    {
        // Disable XR Input Readers
        m_TapStartPositionInput?.DisableDirectActionIfModeUsed();
        m_DragCurrentPositionInput?.DisableDirectActionIfModeUsed();
        m_PinchStartPositionInput?.DisableDirectActionIfModeUsed();
        m_PinchGapInput?.DisableDirectActionIfModeUsed();
        m_TwistRotationInput?.DisableDirectActionIfModeUsed();
    }

    void Update()
    {
        ProcessTouchInput();
        ProcessXRInputReaders();
        UpdateGestureState();
    }

    void ProcessTouchInput()
    {
        // Process Unity Input System touches
        var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
        if (touchscreen == null) return;

        var touches = touchscreen.touches;
        
        // Update active touches
        activeTouches.Clear();
        for (int i = 0; i < touches.Count && i < 10; i++) // Limit to 10 touches
        {
            var touch = touches[i];
            if (touch.isInProgress)
            {
                var touchData = new TouchData
                {
                    position = touch.position.ReadValue(),
                    deltaPosition = touch.delta.ReadValue(),
                    time = Time.time,
                    fingerId = touch.touchId.ReadValue()
                };
                activeTouches[touchData.fingerId] = touchData;
            }
        }

        // Detect gestures based on touch count and state
        DetectGestures();
    }

    void ProcessXRInputReaders()
    {
        // Process XR Input Reader values (Unity AR Template style)
        if (m_PinchGapInput != null && m_PinchGapInput.TryReadValue(out float pinchGap))
        {
            if (currentGestureState == GestureState.None && Mathf.Abs(pinchGap) > pinchThreshold)
            {
                StartPinchGesture(pinchGap);
            }
            else if (currentGestureState == GestureState.Pinch)
            {
                UpdatePinchGesture(pinchGap);
            }
        }

        if (m_TwistRotationInput != null && m_TwistRotationInput.TryReadValue(out float twistRotation))
        {
            if (currentGestureState == GestureState.None && Mathf.Abs(twistRotation) > twistThreshold)
            {
                StartTwistGesture(twistRotation);
            }
            else if (currentGestureState == GestureState.Twist)
            {
                UpdateTwistGesture(twistRotation);
            }
        }
    }

    void DetectGestures()
    {
        int touchCount = activeTouches.Count;

        switch (touchCount)
        {
            case 0:
                if (isGestureActive)
                {
                    EndCurrentGesture();
                }
                break;

            case 1:
                HandleSingleTouch();
                break;

            case 2:
                HandleTwoFingerGestures();
                break;

            default:
                // Handle multi-touch if needed
                break;
        }
    }

    void HandleSingleTouch()
    {
        var touch = GetFirstTouch();
        Vector2 currentPos = touch.position;
        Vector2 deltaPos = touch.deltaPosition;

        switch (currentGestureState)
        {
            case GestureState.None:
                if (Vector2.Distance(currentPos, touch.position) < dragThreshold)
                {
                    // Potential tap
                    if (Time.time - gestureStartTime > tapTimeThreshold)
                    {
                        // Check for double tap
                        if (Time.time - lastTapTime < doubleTapTimeThreshold)
                        {
                            StartDoubleTapGesture(currentPos);
                        }
                        else
                        {
                            StartTapGesture(currentPos);
                        }
                    }
                }
                else
                {
                    StartDragGesture(currentPos);
                }
                break;

            case GestureState.Drag:
                UpdateDragGesture(currentPos);
                break;

            case GestureState.Tap:
                // Tap might become drag if moved too much
                if (Vector2.Distance(currentPos, lastTouchPosition) > dragThreshold)
                {
                    TransitionToGesture(GestureState.Drag);
                    StartDragGesture(currentPos);
                }
                break;
        }

        lastTouchPosition = currentPos;
    }

    void HandleTwoFingerGestures()
    {
        var touches = GetTwoTouches();
        if (touches.Length < 2) return;

        Vector2 touch1Pos = touches[0].position;
        Vector2 touch2Pos = touches[1].position;
        Vector2 centerPoint = (touch1Pos + touch2Pos) * 0.5f;

        float currentPinchDistance = Vector2.Distance(touch1Pos, touch2Pos);
        float currentTwistAngle = Vector2.SignedAngle(Vector2.up, touch2Pos - touch1Pos);

        switch (currentGestureState)
        {
            case GestureState.None:
                // Determine if this is a pinch or twist gesture
                initialPinchDistance = currentPinchDistance;
                initialTwistAngle = currentTwistAngle;
                
                // Wait a frame to see which gesture dominates
                if (Time.time - gestureStartTime > 0.1f)
                {
                    float pinchDelta = Mathf.Abs(currentPinchDistance - initialPinchDistance);
                    float twistDelta = Mathf.Abs(Mathf.DeltaAngle(currentTwistAngle, initialTwistAngle));

                    if (pinchDelta > pinchThreshold && pinchDelta > twistDelta)
                    {
                        StartPinchGesture(currentPinchDistance);
                    }
                    else if (twistDelta > twistThreshold)
                    {
                        StartTwistGesture(currentTwistAngle);
                    }
                }
                break;

            case GestureState.Pinch:
                UpdatePinchGesture(currentPinchDistance);
                break;

            case GestureState.Twist:
                UpdateTwistGesture(currentTwistAngle);
                break;
        }
    }

    void StartTapGesture(Vector2 position)
    {
        currentGestureState = GestureState.Tap;
        isGestureActive = true;
        gestureStartTime = Time.time;
        OnTap?.Invoke(position);
    }

    void StartDoubleTapGesture(Vector2 position)
    {
        currentGestureState = GestureState.DoubleTap;
        isGestureActive = true;
        gestureStartTime = Time.time;
        OnDoubleTap?.Invoke(position);
    }

    void StartDragGesture(Vector2 position)
    {
        currentGestureState = GestureState.Drag;
        isGestureActive = true;
        gestureStartTime = Time.time;
        OnDragStart?.Invoke(position);
    }

    void UpdateDragGesture(Vector2 position)
    {
        OnDragUpdate?.Invoke(position);
    }

    void StartPinchGesture(float distance)
    {
        currentGestureState = GestureState.Pinch;
        isGestureActive = true;
        gestureStartTime = Time.time;
        initialPinchDistance = distance;
        
        Vector2 centerPoint = GetTouchCenterPoint();
        OnPinchStart?.Invoke(centerPoint, distance);
    }

    void UpdatePinchGesture(float distance)
    {
        Vector2 centerPoint = GetTouchCenterPoint();
        float scaleFactor = distance / initialPinchDistance;
        OnPinchUpdate?.Invoke(centerPoint, scaleFactor);
    }

    void StartTwistGesture(float angle)
    {
        currentGestureState = GestureState.Twist;
        isGestureActive = true;
        gestureStartTime = Time.time;
        initialTwistAngle = angle;
        
        Vector2 centerPoint = GetTouchCenterPoint();
        OnTwistStart?.Invoke(centerPoint, angle);
    }

    void UpdateTwistGesture(float angle)
    {
        Vector2 centerPoint = GetTouchCenterPoint();
        float rotationDelta = Mathf.DeltaAngle(initialTwistAngle, angle);
        OnTwistUpdate?.Invoke(centerPoint, rotationDelta);
    }

    void UpdateGestureState()
    {
        // Handle gesture timeouts and transitions
        if (isGestureActive)
        {
            float gestureTime = Time.time - gestureStartTime;
            
            // Handle tap timeout
            if (currentGestureState == GestureState.Tap && gestureTime > tapTimeThreshold)
            {
                EndCurrentGesture();
                lastTapTime = Time.time;
            }
        }
    }

    void EndCurrentGesture()
    {
        Vector2 lastPosition = lastTouchPosition;
        
        switch (currentGestureState)
        {
            case GestureState.Drag:
                OnDragEnd?.Invoke(lastPosition);
                break;
            case GestureState.Pinch:
                OnPinchEnd?.Invoke(GetTouchCenterPoint(), initialPinchDistance);
                break;
            case GestureState.Twist:
                OnTwistEnd?.Invoke(GetTouchCenterPoint(), initialTwistAngle);
                break;
        }

        currentGestureState = GestureState.None;
        isGestureActive = false;
    }

    void TransitionToGesture(GestureState newState)
    {
        EndCurrentGesture();
        currentGestureState = newState;
    }

    TouchData GetFirstTouch()
    {
        foreach (var touch in activeTouches.Values)
        {
            return touch;
        }
        return new TouchData();
    }

    TouchData[] GetTwoTouches()
    {
        var touches = new TouchData[2];
        int index = 0;
        foreach (var touch in activeTouches.Values)
        {
            if (index >= 2) break;
            touches[index] = touch;
            index++;
        }
        return touches;
    }

    Vector2 GetTouchCenterPoint()
    {
        Vector2 center = Vector2.zero;
        int count = 0;
        foreach (var touch in activeTouches.Values)
        {
            center += touch.position;
            count++;
        }
        return count > 0 ? center / count : Vector2.zero;
    }

    #region Public API

    /// <summary>
    /// Check if a specific gesture is currently active
    /// </summary>
    public bool IsGestureOfTypeActive(GestureState gestureType)
    {
        return isGestureActive && currentGestureState == gestureType;
    }

    /// <summary>
    /// Force end the current gesture
    /// </summary>
    public void ForceEndGesture()
    {
        if (isGestureActive)
        {
            EndCurrentGesture();
        }
    }

    /// <summary>
    /// Get the current touch count
    /// </summary>
    public int GetTouchCount()
    {
        return activeTouches.Count;
    }

    /// <summary>
    /// Get the center point of all active touches
    /// </summary>
    public Vector2 GetTouchCenter()
    {
        return GetTouchCenterPoint();
    }

    #endregion
}