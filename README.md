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
    lastCollision - The last collider any touch had hit

* Only 1 AInput InputComponent is expected in the scene

=== Events ===

* AInput events use the function signature: 

    void Function(AInput.Touch)

    for single input and:

    void Function(AInput.Touch, AInput.Touch)

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
    multitapEvent - 3+ taps. You can get the tap count from Input.tapCount
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
