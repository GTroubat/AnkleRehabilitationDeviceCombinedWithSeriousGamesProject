using UnityEngine;
using static UnityEngine.ParticleSystem;

public class TubeProgrammedAI: MonoBehaviour {
    [SerializeField] private EColors tubeColor;
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private ProgrammedCompleteAI[] aiControllerTab;
    [SerializeField] private AdaptativeControlMode[] adaptativeControlMode;
    [SerializeField] private ParticleSystem particules;

    private Animator basketAnimator;

    private void Start() {
        basketAnimator = GetComponent<Animator>();
    }

    public EColors GetColor() { return tubeColor; }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<Marble>(out Marble marble) && 
            (stock.GetGameMode() == EGameMode.Passive || stock.GetGameMode() == EGameMode.Adaptative)) {
            Debug.Log("Contact");
            if (marble.GetColor() == tubeColor) {
                Debug.Log("Correct tube!");
                stock.SetPoints(stock.GetPoints() + 1);
                particules.Play();
            } else {
                Debug.Log("Wrong tube!");
            }
            stock.SetMarbleCounter(stock.GetMarbleCounter() + 1);
            //foreach (var aiController in aiControllerTab){
            //    aiController.ResetSide();
            //}
            foreach (var adaptativeControl in adaptativeControlMode){
                adaptativeControl.ResetSide();
            }
            if (stock.GetIsGamePlaying()) {
                marbleSpawner.SetSpawn(true);
                Debug.Log("TubeProgrammedAI");
            }
        }
        //marble.DestroySelf();
    }
}
