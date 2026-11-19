
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

public class VRUIButtonClick : MonoBehaviour
{
    [Header("Right Controller - Face Buttons")]
    public Button startButton;             // A
    public Button simulationButton;        // B

    [Header("Left Controller - Face Buttons")]
    public Button cardAbilityLegendButton; // X
    public Button buttonMappingLegendButton; // Y

    [Header("Additional Right Controller Buttons")]
    public Button restartButton;           // Right stick click
    public Button exitButton;              // Right trigger

    [Header("Additional Left Controller Buttons")]
    public Button leftAdditionalButton;    // Left stick click
    public Button leftTriggerButton;       // Left trigger

    private InputDevice rightController;
    private InputDevice leftController;

    private readonly Dictionary<string, bool> previousStates =
        new Dictionary<string, bool>();

    private void Update()
    {
        FindControllers();

        if (rightController.isValid)
        {
            CheckButton(rightController, CommonUsages.primaryButton,
                "Right A", startButton);

            CheckButton(rightController, CommonUsages.secondaryButton,
                "Right B", simulationButton);

            CheckButton(rightController, CommonUsages.primary2DAxisClick,
                "Right Stick", restartButton);

            CheckButton(rightController, CommonUsages.triggerButton,
                "Right Trigger", exitButton);
        }

        if (leftController.isValid)
        {
            CheckButton(leftController, CommonUsages.primaryButton,
                "Left X", cardAbilityLegendButton);

            CheckButton(leftController, CommonUsages.secondaryButton,
                "Left Y", buttonMappingLegendButton);

            CheckButton(leftController, CommonUsages.primary2DAxisClick,
                "Left Stick", leftAdditionalButton);

            CheckButton(leftController, CommonUsages.triggerButton,
                "Left Trigger", leftTriggerButton);
        }
    }

    private void FindControllers()
    {
        if (!rightController.isValid)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);

            if (devices.Count > 0)
                rightController = devices[0];
        }

        if (!leftController.isValid)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

            if (devices.Count > 0)
                leftController = devices[0];
        }
    }

    private void CheckButton(
        InputDevice controller,
        InputFeatureUsage<bool> input,
        string inputName,
        Button targetButton)
    {
        bool pressed;

        if (!controller.TryGetFeatureValue(input, out pressed))
            return;

        bool wasPressed = previousStates.ContainsKey(inputName)
            && previousStates[inputName];

        // Only trigger once when the button is initially pressed.
        if (pressed && !wasPressed && targetButton != null)
        {
            Debug.Log(inputName + " pressed - clicking " +
                      targetButton.name);

            targetButton.onClick.Invoke();
        }

        previousStates[inputName] = pressed;
    }
}