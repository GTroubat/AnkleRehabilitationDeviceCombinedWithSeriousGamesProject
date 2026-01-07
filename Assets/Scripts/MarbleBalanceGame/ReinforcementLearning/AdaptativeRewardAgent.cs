using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System.Linq;

public class AdaptiveRewardAgentV1: Agent {
    [Header("References")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private Platform[] plateformes;
    [SerializeField] private Tube[] tubes;
    [SerializeField] private AI_AdaptativeControlMode adaptativeControlMode;

    [Header("Training Parameters")]
    [Tooltip("Number of marbles to end an episode.")]
    [SerializeField] private int marblesPerEpisode = 10;
    [SerializeField] private float assistanceSpeed = 0.01f;

    [Header("Assistance and Reward")]
    [SerializeField] private float successReward = 1.0f;
    [SerializeField] private float failurePenalty = -1.0f;

    // State tracking
    private Marble currentMarble;
    private float playerInputAngle = 0f;
    private float aiSuggestedAngle = 0f;
    private float blendedAngle = 0f;
    private float currentAssistanceLevel = 0f;

    // Per-marble metrics
    private List<float> assistanceHistory = new List<float>();

    // Per-episode metrics
    private int marblesProcessedInEpisode = 0;
    private int consecutiveErrors = 0;
    private int consecutiveSuccesses = 0;

    private readonly float[] MinMax = { -20f, 20f };
    private bool IsSideRight;

    public override void OnEpisodeBegin() {
        // Reset platforms
        foreach (var p in plateformes) {
            //p.transform.rotation = Quaternion.identity;
        }

        // Reset episode metrics
        marblesProcessedInEpisode = 0;
        consecutiveErrors = 0;
        consecutiveSuccesses = 0;
        currentAssistanceLevel = 0f; // Start without assistance

        // Reset per-marble metrics
        assistanceHistory.Clear();

        // Ensure a marble is ready
        marbleSpawner.ResetMarble();

        IsSideRight = false;
    }

    public override void CollectObservations(VectorSensor sensor) {
        currentMarble = marbleSpawner.GetCurrentMarble();

        // Observation of the current marble (if it exists)
        if (currentMarble != null) {
            sensor.AddObservation(currentMarble.transform.localPosition.x);
            sensor.AddObservation(currentMarble.transform.localPosition.y); 
            sensor.AddObservation(currentMarble.GetComponent<Rigidbody2D>().linearVelocity);
            sensor.AddObservation((float) currentMarble.GetColor()/3);
        } else {
            sensor.AddObservation(Vector3.zero); // 3 observations
            sensor.AddObservation(Vector2.zero); // 2 observations
        }

        // Observation of the platform
        float signedAngle = ToSignedAngle(plateformes[0].transform.rotation.eulerAngles.z);
        sensor.AddObservation(signedAngle / MinMax[1]); // Normalized angle

        // Player performance metrics
        sensor.AddObservation(consecutiveErrors);
        sensor.AddObservation(consecutiveSuccesses);

        // Current assistance level
        sensor.AddObservation(currentAssistanceLevel);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Action[0] is the new assistance level chosen by the agent
        currentAssistanceLevel = Mathf.Clamp01(0.5f + actions.ContinuousActions[0]/2);

        // Store assistance level for this frame
        if (currentMarble != null) {
            assistanceHistory.Add(currentAssistanceLevel);
        }

        // Determine optimal angle and apply blended rotation
        aiSuggestedAngle = CalculateOptimalAngle();

        blendedAngle = Mathf.Lerp(playerInputAngle, aiSuggestedAngle, currentAssistanceLevel);
        ApplyRotation(blendedAngle);

        // Small penalty per step to encourage efficiency
        AddReward(-0.001f);

        if (adaptativeControlMode != null) {
            adaptativeControlMode.SetPlatformAngle(blendedAngle);
        }

        stock.SetOptimalAngle(aiSuggestedAngle);
        stock.SetAssistLevel(currentAssistanceLevel);
        stock.SetTargetAngle(blendedAngle);
    }

    public void OnMarbleScored(bool success) {
        float averageAssistance = 0f;
        if (assistanceHistory.Any()) {
            averageAssistance = assistanceHistory.Average();
        }

        if (success) {
            // Reward is higher for success with less average assistance
            float reward = successReward * (1.0f - averageAssistance * 0.5f);
            AddReward(reward);
            consecutiveSuccesses++;
            consecutiveErrors = 0;
        } else {
            // Penalty is higher for failure with low assistance (agent should have helped more)
            float penalty = failurePenalty * (1.0f - averageAssistance * 0.5f);
            AddReward(penalty);
            consecutiveErrors++;
            consecutiveSuccesses = 0;
        }

        float signedCurrentAngle = ToSignedAngle(transform.rotation.eulerAngles.z);
        IsSideRight = (signedCurrentAngle > 0f);

        // Reset for the next marble
        assistanceHistory.Clear();
        marblesProcessedInEpisode++;

        // End the episode if the number of marbles has been reached
        if (marblesProcessedInEpisode >= marblesPerEpisode) {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActions = actionsOut.ContinuousActions;
        // Allow manual control for debugging
        if (Input.GetKey(KeyCode.A)) continuousActions[0] = currentAssistanceLevel + 0.05f;
        if (Input.GetKey(KeyCode.E)) continuousActions[0] = currentAssistanceLevel - 0.05f;
    }

    public void UpdatePlayerInput(float inputAngle) {
        playerInputAngle = inputAngle;
    }

    private void ApplyRotation(float targetAngle) {
        float currentAngle = ToSignedAngle(plateformes[0].transform.rotation.eulerAngles.z);
        float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, assistanceSpeed);
        smoothedAngle = Mathf.Clamp(smoothedAngle, MinMax[0], MinMax[1]);

        if (stock.GetIsGamePlaying() && stock.GetGameMode() == EGameMode.Adaptative) {
            foreach (var p in plateformes) {
                p.transform.rotation = Quaternion.Euler(0, 0, ToUnityAngle(smoothedAngle));
            }

            //stock.SetPitch(smoothedAngle);
            //stock.SetSend(true);
        }
    }

    private float CalculateOptimalAngle() {
        if (currentMarble == null) return 0f;

        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        float targetAngle = 0f;

        // Logique similaire à AdaptativeControlMode mais retourne l'angle optimal
        switch (marbleColor) {
            case EColors.Red:
                targetAngle = MinMax[1]; // Gauche
                break;
            case EColors.Yellow:
                targetAngle = MinMax[0]; // Droite
                break;
            case EColors.Blue:
                if (marblePos.y > 1.6f) {
                    targetAngle = (IsSideRight)
                        ? MinMax[1] / 4f
                        : MinMax[0] / 4f;
                } else {
                    targetAngle = (IsSideRight)
                        ? MinMax[0]
                        : MinMax[1];
                }
                break;
        }

        return targetAngle;
    }

    private float ToSignedAngle(float unityAngle) {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    private float ToUnityAngle(float signedAngle) {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }
}
