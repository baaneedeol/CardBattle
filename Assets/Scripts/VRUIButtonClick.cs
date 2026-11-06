using UnityEngine;
using UnityEngine.UI; // For UI Button
using UnityEngine.XR; // For XR input

public class VRUIButtonClick : MonoBehaviour
{
    [Header("Assign your UI Button here")]
    public Button targetButton;

    

    // XR input device reference
    private InputDevice rightController;

    void Start()
    {
        // Get the right-hand controller
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);

        if (devices.Count > 0)
        {
            rightController = devices[0];
        }
        else
        {
            Debug.LogWarning("Right-hand controller not found.");
        }
    }

    void Update()
    {
        if (!rightController.isValid)
        {
            // Try to reacquire controller if lost
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            if (devices.Count > 0) rightController = devices[0];
            return;
        }

        // Check if primary button (A on Quest 3 right controller) is pressed
        bool primaryButtonPressed;
        if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonPressed) && primaryButtonPressed)
        {
            Debug.Log("Button Pressed");
            // Trigger the UI button click
            if (targetButton != null)
            {
                targetButton.onClick.Invoke();
                Debug.Log("VR controller button clicked UI button.");
                
            }
        }
    }
}
