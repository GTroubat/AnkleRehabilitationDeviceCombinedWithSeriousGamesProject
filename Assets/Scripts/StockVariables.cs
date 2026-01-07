using UnityEngine;

public class StockVariables: MonoBehaviour {
    #region Variable Declaration
    private int[] minMaxPitch = { -20, 20 };
    private int[] minMaxRoll = { -20, 20 };
    private int[] minMaxYaw = { -20, 20 };
    private double[] meanLCStable = null;

    private bool stopLoop;
    private bool send;
    private bool startConfig;
    private int height;
    private float pitch;
    private float roll;
    private float yaw;
    private double loadCell1;
    private double loadCell2;
    private double loadCell3;
    private double loadCell4;

    private int motorValue1;
    private int motorValue2;
    private int motorValue3;
    private int motorValue4;

    private float sensitivity;
    private int baseHeight;

    private bool isGamePlaying;

    private EGameMode currentGameMode;

    //private int[] motorStableValues = { 2982, 835, 3261, 1354 };
    //private int[] motorStableValues = { 2502, 1322, 2722, 1951 };
    //private int[] motorStableValues = { 2404, 1545, 2680, 1968 };
    //private int[] motorStableValues = { 2123, 1685, 3019, 1220 };
    //private int[] motorStableValues = { 1948, 1637, 3133, 1312 };
    private int[] motorStableValues = { 2082, 1496, 3246, 1227 };

    private int points;
    private int marbleCounter;

    private float moveSpeedPhoto = 0.4f;
    private float reticuleHeight = -60f;
    private float timeForPhoto = 3.0f;
    private float highlightedOutlineSize = 1.1f;

    private EDisability currentDisability;
    private float assistLevel = 0f;
    private float targetAngle = 0f;
    private float optimalAngle = 0f;
    private float rewards = 0f;
    private int errors = 0;

    private string patientProfile;
    #endregion

    private void Awake() {
        stopLoop = false;
        send = false;
        height = 150;
        pitch = 0;
        roll = 0;
        yaw = 0;
        loadCell1 = 0.0;
        loadCell2 = 0.0;
        loadCell3 = 0.0;
        loadCell4 = 0.0;
        points = 0;
        marbleCounter = 0;
        motorValue1 = motorStableValues[0];
        motorValue2 = motorStableValues[1];
        motorValue3 = motorStableValues[2];
        motorValue4 = motorStableValues[3];
        isGamePlaying = false;
        sensitivity = 0.55f;
        currentGameMode = EGameMode.Adaptative;
        baseHeight = 150;

        currentDisability = EDisability.Slow;
    }

    private void Update() {
        //Debug.Log("stopLoop: " + stopLoop);
        //Debug.Log("send: " + send);
        //Debug.Log("height: " + height);
        //Debug.Log("pitch: " + pitch);
        //Debug.Log("roll: " + roll);
        //Debug.Log("yaw: " + yaw);
        //Debug.Log("MotorValues: " + motorValue1 + ", " + motorValue2 + ", " + motorValue3 + ", " + motorValue4);
        //Debug.Log(isGamePlaying);
        //Debug.Log("Sensitivity: " + sensitivity);
    }
    public void SetStopLoop(bool stoploop) { stopLoop = stoploop; }
    public bool GetStopLoop() { return stopLoop; }

    public void SetSend(bool send) { this.send = send; }
    public bool GetSend() { return send; }

    public void SetStartConfig(bool startConfig) { this.startConfig = startConfig; }
    public bool GetStartConfig() { return startConfig; }

    public void SetHeight(int height) { this.height = height; }
    public int GetHeight() { return height; }

    public void SetPitch(float pitch) { this.pitch = pitch; }
    public float GetPitch() { return this.pitch; }

    public void SetRoll(float roll) { this.roll = roll; }
    public float GetRoll() { return this.roll; }

    public void SetYaw(float yaw) { this.yaw = yaw; }
    public float GetYaw() { return this.yaw; }

    public void SetLoadCell1(double loadCell1) { this.loadCell1 = loadCell1; }
    public double GetLoadCell1() { return loadCell1; }

    public void SetLoadCell2(double loadCell2) { this.loadCell2 = loadCell2; }
    public double GetLoadCell2() { return loadCell2; }

    public void SetLoadCell3(double loadCell3) { this.loadCell3 = loadCell3; }
    public double GetLoadCell3() { return loadCell3; }

