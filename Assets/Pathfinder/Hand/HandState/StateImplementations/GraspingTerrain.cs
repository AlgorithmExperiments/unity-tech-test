using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraspingTerrain : HandState
{
    float _xScalar = 0.01f;
    float _zScalar = 0.01f;
    Vector2 _startingMousePosition;
    Vector3 _startingCameraPosition;

    public override void OnBegin(HandStateContext context)
    {
        context.HandAnimator.SetBool("graspingTerrain", true);
        _startingMousePosition = Input.mousePosition; 
        _startingCameraPosition = Camera.main.transform.position;
    }

    public override void OnUpdate(HandStateContext context)
    {
        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 mouseDelta = currentMousePosition - _startingMousePosition;

        Vector3 newCameraPosition = Camera.main.transform.position;
        float distanceFromCameraToHand = Vector3.Magnitude(context.HandTransform.position - Camera.main.transform.position);
        newCameraPosition.x = _startingCameraPosition.x + (mouseDelta.x * _xScalar * distanceFromCameraToHand/30);
        newCameraPosition.z = _startingCameraPosition.z + (mouseDelta.y * _zScalar * distanceFromCameraToHand/30); // Assuming Y mouse movement affects Z world position

        Camera.main.transform.position = newCameraPosition;
    }

    public override void OnPress(HandStateContext context)
    {

    }

    public override void OnRelease(HandStateContext context)
    {
        context.HandAnimator.SetBool("graspingTerrain", false);
        context.SetState(new GraspingNothing());
    }
    
}
