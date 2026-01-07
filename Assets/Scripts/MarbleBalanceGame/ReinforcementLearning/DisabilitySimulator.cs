using UnityEngine;
using System;

/// <summary>
/// This class has for objective to simulate a disability to train the AI by reinforcement learning
/// </summary>
public class DisabilitySimulator : MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private AIForAdaptativeMode ai;
    [SerializeField] private AdaptiveRewardAgent adaptiveRewardAgent;
    [SerializeField] private Platform[] plateformes;
    [SerializeField] private Tube[] tubes;

    [SerializeField] float maxSpeed = 0.01f;

    private readonly float heightChangePlatform = 1.6f;

    private float[] limitsLimitedDisability = new float[2];
    private float[] maxAngles = { -20, 20 };
    private float speedSlow;
    private float probaWrongMovement;
    private float FixedAngle;

    private Marble currentMarble;

    private enum Side {
        Left,
        Right,
        Unknown
    }
    private Side side = Side.Unknown;

    private enum Direction {
        FullRight,
        FullLeft,
        MidRight,
        MidLeft,
        Stable
    }
    private Direction direction;

    private EDisability currentDisability;

    private void Start() {
        stock.SetIsGamePlaying(true);
        marbleSpawner.SetSpawn(true);
        ResetDisability();
    }

    private void FixedUpdate() {
        float targetAngle = 0f;
        float moveSpeed = maxSpeed;

        GetOptimalDirection(currentMarble);
        switch (currentDisability) {
            case EDisability.Slow:
                if (direction == Direction.FullRight) {
                    targetAngle = maxAngles[0];
                    moveSpeed = speedSlow;
                } else if (direction == Direction.FullLeft) {
                    targetAngle = maxAngles[1];
                    moveSpeed = speedSlow;
                } else if (direction == Direction.MidRight) {
                    targetAngle = maxAngles[0]/2;
                    moveSpeed = speedSlow;
                } else if (direction == Direction.MidLeft) {
                    targetAngle = maxAngles[1]/2;
                    moveSpeed = speedSlow;
                }
                break;
            case EDisability.AngleLimited:
                if (direction == Direction.FullRight) {
                    targetAngle = limitsLimitedDisability[0];
                    moveSpeed = maxSpeed;
                } else if (direction == Direction.FullLeft) {
                    targetAngle = limitsLimitedDisability[1];
                    moveSpeed = maxSpeed;
                } else if (direction == Direction.MidRight) {
                    if (limitsLimitedDisability[0] > maxAngles[0] / 2) {
                        targetAngle = limitsLimitedDisability[0];
                    } else {
                        targetAngle = maxAngles[0] / 2;
                    }
                    moveSpeed = maxSpeed;
                } else if (direction == Direction.MidLeft) {
                    if (limitsLimitedDisability[1] < maxAngles[1] / 2) {
                        targetAngle = limitsLimitedDisability[1];
                    } else {
                        targetAngle = maxAngles[1] / 2;
                    }
                    moveSpeed = maxSpeed;
                }
                break;
            case EDisability.WrongMovement:
                float randomValue = UnityEngine.Random.Range(0f, 1f);
                Direction moveDirection = Direction.Stable;
                moveSpeed = maxSpeed;

                if (randomValue < probaWrongMovement) {
                    // wrong movement
                    if (direction == Direction.FullRight) {
                        moveDirection = Direction.FullLeft;
                    } else if (direction == Direction.FullLeft) {
                       moveDirection = Direction.FullRight;
                    } else if (direction == Direction.MidRight) {
                        moveDirection = Direction.MidLeft;
                    } else if (direction == Direction.MidLeft) {
                        moveDirection = Direction.MidRight;
                    }
                } else moveDirection = direction;

                if (moveDirection == Direction.FullRight) {
                    targetAngle = limitsLimitedDisability[1];
                } else if (moveDirection == Direction.FullLeft) {
                    targetAngle = limitsLimitedDisability[0];
                } else if (moveDirection == Direction.MidRight) {
                    targetAngle = limitsLimitedDisability[1] / 2;
                } else if (moveDirection == Direction.MidLeft) {
                    targetAngle = limitsLimitedDisability[0] / 2;
                }
                break;
            case EDisability.NoMovement:
                targetAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
                moveSpeed = maxSpeed;
                break;
            case EDisability.FixedAngle:
                targetAngle = FixedAngle;
                moveSpeed = maxSpeed;
                break;
        }
        float currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
        //Debug.Log("Disability: " + currentDisability + " | Direction: " + direction + " | TargetAngle: " + targetAngle + " | MoveSpeed: " + moveSpeed + " | Current Angle: " + currentAngle);
        MovePlatform(targetAngle, moveSpeed);
    }

    private void MovePlatform(float targetAngle, float moveSpeed) {
        float currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
        float error = targetAngle - currentAngle;
        float newAngle = currentAngle + error * moveSpeed;
        newAngle = Mathf.Clamp(newAngle, maxAngles[0], maxAngles[1]);
        float unityAngle = ToUnityAngle(newAngle);

        if (ai != null)
            ai.UpdatePlayerInput(newAngle);
        if (adaptiveRewardAgent != null)
            adaptiveRewardAgent.UpdatePlayerInput(newAngle);

        foreach (Platform platform in plateformes) {
            //platform.gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        } 
    }

    private void GetOptimalDirection(Marble currentMarble) {
        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        switch (marbleColor) {
            case EColors.Red:
                if (marblePos.y > heightChangePlatform) {
                    direction = Direction.FullLeft;
                } else {
                    direction = Direction.FullLeft;
                }
                break;
            case EColors.Yellow:
                if (marblePos.y > heightChangePlatform) {
                    direction = Direction.FullRight;
                } else {
                    direction = Direction.FullRight;
                }
                break;
            case EColors.Blue:
                // use signed angle of current rotation to decide side
                float signedCurrentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
                if (side == Side.Unknown) {
                    side = (signedCurrentAngle > 0f) ? Side.Left : Side.Right;
                }
                if (side == Side.Left) {
                    if (marblePos.y > heightChangePlatform) direction = Direction.MidLeft;
                    else direction = Direction.FullRight;
                } else if (side == Side.Right) {
                    if (marblePos.y > heightChangePlatform) direction = Direction.MidRight;
                    else direction = Direction.FullLeft;
                }
                break;
        }
    }

    public void ResetDisability() {
        currentDisability = GetRandomEnumValue<EDisability>();
        stock.SetCurrentDisability(currentDisability);
        currentMarble = marbleSpawner.GetCurrentMarble();
        side = Side.Unknown;

        float randomValue = UnityEngine.Random.Range(0f, 15f);
        limitsLimitedDisability[0] = -randomValue;
        randomValue = UnityEngine.Random.Range(0f, 15f);
        limitsLimitedDisability[1] = randomValue;

        randomValue = UnityEngine.Random.Range(maxSpeed/30, maxSpeed/2);
        speedSlow = randomValue;

        randomValue = UnityEngine.Random.Range(0.75f, 1f);
        probaWrongMovement = randomValue;

        randomValue = UnityEngine.Random.Range(-20f, 20f);
        FixedAngle = randomValue;
    }

    public T GetRandomEnumValue<T>() where T : Enum {
        System.Random random = new System.Random();

        Array values = Enum.GetValues(typeof(T));

        int randomIndex = random.Next(values.Length);

        return (T) values.GetValue(randomIndex);
    }

    // Convert Unity 0..360 angle to signed -180..180
    private float ToSignedAngle(float unityAngle) {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    // Convert signed -180..180 angle to Unity 0..360
    private float ToUnityAngle(float signedAngle) {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }
}
