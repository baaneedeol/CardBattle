// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class PanelToggle : MonoBehaviour
// {
//     [Header("Panel to Hide or Show")]
//     public GameObject panelToToggle;

//     [Header("Button Text")]
//     public TMP_Text buttonText;

//     private bool isPanelVisible = true;

//     public void TogglePanel()
//     {
//         isPanelVisible = !isPanelVisible;

//         panelToToggle.SetActive(isPanelVisible);

//         if (buttonText != null)
//         {
//             buttonText.text = isPanelVisible ? "x" : "⛶";
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelToggle : MonoBehaviour
{
    [Header("Panel to Hide or Show")]
    public GameObject panelToToggle;

    [Header("Button Text")]
    public TMP_Text buttonText;

    // Change this from true to false so it starts closed
    private bool isPanelVisible = false;

    private void Start()
    {
        // Ensure the panel and button text match the starting state
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(isPanelVisible);
        }

        if (buttonText != null)
        {
            buttonText.text = isPanelVisible ? "x" : "ⓘ";
        }
    }

    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        panelToToggle.SetActive(isPanelVisible);

        if (buttonText != null)
        {
            buttonText.text = isPanelVisible ? "x" : "ⓘ";
        }
    }
}
