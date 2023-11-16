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
            float gizmoAge = 2*(Time.realtimeSinceStartup - _gizmoStartTime);
            Gizmos.color = new Color(0f, 1f, 0f, Mathf.Lerp(0, 0.5f, gizmoAge)); //💬 transparent green
            Gizmos.DrawWireSphere(_gizmoLastTappedTargetPoint, Mathf.Lerp(10f, 0, gizmoAge));

            Gizmos.color = new Color(0f, 1f, 0f, Mathf.Lerp(1f, 0, gizmoAge)); //💬 transparent green
            Gizmos.DrawWireSphere(_gizmoLastTappedTargetPoint, Mathf.Lerp(0f, 1.5f, gizmoAge));

            Gizmos.color = new Color(0f, 1f, 0f, Mathf.Lerp(0, 0.07f, gizmoAge)); //💬 transparent green
            Gizmos.DrawSphere(_gizmoLastTappedTargetPoint, Mathf.Lerp(7f, 0, gizmoAge));

            if (Time.realtimeSinceStartup - _gizmoStartTime > 0.5f) {
                _gizmoFlagTapAnimation = false;
            }
        }
    }
}
