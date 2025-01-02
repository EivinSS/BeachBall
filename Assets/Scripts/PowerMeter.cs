using UnityEngine;
using UnityEngine.UI;

public class PowerMeter : MonoBehaviour
{
    public Slider slider;
    
    private PogoController _pogoController;

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
        slider.value = _pogoController.normalizedJumpCharge;
    }
}
