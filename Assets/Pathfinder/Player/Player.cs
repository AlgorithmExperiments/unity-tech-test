using System;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Player : MonoBehaviour
{
    private PathNode[] _currentPath = Array.Empty<PathNode>();
    private int _nextNodeIndex = 0;
    
    [SerializeField]
    private float _speed = 80f;
    
    Rigidbody _playerRigidbody;
    Collider _playerCollider;

    Vector3 _currentBaseOfPlayer;
    bool _hasKinematicRigidbody;
    float _playerHalfHeightOffset;


    private void Awake()
    {
        _playerRigidbody = gameObject.GetComponent<Rigidbody>();
        _playerCollider = gameObject.GetComponent<Collider>();
        _hasKinematicRigidbody = _playerRigidbody != null;
        _playerHalfHeightOffset = _playerCollider.bounds.extents.y;
    }


    public void SetPathDestination(Vector3 targetPoint, NavGrid navGrid)
    {
        _currentPath = navGrid.GetPath(transform.position, targetPoint);
        _nextNodeIndex = 1;
    }


    void Update()
    {
        float remainingDistanceThisTurn = _speed * Time.deltaTime;

        // Traverse
        while (remainingDistanceThisTurn > 0.01f && _nextNodeIndex < _currentPath.Length)
        {
            
            _currentBaseOfPlayer = transform.position - (_playerHalfHeightOffset * Vector3.up);
            PathNode nextNode = _currentPath[_nextNodeIndex];
            Vector3 vectorToDestination = nextNode.Position - _currentBaseOfPlayer;
            Vector3 normalizedVectorToDestination = vectorToDestination.normalized;
            var distanceToNextNode = vectorToDestination.magnitude;
            

            if (distanceToNextNode < remainingDistanceThisTurn)
            {
                MovePlayerTargetPosition(nextNode.Position, normalizedVectorToDestination);
                remainingDistanceThisTurn -= distanceToNextNode;
                _nextNodeIndex++;
            }
            else
            {
                MovePlayerTargetPosition(_currentBaseOfPlayer + (remainingDistanceThisTurn * normalizedVectorToDestination), normalizedVectorToDestination);
                remainingDistanceThisTurn = 0;
            }

        }
        
    }



    void MovePlayerTargetPosition(Vector3 newPosition, Vector3 normalizedDirectionVector)
    {
        newPosition.y = _playerHalfHeightOffset;

        if (_hasKinematicRigidbody) {
            _playerRigidbody.Move(newPosition, Quaternion.LookRotation(normalizedDirectionVector, Vector3.up));
        }
        else {
            transform.position = newPosition;
            //transform.LookAt(newPosition + normalizedDirectionVector + (Vector3.up * _playerCollider.bounds.extents.y));
        }

    }
}
