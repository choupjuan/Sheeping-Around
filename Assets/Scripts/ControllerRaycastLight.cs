using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class ControllerRaycastLight : MonoBehaviour
{

    public Transform controllerTransform;  // Assign the controller's Transform in the inspector
    public Material highlightMaterial;     // The material for when the object is highlighted
    public XRNode controllerNode = XRNode.RightHand; // Specify which hand's trigger to use
    public float maxDistance = 10f;        // Max distance of the raycast

    private Material originalMaterial;     // Store the original material
    private Material objectMaterial;       // The current material of the object
    private Renderer objectRenderer;       // Reference to the object's Renderer
    private Transform lastHitObject;       // Store the last object hit
    private float triggerValue = 0f;       // Value of the trigger (0 to 1)
    private Color baseColor = Color.white; // The starting color of the object
    private Color targetColor = Color.red; // The color to blend towards when trigger is fully pressed


    void Update()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, maxDistance))
        {
            Transform hitObject = hit.transform;
            objectRenderer = hitObject.GetComponent<Renderer>();
            if (objectRenderer!= null && hit.transform.tag =="Leg")
            {
                
                if(lastHitObject != hitObject)
                {
                    ResetMaterial();
                    originalMaterial = objectRenderer.material;
                    objectMaterial = originalMaterial;
                    objectRenderer.material = highlightMaterial;
                    lastHitObject = hitObject;
                }
                
                triggerValue = InputValueFromController();
                if(highlightMaterial != null)
                {
                    
                    highlightMaterial.color = Color.Lerp(baseColor, targetColor, triggerValue);
                    
                }
                // ApplyHapticFeedback(triggerValue);
            }
        }
        else
        {
            
        ResetMaterial();
           
        }
    }

    void ResetMaterial()
    {
        
        if (lastHitObject != null && originalMaterial != null)
        {
            Renderer tempRenderer = lastHitObject.GetComponent<Renderer>();
            if (tempRenderer != null) { 
                tempRenderer.material = originalMaterial;
                highlightMaterial.color = baseColor;
            }
            
            lastHitObject = null;
        }
    }

    private float InputValueFromController()
    {
        if (controllerNode == XRNode.RightHand || controllerNode == XRNode.LeftHand)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (device != null)
            {
                float value;
                if (device.TryGetFeatureValue(CommonUsages.trigger, out value))
                {
                    return value;
                }
            }
        }
        return 0.0f;
    }

    private void ApplyHapticFeedback(float value)
    {
        if (controllerNode == XRNode.RightHand || controllerNode == XRNode.LeftHand)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (device != null)
            {
                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        uint channel = 0;
                        float amplitude = value;
                        float duration = 0.05f;
                        device.SendHapticImpulse(channel, amplitude, duration);
                    }
                }
            }
        }
    }
}
