using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;

public class ForceBasedAssistAgent: Agent {
    [Header("References")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private Platform[] platforms; 

    [Header("Settings")]
    [SerializeField] private float maxGameAngle = 20f;
    [SerializeField] private float forceSensitivity = 1.0f; 

    [Header("Simulator")]
    [SerializeField] private PatientSimulator patientSim;
    [SerializeField] private bool useSimulator = true; 

    // Variables internes
    private Marble currentMarble;
    private float currentAssistance = 0f; // 0 = no help, 1 = full help
    private float[] stableForces; // Calibration (MeanLCStable)

    private float currentZ;

    private readonly float[] pitchSignVector = { -1f, 1f, 1f, -1f };

    private void Start() {
        marbleSpawner.ResetMarble();
        currentZ = 1;
    }

    public override void Initialize() {
        stableForces = new float[4];
        UpdateCalibration();
    }

    public override void OnEpisodeBegin() {
        // Reset each marble
        currentAssistance = 0f;
        UpdateCalibration(); 

        if (useSimulator && patientSim != null) {
            patientSim.ResetPatientDifficulty(); 
            UpdateCalibration();
        }
    }

    public void UpdateCalibration() {
        double[] dbStable = stock.GetMeanLCStable();
        if (dbStable != null) {
            for (int i = 0; i < 4; i++) stableForces[i] = (float)dbStable[i];
        }
    }

    public override void CollectObservations(VectorSensor sensor) {
        currentMarble = marbleSpawner.GetCurrentMarble();

        // Marble
        float targetDir = 0f; // -1 (left), 0 (Centre), 1 (right)
        if (currentMarble != null) {
            EColors c = currentMarble.GetColor();
            if (c == EColors.Red) targetDir = -1f;       // Red -> Gauche
            else if (c == EColors.Yellow) targetDir = 1f; // yellow -> Droite
                                                          // Blue -> 0

            sensor.AddObservation(currentMarble.transform.localPosition.x);
            sensor.AddObservation(currentMarble.transform.localPosition.y);
            sensor.AddObservation(currentMarble.GetComponent<Rigidbody2D>().linearVelocity);
            sensor.AddObservation(targetDir);
        } else {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
        }

        float playerRawIntent = CalculatePlayerIntent();
        sensor.AddObservation(playerRawIntent); 

        sensor.AddObservation(currentAssistance);

        float currentAngle = ToSignedAngle(platforms[0].transform.rotation.eulerAngles.z);
        sensor.AddObservation(currentAngle / maxGameAngle);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float targetAssistance = Mathf.Clamp01(0.5f + actions.ContinuousActions[0] * 0.5f);

        currentAssistance = Mathf.Lerp(currentAssistance, targetAssistance, 0.1f);

        float playerInput = CalculatePlayerIntent() * maxGameAngle * forceSensitivity;

        float optimalAngle = CalculateOptimalAngle();

        float finalAngle = Mathf.Lerp(playerInput, optimalAngle, currentAssistance);

        ApplyRotation(finalAngle);

        CalculateRewards(playerInput, optimalAngle, finalAngle);

        //To print on screen
        stock.SetAssistLevel(currentAssistance);
        stock.SetOptimalAngle(optimalAngle);
        stock.SetTargetAngle(finalAngle);
    }

    private void CalculateRewards(float playerInput, float optimalAngle, float finalAngle) {
        // Reward if in good side
        if (Mathf.Sign(playerInput) == Mathf.Sign(optimalAngle) && Mathf.Abs(playerInput) > 0.5f) {
            AddReward(0.01f); 
        }

        // Avoid too much assistance
        AddReward(-0.005f * currentAssistance);

        // Negative reward if going opposite to optimal
        if (Mathf.Sign(playerInput) != Mathf.Sign(optimalAngle) && Mathf.Abs(playerInput) > 2f) {
            AddReward(-0.01f);
        }

        stock.SetRewards(GetCumulativeReward());
    }

    private float CalculatePlayerIntent() {
        float totalForce = 0f;
        float[] currentForces = GetCurrentForces(); 

        for (int i = 0; i < 4; i++) {
            float delta = currentForces[i] - stableForces[i];
            totalForce += delta * pitchSignVector[i];
        }
        return Mathf.Clamp(totalForce / 200000f, -1f, 1f); 
    }

    private float CalculateOptimalAngle() {
        if (currentMarble == null) return 0f;
        EColors c = currentMarble.GetColor();

        if (c == EColors.Red) return maxGameAngle;      
        if (c == EColors.Yellow) return -maxGameAngle;  

        if (currentMarble.transform.localPosition.y > 1.6f) {
            return (currentZ > 0) ? maxGameAngle / 4 : -maxGameAngle / 4;
        }
        return (currentZ > 0) ? -maxGameAngle : maxGameAngle;
    }

    private void ApplyRotation(float angle) {
        angle = Mathf.Clamp(angle, -maxGameAngle, maxGameAngle);
        float unityAngle = (angle < 0f) ? angle + 360f : angle;

        if (stock.GetIsGamePlaying()) {
            foreach (var p in platforms) {
                p.transform.rotation = Quaternion.Euler(0, 0, -unityAngle);
            }
            stock.SetPitch(angle);
            stock.SetSend(true);
        }
    }

    public void OnMarbleScored(bool success) {
        if (success) {
            AddReward(1.0f); 
        } else {
            AddReward(-1.0f); 
        }
        currentZ = ToSignedAngle(platforms[0].transform.rotation.eulerAngles.z);
        EndEpisode();
    }

    private float[] GetCurrentForces() {
        if (useSimulator && patientSim != null) {
            float targetDir = 0f;
            if (currentMarble != null) {
                if (currentMarble.GetColor() == EColors.Red) targetDir = -1f;
                else if (currentMarble.GetColor() == EColors.Yellow) targetDir = 1f;
            }
            return patientSim.GetSimulatedLoadCells(targetDir);
        } else {
            // if not using simulator, get real load cells
            return new float[] {
            (float)stock.GetLoadCell1(),
            (float)stock.GetLoadCell2(),
            (float)stock.GetLoadCell3(),
            (float)stock.GetLoadCell4()
        };
        }
    }

    private void FixedUpdate() {
        if( stock.GetIsGamePlaying()) 
            UpdateCalibration(); 
    }

    private float ToSignedAngle(float unityAngle) => (unityAngle > 180f) ? unityAngle - 360f : unityAngle;

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActions = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.UpArrow)) continuousActions[0] = 1.0f;
        else if (Input.GetKey(KeyCode.DownArrow)) continuousActions[0] = 0.0f;
        else continuousActions[0] = currentAssistance;
    }
}