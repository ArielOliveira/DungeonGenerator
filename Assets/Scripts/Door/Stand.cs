using UnityEngine;

public class Stand : MonoBehaviour {
    [SerializeField]
    Animator doorAnimator;
    private void OnTriggerEnter(Collider other) {
        doorAnimator.SetTrigger(gameObject.name);
        doorAnimator.SetTrigger("OpenDoor");
    }
}
