using UnityEngine;
using UnityEngine.EventSystems;

public class SliderInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    [SerializeField] private GameObject sliderInfoMenu1;
    [SerializeField] private GameObject sliderInfoMenu2;
    [SerializeField] private GameObject sliderInfoMenu3;
    [SerializeField] private GameObject sliderInfoMenu4;

    private int hoverCount = 0;

    public void OnPointerEnter(PointerEventData eventData) {
        hoverCount = Mathf.Max(hoverCount + 1, 1);
        SetMenusActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        hoverCount = Mathf.Max(hoverCount - 1, 0);
        if (hoverCount == 0) SetMenusActive(false);
    }

    private void SetMenusActive(bool active) {
        if (sliderInfoMenu1 != null) sliderInfoMenu1.SetActive(active);
        if (sliderInfoMenu2 != null) sliderInfoMenu2.SetActive(active);
        if (sliderInfoMenu3 != null) sliderInfoMenu3.SetActive(active);
        if (sliderInfoMenu4 != null) sliderInfoMenu4.SetActive(active);
    }
}
