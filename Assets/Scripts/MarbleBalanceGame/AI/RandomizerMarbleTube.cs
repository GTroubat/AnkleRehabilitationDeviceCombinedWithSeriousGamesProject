using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.CompilerServices;

public class RandomizerMarbleTube: MonoBehaviour
{
    [SerializeField] private MarbleAI marble;
    private EColors color;

    [SerializeField] private TubeAI[] tubes;
    private float[] tubePositionsX = {-6.5f, 0f, 6.5f };
    private float tubePositionY = -5f;

    public void RandomMarble() {
        color = GetRandomColor();
        marble.SetColor(color);
        switch (color) {
            case EColors.Red:
                marble.GetComponent<SpriteRenderer>().color = Color.red;
                break;
            case EColors.Yellow:
                marble.GetComponent<SpriteRenderer>().color = Color.yellow;
                break;
            case EColors.Blue:
                marble.GetComponent<SpriteRenderer>().color = Color.blue;
                break;
        }
        
    }

    public EColors GetRandomColor() {
        EColors color = EColors.Green;
        Array colors = Enum.GetValues(typeof(EColors));
        while (color == EColors.Green) {
            color = (EColors)colors.GetValue(UnityEngine.Random.Range(0, colors.Length));
        }   
        return color;
    }

    public void RandomTube() {
        int randomValue = UnityEngine.Random.Range(0, 6);
        switch (randomValue) {
            case 0:
                tubes[0].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                break;
            case 1:
                tubes[0].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                break;
            case 2:
                tubes[0].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                break;
            case 3:
                tubes[0].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                break;
            case 4:
                tubes[0].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                break;
            case 5:
                tubes[0].transform.position = new Vector3(tubePositionsX[2], tubePositionY, 0f);
                tubes[1].transform.position = new Vector3(tubePositionsX[1], tubePositionY, 0f);
                tubes[2].transform.position = new Vector3(tubePositionsX[0], tubePositionY, 0f);
                break;
        }
    }
}
