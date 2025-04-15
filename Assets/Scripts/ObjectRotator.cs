using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;

public class ObjectRotator : MonoBehaviour
{
    private GrabbableObject grabbable;
    private Quaternion initialObjectRotation;
    private Quaternion initialControllerRotation;
    private Transform controllerTransform;

    private void Start()
    {
        grabbable = GetComponent<GrabbableObject>();

        if (grabbable == null)
        {
            Debug.LogError("GrabbableObject component not found on " + gameObject.name);
            return;
        }

        controllerTransform = grabbable.controllerTransform;
        if(controllerTransform == null)
        {
            Debug.LogError("Controller Transform not assigned in GrabbableObject.");
            return;
        }

    }

    void Update()
    {
        if(grabbable != null && grabbable.isGrabbed && grabbable.isPointedAt)
        {
            if(controllerTransform!= null)
            {
                if (initialObjectRotation == Quaternion.identity)
                {
                    initialObjectRotation = transform.rotation;
                    initialControllerRotation = controllerTransform.rotation;
                }

                // Calculate the relative rotation from the controller's initial to current rotation
                Quaternion rotationDifference = controllerTransform.rotation * Quaternion.Inverse(initialControllerRotation);

                // Extract the Z-axis rotation from the rotation difference
                float zRotationDifference = rotationDifference.eulerAngles.z;

                // Apply only the Z-axis rotation to the object
                transform.rotation = Quaternion.Euler(initialObjectRotation.eulerAngles.x, initialObjectRotation.eulerAngles.y, initialObjectRotation.eulerAngles.z + zRotationDifference);
            }
        }
        else
        {
            initialObjectRotation = Quaternion.identity;
            initialControllerRotation = Quaternion.identity;
        }
    }

}
