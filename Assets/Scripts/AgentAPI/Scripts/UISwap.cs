using UnityEngine;
using UnityEngine.UI;

public class UISwap : MonoBehaviour
{
    public Button robotButton; // Reference to the button
    public GameObject robot; // Reference to the GameObject you want to move
    public GameObject hannah;
    public static bool hannahActive = true;

    void Start()
    {
        // Ensure the button and targetObject are assigned
        if (robotButton != null && robot != null)
        {
            // Add a listener to the button to call the MoveObject method when clicked
            robotButton.onClick.AddListener(SwapAgents);
        }
    }

    void SwapAgents()
    {
        if (hannahActive)
        { 
            robot.transform.position = Vector3.zero;
            hannah.transform.position = new Vector3(-10, 0, 0);
        }
        else
        {
            hannah.transform.position = Vector3.zero;
            robot.transform.position = new Vector3(-10, 0, 0);
        }
        hannahActive = !hannahActive;
    }
}