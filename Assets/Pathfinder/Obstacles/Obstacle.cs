using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle : MonoBehaviour
{
    [SerializeField]
    NavGrid _navGrid;

    [SerializeField]
    bool _isTree;

    Collider _collider;

    Rigidbody _rigidbody;

    int _sleepCounter = 0;

    int _sleepThreshold = 5;


    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        gameObject.layer = LayerManager.DefaultObstacleLayer;
        if(_isTree) {
            gameObject.tag = TagManager.DefaultTreeTag;
        }
        
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
