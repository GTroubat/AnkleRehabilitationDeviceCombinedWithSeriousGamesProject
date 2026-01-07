using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System.Linq;

public class AdaptiveRewardAgent: Agent {
    [Header("References")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private Platform[] plateformes;
    [SerializeField] private Tube[] tubes;
    [SerializeField] private AI_AdaptativeControlMode adaptativeControlMode;

    [Header("Training Parameters")]
    [SerializeField] private float assistanceSpeed = 0.01f;

    [Header("Rewards & Penalties")]
    [SerializeField] private float successReward = 5.0f;
    [SerializeField] private float failurePenalty = -5.0f;
    [SerializeField] private float oscillationPenaltyWeight = 0.1f;
    [SerializeField] private float proRewardWeight = 0.01f;
    [SerializeField] private float goodRewardWeight = 0.01f;
    [SerializeField] private float difficultyRewardWeight = 0.01f;

    // State tracking
    private Marble currentMarble;
    private float playerInputAngle = 0f;
    private float aiSuggestedAngle = 0f;
    private float blendedAngle = 0f;

    // Assistance tracking
    private float currentAssistanceLevel = 0f;
    private float previousAssistanceLevel = 0f; // To determine oscillations

    private List<float> assistanceHistory = new List<float>();

    private int consecutiveErrors = 0;
    private int consecutiveSuccesses = 0;

    private readonly float[] MinMax = { -20f, 20f };
    private bool IsSideRight;

    private void Start() {
        IsSideRight = false;
    }

    public override void OnEpisodeBegin() {
        SetReward(0);
        previousAssistanceLevel = 0f; 
        assistanceHistory.Clear();
        marbleSpawner.ResetMarble();
    }

    public override void CollectObservations(VectorSensor sensor) {
        currentMarble = marbleSpawner.GetCurrentMarble();

        if (currentMarble != null) {
            sensor.AddObservation(currentMarble.transform.localPosition.x);
            sensor.AddObservation(currentMarble.transform.localPosition.y);
            sensor.AddObservation(currentMarble.GetComponent<Rigidbody2D>().linearVelocity);
            sensor.AddObservation((float)currentMarble.GetColor() / 3);
        } else {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector2.zero);
        }

        float signedAngle = ToSignedAngle(plateformes[0].transform.rotation.eulerAngles.z);
        sensor.AddObservation(signedAngle / MinMax[1]);

        // Player performance
        sensor.AddObservation(consecutiveErrors);
        sensor.AddObservation(consecutiveSuccesses);

        sensor.AddObservation(currentAssistanceLevel);

        float deltaPlayerOptimal = Mathf.Abs(playerInputAngle - CalculateOptimalAngle()) / 40f; 
        sensor.AddObservation(deltaPlayerOptimal);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Sauvegarde de l'ancienne valeur pour calculer l'oscillation
        previousAssistanceLevel = currentAssistanceLevel;

        // Action[0] : Niveau d'assistance (0 à 1)
        currentAssistanceLevel = Mathf.Clamp01(0.5f + actions.ContinuousActions[0] / 2);

        // --- 1. PÉNALITÉ D'OSCILLATION ---
        // On pénalise la valeur absolue de la différence entre cette frame et la précédente
        float oscillation = Mathf.Abs(currentAssistanceLevel - previousAssistanceLevel);
        if (oscillation > 0.05f) // Seuil de tolérance pour éviter le jittering minime
        {
            AddReward(-oscillation * oscillationPenaltyWeight);
        }

        // --- 2. RÉCOMPENSE D'ADAPTATION (Coaching) ---
        // On donne une petite récompense/pénalité à chaque step pour guider le comportement
        float stepReward = 0f;

        if (consecutiveSuccesses > 2) {
            // Le joueur est fort : on veut que l'assistance tende vers 0
            // Récompense si assistance faible, Pénalité si assistance forte
            stepReward = - currentAssistanceLevel * proRewardWeight;
        } else if (consecutiveSuccesses > 2) {
            // Le joueur est en difficulté : on veut que l'assistance tende vers 1
            stepReward = currentAssistanceLevel * difficultyRewardWeight;
        } else {
            // Zone neutre : on encourage une assistance modérée ou on ne fait rien
            // Ici, on encourage l'économie d'énergie (moins d'assistance est mieux par défaut)
            float errorMultiplier = Mathf.Min(consecutiveErrors, 5f);
            stepReward = currentAssistanceLevel * goodRewardWeight * errorMultiplier;
        }
        AddReward(stepReward);


        // --- Logique Physique Standard ---
        if (currentMarble != null) {
            assistanceHistory.Add(currentAssistanceLevel);
        }

        aiSuggestedAngle = CalculateOptimalAngle();
        blendedAngle = Mathf.Lerp(playerInputAngle, aiSuggestedAngle, currentAssistanceLevel);
        ApplyRotation(blendedAngle);

        // Petite pénalité d'existence pour encourager la rapidité
        AddReward(-0.0001f);

        if (adaptativeControlMode != null) {
            adaptativeControlMode.SetPlatformAngle(blendedAngle);
        }

        // for debug and print
        stock.SetOptimalAngle(aiSuggestedAngle);
        stock.SetAssistLevel(currentAssistanceLevel);
        stock.SetTargetAngle(blendedAngle);
        stock.SetRewards(GetCumulativeReward());
        stock.SetErrors(consecutiveSuccesses - consecutiveErrors);
    }

