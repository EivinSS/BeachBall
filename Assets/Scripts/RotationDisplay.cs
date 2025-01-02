using UnityEngine;
using TMPro;

public class RotationDisplay : MonoBehaviour
{
    private PogoController _pogoController;

    public TMP_Text northNumber;
    public TMP_Text southNumber;
    public TMP_Text eastNumber;
    public TMP_Text westNumber;

    void Start() 
    {
        GameObject pogoStickObject = GameObject.Find("PogoStick");
        
        if (pogoStickObject != null)
        {
            _pogoController = pogoStickObject.GetComponent<PogoController>();

            if (_pogoController == null)
            {
                Debug.LogWarning("PogoController script not found on PogoStick GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("PogoStick GameObject not found in the scene.");
        }
    }

    void Update()
    {
        // Get the current rotation of the pogo stick in Euler angles
        Vector3 rotationAngles = _pogoController.gameObject.transform.eulerAngles;

        // Normalize the angles to range from -180 to 180 and round to whole numbers
        int normalizedX = Mathf.RoundToInt(NormalizeAngle(rotationAngles.x));
        int normalizedZ = Mathf.RoundToInt(NormalizeAngle(rotationAngles.z));

        // Check if upright (both X and Z close to 0)
        bool isUpright = normalizedX == 0 && normalizedZ == 0;

        if (isUpright)
        {
            // If upright, display "-" for all directions
            northNumber.text = "-";
            southNumber.text = "-";
            eastNumber.text = "-";
            westNumber.text = "-";
        }
        else
        {
            // Display based on X rotation (North/South)
            if (normalizedX > 0)
            {
                northNumber.text = normalizedX != 0 ? $"{normalizedX}°" : "-";
                southNumber.text = "-";
            }
            else if (normalizedX < 0)
            {
                northNumber.text = "-";
                southNumber.text = Mathf.Abs(normalizedX) != 0 ? $"{Mathf.Abs(normalizedX)}°" : "-";
            }
            else
            {
                northNumber.text = "-";
                southNumber.text = "-";
            }

            // Display based on Z rotation (West/East)
            if (normalizedZ > 0)
            {
                westNumber.text = normalizedZ != 0 ? $"{normalizedZ}°" : "-";
                eastNumber.text = "-";
            }
            else if (normalizedZ < 0)
            {
                westNumber.text = "-";
                eastNumber.text = Mathf.Abs(normalizedZ) != 0 ? $"{Mathf.Abs(normalizedZ)}°" : "-";
            }
            else
            {
                eastNumber.text = "-";
                westNumber.text = "-";
            }
        }
    }

    // Helper method to normalize angle to range from -180 to 180
    float NormalizeAngle(float angle)
    {
        return (angle > 180) ? angle - 360 : angle;
    }
}