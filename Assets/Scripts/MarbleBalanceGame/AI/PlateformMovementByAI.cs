using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlateformMovementByAI : Agent {

    [SerializeField] float speed = 0.3f;
    [SerializeField] private MarbleAI marble;
    [SerializeField] private TubeAI[] tubes;
    [SerializeField] private RandomizerMarbleTube randomizer;

    [SerializeField] private Platform[] plateformes;
    [SerializeField] private StockVariables stock;

    private Collider2D[] collider2Ds;
    private float angleMax = 40f;

    private void Start() {
        randomizer.RandomMarble();
        //randomizer.RandomTube();
        collider2Ds = new Collider2D[tubes.Length];
        for (int i = 0; i < tubes.Length; i++) {
            collider2Ds[i] = tubes[i].GetComponent<Collider2D>();
        }
    }

    public override void OnEpisodeBegin() {
        randomizer.RandomMarble();
        //randomizer.RandomTube();
        //transform.rotation = Quaternion.Euler(0, 0, 0);
        for (int i = 0; i < plateformes.Length; i++) {
            plateformes[i].transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        marble.GetComponentInParent<Transform>().position = new Vector3(0, 5f, 0);
        marble.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        marble.GetComponent<Rigidbody2D>().inertia = 0f;
    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(transform.rotation.z);
        sensor.AddObservation((float)marble.GetColor());
        sensor.AddObservation(marble.GetComponentInParent<Transform>().position.x);
        sensor.AddObservation(marble.GetComponentInParent<Transform>().position.y);
        sensor.AddObservation(marble.GetComponent<Rigidbody2D>().linearVelocity);
        sensor.AddObservation((float)tubes[0].GetColor());
        sensor.AddObservation((float)tubes[1].GetColor());
        sensor.AddObservation((float)tubes[2].GetColor());
        //sensor.AddObservation(tubes[0].transform.position.x);
        //sensor.AddObservation(tubes[1].transform.position.x);
        //sensor.AddObservation(tubes[2].transform.position.x);
    }

    public override void OnActionReceived(ActionBuffers actions) {     
        float angle = actions.ContinuousActions[0];
        for (int i = 0; i < plateformes.Length; i++) {
            plateformes[i].transform.Rotate(new Vector3(0, 0, angle * speed));
        }
        angle = plateformes[0].transform.rotation.eulerAngles.z;
        //Debug.Log("angle: " + angle);
        if ((angle>180 && angle<360-angleMax) || (angle<180 && angle>angleMax)) {
            SetReward(-1f);
            Debug.Log("Trop penché");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = -Input.GetAxisRaw("Horizontal");
        //Debug.Log("heuristic: " + continuousActions[0]);
    }

    public float GetRotation() {
        if(transform.rotation.eulerAngles.z > 180) {
            return transform.rotation.eulerAngles.z - 360;
        }
        return transform.rotation.eulerAngles.z;
    }

    private void Update() {
        for (int i = 0; i < collider2Ds.Length; i++) {
            if (collider2Ds[i].IsTouching(marble.GetComponent<Collider2D>())) {
                if (marble.GetColor() == tubes[i].GetColor()) {
                    SetReward(5f);
                    Debug.Log("Correct Tube: " + marble.GetColor());
                } else {
                    SetReward(-5f);
                    Debug.Log("Marble " + marble.GetColor() + " in " + tubes[i].GetColor() + " Tube");
                }
                //Debug.Log("Reward: " + GetCumulativeReward());
                EndEpisode();
            }
        }
    }
}
