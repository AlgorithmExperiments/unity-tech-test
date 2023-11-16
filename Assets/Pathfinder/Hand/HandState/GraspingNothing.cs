using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingNothing : HandState
{


    float _timeOfStartPress = float.MaxValue;
    bool _isPressingDown = true;


    public override void OnBegin(HandStateContext context)
    {
        Debug.Log("GRASPING NOTHING: OnBegin()");
        //RaycastHit hit;
        //if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultObstacleLayer))
        //{
        //    if (hit.collider.gameObject.tag == TagManager.DefaultTreeTag)
        //    {
                
        //        context.HandAnimator.SetBool("graspingTree", true);
        //    }
        //}
    }

    public override void OnUpdate(HandStateContext context)
    {
        Ray ray;
        RaycastHit hit;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000f, LayerManager.DefaultObstacleLayerMask | LayerManager.DefaultTerrainLayerMask))
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
            if (hit.collider.tag == TagManager.DefaultTreeTag)
            {
                context.SetState(new GraspingTree());
            }
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

            Debug.Log("GRASPING NOTHING: OnRelease() tap target");

            Ray ray = new Ray(context.FingerTipIndexFinger.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 36f, LayerManager.DefaultObstacleLayerMask | LayerManager.DefaultTerrainLayerMask))
            {
                context.Player.SetPathDestination(hit.point, context.NavGrid);
                context.GizmoTapIndicator.PlayTapIndicatorGizmoAnimation(hit.point);
            }
        }
        _isPressingDown = false;
    }
}