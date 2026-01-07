using UnityEngine;

public class PatientSimulator: MonoBehaviour {

    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [Header("Patient Settings")]
    [Range(0.1f, 5f)] public float reactionSpeed = 2.0f; 
    [Range(0f, 1f)] public float weakness = 0.0f;        
    [Range(0f, 1f)] public float tremor = 0.1f;          
    [Range(0f, 1f)] public float errorProbability = 0.0f;

    // Enum for different difficulties
    public enum PatientState { Normal, Weak, Frozen, WrongDirection, Spastic }
    private PatientState currentState = PatientState.Normal;

    // Data from excel datasheet
    [System.Serializable]
    public struct ForceProfile {
        public float[] mean;
        public float[] min;
        public float[] max;
    }

    //Stable position
    private ForceProfile stableProfile = new ForceProfile {
        mean = new float[] { 8988950f, 8377688f, 8762872f, 8803933f },
        min = new float[] { 8935710f, 8377601f, 8737854f, 8756583f },
        max = new float[] { 9168598f, 8377796f, 8847558f, 8885770f }
    };

    // Pitch Down 
    private ForceProfile forwardProfile = new ForceProfile {
        mean = new float[] { 8600843f, 8377689f, 8956795f, 8563935f },
        min = new float[] { 8415676f, 8377601f, 8762227f, 8439207f },
        max = new float[] { 8970983f, 8377774f, 9010447f, 8786877f }
    };

    // Pitch Up 
    private ForceProfile backwardProfile = new ForceProfile {
        mean = new float[] { 9074316f, 8377702f, 8652212f, 9123886f },
        min = new float[] { 8633452f, 8377545f, 8492958f, 8583898f },
        max = new float[] { 9293900f, 8377799f, 9023567f, 9382300f }
    };

    private float[] currentForces = new float[4];
    private float[] targetForces = new float[4];

    private void Start() {
        System.Array.Copy(stableProfile.mean, currentForces, 4);
        System.Array.Copy(stableProfile.mean, targetForces, 4);

        // Convert float[] to double[] before passing to SetMeanLCStable
        double[] stableMeanDouble = new double[stableProfile.mean.Length];
        for (int i = 0; i < stableProfile.mean.Length; i++) {
            stableMeanDouble[i] = stableProfile.mean[i];
        }
        stock.SetMeanLCStable(stableMeanDouble);

        stock.SetIsGamePlaying(true);
        marbleSpawner.SetSpawn(true);
    }

    /// <summary>
    /// targetDirection : -1 (left/backward), 0 (Stable), 1 (right/forward)
    /// </summary>
    public float[] GetSimulatedLoadCells(float targetDirection) {

        ForceProfile targetProfile = DetermineTargetProfile(targetDirection);

        // Add noise and weakness to target forces
        for (int i = 0; i < 4; i++) {
            // Base : mean from datasheet
            float val = targetProfile.mean[i];

            // weakness
            if (targetDirection != 0) { 
                float delta = val - stableProfile.mean[i];
                val = stableProfile.mean[i] + (delta * (1.0f - weakness));
            }

            // noise (tremor)
            float noiseRange = (targetProfile.max[i] - targetProfile.min[i]) * 0.5f * tremor;
            float noise = Random.Range(-noiseRange, noiseRange);

            targetForces[i] = val + noise;
        }

        for (int i = 0; i < 4; i++) {
            currentForces[i] = Mathf.Lerp(currentForces[i], targetForces[i], Time.fixedDeltaTime * reactionSpeed);
        }

        return currentForces;
    }

    private ForceProfile DetermineTargetProfile(float requiredDirection) {
        float directionToApply = requiredDirection;

        switch (currentState) {
            case PatientState.Frozen:
                directionToApply = 0; 
                break;
            case PatientState.WrongDirection:
                directionToApply = -requiredDirection; 
                break;
            case PatientState.Spastic:
                if (Random.value > 0.5f) directionToApply = 1;
                else directionToApply = -1;
                break;
            case PatientState.Normal:
            case PatientState.Weak:
                break;
        }

        if (directionToApply > 0.5f) return forwardProfile;     
        if (directionToApply < -0.5f) return backwardProfile;   
        return stableProfile;                                  
    }

    public void ResetPatientDifficulty() {
        // change patient state randomly
        float rand = Random.value;
        if (rand < 0.6f) {
            currentState = PatientState.Normal;
            weakness = Random.Range(0f, 0.2f);
            tremor = Random.Range(0.1f, 0.3f);
        } else if (rand < 0.8f) {
            currentState = PatientState.Weak;
            weakness = Random.Range(0.5f, 0.9f); 
        } else if (rand < 0.9f) {
            currentState = PatientState.Frozen; 
        } else {
            currentState = PatientState.WrongDirection; 
        }
        stock.SetPatientProfile(currentState.ToString());
    }
}