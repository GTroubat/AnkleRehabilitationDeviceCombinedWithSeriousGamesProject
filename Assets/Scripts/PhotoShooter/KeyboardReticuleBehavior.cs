using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class KeyboardReticuleBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StockVariables stock;
    [SerializeField] private Camera UICamera;
    [SerializeField] private Canvas UICanvas;
    [SerializeField] private Image Reticule;
    [SerializeField] private Image TimerImage;
    [SerializeField] private Image reticuleCenter;
    [SerializeField] private Image targetAnimal;

    [Header("Platform Parameters")]
    [SerializeField] private float rotationSpeed = 0.05f;
    [SerializeField] private int nbMeanValues = 10;

    [Header("Game Parameters")]  
    [SerializeField] private float MoveSpeedKeyboard = 2f;
    [SerializeField] private float MoveSpeedActive = 4f;
    [SerializeField] private float RotationSpeed = 0.01f;
    [SerializeField] private float MoveSpeedPassive = 1f;

    [Header("Passive Mode Parameters")]
    [SerializeField] private CanBePhotoShot[] animalsInOrder;

    [Header("Behavior parameters")]
    [SerializeField] private float HighlightSweepHalfAngle = 10f;
    [SerializeField] private float maxDistance = 20f;

    [Header("Reticule smoothing")]
    [SerializeField] private float reticuleSmoothTime = 0.08f; // lower = faster response, higher = smoother
    private float smoothedReticuleAngle = 0f;
    private float reticuleSmoothVelocity = 0f;

    #region Variable Declaration
    //Active mode variables
    private float gameAngle = 0f;
    private float percentSpeed = 0f;
    private float platformYawAngle = 0f;
    private float forceAngle = 0f;
    private static float valueRapport = 100000;
    private float[] rapport = { valueRapport, valueRapport / 2, valueRapport / 2, valueRapport };
    private float[] rapportWithSensitivity;

    private float[] gameMinMaxRange = { -30f, 30f };
    private float platformAngleRange;

    private double[,] Values;
    private double[] currentMean;
    private int counter;

    // Gameplay variables
    private Ray rayObject;
    private RaycastHit hitObject;
    private CanBePhotoShot[] ObjectsHit;
    private int objectsHitCounter = 0;

    private bool IsInReticule = false;

    //passive mode variables
    private CanBePhotoShot[] allAnimals;
    private int animalCounter = 0;

    // Timer variables
    private float TimeBeforeDestroy;
    private float timeInReticule = 0f;
    #endregion

    private void Start() {
        //Initialize variables
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        allAnimals = GameObject.FindObjectsByType<CanBePhotoShot>(FindObjectsSortMode.None);

        TimeBeforeDestroy = stock.GetTimeForPhoto();
        TimerImage.fillAmount = 0f; //timer image is empty at start
    }

    void FixedUpdate() {
        if (stock.GetGameMode() == EGameMode.Passive) {
            MoveReticuleToAnimalInOrderX(animalsInOrder);
        } else {
            //ChangeReticulePositionKeyboard(); //Activate if you want to control with keyboard
            ChangeReticulePositionPlatform();
            //MoveReticuleToClosestY(); //Doesn't work well, deactivated
        }
        ReticuleCenterRayBehavior();
        RayHighlightBehavior();
        TimeInReticule();
    }

    /// <summary>
    /// For keyboard control of the reticule position in X axis
    /// </summary>
    private void ChangeReticulePositionKeyboard() {
        if (Input.GetKey(KeyCode.LeftArrow)) {
            Reticule.rectTransform.anchoredPosition += new Vector2(-MoveSpeedKeyboard, 0);
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            Reticule.rectTransform.anchoredPosition += new Vector2(MoveSpeedKeyboard, 0);
        }
    }


    #region ActiveMode

    private void ChangeReticulePositionPlatform() { //Just serves to have a cleaner name in FixedUpdate
        FindYaw();
    }

    /// <summary>
    /// Same as the other active modes, calculates and applies the yaw based on load cell values
    /// </summary>
    private void FindYaw() {
        platformAngleRange = stock.GetMinMaxYaw()[1] - stock.GetMinMaxYaw()[0]; //refresh in case of changes in menu

        //Get and store load cell values
        Values[counter % nbMeanValues, 0] = stock.GetLoadCell1();
        Values[counter % nbMeanValues, 1] = stock.GetLoadCell2();
        Values[counter % nbMeanValues, 2] = stock.GetLoadCell3();
        Values[counter % nbMeanValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            for (int i = 0; i < 4; i++) {
                //Refresh rapport according to sensitivity in case of changes in menu
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            ChangeYaw();
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
            for (int j = 0; j < nbMeanValues; j++) {
                sum += values[j, i];
            }
            mean[i] = sum / nbMeanValues;
        }
        return mean;
    }

    /// <summary>
    /// Adjusts the current yaw angle based on the calculated rapport and set a normalized speed percentage
    /// </summary>
    /// <remarks>This method updates the yaw angle by computing a force value derived from the rapport between
    /// the current mean and a reference mean, scaled by sensitivity. The resulting angle is clamped within the allowed
    /// range, and the normalized speed percentage is updated accordingly.</remarks>
    private void ChangeYaw() {
        forceAngle = CalculateRapport(currentMean, stock.GetMeanLCStable(), rapportWithSensitivity);
        gameAngle += forceAngle * rotationSpeed;
        gameAngle = Mathf.Clamp(gameAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
        percentSpeed = gameAngle / (gameMinMaxRange[1] - gameMinMaxRange[0]);
    }

    /// <summary>
    /// Calculates a weighted average rapport score based on the differences between corresponding elements of two
    /// arrays (current mean of load cells and the base at stable position) and a rapport factor.
    /// </summary>
    /// <remarks></remarks>
    /// <param name="array">An array of double values representing the current data set. Must have the same length as <paramref
    /// name="baseArray"/> and <paramref name="rapport"/>. (MeanLCValues)</param>
    /// <param name="baseArray">An array of double values representing the baseline or reference data. Must have the same length as <paramref
    /// name="array"/> and <paramref name="rapport"/>. (MeanLCStable)</param>
    /// <param name="rapport">An array of float values used as divisors to scale the difference between <paramref name="array"/> and <paramref
    /// name="baseArray"/>. Must have the same length as the other arrays. (rapportWithSensitivity)</param>
    /// <returns>The calculated rapport score as a float value, representing the weighted average of
    /// the scaled differences.</returns>
    private float CalculateRapport(double[] array, double[] baseArray, float[] rapport) {
        int columns = array.GetLength(0);
        float[] importance = { -1f, 2f, -2f, 1f }; //weights for each load cell, the two front load cells have more importance
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
    /// Updates the platform's yaw angle and reticule position based on the current game angle and range settings.
    /// </summary>
    /// <remarks>This method synchronizes the platform orientation and reticule display with the current game
    /// state. It applies smoothing to the reticule movement for a visually stable update and ensures the platform's yaw
    /// and reticule height are set accordingly. Call this method when the game angle or range changes to reflect the
    /// new aiming direction.</remarks>
    private void Display() {
        platformYawAngle = stock.GetMinMaxPitch()[0] +
            ((gameAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]); //Map game angle to platform angle
        stock.SetYaw(platformYawAngle); //Send angle to stock

        float reticuleAngle = -gameAngle * 1920f / 60f; //Map game angle to reticule position in pixels (60 degrees of camera field of view = 1920 pixels)

        smoothedReticuleAngle = Mathf.SmoothDamp(smoothedReticuleAngle, reticuleAngle, ref reticuleSmoothVelocity, reticuleSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        Reticule.rectTransform.anchoredPosition = new Vector2(reticuleAngle, stock.GetReticuleHeight()); //Set reticule position
        //Reticule.rectTransform.anchoredPosition = new Vector2(smoothedReticuleAngle, stock.GetReticuleHeight()); //Set smoothed reticule position

        stock.SetSend(true); //Send the values of angles from stock to platform
    }
    #endregion

    /// <summary>
    /// Convert a world position to a local point within the camera canvas.
    /// </summary>
    /// <param name="worldPos"> The position of the object (animal)</param>
    /// <param name="localPoint"> returns the point on the canvas</param>
    /// <returns> Returns false if conversion fails (behind camera or null canvas).</returns>
    private bool TryWorldToCanvasLocalPoint(Vector3 worldPos, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (UICanvas == null) return false;

        // Use a camera to get a screen point from world. For world-to-screen, use UICamera if set, else Camera.main.
        Camera worldCam = UICamera != null ? UICamera : Camera.main;
        if (worldCam == null) return false;

        Vector3 screenPoint = worldCam.WorldToScreenPoint(worldPos);
        if (screenPoint.z <= 0f) return false; // behind camera

        RectTransform canvasRect = UICanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return false; // no rect transform on canvas

        // For ScreenSpaceOverlay the camera parameter must be null.
        Camera uiCam = (UICanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (UICamera != null ? UICamera : Camera.main);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCam, out localPoint); // returns false if outside canvas
        // and localPoint is set to the local position within the canvas
    }

    /// <summary>
    /// Updates the state indicating whether an object that can be photographed is currently centered in the reticule.
    /// </summary>
    /// <remarks>This method casts a ray from the center of the reticule using the UI camera and checks if it
    /// intersects with a game object that implements the <see cref="CanBePhotoShot"/> component. If such an object is
    /// detected, it is marked as being in the reticule, and the <see cref="IsInReticule"/> property is set to <see
    /// langword="true"/>; otherwise, <see cref="IsInReticule"/> is set to <see langword="false"/>.</remarks>
    private void ReticuleCenterRayBehavior() {
        rayObject = UICamera.ScreenPointToRay(Reticule.rectTransform.position);//Create ray from reticule center
        if (Physics.Raycast(rayObject, out hitObject, maxDistance)) {
            if (hitObject.collider.gameObject.TryGetComponent<CanBePhotoShot>(out CanBePhotoShot photoShotComponent)) {
                Debug.Log(hitObject.collider.gameObject.name + " is in the reticule");
                hitObject.collider.gameObject.GetComponent<CanBePhotoShot>().SetIsInReticule();
                IsInReticule = true; //set the boolean to true if an animal (object with CanBePhotoShot) is in the reticule
            } else
                IsInReticule = false;
        } else
            IsInReticule = false;
    }

    /// <summary>
    /// Performs a horizontal ray sweep from the reticule position to detect and highlight nearby objects that can be
    /// photographed.
    /// </summary>
    /// <remarks>This method casts multiple rays in a horizontal arc from the camera's reticule position,
    /// identifying objects that implement the <see cref="CanBePhotoShot"/> component.  Detected objects are stored and
    /// updated to reflect their proximity and distance from the camera. The sweep angle and number of rays are
    /// determined by the configured parameters.</remarks>
    private void RayHighlightBehavior() {
        rayObject = UICamera.ScreenPointToRay(Reticule.rectTransform.position);
        Vector3 origin = rayObject.origin;
        Vector3 baseDirection = rayObject.direction;

        RaycastHit bestHit;
        bool found = false;
        float bestDistance = float.MaxValue;
        ObjectsHit = new CanBePhotoShot[21];
        objectsHitCounter = 0;

        // Safety clamp
        int rayCount = Mathf.Max(1, 20);

        // Sweep from right to left: start at +halfAngle -> -halfAngle
        for (int i = 0; i < rayCount; i++) {
            float t = (rayCount == 1) ? 0.5f : (float)i / (rayCount - 1); // 0..1
            float angle = Mathf.Lerp(HighlightSweepHalfAngle, -HighlightSweepHalfAngle, t);

            // Rotate baseDirection around camera's up axis to sweep horizontally
            Quaternion rot = Quaternion.AngleAxis(angle, UICamera.transform.up);
            Vector3 dir = rot * baseDirection;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance)) {
                if (hit.collider.gameObject.TryGetComponent<CanBePhotoShot>(out CanBePhotoShot photoShotComponent)) {
                    // choose the closest hit among valid ones
                    if (hit.distance < bestDistance) {
                        bestDistance = hit.distance;
                        bestHit = hit;
                        found = true;
                        foreach (CanBePhotoShot animal in ObjectsHit) {
                            Debug.Log(animal + " and " + hit.collider.gameObject.name);
                            if (animal != hit.collider.gameObject.GetComponent<CanBePhotoShot>()) {
                                ObjectsHit[objectsHitCounter] = hit.collider.gameObject.GetComponent<CanBePhotoShot>();
                                objectsHitCounter++;
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (found) {
            for (int i = 0; i < objectsHitCounter; i++) {
                if (ObjectsHit[i] != null) {
                    ObjectsHit[i].SetIsNear();
                    ObjectsHit[i].SetDistance(Vector3.Distance(UICamera.transform.position, ObjectsHit[i].transform.position));
                }
            }
        } 
    }

    /// <summary>
    /// Updates the timer for photographing an object within the reticule and handles the destruction of the object
    /// </summary>
    /// <remarks>This method tracks the duration an object remains centered in the reticule. If the object stays within 3 seconds, 
    /// the object is considered photographed, triggering its destruction and incrementing the animal counter. The timer is visually represented
    /// by a fillable image that updates based on the time spent in the reticule.</remarks>
    private void TimeInReticule() {
        if (IsInReticule) {
            timeInReticule += Time.fixedDeltaTime; //increments the timer
            if (timeInReticule >= TimeBeforeDestroy) { //if the timer reach 3 seconds
                if (hitObject.collider.gameObject.TryGetComponent<CanBePhotoShot>(out CanBePhotoShot photoShotComponent)) {
                    Debug.Log(photoShotComponent.gameObject.name + " has been photo shot");
                    RemoveAnimal(photoShotComponent);
                    photoShotComponent.DestroySelf();
                    animalCounter++;
                }
                timeInReticule = 0f; 
            }
        } else {
            timeInReticule -= Time.fixedDeltaTime; //decrements the timer if no animal is in the reticule
        }
        if (timeInReticule < 0f) 
            timeInReticule = 0f; //clamp the timer to 0
        TimerImage.transform.position = Reticule.transform.position; //updates the position of the timer image to be at the reticule center
        TimerImage.fillAmount = timeInReticule / TimeBeforeDestroy; //updates the fill amount of the timer image

        reticuleCenter.transform.position = Reticule.transform.position; //updates the position of the reticule center image to be at the reticule center
    }

    /// <summary>
    /// Removes the specified animal from the array of animals that can be photographed.
    /// </summary>
    /// <param name="animal">The animal to remove from the array. Cannot be <c>null</c>.</param>
    private void RemoveAnimal(CanBePhotoShot animal) {
        List<CanBePhotoShot> animalList = new List<CanBePhotoShot>(allAnimals);
        animalList.Remove(animal);
        allAnimals = animalList.ToArray();
    }

    /// <summary>
    /// Moves the reticule toward the X position of the targeted animal in the ordered list.
    /// The targeted animal is selected in order using <see cref="animalCounter"/> : index = animalCounter % orderedAnimals.Length.
    /// Uses anchoredPosition (canvas local space) for movement calculations to avoid strange values from WorldToScreenPoint.
    /// </summary>
    private void MoveReticuleToAnimalInOrderX(CanBePhotoShot[] orderedAnimals = null)
    {
        // Choose ordered list (fallback to allAnimals if necessary)
        CanBePhotoShot[] ordered = (orderedAnimals != null && orderedAnimals.Length > 0) ? orderedAnimals : allAnimals;
        if (ordered == null || ordered.Length == 0) return;

        int idx = ((animalCounter % ordered.Length) + ordered.Length) % ordered.Length;
        CanBePhotoShot targetAnimalComponent = ordered[idx];
        if (targetAnimalComponent == null) return;

        Transform targetTransform = targetAnimalComponent.GetReticuleTransform();
        if (targetTransform == null) return;

        // Convert target world position to canvas-local point (anchored coordinates)
        if (!TryWorldToCanvasLocalPoint(targetTransform.position, out Vector2 targetLocal)) {
            return;
        }

        // Current anchored position (canvas local space)
        Vector2 currentAnchored = Reticule.rectTransform.anchoredPosition;

        // Only move on X toward targetLocal.x
        float deltaX = targetLocal.x - currentAnchored.x;

        // Snap if close
        if (Mathf.Abs(deltaX) < 0.5f) {
            Reticule.rectTransform.anchoredPosition = new Vector2(currentAnchored.x + deltaX, stock.GetReticuleHeight());
            return;
        }

        // Move at most MoveSpeedActive (scaled to pixels) per FixedUpdate
        float maxMoveThisFrame = MoveSpeedPassive * 100f * Time.fixedDeltaTime; // tweak scale if needed
        float move = Mathf.Clamp(deltaX, -maxMoveThisFrame, maxMoveThisFrame);

        float newX = currentAnchored.x + move;
        Reticule.rectTransform.anchoredPosition = new Vector2(currentAnchored.x + move, stock.GetReticuleHeight());
        float angle = -newX * 60f / 1920f;
        stock.SetYaw(angle);
        stock.SetSend(true);
    }

    /// <summary>
    /// Unused method to move the reticule vertically toward the closest animal in Y. It was to permit aiming animals that are not at the right Y position.
    /// This method doesn't work
    /// </summary>
    private void MoveReticuleToClosestY() {
        Transform[] animalCenters = new Transform[allAnimals.Length];
        for (int i = 0; i < allAnimals.Length; i++) {
            animalCenters[i] = allAnimals[i].GetReticuleTransform();
        }
        Vector2 closestAnimal = GetClosestAnimal(animalCenters);
        if (closestAnimal != Vector2.zero) {
            Reticule.rectTransform.anchoredPosition += new Vector2(0, closestAnimal.y * RotationSpeed * Time.deltaTime);
        }

    }
    /// <summary>
    /// Unused method to get the closest animal to the reticule in screen space.
    /// </summary>
    /// <param name="animalCenters"></param>
    /// <returns></returns>
    private Vector2 GetClosestAnimal(Transform[] animalCenters) {
        Vector2 closestAnimalVector = Vector2.zero;
        Vector2 closestAnimalPosition = Vector2.zero;

        foreach (var center in animalCenters) {
            Vector3 targetScreen3 = UICamera.WorldToScreenPoint(center.position);
            if (targetScreen3.z <= 0f) continue;
            if (targetScreen3.x < 0f || targetScreen3.x > Screen.width ||
                targetScreen3.y < 0f || targetScreen3.y > Screen.height) continue;
            Vector2 targetScreen = new Vector2(targetScreen3.x, targetScreen3.y);
            Vector2 reticleScreen = RectTransformUtility.WorldToScreenPoint(UICamera, Reticule.rectTransform.position);
            Vector2 delta = targetScreen - reticleScreen; 
            float sqr = delta.sqrMagnitude;
            if (closestAnimalVector == Vector2.zero || sqr < closestAnimalVector.sqrMagnitude) {
                closestAnimalVector = delta;
                closestAnimalPosition = targetScreen;
            }
        }
        targetAnimal.transform.position = closestAnimalPosition;
        return closestAnimalVector;
    }
}
