using UnityEngine;

public class ChangeSizePosts : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private PostsCheckPoint[] postsCheckPoints;

    private bool[] triggeredStates;

    private float followedTriggered = 0;
    private float maxFollowedTriggered = 0;
    private float totalSize = 5f;

    private void Start() {
        triggeredStates = new bool[postsCheckPoints.Length];
        for (int i = 0; i < postsCheckPoints.Length; i++) {
            triggeredStates[i] = false;
        }
    }

    private void FixedUpdate() {
        if (stock.GetIsGamePlaying()) {
            for (int i = 0; i < postsCheckPoints.Length; i++) {
                postsCheckPoints[i].SetSize(totalSize);
                if (postsCheckPoints[i].HasBeenTriggered()) {
                    triggeredStates[i] = true;
                    followedTriggered++;
                } else {
                    triggeredStates[i] = false;
                    if (followedTriggered > maxFollowedTriggered) {
                        maxFollowedTriggered = followedTriggered;
                    }
                    followedTriggered = 0;
                    maxFollowedTriggered -= 0.5f;
                }
            }
            totalSize = 5f - maxFollowedTriggered * 0.25f;
            if (totalSize < 2.5f) {
                totalSize = 2.5f;
            }
        }
    }
}
