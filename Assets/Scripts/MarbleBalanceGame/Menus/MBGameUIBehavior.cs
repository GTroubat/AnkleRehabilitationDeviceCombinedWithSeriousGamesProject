using UnityEngine;
using UnityEngine.UI;

public class MBGameUIBehavior : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    [SerializeField] private GameObject mainMenu;

    [SerializeField] private Button menuBtn;

    [SerializeField] private bool isTrainingAI = false;

    private void Start() {
        menuBtn.onClick.AddListener(OnMenuClicked);
        gameObject.SetActive(isTrainingAI);
    }

    public void OnMenuClicked() {
        stock.SetIsGamePlaying(false);
        mainMenu.SetActive(true);
        foreach (var marble in Object.FindObjectsByType<Marble>(FindObjectsSortMode.None)) {
            Destroy(marble.gameObject);
        }
        gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        if (gameObject.activeSelf && stock.GetIsGamePlaying()) {
            mainMenu.TryGetComponent<MainMenuBehavior>(out MainMenuBehavior mainMenuBehavior);
            mainMenuBehavior.SetOnceTrue();
        }
    }
}
