using UnityEngine;

public class MyPathGenerator : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private PostsCheckPoint[] checkpoints;
    [SerializeField] private int segmentsPerCheckpoint;
    [SerializeField] private Transform playerPosistion;

    private Vector3[] midPoints;
    private int counter;

    private void Start(){
        midPoints = new Vector3[segmentsPerCheckpoint];
        GeneratePath();
    }

    private void GeneratePath() {
        line.positionCount = checkpoints.Length * segmentsPerCheckpoint;

        counter = 0;
        foreach (var cp in checkpoints) {
            Debug.Log("Checkpoint Center: " + cp.GetCenter());
            line.SetPosition(counter, cp.GetCenter());
            counter++;
        }
    }
}
