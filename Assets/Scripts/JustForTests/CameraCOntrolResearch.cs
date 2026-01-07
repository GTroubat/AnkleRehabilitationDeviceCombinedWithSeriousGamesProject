using UnityEngine;

public class CameraCOntrolResearch : MonoBehaviour
{
    private InputSystem_Actions inputAction;

    private Vector2 inputVector;
    private float upDownInput;
    private Vector2 rotationVector;
    void Start(){
        inputAction = new InputSystem_Actions();
        inputAction.Player.Enable();

        //gameObject.transform.position = new Vector3(-45f, 60f, -16f);
    }

    void Update(){
        inputVector = inputAction.Player.Move.ReadValue<Vector2>();
        inputVector = inputVector.normalized;
        upDownInput = inputAction.Player.UpDown.ReadValue<float>();
        rotationVector = inputAction.Player.Look.ReadValue<Vector2>();

        Vector3 moveDir = new Vector3(inputVector.x, upDownInput, inputVector.y);
        Vector3 rotationDir = new Vector3(-rotationVector.y, rotationVector.x, 0f);
        float moveSpeed = 50f;
        gameObject.transform.position += moveDir * moveSpeed * Time.deltaTime;
        gameObject.transform.eulerAngles += rotationDir * 10f * Time.deltaTime;
    }
}
