using UnityEngine;
using System;
using System.Numerics;

public class MathematicalModel : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    private const float circomference = 58*Mathf.PI; // mm
    private const float turnTicks = 4096;
    private const float length = 280;
    private const float width = 120;

    private float height;
    private float pitch;
    private float roll;
    private float yaw;

    private float phi;
    private float theta;
    private float psi;

    private float[] pivotPoint = { 0, 0, 0};
    private float[] changeBase = { 0, 0, 0 };
    private int[] cableLengthHeight0 = { 360, 360, 360, 360 };
    //private int[] cableLengthHeight0 = { 358, 363, 364, 362 };
    private int[] motorStableValues;
    //private int[] motorMaxValues = { -1831, 5602, -1691, 6443 };
    private int[] motorMaxValues = { -2859, 6541, -2293, 6459 };
    private float[,] baseF = new float[4, 3];
    private float[,] baseM = new float[4, 3];
    private float[,] baseFp = new float[4, 3];
    private float[,] baseFpp = new float[4, 3];
    private float[,] baseFf = new float[4, 3];
    private float[,] cableVectors = new float[4, 3];
    private float[] cableLengths = new float[4];
    private int[] goalPositions = new int[4];
    private int[] clampeGoalPosition = new int[4];
    private float[,] rotationMatrix;

    void Start() { 
        height = 150;
        pitch = 0;
        roll = 0;
        yaw = 0;
        baseF = new float[4,3] { { -35 + changeBase[0], 60 + changeBase[1], 80 + changeBase[2] }, 
            { 245 + changeBase[0], 60 + changeBase[1], 80 + changeBase[2] }, 
            { 245 + changeBase[0], -60 + changeBase[1], 80 + changeBase[2] }, 
            { -35 + changeBase[0], -60 + changeBase[1], 80 + changeBase[2] } };
        motorStableValues = stock.GetMotorStableValues();
    }

    private void FixedUpdate() {
        height = stock.GetHeight();
        pitch = stock.GetPitch();
        roll = stock.GetRoll();
        yaw = stock.GetYaw();
        phi = ToRadian(roll);
        psi = ToRadian(yaw);
        theta = ToRadian(-pitch);
        baseM = new float[4, 3] { { -28, 150, -285 + height }, {195, 150, -285 + height },
            {195, -150, -285 + height }, {-28, -150, -285 + height } };
        rotationMatrix = EulerRotationMatrix(phi, theta, psi);
        baseFp = SubstractVectorToMatrix(baseF, pivotPoint);
        //baseAp = new float[4, 3] { { baseA[0, 0] - pivotPoint[0], baseA[0, 1] - pivotPoint[0], baseA[0, 2] - pivotPoint[0] },
        //    { baseA[1, 0] - pivotPoint[1], baseA[1, 1] - pivotPoint[1], baseA[1, 2] - pivotPoint[1]  },
        //    { baseA[2, 0] - pivotPoint[2], baseA[2, 1] - pivotPoint[2], baseA[2, 2] - pivotPoint[2]  },
        //    { baseA[3, 0] - pivotPoint[3], baseA[3, 1] - pivotPoint[3], baseA[3, 2] - pivotPoint[3]  } };
        baseFpp = MultiplyMatrices(baseFp, rotationMatrix);
        baseFf = AddVectorToMatrix(baseFpp, pivotPoint);
        cableVectors = SubstractTwoMatrix(baseFf, baseM);
        cableLengths = VectorNormOfMatrix(cableVectors);
        int[] signVector = { -1, 1, -1, 1 };
        goalPositions = LengthToMotorPosition(cableLengths, signVector, motorStableValues, cableLengthHeight0);
        clampeGoalPosition = ClampArray(goalPositions, motorStableValues, motorMaxValues);
        stock.SetMotorValue1(clampeGoalPosition[0]);
        stock.SetMotorValue2(clampeGoalPosition[1]);
        stock.SetMotorValue3(clampeGoalPosition[2]);
        stock.SetMotorValue4(clampeGoalPosition[3]);
    }

    public float[] GetCableLengths() {
        return cableLengths;
    }

    public float[,] GetCableVectors() {
        return cableVectors;
    }

    private float ToRadian(float angle) {
        return angle * Mathf.PI / 180;
    }

    private float[,] EulerRotationMatrix(float phi, float theta, float psi) {
        float[,] rotationMatrix = new float[3, 3];
        rotationMatrix[0,0] = Mathf.Cos(psi) * Mathf.Cos(theta);
        rotationMatrix[0,1] = Mathf.Cos(psi) * Mathf.Sin(theta) * Mathf.Sin(phi) - Mathf.Sin(psi) * Mathf.Cos(phi);
        rotationMatrix[0,2] = Mathf.Cos(psi) * Mathf.Sin(theta) * Mathf.Cos(phi) + Mathf.Sin(psi) * Mathf.Sin(phi);

        rotationMatrix[1,0] = Mathf.Sin(psi) * Mathf.Cos(theta);
        rotationMatrix[1,1] = Mathf.Sin(psi) * Mathf.Sin(theta) * Mathf.Sin(phi) + Mathf.Cos(psi) * Mathf.Cos(phi);
        rotationMatrix[1,2] = Mathf.Sin(psi) * Mathf.Sin(theta) * Mathf.Cos(phi) - Mathf.Cos(psi) * Mathf.Sin(phi);

        rotationMatrix[2,0] = -Mathf.Sin(theta);
        rotationMatrix[2,1] = Mathf.Cos(theta) * Mathf.Sin(phi);
        rotationMatrix[2,2] = Mathf.Cos(theta) * Mathf.Cos(phi);

        return rotationMatrix;
    }

    private float[,] MultiplyMatrices(float[,] matrix1, float[,] matrix2) {
        // DMatrix Dimensions
        int matrix1Rows = matrix1.GetLength(0); 
        int matrix1Cols = matrix1.GetLength(1); 
        int matrix2Rows = matrix2.GetLength(0); 
        int matrix2Cols = matrix2.GetLength(1); 

        // Vérification de la compatibilité des dimensions
        if (matrix1Cols != matrix2Rows) {
            throw new InvalidOperationException(
                "Multiplication Impossible. " +
                "Columns number of the first matrix must be equal to the lines number of the second."
            );
        }

        float[,] resultMatrix = new float[matrix1Rows, matrix2Cols];

        for (int i = 0; i < matrix1Rows; i++) 
        {
            for (int j = 0; j < matrix2Cols; j++) 
            {
                float sum = 0;
                for (int k = 0; k < matrix1Cols; k++) 
                {
                    sum += matrix1[i, k] * matrix2[k, j];
                }
                resultMatrix[i, j] = sum;
            }
        }

        return resultMatrix;
    }

    private float[,] AddVectorToMatrix(float[,] matrix, float[] vector) {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        if (vector.Length != cols) {
            throw new InvalidOperationException(
                "Addition Impossible. " +
                "Vector length must be equal to the number of rows in the matrix."
            );
        }
        float[,] resultMatrix = new float[rows, cols];
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
                resultMatrix[i, j] = matrix[i, j] + vector[j];
            }
        }
        return resultMatrix;
    }

    private float[,] SubstractVectorToMatrix(float[,] matrix, float[] vector) {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        if (vector.Length != cols) {
            throw new InvalidOperationException(
                "Subtraction Impossible. " +
                "Vector length must be equal to the number of rows in the matrix."
            );
        }
        float[,] resultMatrix = new float[rows, cols];
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
                resultMatrix[i, j] = matrix[i, j] - vector[j];
            }
        }
        return resultMatrix;
    }

    private float[,] AddTwoMatrix(float[,] matrix1, float[,] matrix2) {
        int rows1 = matrix1.GetLength(0);
        int cols1 = matrix1.GetLength(1);
        int rows2 = matrix2.GetLength(0);
        int cols2 = matrix2.GetLength(1);
        if (rows1 != rows2 || cols1 != cols2) {
            throw new InvalidOperationException(
                "Addition Impossible. " +
                "Matrices must have the same dimensions."
            );
        }
        float[,] resultMatrix = new float[rows1, cols1];
        for (int i = 0; i < rows1; i++) {
            for (int j = 0; j < cols1; j++) {
                resultMatrix[i, j] = matrix1[i, j] + matrix2[i, j];
            }
        }
        return resultMatrix;
    }

    private float[,] SubstractTwoMatrix(float[,] matrix1, float[,] matrix2) {
        int rows1 = matrix1.GetLength(0);
        int cols1 = matrix1.GetLength(1);
        int rows2 = matrix2.GetLength(0);
        int cols2 = matrix2.GetLength(1);
        if (rows1 != rows2 || cols1 != cols2) {
            throw new InvalidOperationException(
                "Subtraction Impossible. " +
                "Matrices must have the same dimensions."
            );
        }
        float[,] resultMatrix = new float[rows1, cols1];
        for (int i = 0; i < rows1; i++) {
            for (int j = 0; j < cols1; j++) {
                resultMatrix[i, j] = matrix1[i, j] - matrix2[i, j];
            }
        }
        return resultMatrix;
    }

    private float[] VectorNormOfMatrix(float[,] matrix) {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        float[] norms = new float[rows];
        for (int i = 0; i < rows; i++) {
            float sum = 0;
            for (int j = 0; j < cols; j++) {
                sum += matrix[i, j] * matrix[i, j];
            }
            norms[i] = Mathf.Sqrt(sum);
        }
        return norms;
    }

    private int[] LengthToMotorPosition(float[] lengths, int[] signVector, int[] basePositions, int[] baseCableLength) {
        int[] motorPositions = new int[lengths.Length];
        for (int i = 0; i < lengths.Length; i++) {
            motorPositions[i] = Mathf.RoundToInt(basePositions[i] + signVector[i] *
                (baseCableLength[i] - lengths[i]) * turnTicks / circomference);
        }
        return motorPositions;
    }

    private int[] ClampArray(int[] array, int[] limitMin, int[] limitMax) {
        int[] limitedArray = new int[array.Length];
        if ((array.Length != limitMax.Length) && (array.Length != limitMin.Length)) {
            throw new InvalidOperationException(
                "Impossible to limit array. " +
                "Limit vectors must be of the same size of the array."
            );
        }
        // Clamp max values
        limitedArray[0] = Mathf.Max(array[0], limitMax[0]);
        limitedArray[1] = Mathf.Min(array[1], limitMax[1]);
        limitedArray[2] = Mathf.Max(array[2], limitMax[2]);
        limitedArray[3] = Mathf.Min(array[3], limitMax[3]);

        // Clamp min values
        limitedArray[0] = Mathf.Min(limitedArray[0], limitMin[0]);
        limitedArray[1] = Mathf.Max(limitedArray[1], limitMin[1]);
        limitedArray[2] = Mathf.Min(limitedArray[2], limitMin[2]);
        limitedArray[3] = Mathf.Max(limitedArray[3], limitMin[3]);

        if (array != limitedArray) {
            //Debug.Log("Warning: Motor position out of range. Clamped to min or max value");
        }
        return limitedArray;
    }

    private void Display1DArray(float[] array, string text) {
        int columns = array.GetLength(0);

        for (int i = 0; i < columns; i++) {
            Debug.Log(text  + " Index: " + i + "; value: " + array[i] + "\n");
        }
    }
    private void Display1DArray(int[] array, string text) {
        int columns = array.GetLength(0);

        for (int i = 0; i < columns; i++) {
            Debug.Log(text + " Index: " + i + "; value: " + array[i] + "\n");
        }
    }

    private void Display2DArray(float[,] array, string text) {
        int columns = array.GetLength(0);
        int rows = array.GetLength(1);
        for (int i = 0; i < columns; i++) {
            for (int j = 0; j < rows; j++) {
                Debug.Log(text + " Index: " + i + "," + j + "; value: " + array[i, j] + "\n");
            }
        }
    }
}
