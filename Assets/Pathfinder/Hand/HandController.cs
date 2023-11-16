using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandController : MonoBehaviour
{
    

    [SerializeField]
    private NavGrid _navGrid;

    [SerializeField] 
    Player _player;

    [SerializeField] 
    Transform _fingerTipIndexFinger;

    [SerializeField]
    Animator _handAnimator;

    [SerializeField]
    float _pressDurationThreshold = 0.5f;

    [SerializeField]
    AudioSource _grabTreeAudioSource;

    [SerializeField]
    AudioSource _grabBlockAudioSource;

    [SerializeField]
    AudioSource _grabTerrainAudioSource;

    [SerializeField]
    GizmoTapIndicator _gizmoTapIndicator;

    [SerializeField]
    AudioSource _dropObjectAudioSource;


    bool _wasMouseOutsideGameViewLastFrame = true;
    HandState _currentState;
    HandStateContext _context;


    private void Start()
    {
        
        _context = new HandStateContext
        {
            HandTransform = transform,
            FingerTipIndexFinger = _fingerTipIndexFinger,
            HandAnimator = _handAnimator,
            PressDurationThreshold = _pressDurationThreshold,
            AudioSourceGrabTree = _grabTreeAudioSource,
            AudioSourceGrabBlock = _grabBlockAudioSource,
            AudioSourceGrabTerrain = _grabTerrainAudioSource,
            AudioSourceDropObject = _dropObjectAudioSource,
            GizmoTapIndicator = _gizmoTapIndicator,
            RotationAtGameStart = transform.rotation,
            NavGrid = _navGrid,
            Player = _player,
            SetState = SetState
        };

        SetState(new GraspingNothing());
    }

  

    bool IsMouseOutsideGameView()
    {
        Vector3 mouseViewportPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        return mouseViewportPos.x < 0 || mouseViewportPos.x > 1 ||
               mouseViewportPos.y < 0 || mouseViewportPos.y > 1;
    }

    void Update()
    {

        if (IsMouseOutsideGameView() != _wasMouseOutsideGameViewLastFrame) {
            Cursor.visible = !_wasMouseOutsideGameViewLastFrame;
            _wasMouseOutsideGameViewLastFrame = !_wasMouseOutsideGameViewLastFrame;
        }

        if (Input.GetMouseButtonDown(0)) //💬 Mouse button pressed
        {
            _currentState.OnPress(_context);
        }

        if (Input.GetMouseButtonUp(0)) //💬 Mouse button released
        {
            _currentState.OnRelease(_context);
        }

        _currentState.OnUpdate(_context);        
    }



    public void SetState(HandState newState)
    {
        _currentState = newState;
        newState.OnBegin(_context);
    }

    


    void OnValidate()
    {
        if (!_navGrid)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a NavGrid");
        
        if (!_player)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a Player");
        
        if (!_fingerTipIndexFinger)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a _fingertipIndexFinger");
        
        if (!_handAnimator)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to an Animator");
        
        if (!_grabTreeAudioSource)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to an audio source");
    
    
    }
}