    public void OnMarbleScored(bool success) {
        float averageAssistance = assistanceHistory.Any() ? assistanceHistory.Average() : 0f;
        if (consecutiveSuccesses < 3) {
            if (success) {
                // Si succès : grosse récompense.
                // Bonus si l'assistance était faible (le joueur a réussi "seul").
                float reward = successReward;
                AddReward(reward);
            } else if (consecutiveErrors > 2) {
                // Si échec : pénalité.
                // La pénalité est PLUS FORTE si l'assistance était faible.
                // (L'IA aurait dû aider plus car le joueur a échoué).
                float penalty = failurePenalty * (1.0f - averageAssistance) * (consecutiveErrors - 1) * (consecutiveErrors - 1);
                AddReward(penalty);
            }
        }

        if (success) {
            consecutiveSuccesses++;
            consecutiveErrors = 0;
        } else {
            consecutiveErrors++;
            consecutiveSuccesses = 0;
        }

        float signedCurrentAngle = ToSignedAngle(transform.rotation.eulerAngles.z);
        IsSideRight = (signedCurrentAngle > 0f);

        Debug.Log("Reward: " + GetCumulativeReward().ToString("F4") + "; success: " + consecutiveSuccesses + "; errors: " + consecutiveErrors + 
            "; Assistance: " + averageAssistance);

        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActions = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.A)) continuousActions[0] = 1.0f; // Max help
        else if (Input.GetKey(KeyCode.E)) continuousActions[0] = -1.0f; // No help
        else continuousActions[0] = 0.0f; // Stable
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
        }
    }

    private float CalculateOptimalAngle() {
        if (currentMarble == null) return 0f;
        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        float targetAngle = 0f;

        switch (marbleColor) {
            case EColors.Red: targetAngle = MinMax[1]; break;
            case EColors.Yellow: targetAngle = MinMax[0]; break;
            case EColors.Blue:
                if (marblePos.y > 1.6f) targetAngle = (IsSideRight) ? MinMax[1] / 4f : MinMax[0] / 4f;
                else targetAngle = (IsSideRight) ? MinMax[0] : MinMax[1];
                break;
        }
        return targetAngle;
    }

    private void FixedUpdate() {
        RequestDecision();
    }

    private float ToSignedAngle(float unityAngle) => (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    private float ToUnityAngle(float signedAngle) => (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
}