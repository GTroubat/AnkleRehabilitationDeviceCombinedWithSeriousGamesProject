using System.IO;
using System.Net.WebSockets;
using UnityEngine;

/// <summary>
/// Controls the movement and orientation of a game object using force-based input in both active and passive modes.
/// </summary>
public class ControlByForce1 : MonoBehaviour {
    [Header("General")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private PathGeneratorOld pathGenerator;

    [Header("Active Mode")]
    [SerializeField] private float rotationSpeed = 0.05f;
    [SerializeField] private float playerSpeed = 1f;
    [SerializeField] private int nbMeanValues = 10;

    [Header("Passive Mode")]
    [SerializeField] private float MaxSpeed = 20f;
    [SerializeField] private float platformSpeed = 0.1f;

    #region Declaration of variables
    //Active mode variables
    private float gameAngle = 0f;
    private float percentSpeed = 0f;
    private float platformRollAngle = 0f;
    private float forceAngle = 0f;
    private static float valueRapport = 50000;
    private float[] rapport = { valueRapport, valueRapport/2, valueRapport/2, valueRapport };
    private float[] rapportWithSensitivity;

    private float[] gameMinMaxRange = { -20f, 20f };
    private float platformAngleRange;

    private double[,] Values;
    private double[] currentMean;
    private int counter;

    //Passive mode
    private float lateralOffset = 0f;
    private float command = 0f;

    //General
    private Rigidbody rb;
    private float velocityMax = 8f;
    #endregion

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];
        gameObject.TryGetComponent<Rigidbody>(out rb);
        rb.isKinematic = true;
    }

    private void FixedUpdate() {
        if (stock.GetIsGamePlaying()) rb.isKinematic = false;
        else rb.isKinematic = true;

        if (stock.GetGameMode() == EGameMode.Passive)
            FollowLine();
        else
            FindRoll();
        SlowIfFar();
        limitVelocity();
    }

    #region ActiveMode
    private void FindRoll() {
        platformAngleRange = stock.GetMinMaxRoll()[1] - stock.GetMinMaxRoll()[0];

        Values[counter % nbMeanValues, 0] = stock.GetLoadCell1();
        Values[counter % nbMeanValues, 1] = stock.GetLoadCell2();
        Values[counter % nbMeanValues, 2] = stock.GetLoadCell3();
        Values[counter % nbMeanValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            Debug.Log("Mean not null");
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            ChangeRoll();
            Display();
        } 
    }

    private float CalculateRapport(double[] array, double[] baseArray, float[] rapport) {
        int columns = array.GetLength(0);
        float[] importance = { -2f, -4f, 1.5f, 0.5f};
        double newValue;
        float valuesWithRapport = 0;
        for (int i = 0; i < columns; i++) {
            newValue = (array[i] - baseArray[i]) / rapport[i];
            valuesWithRapport += (float)newValue * importance[i];
        }
        valuesWithRapport = valuesWithRapport / columns;
        return valuesWithRapport;
    }

    private double[] CalculMean(double[,] values) {
        double[] mean = new double[4];
        for (int i = 0; i < 4; i++) {
            double sum = 0.0;
            for (int j = 0; j < nbMeanValues; j++) {
                sum += values[j, i];
            }
            mean[i] = sum / nbMeanValues;
        }
        return mean;
    }

    private void ChangeRoll() {
        forceAngle = CalculateRapport(currentMean, stock.GetMeanLCStable(), rapportWithSensitivity);
        gameAngle += forceAngle * rotationSpeed;
        gameAngle = Mathf.Clamp(gameAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
        percentSpeed = gameAngle / (gameMinMaxRange[1] - gameMinMaxRange[0]);
    }

    private void Display() {
        platformRollAngle = stock.GetMinMaxPitch()[0] +
            ((gameAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]);
        stock.SetRoll(platformRollAngle);
        transform.position -= new Vector3(percentSpeed * playerSpeed, 0f, 0f);
        stock.SetSend(true);
    }
    #endregion

    #region PassiveMode
    /// <summary>
    /// Adjusts the object's position to follow a predefined path by correcting its lateral offset.
    /// </summary>
    /// <remarks>This method calculates the lateral distance from the object's current position to the target
    /// path and applies a corrective movement to reduce the offset. </remarks>
    private void FollowLine() {
        lateralOffset = pathGenerator.GetVerticalDistanceToPath(transform.position);
        if (Mathf.Abs(lateralOffset) > 100) return;
        command = -lateralOffset * MaxSpeed * Time.deltaTime;
        Debug.Log("Lateral Offset: " + lateralOffset + " Command: " + command);
        transform.position += new Vector3(command, 0f, 0f);

        MovePlatform();
    }

    /// <summary>
    /// Updates the platform's roll angle based on the current command
    /// </summary>
    private void MovePlatform() {
        float platformRoll = stock.GetRoll() - command * platformSpeed;
        platformRoll = Mathf.Clamp(platformRoll, stock.GetMinMaxRoll()[0], stock.GetMinMaxRoll()[1]);
        //Debug.Log("Platform Roll: " + platformRoll);
        stock.SetRoll(platformRoll);
        stock.SetSend(true);
    }
    #endregion

    #region General
    private void limitVelocity() {
        Vector3 maxVelocity = new Vector3(velocityMax, velocityMax, velocityMax);
        Vector3 currentVelocity = rb.linearVelocity;
        if (currentVelocity.x > maxVelocity.x) { currentVelocity.x = maxVelocity.x; }
        if (currentVelocity.y > maxVelocity.y) { currentVelocity.y = maxVelocity.y; }
        if (currentVelocity.z > maxVelocity.z) { currentVelocity.z = maxVelocity.z; }
        if (currentVelocity.x < -maxVelocity.x) { currentVelocity.x = -maxVelocity.x; }
        if (currentVelocity.y < -maxVelocity.y) { currentVelocity.y = -maxVelocity.y; }
        if (currentVelocity.z < -maxVelocity.z) { currentVelocity.z = -maxVelocity.z; }
        gameObject.GetComponent<Rigidbody>().linearVelocity = currentVelocity;
    }

    private void SlowIfFar() {
        pathGenerator.GetClosestPoint(transform.position, out float distance, out float sampleIndexNormalized);
        if (distance > 2f) {
            velocityMax = Mathf.Clamp(10f - distance / 2, 0f, 10f);
        } else velocityMax = 8f;
    }
    #endregion
}
