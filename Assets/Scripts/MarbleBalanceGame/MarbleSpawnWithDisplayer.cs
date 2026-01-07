using UnityEngine;
using UnityEngine.Rendering;
using System;

public class MarbleSpawnWithDisplayer: MonoBehaviour {
    [SerializeField] private GameObject[] marbleArray;
    [SerializeField] private Transform marbleSpawnPoint;
    [SerializeField] private SpriteRenderer nextMarbleDisplayer;
    [SerializeField] private Sprite blueMarbleIcon;
    [SerializeField] private Sprite redMarbleIcon;
    [SerializeField] private Sprite yellowMarbleIcon;

    private float spawnTimer = 0f;
    private float spawnInterval = 10f;
    private bool isPlaying = false;

    private Marble nextMarble;
    private EColors nextColor;
    private Marble currentMarble;

    private void Awake() {
        nextMarble = marbleArray[UnityEngine.Random.Range(0, marbleArray.Length)].GetComponent<Marble>();
        nextColor = nextMarble.GetColor();
        ChangeMarbleDisplayer();
        currentMarble = nextMarble;
    }

    //Unused
    private void Update() {
        if ( (isPlaying)) {
            //spawnTimer += Time.deltaTime;
        }   

        if (spawnTimer >= spawnInterval) {
            Instantiate(nextMarble, marbleSpawnPoint.position, Quaternion.identity);
            nextMarble = marbleArray[UnityEngine.Random.Range(0, marbleArray.Length)].GetComponent<Marble>();
            nextColor = nextMarble.GetColor();
            ChangeMarbleDisplayer();
            spawnTimer = 0f;
        }
    }

    private Color SetColor(EColors eColor) {
        if (eColor == EColors.Red) return Color.red;
        if (eColor == EColors.Yellow) return Color.yellow;
        if (eColor == EColors.Green) return Color.green;
        if (eColor == EColors.Blue) return Color.blue;
        return Color.black;
    }

    private void ChangeMarbleDisplayer() {
        //Debug.Log("Next Color: " + nextColor.ToString());
        if (nextColor == EColors.Blue && blueMarbleIcon is not null) {
            nextMarbleDisplayer.sprite = blueMarbleIcon;
            nextMarbleDisplayer.color = Color.white;
        } else if (nextColor == EColors.Red && redMarbleIcon is not null) {
            nextMarbleDisplayer.sprite = redMarbleIcon;
            nextMarbleDisplayer.color = Color.white;
        } else if (nextColor == EColors.Yellow && yellowMarbleIcon is not null) {
            nextMarbleDisplayer.sprite = yellowMarbleIcon;
            nextMarbleDisplayer.color = Color.white;
        } else {
            nextMarbleDisplayer.color = SetColor(nextColor);
        }
    }

    public void SetSpawn(bool value) {
        if (value) {
            //Get current marble
            currentMarble = Instantiate(nextMarble, marbleSpawnPoint.position, Quaternion.identity);

            //Prepare next marble
            nextMarble = marbleArray[UnityEngine.Random.Range(0, marbleArray.Length)].GetComponent<Marble>();
            nextColor = nextMarble.GetColor();
            ChangeMarbleDisplayer();
        }
    }

    public void ResetMarble() {
        nextMarble = marbleArray[UnityEngine.Random.Range(0, marbleArray.Length)].GetComponent<Marble>();
        nextColor = nextMarble.GetColor();
        ChangeMarbleDisplayer();
    }

    public Marble GetCurrentMarble() {
        return currentMarble;
    }
}
