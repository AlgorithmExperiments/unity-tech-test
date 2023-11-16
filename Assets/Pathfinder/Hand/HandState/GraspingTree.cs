using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingTree : HandState
{
    bool _isTreeStillRooted = true;
    Transform _lastTargetedTree;
    Rigidbody _lastTargetedTreeRigidbody;
    Vector3 _newVelocity;
    Vector3 _previousVelocity;
    float _releaseCountdown;

    Vector3 _newPosition;
    Vector3 _previousPosition;

    public override void OnBegin(HandStateContext context)
    {
        Debug.Log("GRASPING TREE: OnBegin()");
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000f, LayerManager.DefaultObstacleLayerMask)
            && hit.collider.gameObject.tag == TagManager.DefaultTreeTag)
        {
            Debug.Log("GRASPING TREE: OnBegin() FOUND TREE");
            _isTreeStillRooted = true;
            _lastTargetedTree = hit.transform;
            _lastTargetedTreeRigidbody = _lastTargetedTree.GetComponent<Rigidbody>();
            context.HandAnimator.SetBool("graspingTree", true);
            context.HandTransform.position = _lastTargetedTreeRigidbody.position;
        }
        else {
            context.SetState(new GraspingNothing());
        }
    }

    public override void OnUpdate(HandStateContext context)
    {
        if (_isTreeStillRooted)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultTerrainLayerMask))
            {
                if (Vector3.Magnitude(hit.point - _lastTargetedTreeRigidbody.position) > 15)
                {
                    _isTreeStillRooted = false;
                    _lastTargetedTreeRigidbody.isKinematic = true;
                    _lastTargetedTreeRigidbody.freezeRotation = true;
                    context.AudioSourceGrabTree.PlayOneShot(context.AudioSourceGrabTree.clip);
                }
                else
                {   //💬 Animate hand slowly down to tree
                    context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, _lastTargetedTreeRigidbody.position, 0.05f);
                }
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultTerrainLayerMask))
            {

                context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, hit.point, 0.05f) + (0.6f * Vector3.up);
                _lastTargetedTreeRigidbody.position = context.HandTransform.position;
                _previousVelocity = _newVelocity;
                _previousPosition = _newPosition;
                _newPosition = context.HandTransform.position;
                _newVelocity = (_newPosition - _previousPosition) / Time.deltaTime;



            }
        }
    }

    public override void OnPress(HandStateContext context)
    {
        // Do nothing
    }

    public override void OnRelease(HandStateContext context)
    {
        if (!_isTreeStillRooted)
        {
            _lastTargetedTreeRigidbody.MovePosition(context.HandTransform.position + _newVelocity);

            Vector3 averageVelocity = (_newVelocity + _previousVelocity) / 2;
            averageVelocity.y = 0;

            //💬 Throw tree =)
            if (averageVelocity.sqrMagnitude > 0.1f){
                
                _lastTargetedTreeRigidbody.freezeRotation = false;
                _lastTargetedTreeRigidbody.isKinematic = false;
                _lastTargetedTreeRigidbody.velocity = averageVelocity/2;
            }
            //💬 Plant tree:
            else {
                if (Physics.Raycast(_lastTargetedTreeRigidbody.position + Vector3.up, Vector3.down, out RaycastHit hit, 20f, LayerManager.DefaultTerrainLayerMask))
                {
                    context.AudioSourceGrabTree.PlayOneShot(context.AudioSourceDropObject.clip);
                    _lastTargetedTreeRigidbody.position = hit.point;
                }
            }
        }
        _isTreeStillRooted = false;
        context.HandAnimator.SetBool("graspingTree", false);
        context.SetState(new GraspingNothing());
    }
}