using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private GameObject playerVisual;

    private InputSystem_Actions inputAction;

    private float velocityMax = 10f;

    private void Awake() {
        inputAction = new InputSystem_Actions();
        inputAction.Player.Enable();
    }

    void Start()
    {
        //gameObject.transform.position = new Vector3(-2f, 1.5f, -2f);
    }

    void Update()
    {
        HandleMovement();
        limitVelocity();
        Debug.Log(gameObject.GetComponent<Rigidbody>().linearVelocity);
    }

    public Vector2 GetMovementVectorNormalized() {
        Vector2 inputVector = inputAction.Player.Move.ReadValue<Vector2>();
        inputVector = inputVector.normalized; //pour que norme vecteur vaille 1 meme si (1,1)

        return inputVector;
    }

    private void limitVelocity() {
        Vector3 maxVelocity = new Vector3(velocityMax, velocityMax, velocityMax);
        Vector3 currentVelocity = gameObject.GetComponent<Rigidbody>().linearVelocity;
        if (currentVelocity.x > maxVelocity.x) { currentVelocity.x = maxVelocity.x; }
        if (currentVelocity.y > maxVelocity.y) { currentVelocity.y = maxVelocity.y; }
        if (currentVelocity.z > maxVelocity.z) { currentVelocity.z = maxVelocity.z; }
        if (currentVelocity.x < -maxVelocity.x) { currentVelocity.x = -maxVelocity.x; }
        if (currentVelocity.y < -maxVelocity.y) { currentVelocity.y = -maxVelocity.y; }
        if (currentVelocity.z < -maxVelocity.z) { currentVelocity.z = -maxVelocity. z; }
        gameObject.GetComponent<Rigidbody>().linearVelocity = currentVelocity;
    }

    private void HandleMovement() {
        Vector2 inputVector = GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        float moveDistance = moveSpeed * Time.deltaTime;

        transform.position += moveDir * moveDistance;

        float rotaSpeed = 10f; //Vitesse de rotation quand change direction
        playerVisual.transform.forward = Vector3.Slerp(playerVisual.transform.forward, moveDir, Time.deltaTime * rotaSpeed);
    }

    public void SetVelocityMax(float newVelMax) {
        velocityMax = newVelMax;
    }
}
