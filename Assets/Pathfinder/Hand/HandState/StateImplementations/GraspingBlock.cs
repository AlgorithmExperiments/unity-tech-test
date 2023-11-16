using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingObject : HandState
{
    bool _isObjectStillGrounded = true;
    Transform _targetObject;
    Rigidbody _targetObjectRigidbody;
    Vector3 _gripPointOffsetFromObjectCenter;
    Vector3 _newObjectPosition;
    Vector3 _previousObjectPosition;
    Vector3 _newObjectVelocity;
    Vector3 _previousObjectVelocity;

    

    public override void OnBegin(HandStateContext context)
    {
        //Debug.Log("GRASPING TREE: OnBegin()");
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000f, LayerManager.DefaultObstacleLayerMask))
        {
            //Debug.Log("GRASPING Object: OnBegin() FOUND OBSTACLE");
            _isObjectStillGrounded = true;
            _targetObject = hit.transform;
            _targetObjectRigidbody = _targetObject.GetComponent<Rigidbody>();
            _gripPointOffsetFromObjectCenter = context.HandTransform.position - _targetObject.position;
            context.HandAnimator.SetBool("graspingObject", true);
        }
        else {
            context.SetState(new GraspingNothing());
        }
    }

    public override void OnUpdate(HandStateContext context)
    {
        if (_isObjectStillGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultTerrainLayerMask))
            {
                if (Vector3.Magnitude(hit.point - _targetObjectRigidbody.position) > 15)
                {
                    _isObjectStillGrounded = false;
                    _targetObjectRigidbody.isKinematic = true;
                    _targetObjectRigidbody.freezeRotation = true;
                    context.AudioSourceGrabObject.PlayOneShot(context.AudioSourceGrabObject.clip);
                }
                else
                {   //💬 Smoothly settle hand onto surface of the Object
                    //context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, _targetObjectRigidbody.position, 0.05f);
                }
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerManager.DefaultTerrainLayerMask))
            {

                context.HandTransform.position = Vector3.Lerp(context.HandTransform.position, hit.point, 0.05f) + (0.6f * Vector3.up);
                _targetObjectRigidbody.position = context.HandTransform.position - _gripPointOffsetFromObjectCenter;
                _previousObjectVelocity = _newObjectVelocity;
                _previousObjectPosition = _newObjectPosition;
                _newObjectPosition = context.HandTransform.position;
                _newObjectVelocity = (_newObjectPosition - _previousObjectPosition) / Time.deltaTime;



            }
        }
    }

    public override void OnPress(HandStateContext context)
    {
        // Do nothing
    }

    public override void OnRelease(HandStateContext context)
    {
        if (!_isObjectStillGrounded)
        {
            _targetObjectRigidbody.MovePosition(context.HandTransform.position + _newObjectVelocity);

            Vector3 averageVelocity = (_newObjectVelocity + _previousObjectVelocity) / 2;
            averageVelocity.y = 0;

            _targetObjectRigidbody.freezeRotation = false;
            _targetObjectRigidbody.isKinematic = false;

            //💬 Throw Object =)
            if (averageVelocity.sqrMagnitude > 0.1f){
                _targetObjectRigidbody.velocity = averageVelocity/2;
            }
            //💬 Place Object:
            else {
                if (Physics.Raycast(_targetObjectRigidbody.position + Vector3.up, Vector3.down, out RaycastHit hit, 20f, LayerManager.DefaultTerrainLayerMask))
                {
                    context.AudioSourceGrabTree.PlayOneShot(context.AudioSourceDropObject.clip);
                    //💬 Offset vertically so not trapped in the ground plane:
                    _targetObjectRigidbody.position = hit.point + (Vector3.up * _targetObject.gameObject.GetComponent<Collider>().bounds.extents.y);
                }
            }
            
        }
        _isObjectStillGrounded = false;
        context.HandAnimator.SetBool("graspingObject", false);
        context.SetState(new GraspingNothing());
    }
}