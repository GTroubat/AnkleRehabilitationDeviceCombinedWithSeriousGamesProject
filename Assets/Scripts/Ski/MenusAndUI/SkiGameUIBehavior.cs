using UnityEngine;
using UnityEngine.UI;

public class SkiGameUIBehavior : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    [SerializeField] private GameObject mainMenu;

    [SerializeField] private Button menuBtn;

    private void Start() {
        menuBtn.onClick.AddListener(OnMenuClicked);
        gameObject.SetActive(false);
    }

    public void OnMenuClicked() {
        stock.SetIsGamePlaying(false);
        mainMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        if (gameObject.activeSelf && stock.GetIsGamePlaying()) {
            mainMenu.TryGetComponent<MainMenuBehaviorSki>(out MainMenuBehaviorSki mainMenuBehavior);
            mainMenuBehavior.SetOnceTrue();
        }
    }
}
