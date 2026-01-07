using System.Drawing.Text;
using UnityEngine;

public class AdaptativeControlMode: MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private int tubeNumber = 3;
    [SerializeField] private float passiveSpeed = 0.01f;
    [SerializeField] private float activeSpeed = 0.015f;
    [SerializeField] private float assistanceSpeed = 0.01f;

    //Passive mode variables
    private readonly float heightChangePlatform = 1.6f;

    private float targetAngle;
    private float currentAngle;
    private float error;
    private float newAngle;

    private Marble currentMarble = null;

    float signedCurrentAngle;

    //Active mode variables
    private float gamePitchAngle = 0f;
    private float platformPitchAngle = 0f;
    private float forceAngle = 0f;
    private float[] rapport = { 20000, 4000, 4000, 20000 };
    private float[] rapportWithSensitivity;

    private float[] gameMinMaxRange = { -20f, 20f };
    private float platformAngleRange;

    [SerializeField] private int nbValues = 10;
    private double[,] Values;
    private double[] currentMean;
    private int counter;

    // Assisting mode variables
    private int nbConsecutiveErrors = 0;
    private int previousPoints = 0;
    private int previousMarbleCount = 0;

    private enum Side {
        Left,
        Right,
        Unknown
    }
    private Side side = Side.Unknown;

    private enum AdaptationLevel {
        Active,
        Assistance1,
        Assistance2,
        Passive
    }
    private AdaptationLevel level;

    private enum AdaptationDirection {
        Left,
        midLeft,
        Right,
        midRight,
        None
    }
    private AdaptationDirection direction;

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];
        level = AdaptationLevel.Active;
        direction = AdaptationDirection.None;
    }

    private void FixedUpdate() {
        level = FindAdaptativeLevel();

        if (stock.GetIsGamePlaying() && ((stock.GetGameMode() == EGameMode.Adaptative) 
            && (level == AdaptationLevel.Passive)) || stock.GetGameMode() == EGameMode.Passive) {
            currentMarble = marbleSpawner.GetCurrentMarble();
            if (currentMarble != null) {
                AdjustBoardToMarblePosition(currentMarble);
            }
            Debug.Log("Passive Mode");
        }
        else if (stock.GetIsGamePlaying() && ((stock.GetGameMode() == EGameMode.Adaptative)
            && (level == AdaptationLevel.Active)) || stock.GetGameMode() == EGameMode.Active) {
            FindPitch();
            Debug.Log("Active Mode");
        }
        else if (stock.GetIsGamePlaying() && (stock.GetGameMode() == EGameMode.Adaptative)
            && (level == AdaptationLevel.Assistance1)) {
            Assistance(1);
            Debug.Log("Assistance Level 1");
        }
        else if (stock.GetIsGamePlaying() && (stock.GetGameMode() == EGameMode.Adaptative)
            && (level == AdaptationLevel.Assistance2)) {
            Assistance(2);
            Debug.Log("Assistance Level 2");
        }
    }

    #region assistance mode functions
    /// <summary>
    /// Applies assistance logic based on the specified assistance level
    /// </summary>
    /// <remarks>This method collects and processes load cell sensor data, updates internal calculations, and
    /// applies assistance if the system is in a valid state (i.e., stable sensor readings and an active game session).
    /// The assistance level influences how much support is provided during operation.</remarks>
    /// <param name="assitanceLevel">The level of assistance to apply. Higher values increase the degree of assistance provided by the system.</param>
    private void Assistance(int assitanceLevel) {
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 1f / 0.5f));
            }
            currentMean = CalculMean(Values);
            direction = FindDirection();
            AddAssistance(assitanceLevel);
            Display();
        }
    }

    /// <summary>
    /// Determines the adaptation direction for the current marble based on its color and position.
    /// </summary>
    /// <returns>An <see cref="AdaptationDirection"/> value indicating the direction the marble should adapt to. Returns <see
    /// cref="AdaptationDirection.None"/> if no direction is determined.</returns>
    private AdaptationDirection FindDirection() {
        AdaptationDirection direction = AdaptationDirection.None;
        currentMarble = marbleSpawner.GetCurrentMarble();
        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        switch (marbleColor) {
            case EColors.Red:
                if (tubeNumber == 3) {
                    direction = AdaptationDirection.Left;
                }
                break;
            case EColors.Yellow:
                if (tubeNumber == 3) {
                    direction = AdaptationDirection.Right;
                }
                break;
            case EColors.Green:
                // nothing right now
                break;
            case EColors.Blue:
                if (tubeNumber == 3) {
                    // use signed angle of current rotation to decide side
                    signedCurrentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);                    
                    if (side == Side.Unknown) {
                        side = (signedCurrentAngle > 0f) ? Side.Left : Side.Right;
                    }
                    if (side == Side.Left) {
                        if (marblePos.y > heightChangePlatform) direction = AdaptationDirection.midLeft;
                        else direction = AdaptationDirection.Right;
                    } else if (side == Side.Right) {
                        if (marblePos.y > heightChangePlatform) direction = AdaptationDirection.midRight;
                        else direction = AdaptationDirection.Left;
                    }
                }
                break;
        }
        return direction;
    }

    /// <summary>
    /// Applies an assistance adjustment to the game pitch angle based on the specified assistance level and current
    /// adaptation direction.
    /// </summary>
    /// <remarks>The adjustment is determined by both the assistance level and the current adaptation
    /// direction. The resulting pitch angle is clamped to remain within the allowed game range.</remarks>
    /// <param name="assistancelevel">The level of assistance to apply. Valid values are 1 or 2.</param>
    private void AddAssistance(int assistancelevel) {
        forceAngle = CalculateRapport(currentMean, stock.GetMeanLCStable(), rapportWithSensitivity);
        gamePitchAngle -= forceAngle * assistanceSpeed;

        switch (assistancelevel) {
            case 1:
                switch (direction) {
                    case AdaptationDirection.Left:
                        gamePitchAngle += 0.05f;
                        break;
                    case AdaptationDirection.midLeft:
                        if (signedCurrentAngle < 0.5 * stock.GetMinMaxPitch()[1])
                            gamePitchAngle += 0.05f;
                        else
                            gamePitchAngle -= 0.05f;
                        break;
                    case AdaptationDirection.Right:
                        gamePitchAngle -= 0.05f;
                        break;  
                    case AdaptationDirection.midRight:
                        if (signedCurrentAngle > 0.5 * stock.GetMinMaxPitch()[0])
                            gamePitchAngle -= 0.05f;
                        else
                            gamePitchAngle += 0.05f;
                        break;
                    case AdaptationDirection.None:
                        break;
                }
                break;
            case 2:
                switch (direction) {
                    case AdaptationDirection.Left:
                        gamePitchAngle += 0.1f;
                        break;
                    case AdaptationDirection.midLeft:
                        if (signedCurrentAngle < 0.5 * stock.GetMinMaxPitch()[1])
                            gamePitchAngle += 0.1f;
                        else
                            gamePitchAngle -= 0.1f;
                        break;
                    case AdaptationDirection.Right:
                        gamePitchAngle -= 0.1f; 
                        break;
                    case AdaptationDirection.midRight:
                        if (signedCurrentAngle > 0.5 * stock.GetMinMaxPitch()[0])
                            gamePitchAngle -= 0.1f;
                        else
                            gamePitchAngle += 0.1f;
                        break;
                    case AdaptationDirection.None:
                        break;
                }
                break;
        }

        gamePitchAngle = Mathf.Clamp(gamePitchAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
    }

    /// <summary>
    /// Determines the next adaptation level based on the current error count .
    /// </summary>
    /// <remarks>The adaptation level is selected according to the number of consecutive errors detected. The
    /// method updates internal counters based on changes in marble count and points, and returns the appropriate <see
    /// cref="AdaptationLevel"/> value. </remarks>
    /// <returns>The <see cref="AdaptationLevel"/> that reflects the level of assistance.</returns>
    private AdaptationLevel FindAdaptativeLevel() {
        AdaptationLevel nextAdaptationLevel = level;
        if (stock.GetMarbleCounter() > previousMarbleCount) {
            if (stock.GetPoints() == previousPoints) {
                nbConsecutiveErrors++;
            } else {
                nbConsecutiveErrors--;
                nbConsecutiveErrors = Mathf.Clamp(nbConsecutiveErrors, 0, 10);
            }
        } 

        if (nbConsecutiveErrors >= 6) {
            nextAdaptationLevel = AdaptationLevel.Passive;
        } else if (nbConsecutiveErrors >= 4) {
            nextAdaptationLevel = AdaptationLevel.Assistance2;
        } else if (nbConsecutiveErrors >= 2) {
            nextAdaptationLevel = AdaptationLevel.Assistance1;
        } else {
            nextAdaptationLevel = AdaptationLevel.Active;
        }

        previousPoints = stock.GetPoints();
        previousMarbleCount = stock.GetMarbleCounter();
        return nextAdaptationLevel;
    }
    #endregion

    #region active mode functions
    /// <summary>
    /// Updates the pitch calculation based on the latest load cell readings and current system state.
    /// </summary>
    /// <remarks>This method collects new load cell data, updates internal buffers, and, if the system is in a
    /// stable and active state, recalculates the pitch using the most recent values and sensitivity settings.</remarks>
    private void FindPitch() {
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];//refresh in case of changes in menu

        //Get and store load cell values
        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            for (int i = 0; i < 4; i++) {
                //Refresh ratio according to sensitivity in case of changes in menu
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            ChangePitch();
            Display();
        }
    }

    /// <summary>
    /// Calculate the mean of the rows of a 2D array
    /// </summary>
    /// <param name="values"> The base 2D array </param>
    /// <returns> The mean in 1D array </returns>
    private double[] CalculMean(double[,] values) {
        double[] mean = new double[4];
        for (int i = 0; i < 4; i++) {
            double sum = 0.0;
            for (int j = 0; j < nbValues; j++) {
                sum += values[j, i];
            }
            mean[i] = sum / nbValues;
        }
        return mean;
    }

    /// <summary>
    /// Adjusts the current pitch angle based on the calculated force and active speed.
    /// </summary>
    /// <remarks>This method recalculates the pitch angle by applying a force determined from the current mean, a
    /// reference mean, and a sensitivity factor. The resulting angle is clamped to stay within the defined minimum and
    /// maximum pitch limits.</remarks>
    private void ChangePitch() {
        forceAngle = CalculateRapport(currentMean, stock.GetMeanLCStable(), rapportWithSensitivity);
        gamePitchAngle -= forceAngle * activeSpeed;
        //Debug.Log("forceAngle wirh force: " + (forceAngle * speed));
        gamePitchAngle = Mathf.Clamp(gamePitchAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
    }

    /// <summary>
    /// Calculates a weighted average of the normalized differences between two arrays ( (current mean of load cells and the base 
    /// at stable position) using a rapport factor.
    /// </summary>
    /// <remarks>The calculation applies a fixed set of importance weights to each element. </remarks>
    /// <param name="array">The array of current values to compare.</param>
    /// <param name="baseArray">The array of baseline values to use as a reference for comparison. Must have the same length as <paramref
    /// name="array"/>.</param>
    /// <param name="rapport">An array of rapport factors used to normalize the differences between <paramref name="array"/> and <paramref
    /// name="baseArray"/>. Must have the same length as <paramref name="array"/>.</param>
    /// <returns>The weighted average of the normalized differences as a single-precision floating-point value.</returns>
    private float CalculateRapport(double[] array, double[] baseArray, float[] rapport) {
        int columns = array.GetLength(0);
        float[] importance = {0.75f, 1.25f, 1.25f, 0.75f}; //weights for each load cell, the two front load cells have more importance
        double newValue; //temporary variable to store the new calculated value for each load cell
        float valuesWithRapport = 0; //final value to return
        for (int i = 0; i < columns; i++) {
            newValue = (array[i] - baseArray[i]) / rapport[i]; //calculate the weighted value for each load cell according to its rapport
            valuesWithRapport += (float)newValue * importance[i]; //add the weighted value to the final value according to its importance
        }
        valuesWithRapport = valuesWithRapport / columns; //normalize the final value
        return valuesWithRapport;
    }

    /// <summary>
    /// Updates the platform's pitch angle and rotation based on the current game pitch angle.
    /// </summary>
    /// <remarks>This method calculates the corresponding platform pitch angle from the game pitch angle,
    /// applies it to the platform, and updates the platform's rotation. </remarks>
    public void Display() {
        platformPitchAngle = stock.GetMinMaxPitch()[0] +
            ((gamePitchAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]); // Map game angle to platform angle
        stock.SetPitch(platformPitchAngle); //Send angle to stock
        transform.rotation = Quaternion.Euler(0f, 0f, gamePitchAngle); //apply the rotation to the game platform
        stock.SetSend(true); //Apply the rotation to the physical platform
    }
    #endregion

    #region passive mode functions
    /// <summary>
    /// Adjusts the board's orientation to align with the specified marble's current location and color.
    /// </summary>
    /// <remarks>The adjustment logic depends on the marble's color and position, as well as the current board state. </remarks>
    /// <param name="currentMarble">The marble whose position and color determine how the board should be adjusted. Cannot be <c>null</c>.</param>
    private void AdjustBoardToMarblePosition(Marble currentMarble) {
        Vector3 marblePos = currentMarble.transform.position;//get marble position
        EColors marbleColor = currentMarble.GetColor();//get marble color
        switch (marbleColor) {
            case EColors.Red:
                if (tubeNumber == 3) {
                    if (marblePos.y > heightChangePlatform) {
                        GoFullLeft();
                    } else {
                        GoFullLeft();
                    }
                }
                break;
            case EColors.Yellow:
                if (tubeNumber == 3) {
                    if (marblePos.y > heightChangePlatform) {
                        GoFullRight();
                    } else {
                        GoFullRight();
                    }
                }
                break;
            case EColors.Green:
                // nothing right now
                break;
            case EColors.Blue:
                if (tubeNumber == 3) {
                    // use signed angle of current rotation to decide side
                    float signedCurrentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
                    if (side == Side.Unknown) {
                        side = (signedCurrentAngle > 0f) ? Side.Left : Side.Right;
                    }
                    if (side == Side.Left) {
                        if (marblePos.y > heightChangePlatform) GoMidLeft();
                        else GoFullRight();
                    } else if (side == Side.Right) {
                        if (marblePos.y > heightChangePlatform) GoMidRight();
                        else GoFullLeft();
                    }
                }
                break;
        }

        // Clamp pitch in stock (signed) domain, then apply conversion to Unity angle when setting transform
        int[] minMax = stock.GetMinMaxPitch();
        float clampedPitch = Mathf.Clamp(stock.GetPitch(), (float)minMax[0], (float)minMax[1]);
        stock.SetPitch(clampedPitch);

        float unityAngle = ToUnityAngle(clampedPitch);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoFullRight() {
        targetAngle = stock.GetMinMaxPitch()[0]; // signed target (-..+)

        // current angle read from Transform (0..360), convert to signed (-180..180) for calculation
        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        // shortest signed error from current to target
        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * passiveSpeed;
        // store new consigne (signed) in stock and apply to transform as Unity angle
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        //Debug.Log("New Angle (signed): " + newAngle + " => Unity: " + unityAngle);
        stock.SetSend(true);
    }

    private void GoFullLeft() {
        targetAngle = stock.GetMinMaxPitch()[1];

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * passiveSpeed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoMidRight() {
        targetAngle = stock.GetMinMaxPitch()[0] / 3f;

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * passiveSpeed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoMidLeft() {
        targetAngle = stock.GetMinMaxPitch()[1] / 3f;

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * passiveSpeed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }
    #endregion

    // Convert Unity 0..360 angle to signed -180..180
    private float ToSignedAngle(float unityAngle) {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    // Convert signed -180..180 angle to Unity 0..360
    private float ToUnityAngle(float signedAngle) {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }

    /// <summary>
    /// Resets the current side to an unknown state.
    /// </summary>
    /// <remarks>Use this method to clear any previously set side value, restoring it to its default,
    /// unspecified state.</remarks>
    public void ResetSide() {
        side = Side.Unknown;
    }
}