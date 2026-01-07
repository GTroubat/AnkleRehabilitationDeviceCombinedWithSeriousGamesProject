using System.Drawing.Text;
using UnityEngine;

public class ControlByForce2With5Steps : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private float speed = 0.3f;
    [SerializeField] private int nbMeanValues;
    [SerializeField] private PathGeneratorOld pathGenerator;

    private static float valueRapport = 50000; // To change from one step to another
    private float[] rapport = { valueRapport, valueRapport, valueRapport, valueRapport };
    private float[] rapportWithSensitivity;

    private enum Steps {
        Stable,
        SlightlyRight,
        SlightlyLeft,
        Right,
        Left
    }
    private Steps currentStep;


    private double[,] Values;
    private double[] currentMean;
    private int counter;

    private Rigidbody rb;
    private float velocityMax = 10f;

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        gameObject.TryGetComponent<Rigidbody>(out rb);
        rb.isKinematic = true;
    }

    private void FixedUpdate() {
        FindRoll();
        SlowIfFar();
        limitVelocity();
    }

    private void FindRoll() {
        Values[counter % nbMeanValues, 0] = stock.GetLoadCell1();
        Values[counter % nbMeanValues, 1] = stock.GetLoadCell2();
        Values[counter % nbMeanValues, 2] = stock.GetLoadCell3();
        Values[counter % nbMeanValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            rb.isKinematic = false;
            Debug.Log("Mean not null");
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            currentStep = FindStep();
            ApplyForceAccordingToStep();
            stock.SetSend(true);
        } else
            rb.isKinematic = true;
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

    private Steps FindStep() {
        float[] currentDifference = new float[4];
        Steps step;

        for (int i = 0; i < 4; i++) {
            currentDifference[i] = (float)(currentMean[i] - stock.GetMeanLCStable()[i]);
        }
        Debug.Log("Current Differences: " + currentDifference[0] + ", " + currentDifference[1] + ", " +
                  currentDifference[2] + ", " + currentDifference[3]);

        if (currentDifference[0] < -2*rapportWithSensitivity[0] && currentDifference[1] < -2*rapportWithSensitivity[1] &&
            currentDifference[3] > 2*rapportWithSensitivity[3] && currentDifference[4] > 2*rapportWithSensitivity[4]) {
            step = Steps.Left;
        } else if (currentDifference[0] > 2*rapportWithSensitivity[0] && currentDifference[1] > 2*rapportWithSensitivity[1] &&
                   currentDifference[3] > -2*rapportWithSensitivity[3] && currentDifference[4] < -2*rapportWithSensitivity[4]) {
            step = Steps.Right;
        } else if (currentDifference[0] < -rapportWithSensitivity[0] && currentDifference[1] < -rapportWithSensitivity[1] &&
            currentDifference[3] > rapportWithSensitivity[3] && currentDifference[4] > rapportWithSensitivity[4]) {
            step = Steps.SlightlyLeft;
        } else if (currentDifference[0] > rapportWithSensitivity[0] && currentDifference[1] > rapportWithSensitivity[1] &&
                   currentDifference[3] < -rapportWithSensitivity[3] && currentDifference[4] < -rapportWithSensitivity[4]) {
            step = Steps.SlightlyRight;
        } else {
            step = Steps.Stable;
        }
        Debug.Log("Current Step: " + step.ToString());

        return step;
    }

    private void ApplyForceAccordingToStep() {
        Vector3 force = Vector3.zero;
        switch (currentStep) {
            case Steps.Stable:
                stock.SetRoll(0);
                break;
            case Steps.SlightlyRight:
                stock.SetRoll(0.5f * stock.GetMinMaxRoll()[1]);
                force = Vector3.right * 0.5f * speed;
                break;
            case Steps.SlightlyLeft:
                stock.SetRoll(0.5f * stock.GetMinMaxRoll()[0]);
                force =  Vector3.left * 0.5f * speed;
                break;
            case Steps.Right:
                stock.SetRoll(stock.GetMinMaxRoll()[1]);
                force = Vector3.right * speed;
                break;
            case Steps.Left:
                stock.SetRoll(stock.GetMinMaxRoll()[0]);
                force = Vector3.left * speed;
                break;
        }
        rb.AddForce(force, ForceMode.Acceleration);
    }

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
        if (distance > 6f) {
            velocityMax = 10f - distance / 10;
        } else velocityMax = 10f;
    }
}
