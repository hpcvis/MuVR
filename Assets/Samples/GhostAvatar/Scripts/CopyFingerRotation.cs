using UnityEngine;

public class CopyFingerRotation : MonoBehaviour {
    public Transform target;

    private void Update() {
        transform.localRotation = target.localRotation;
    }
}
