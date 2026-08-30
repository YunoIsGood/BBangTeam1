using UnityEngine;
using UnityEngine.InputSystem;

namespace Script
{
    public class CameraInputReader : MonoBehaviour
    {
        [Header("Settings")] 
        [Range(0.01f, 10f)][field:SerializeField]public float Sensitivity { get; private set; }
        
        [Header("InputActions")] 
        private InputAction cameraLook;

        public void Init()
        {
            cameraLook = new InputAction("cameraLook", InputActionType.Value, binding: "<Mouse>/delta");
        }
    }
}