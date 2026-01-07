using NUnit.Framework.Internal;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Google.Protobuf.WellKnownTypes.Field;

public class RotateToGoalAgent : Agent {
    [SerializeField] float speed = 0.3f;
    [SerializeField] private MarbleSpawnWithDisplayer spawner;

    [SerializeField] private Platform[] plateformes;
    [SerializeField] private Tube[] tubes;
    [SerializeField] private StockVariables stock;

    private Marble marble;
    private Collider2D[] collider2Ds;

    private float[] maxAngles = { -20, 20 };

    private void Start() {
        marble = spawner.GetCurrentMarble();
        stock.SetIsGamePlaying(true);
        collider2Ds = new Collider2D[tubes.Length];
        for (int i = 0; i < tubes.Length; i++) {
            collider2Ds[i] = tubes[i].GetComponent<Collider2D>();
        }
    }

    public void MarbleIsInBasket(bool success, EColors tubeColor, EColors marbleColor) {
        if (success) {
            AddReward(500f);
            Debug.Log("Well Done for color " + tubeColor);
        } else {
            AddReward(-500f);
            Debug.Log("Marble " + marbleColor + " in " + tubeColor + " Tube");
        }
        EndEpisode();
    }

    public override void OnEpisodeBegin() {
        if (stock.GetIsGamePlaying()) {
            for (int i = 0; i < plateformes.Length; i++) {
                plateformes[i].transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            marble = spawner.GetCurrentMarble();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = -Input.GetAxisRaw("Horizontal");
    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(transform.rotation.z);
        if (marble == null) {
            marble = spawner.GetCurrentMarble();
        }
        if (marble != null) {
            sensor.AddObservation(marble.transform.position.x);
            sensor.AddObservation(marble.transform.position.y);
            sensor.AddObservation((float) marble.GetColor());
            sensor.AddObservation(marble.GetComponent<Rigidbody2D>().linearVelocityX);
            sensor.AddObservation(marble.GetComponent<Rigidbody2D>().linearVelocityY);
        }
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float signedAngle = actions.ContinuousActions[0];
        float unityAngle = ToUnityAngle(signedAngle);

        plateformes[0].transform.Rotate(new Vector3(0, 0, unityAngle * speed));

        unityAngle = plateformes[0].transform.rotation.eulerAngles.z;
        signedAngle = ToSignedAngle(signedAngle);

        if (signedAngle < maxAngles[0]) {
            AddReward(-10f);
            Debug.Log("Trop penché");
            signedAngle = maxAngles[0];
        }

        if (signedAngle > maxAngles[1]) {
            AddReward(-10f);
            Debug.Log("Trop penché");
            signedAngle = maxAngles[1];
        }

        for (int i = 0; i < plateformes.Length; i++) {
            Quaternion newRotation = Quaternion.Euler(0, 0, ToUnityAngle(signedAngle) * speed);
            plateformes[i].transform.localRotation = newRotation;
        }

        AddReward(-1f); //To avoid too much steps
    }


    // Convert Unity 0..360 angle to signed -180..180
    private float ToSignedAngle(float unityAngle) {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    // Convert signed -180..180 angle to Unity 0..360
    private float ToUnityAngle(float signedAngle) {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }

}
