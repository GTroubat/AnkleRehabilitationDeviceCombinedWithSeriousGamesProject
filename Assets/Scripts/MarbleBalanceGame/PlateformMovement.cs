using UnityEngine;

public class PlateformMovement : MonoBehaviour {

    [SerializeField] float speed = 1f;

    private PlatformInputAction inputActions;

    private void Awake() {
        inputActions = new PlatformInputAction();
        inputActions.Platform.Enable();

        inputActions.Platform.Right.performed += Right_performed;
        inputActions.Platform.Left.performed += Left_performed;
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.transform.Rotate(new Vector3(0, 0, speed));
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            this.transform.Rotate(new Vector3(0, 0, -speed));
        }
    }

    private void Left_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) { 
        this.transform.Rotate(new Vector3(0, 0, 1));
    }

    private void Right_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        this.transform.Rotate(new Vector3(0, 0, -1));
    }
}
