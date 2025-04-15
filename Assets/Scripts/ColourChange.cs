using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ColourChange : MonoBehaviour
{
    public Material material;
    public XRNode controllerNode = XRNode.RightHand;
    public string triggerAxisName = "Trigger";

    private Color baseColour = Color.white;
    private Color targetColour = Color.red;
    private float triggerValue = 0.0f;

    void Start()
    {
        if (material == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                material = renderer.material;
            }
        }
        if (material != null)
        {
            baseColour = material.color;
        }

    }

    void Update()
    {
        triggerValue = InputValueFromController();

        if(material != null)
        {
            material.color = Color.Lerp(baseColour, targetColour, triggerValue);
        }
    }

    private float InputValueFromController()
    {
        if(controllerNode == XRNode.RightHand || controllerNode == XRNode.LeftHand)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if(device != null)
            {
                float value;
                if(device.TryGetFeatureValue(CommonUsages.trigger, out value))
                {
                    return value;
                }
            }
        }
        return 0.0f;
    }

}
