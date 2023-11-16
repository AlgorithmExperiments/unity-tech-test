using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingTree : HandState
{
    bool _isTreeStillRooted = true;
    Transform _targetTree;
    Rigidbody _targetTreeRigidbody;
    Vector3 _newTreePosition;
    Vector3 _previousTreePosition;
    Vector3 _newTreeVelocity;
    Vector3 _previousTreeVelocity;

    

    public override void OnBegin(HandStateContext context)
    {
        //Debug.Log("GRASPING TREE: OnBegin()");
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000f, LayerManager.DefaultObstacleLayerMask)
            && hit.collider.gameObject.tag == TagManager.DefaultTreeTag)
        {
            //Debug.Log("GRASPING TREE: OnBegin() FOUND TREE");
            _isTreeStillRooted = true;
            _targetTree = hit.transform;
            _targetTreeRigidbody = _targetTree.GetComponent<Rigidbody>();
            context.HandAnimator.SetBool("graspingTree", true);
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
                if (Vector3.Magnitude(hit.point - _targetTreeRigidbody.position) > 15)
                {
                    _isTreeStillRooted = false;
                    _targetTreeRigidbody.isKinematic = true;
                    _targetTreeRigidbody.freezeRotation = true;
                    context.AudioSourceGrabTree.PlayOneShot(context.AudioSourceGrabTree.clip);
                }
                else
                {   //💬 Animate hand slowly down to base of tree trunk
                    context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, _targetTreeRigidbody.position, 0.05f);
                }
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultTerrainLayerMask))
            {

                context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, hit.point, 0.05f) + (0.6f * Vector3.up);
                _targetTreeRigidbody.position = context.HandTransform.position;
                _previousTreeVelocity = _newTreeVelocity;
                _previousTreePosition = _newTreePosition;
                _newTreePosition = context.HandTransform.position;
                _newTreeVelocity = (_newTreePosition - _previousTreePosition) / Time.deltaTime;



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
            _targetTreeRigidbody.MovePosition(context.HandTransform.position + _newTreeVelocity);

            Vector3 averageVelocity = (_newTreeVelocity + _previousTreeVelocity) / 2;
            averageVelocity.y = 0;

            //💬 Throw tree =)
            if (averageVelocity.sqrMagnitude > 0.1f){
                
                _targetTreeRigidbody.freezeRotation = false;
                _targetTreeRigidbody.isKinematic = false;
                _targetTreeRigidbody.velocity = averageVelocity * 0.7f;
            }
            //💬 Plant tree:
            else {
                if (Physics.Raycast(_targetTreeRigidbody.position + Vector3.up, Vector3.down, out RaycastHit hit, 20f, LayerManager.DefaultTerrainLayerMask))
                {
                    context.AudioSourceGrabTree.PlayOneShot(context.AudioSourceDropObject.clip);
                    _targetTreeRigidbody.position = hit.point;
                }
            }
        }
        _isTreeStillRooted = false;
        context.HandAnimator.SetBool("graspingTree", false);
        context.SetState(new GraspingNothing());
    }
}