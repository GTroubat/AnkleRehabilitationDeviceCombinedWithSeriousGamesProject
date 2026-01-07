using UnityEngine;

public class BasketAnimation : MonoBehaviour
{
    [SerializeField] private Animator basketAnimator;
    //[SerializeField] private ParticleSystem particules;

    public void OnTriggerEnter2D(Collider2D collision) {
        if (basketAnimator != null) {
            basketAnimator.SetTrigger("IsEntering");
            //particules.Play();
        }
    }
}
