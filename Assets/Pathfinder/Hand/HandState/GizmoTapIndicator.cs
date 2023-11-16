using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoTapIndicator : MonoBehaviour
{

    public bool _gizmoFlagTapAnimation;
    float _gizmoStartTime;
    Vector3 _gizmoLastTappedTargetPoint;

    public void PlayTapIndicatorGizmoAnimation(Vector3 targetPoint)
    {
        _gizmoFlagTapAnimation = true;
        _gizmoStartTime = Time.realtimeSinceStartup;
        _gizmoLastTappedTargetPoint = targetPoint;
    }

    void OnDrawGizmos()
    {
        if (_gizmoFlagTapAnimation)
        {
            //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws green shrinking sphere at taap location:
            float gizmoAge = Time.realtimeSinceStartup - _gizmoStartTime;
            Gizmos.color = new Color(0f, 1f, 0f, Mathf.Lerp(0, 0.2f, gizmoAge)); //💬 solid green

            Gizmos.DrawWireSphere(_gizmoLastTappedTargetPoint, Mathf.Lerp(10f, 0, gizmoAge));
            if (Time.realtimeSinceStartup - _gizmoStartTime > 1) {
                _gizmoFlagTapAnimation = false;
            }
        }
    }
}
