using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    [SerializeField] [Range(1,180)]
    int _frameRate = 60;
    int _previousFrameRate;


    void Awake()
    {
        Application.targetFrameRate = _frameRate;
        _previousFrameRate = _frameRate;
    }

    void OnValidate()
    {
        if (_frameRate != _previousFrameRate) {
            Application.targetFrameRate = _frameRate;
            _previousFrameRate = _frameRate;
        }
            
    }


}
