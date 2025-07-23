using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3F;
    private Vector3 velocity = Vector3.zero;
    public float cameraToPlayerOffset = 3;    
    int currentFrame = -1;
    float lateFixedTime = 0;

    private void Start()
    {   lateFixedTime = Time.fixedTime;
        target = FindObjectOfType<PlayerMove>().transform;
    }
    void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.TransformPoint(new Vector3(0, cameraToPlayerOffset, -10));

            // Smoothly move the camera towards that target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}