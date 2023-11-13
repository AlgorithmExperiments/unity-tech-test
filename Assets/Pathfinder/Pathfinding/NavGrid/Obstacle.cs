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

    public static LayerMask UniversalObstacleLayer => 1 << 4; //💬 "Water" Layer (Layer 4)


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
        Vector3 velocity = 1000 * _rigidbody.velocity;
        Vector3 spinVelocity = 1000 * _rigidbody.angularVelocity;
        if (Mathf.Approximately(velocity.sqrMagnitude + spinVelocity.sqrMagnitude, 0)) 
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
