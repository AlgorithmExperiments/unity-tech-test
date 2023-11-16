using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingNothing : HandState
{


    float _timeOfStartPress = float.MaxValue;
    bool _isPressingDown = true;
    bool _isPositionLockedForTapAnimation = false;
    float _tapAnimationDuration = 0.7f;
    bool _hasMidpointOfTapAnimationBeenReached = false;
    float _zoomRate = 20f;
    float _minZoomDistance = 10f;
    float _maxZoomDistance = 50f;
    float _targetZoomDistance;


    public override void OnBegin(HandStateContext context)
    {
    }

    public override void OnUpdate(HandStateContext context)
    {
        Ray ray;
        RaycastHit hit;
        if (_isPositionLockedForTapAnimation)
        {
            if (Time.realtimeSinceStartup - _timeOfStartPress < _tapAnimationDuration * 0.6f)
            {
                _hasMidpointOfTapAnimationBeenReached = false;
                return;
            }
                
            else if (!_hasMidpointOfTapAnimationBeenReached)
            {
                _hasMidpointOfTapAnimationBeenReached = true;
                ray = new Ray(context.FingerTipIndexFinger.position + (3*Vector3.up), Vector3.down);
                if (Physics.Raycast(ray, out hit, 36f, LayerManager.DefaultObstacleLayerMask | LayerManager.DefaultTerrainLayerMask))
                {
                    context.Player.SetPathDestination(hit.point, context.NavGrid);
                    context.GizmoTapIndicator.PlayTapIndicatorGizmoAnimation(hit.point);
                }
            }
            else {
                _isPositionLockedForTapAnimation = false;
            }
                
        }
            
        
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool success = Physics.Raycast(ray, out hit, 1000f, LayerManager.DefaultObstacleLayerMask | LayerManager.DefaultTerrainLayerMask);
        if (success)
        {
            context.HandTransform.position += (hit.point + (0.4f * hit.normal) - context.HandTransform.position) / 6;

            if (hit.collider.gameObject.layer == LayerManager.DefaultObstacleLayer)
            {
                Vector3 handToTopOfObstacleDirectionVector = Vector3.Normalize((hit.collider.bounds.center + hit.collider.bounds.extents.y * Vector3.up) - context.HandTransform.position);
                context.HandTransform.rotation = Quaternion.Slerp(context.HandTransform.rotation, Quaternion.LookRotation(handToTopOfObstacleDirectionVector), 0.1f);
            }
            else {
                context.HandTransform.rotation = Quaternion.Slerp(context.HandTransform.rotation, context.RotationAtGameStart, 0.1f);
            }
        }

        if (_isPressingDown && (Time.realtimeSinceStartup - _timeOfStartPress > context.PressDurationThreshold))
        {
            if (hit.collider.tag == TagManager.DefaultTreeTag) {
                context.SetState(new GraspingTree());
            }
            else if (hit.transform.gameObject.layer == LayerManager.DefaultObstacleLayer) {
                context.SetState(new GraspingObject());
            }
            else if (hit.transform.gameObject.layer == LayerManager.DefaultTerrainLayer) {
                context.SetState(new GraspingTerrain());
            }
        }

        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            UpdateTargetZoomDistance(context, scroll);
            SmoothCameraZoom(context);
        }
        
    }

    public override void OnPress(HandStateContext context)
    {
        _timeOfStartPress = Time.realtimeSinceStartup;
        _isPressingDown = true;
    }

    public override void OnRelease(HandStateContext context)
    {
        if (_isPressingDown && Time.realtimeSinceStartup - _timeOfStartPress < context.PressDurationThreshold)
        {
            //💬 Tap the ground to direct the player:
            context.HandAnimator.SetTrigger("tapTarget");
            _isPositionLockedForTapAnimation = true;

            Debug.Log("GRASPING NOTHING: OnRelease() tap target");
        }
        _isPressingDown = false;
    }


    private void UpdateTargetZoomDistance(HandStateContext context, float scrollAmount)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (_targetZoomDistance == 0)
            {
                _targetZoomDistance = Vector3.Distance(mainCamera.transform.position, context.HandTransform.position);
            }

            _targetZoomDistance += scrollAmount * _zoomRate; // Adjust zoom direction based on scroll amount
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance, _minZoomDistance, _maxZoomDistance);
        }
    }

    private void SmoothCameraZoom(HandStateContext context)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 cameraDirection = (mainCamera.transform.position - context.HandTransform.position).normalized;
            Vector3 targetPosition = context.HandTransform.position + cameraDirection * _targetZoomDistance;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * _zoomRate);
        }
    }
}