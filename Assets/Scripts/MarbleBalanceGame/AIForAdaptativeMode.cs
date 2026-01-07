using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AIForAdaptativeMode : Agent
{
    [Header("References")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private Platform[] plateformes;
    [SerializeField] private Tube[] tubes;
    [SerializeField] AI_AdaptativeControlMode adaptativeControlMode;

    [Header("Assistance Parameters")]
    [SerializeField] private float minAssistance = 0f;  // Mode totalement actif
    [SerializeField] private float maxAssistance = 1f;  // Mode totalement passif
    [SerializeField] private float assistanceSpeed = 0.01f;

    private Marble currentMarble;
    private float currentAssistanceLevel = 0f; // 0 = actif, 1 = passif
    private float playerInputAngle = 0f;
    private float aiSuggestedAngle = 0f;
    private float blendedAngle = 0f;

    // Metrics pour observations
    private float timeSinceLastSuccess = 0f;
    private float marbleDistanceToTarget = 0f;
    private int consecutiveErrors = 0;
    private int consecutiveSuccesses = 0;
    private float playerInputVariance = 0f; // Mesure de l'erraticité du joueur
    private float[] recentInputs = new float[10];
    private int inputIndex = 0;

    private float[] MinMax = {-20, 20};

    private int previousPoints = 0;
    private int previousMarbleCount = 0;

    private bool IsSideRight;

    private void Start() {
        //stock.SetIsGamePlaying(true);
    }

    public override void OnEpisodeBegin()
    {
        // Reset metrics
        timeSinceLastSuccess = 0f;
        consecutiveErrors = 0;
        consecutiveSuccesses = 0;
        currentAssistanceLevel = 0f; // Commence en mode actif
        
        for (int i = 0; i < plateformes.Length; i++)
        {
            plateformes[i].transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        IsSideRight = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        currentMarble = marbleSpawner.GetCurrentMarble();
        
        // 1. État de la bille actuelle (5 observations)
        if (currentMarble != null)
        {
            sensor.AddObservation(currentMarble.transform.position.x);
            sensor.AddObservation(currentMarble.transform.position.y);
            sensor.AddObservation((float)currentMarble.GetColor() / 3f); // Normalisé
            sensor.AddObservation(currentMarble.GetComponent<Rigidbody2D>().linearVelocity);

            // Distance à la cible correcte
            marbleDistanceToTarget = CalculateDistanceToCorrectTube();
            //sensor.AddObservation(marbleDistanceToTarget);
        }
        else
        {
            // Padding si pas de bille
            for (int i = 0; i < 5; i++) sensor.AddObservation(0f);
        }

        // 2. État de la plateforme (2 observations)
        float signedAngle = ToSignedAngle(transform.rotation.eulerAngles.z);
        sensor.AddObservation(signedAngle / 20f); // Normalisé par angle max
        sensor.AddObservation(currentAssistanceLevel);

        // 3. Performance du joueur (5 observations)
        sensor.AddObservation(timeSinceLastSuccess / 60f); // Normalisé (max 60s)
        sensor.AddObservation(consecutiveErrors / 10f); // Normalisé
        sensor.AddObservation(consecutiveSuccesses / 10f);
        //sensor.AddObservation(playerInputVariance); // Déjà entre 0-1
        sensor.AddObservation(stock.GetPoints() / 100f); // Score normalisé
        //sensor.AddObservation((int)stock.GetCurrentDisability() / 5f); // Normalisé (5 types)

        // 4. Input du joueur vs AI optimal (2 observations)
        sensor.AddObservation(playerInputAngle / 20f);
        sensor.AddObservation(aiSuggestedAngle / 20f);
        
        // Total: 15 observations
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // L'action est le niveau d'assistance (0-1)
        // 0 = laisse le joueur contrôler totalement
        // 1 = l'IA contrôle totalement
        currentAssistanceLevel = Mathf.Clamp01(actions.ContinuousActions[0]);

        // Calculer l'angle suggéré par l'IA (comportement optimal)
        aiSuggestedAngle = CalculateOptimalAngle();
        stock.SetOptimalAngle(aiSuggestedAngle);

        // Mélanger l'input du joueur avec la suggestion de l'IA
        blendedAngle = Mathf.Lerp(playerInputAngle, aiSuggestedAngle, currentAssistanceLevel);

        if (adaptativeControlMode != null) {
            adaptativeControlMode.SetPlatformAngle(blendedAngle);
        }
        // Appliquer l'angle mélangé
        ApplyRotation(blendedAngle);
        
        // Update metrics
        timeSinceLastSuccess += Time.fixedDeltaTime;
        UpdatePlayerInputVariance();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Pour tester: utiliser les touches pour ajuster l'assistance manuellement
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        
        if (Input.GetKey(KeyCode.P)) continuousActions[0] = 0f;    // Mode actif
        else if (Input.GetKey(KeyCode.M)) continuousActions[0] = 0.33f; // Assistance faible
        else if (Input.GetKey(KeyCode.O)) continuousActions[0] = 0.66f; // Assistance forte
        else if (Input.GetKey(KeyCode.L)) continuousActions[0] = 1f;    // Mode passif
        else continuousActions[0] = currentAssistanceLevel; // Maintenir le niveau actuel
    }

    // Appelé quand une bille entre dans un tube
    public void OnMarbleScored(bool success)
    {
        if (success)
        {
            // Récompense basée sur le niveau d'assistance utilisé
            // Moins d'assistance = plus de récompense
            float reward = 1f - (currentAssistanceLevel * 0.5f);
            AddReward(10 * reward);
            
            consecutiveSuccesses++;
            consecutiveErrors = 0;
            timeSinceLastSuccess = 0f;
            
            Debug.Log($"Success! Assistance: {currentAssistanceLevel:F2}, Reward: {GetCumulativeReward():F2}");
        }
        else
        {
            // Pénalité si échec avec peu d'assistance
            float penalty = -0.5f - (currentAssistanceLevel * -0.5f);
            AddReward(10 * penalty);
            
            consecutiveErrors++;
            consecutiveSuccesses = 0;
            
            Debug.Log($"Failed! Assistance: {currentAssistanceLevel:F2}, Penalty: {GetCumulativeReward():F2}");
        }
        
        previousPoints = stock.GetPoints();
        previousMarbleCount = stock.GetMarbleCounter();

        float signedCurrentAngle = ToSignedAngle(transform.rotation.eulerAngles.z);
        IsSideRight = (signedCurrentAngle > 0f);
        Debug.Log("IsSideRight: " + IsSideRight);
    }

    // Méthode appelée chaque frame pour récupérer l'input du joueur
    public void UpdatePlayerInput(float inputAngle)
    {
        playerInputAngle = inputAngle;
        
        // Stocker pour calculer la variance
        recentInputs[inputIndex % recentInputs.Length] = inputAngle;
        inputIndex++;
    }

    private float CalculateOptimalAngle()
    {
        if (currentMarble == null) return 0f;

        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        float targetAngle = 0f;

        // Logique similaire à AdaptativeControlMode mais retourne l'angle optimal
        switch (marbleColor)
        {
            case EColors.Red:
                targetAngle = MinMax[1]; // Gauche
                break;
            case EColors.Yellow:
                targetAngle = MinMax[0]; // Droite
                break;
            case EColors.Blue:
                if (marblePos.y > 1.6f)
                {
                    targetAngle = (IsSideRight) 
                        ? MinMax[1] / 4f 
                        : MinMax[0] / 4f;
                }
                else
                {
                    targetAngle = (IsSideRight) 
                        ? MinMax[0] 
                        : MinMax[1];
                }
                break;
        }

        return targetAngle;
    }

    private float CalculateDistanceToCorrectTube()
    {
        if (currentMarble == null) return 100f;

        EColors marbleColor = currentMarble.GetColor();
        float minDistance = float.MaxValue;

        foreach (Tube tube in tubes)
        {
            if (tube.GetColor() == marbleColor)
            {
                float distance = Vector2.Distance(
                    currentMarble.transform.position, 
                    tube.transform.position
                );
                minDistance = Mathf.Min(minDistance, distance);
            }
        }

        return minDistance;
    }

    private void UpdatePlayerInputVariance()
    {
        if (inputIndex < recentInputs.Length) return;

        float mean = 0f;
        foreach (float input in recentInputs)
        {
            mean += input;
        }
        mean /= recentInputs.Length;

        float variance = 0f;
        foreach (float input in recentInputs)
        {
            variance += Mathf.Pow(input - mean, 2);
        }
        variance /= recentInputs.Length;

        playerInputVariance = Mathf.Clamp01(variance / 400f); // Normalisé
    }

    private void ApplyRotation(float targetAngle)
    {
        float currentAngle = ToSignedAngle(transform.rotation.eulerAngles.z);
        float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, assistanceSpeed);
        float unityAngle = 0;


        smoothedAngle = Mathf.Clamp(smoothedAngle, MinMax[0], MinMax[1]);
        
        for (int i = 0; i < plateformes.Length; i++)
        {
            unityAngle = ToUnityAngle(smoothedAngle);
            plateformes[i].transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        }

        stock.SetPitch(smoothedAngle);
        stock.SetSend(true);
    }

    private float ToSignedAngle(float unityAngle)
    {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    private float ToUnityAngle(float signedAngle)
    {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }

    // Pénalités additionnelles pour encourager l'efficacité
    private void FixedUpdate()
    {
        RequestDecision();
        // Petite pénalité continue si trop d'assistance est utilisée
        // Encourage l'agent à minimiser l'assistance
        AddReward(-0.0001f * currentAssistanceLevel);
        //Debug.Log("assistance: " + currentAssistanceLevel);

        // Bonus si le joueur s'améliore progressivement
        if (currentMarble != null)
        {
            float distanceReward = -marbleDistanceToTarget * 0.0001f;
            //AddReward(distanceReward);
        }

        stock.SetAssistLevel(currentAssistanceLevel);
        stock.SetTargetAngle(blendedAngle);
    }
}
