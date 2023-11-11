using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    private PathNode[] _currentPath = Array.Empty<PathNode>();
    private int _currentPathIndex = 0;
    
    [SerializeField]
    private NavGrid _navGrid;

    [SerializeField]
    private float _speed = 10.0f;

    

    void Update()
    {
        // Check Input
        if (Input.GetMouseButtonUp(0))
        {
            LayerMask _terrainLayerMask= LayerMask.GetMask("Default");
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, _terrainLayerMask))
            {
                _currentPath = _navGrid.GetPath(transform.position, hitInfo.point);

                _currentPathIndex = 1;
            }
        }

        // Traverse
        if (_currentPathIndex < _currentPath.Length)
        {
            var currentNode = _currentPath[_currentPathIndex];
            
            var maxDistance = _speed * Time.deltaTime;
            Vector3 vectorToDestination = new(currentNode.Position.x - transform.position.x, (/* y = */ 0), currentNode.Position.z - transform.position.z);
            var moveDistance = Mathf.Min(vectorToDestination.magnitude, maxDistance);

            transform.LookAt(currentNode.Position + new Vector3(0,1,0), Vector3.up);

            var moveVector = vectorToDestination.normalized * moveDistance;
            transform.position += moveVector;

            if (vectorToDestination.sqrMagnitude < 0.1f) 
            {
                _currentPathIndex++;
            }
                
        }
    }
}
