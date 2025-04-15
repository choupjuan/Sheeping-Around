using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour
{
    public Transform rightHandAnchor;
    public float maxDistance = 10f;
    public LineRenderer lineRenderer;

    void Update()
    {
        Vector3 startPoint = rightHandAnchor.position;
        Vector3 endPoint = rightHandAnchor.position + rightHandAnchor.forward * maxDistance;

        RaycastHit hit;
        if (Physics.Raycast(startPoint, rightHandAnchor.forward, out hit, maxDistance))
        {
            endPoint = hit.point;
        }

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}