    public void SetLoadCell4(double loadCell4) { this.loadCell4 = loadCell4; }
    public double GetLoadCell4() { return loadCell4; }

    public void SetMinMaxPitch(int[] minMaxPitch) { this.minMaxPitch = minMaxPitch; }
    public int[] GetMinMaxPitch() { return minMaxPitch; }

    public void SetMinMaxRoll(int[] minMaxRoll) { this.minMaxRoll = minMaxRoll; }
    public int[] GetMinMaxRoll() { return minMaxRoll; }

    public void SetMinMaxYaw(int[] minMaxYaw) { this.minMaxYaw = minMaxYaw; }
    public int[] GetMinMaxYaw() { return minMaxYaw; }

    public void SetMeanLCStable(double[] meanLCStable) { this.meanLCStable = meanLCStable; }
    public double[] GetMeanLCStable() { return meanLCStable; }

    public void SetPoints(int points) { this.points = points; }
    public int GetPoints() { return points; }

    public void SetMarbleCounter(int marbleCounter) { this.marbleCounter = marbleCounter; }
    public int GetMarbleCounter() { return marbleCounter; }

    public void SetMotorValue1(int value) { this.motorValue1 = value; }
    public int GetMotorValue1() { return motorValue1; }

    public void SetMotorValue2(int value) { this.motorValue2 = value; }
    public int GetMotorValue2() { return motorValue2; }

    public void SetMotorValue3(int value) { this.motorValue3 = value; }
    public int GetMotorValue3() { return motorValue3; }

    public void SetMotorValue4(int value) { this.motorValue4 = value; }
    public int GetMotorValue4() { return motorValue4; }

    public void SetMotorStableValues(int[] values) { this.motorStableValues = values; }
    public int[] GetMotorStableValues() { return motorStableValues; }

    public void SetIsGamePlaying(bool isGamePlaying) { this.isGamePlaying = isGamePlaying; }
    public bool GetIsGamePlaying() { return isGamePlaying; }

    public void SetSensitivity(float sensitivity) { this.sensitivity = sensitivity; }
    public float GetSensitivity() { return sensitivity; }

    public void SetBaseHeight(int baseHeight) { this.baseHeight = baseHeight; }
    public int GetBaseHeight() { return baseHeight; }

    /// <summary>
    /// change the current game mode
    /// </summary>
    /// <param name="mode">must be one of the following string: "Active", "Passive", "Adaptative"</param>
    /// <remarks>
    /// The parameter mode must be one of the following string: "Active", "Passive", "Adaptative"
    /// </remarks>
    public void SetGameMode(EGameMode gameMode) {
        currentGameMode = gameMode;
    }
    public EGameMode GetGameMode() {
        return currentGameMode;
    }

    public void SetTimeForPhoto(float time) { this.timeForPhoto = time; }
    public float GetTimeForPhoto() { return timeForPhoto; }

    public void SetHighlightedOutlineSize(float size) { this.highlightedOutlineSize = size; }
    public float GetHighlightedOutlineSize() { return highlightedOutlineSize; }

    public void SetMoveSpeedPhoto(float speed) { this.moveSpeedPhoto = speed; }
    public float GetMoveSpeedPhoto() { return moveSpeedPhoto; }
    public float GetReticuleHeight() { return reticuleHeight; }
    public void SetReticuleHeight(float height) { this.reticuleHeight = height; }

    public void SetCurrentDisability(EDisability disability) { this.currentDisability = disability; }
    public EDisability GetCurrentDisability() { return currentDisability; }
    public void SetAssistLevel(float level) { this.assistLevel = level; }
    public float GetAssistLevel() { return assistLevel; }
    public void SetTargetAngle(float angle) { this.targetAngle = angle; }
    public float GetTargetAngle() { return targetAngle; }
    public void SetOptimalAngle(float angle) { this.optimalAngle = angle; }
    public float GetOptimalAngle() { return optimalAngle; }
    public void SetRewards(float rewards) { this.rewards = rewards; }
    public float GetRewards() { return rewards; }
    public void SetErrors(int errors) { this.errors = errors; }
    public int GetErrors() { return errors; }

    public void SetPatientProfile(string profile) { this.patientProfile = profile; }
    public string GetPatientProfile() { return patientProfile; }
}
