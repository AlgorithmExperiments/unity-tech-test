using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle : MonoBehaviour
{
    [SerializeField]
    NavGrid _navGrid;

    Collider _collider;

    Rigidbody _rigidbody;

    int _sleepCounter = 0;

    int _sleepThreshold = 5;

    
    void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Water");
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }


    void Update()
    {
        SleepIfCompletelyStatic();
    }

    

    void OnCollisionEnter(Collision collision)
    {
        //💬
        //Debug.Log("OBSTACLE: OnCollisionEnter() was triggered between " + gameObject.name + " and " + collision.gameObject.name);

        this.enabled = true;
        _navGrid.RegisterObstacle(_collider);
    }


    void SleepIfCompletelyStatic()
    {
        if (Mathf.Approximately(_rigidbody.velocity.sqrMagnitude, 0f) && Mathf.Approximately(_rigidbody.angularVelocity.sqrMagnitude, 0f))
            _sleepCounter++;
        else
            _sleepCounter = 0;


        if (_sleepCounter == _sleepThreshold)
        {
            _sleepCounter = 0;
            _navGrid.UnregisterObstacle(_collider);
            enabled = false;
        }
    }
}
