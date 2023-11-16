using UnityEngine;

public delegate void SetStateDelegate(HandState newState);

public class HandStateContext
{
    public Transform HandTransform;
    public Transform FingerTipIndexFinger;
    public Animator HandAnimator;
    public float PressDurationThreshold;
    public AudioSource AudioSourceGrabTree;
    public AudioSource AudioSourceGrabObject;
    public AudioSource AudioSourceGrabTerrain;
    public AudioSource AudioSourceDropObject;
    public GizmoTapIndicator GizmoTapIndicator;
    public Quaternion RotationAtGameStart;
    public NavGrid NavGrid;
    public Player Player;
    public SetStateDelegate SetState { get; set; }
    //💬 Raycast Hit?
}
