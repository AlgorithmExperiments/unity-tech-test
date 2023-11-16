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

    int _obstacleLayer;
    LayerMask _terrainLayerMask;
    LayerMask _obstacleLayerMask;
    LayerMask _combinedLayerMask;
    Quaternion _rotationAtGameStart;
    bool _wasMouseOutsideGameViewLastFrame = true;



    private void Start()
    {
        _obstacleLayer = LayerManager.DefaultObstacleLayer;

        _terrainLayerMask = LayerManager.DefaultTerrainLayerMask;
        _obstacleLayerMask = LayerManager.DefaultObstacleLayerMask;
        _combinedLayerMask = _obstacleLayerMask | _terrainLayerMask;

        _rotationAtGameStart = transform.rotation;
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

        Ray ray;
        RaycastHit hit;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000f, _combinedLayerMask))
        {
            transform.position += (hit.point + (0.4f*hit.normal) - transform.position)/6;

            if (hit.collider.gameObject.layer == _obstacleLayer) {
                Vector3 handToTopOfObstacleDirectionVector = Vector3.Normalize((hit.collider.bounds.center + hit.collider.bounds.extents.y*Vector3.up) - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(handToTopOfObstacleDirectionVector), 0.1f);
            }
            else {
                transform.rotation = Quaternion.Slerp(transform.rotation, _rotationAtGameStart, 0.1f);
            }
        }
        //if (Input.GetMouseButtonDown(0))
        //{
        //    ray = new Ray(_fingerTipIndexFinger.position, Vector3.down);
        //    if (Physics.Raycast(ray, out hit, 36f, _combinedLayerMask))
        //    {
        //        _player.SetPathDestination(hit.point, _navGrid);
        //    }
        //}
        if (Input.GetMouseButtonUp(0))
        {
            ray = new Ray(_fingerTipIndexFinger.position, Vector3.down);
            if (Physics.Raycast(ray, out hit, 36f, _combinedLayerMask))
            {
                _player.SetPathDestination(hit.point, _navGrid);
            }
        }
        
    }

    


    void OnValidate()
    {
        if (!_navGrid)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a NavGrid");
        
        if (!_player)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a Player");
        
        if (!_fingerTipIndexFinger)
            Debug.LogError("HAND CONTROLLER: HandController gameobject requires an inspector reference to a _fingertipIndexFinger");
    }
}
