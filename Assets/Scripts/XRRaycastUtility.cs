using UnityEngine;
using UnityEngine.XR;

public static class XRRaycastUtility
{
    public static bool TryGetWorldRay(Transform xrOrigin, InputDevice device, out Ray worldRay)
    {
        worldRay = new Ray();

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition) ||
            !device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation))
        {
            return false;
        }

        // Convert local controller position/rotation to world space
        Vector3 worldPosition = xrOrigin.TransformPoint(localPosition);
        Quaternion worldRotation = xrOrigin.rotation * localRotation;

        worldRay = new Ray(worldPosition, worldRotation * Vector3.forward);
        return true;
    }

    public static bool TryGetControllerRay(Transform xrOrigin, XRNode node, out Ray worldRay)
    {
        worldRay = new Ray();
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        
        if (!device.isValid)
            return false;

        return TryGetWorldRay(xrOrigin, device, out worldRay);
    }
}