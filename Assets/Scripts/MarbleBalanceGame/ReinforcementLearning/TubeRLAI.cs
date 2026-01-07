using UnityEngine;
using static UnityEngine.ParticleSystem;

public class TubeRLAI : MonoBehaviour {
    [SerializeField] private EColors tubeColor;
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private ProgrammedCompleteAI[] aiControllerTab;
    [SerializeField] private AdaptativeControlMode[] adaptativeControlMode;
    [SerializeField] private RotateToGoalAgent rotateToGoalAgent;
    [SerializeField] private DisabilitySimulator disabilitySimulator;
    [SerializeField] private ParticleSystem particules;
    [SerializeField] private AIForAdaptativeMode adaptativeAI;
    [SerializeField] private AdaptiveRewardAgent adaptiveRewardAgent;
    [SerializeField] private ForceBasedAssistAgent forceBasedAssistAgent;

    private Animator basketAnimator;

    private void Start() {
        basketAnimator = GetComponent<Animator>();
    }

    public EColors GetColor() { return tubeColor; }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<Marble>(out Marble marble) && 
            (stock.GetGameMode() == EGameMode.Passive || stock.GetGameMode() == EGameMode.Adaptative)) {
            bool success = marble.GetColor() == tubeColor;
            if (success) stock.SetPoints(stock.GetPoints() + 1);
            Debug.Log("Contact");
            if (success && rotateToGoalAgent != null) {
                rotateToGoalAgent.MarbleIsInBasket(true, tubeColor, marble.GetColor());
                particules.Play();
            } else if (rotateToGoalAgent != null) {
                rotateToGoalAgent.MarbleIsInBasket(false, tubeColor, marble.GetColor());
            }
            stock.SetMarbleCounter(stock.GetMarbleCounter() + 1);
            foreach (var adaptativeControl in adaptativeControlMode){
                adaptativeControl.ResetSide();
            }
            if (stock.GetGameMode() == EGameMode.Adaptative && adaptativeAI != null) {
                adaptativeAI.OnMarbleScored(success);
            }
            if (stock.GetGameMode() == EGameMode.Adaptative && adaptiveRewardAgent != null) {
                adaptiveRewardAgent.OnMarbleScored(success);
            }
            if(EGameMode.Adaptative == stock.GetGameMode() && forceBasedAssistAgent != null) {
                forceBasedAssistAgent.OnMarbleScored(success);
            }

            if (stock.GetIsGamePlaying()) {
                marbleSpawner.SetSpawn(true);
                Debug.Log("Spawn set to true");
            }
            if (stock.GetGameMode() == EGameMode.Adaptative && disabilitySimulator != null) {
                disabilitySimulator.ResetDisability();
            }
        }
        //marble.DestroySelf();
    }
}
