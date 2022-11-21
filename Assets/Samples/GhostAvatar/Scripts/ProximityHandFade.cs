using UnityEngine;

// Class which fades away the ghost hand when it is close to the physical hand
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ProximityHandFade : MonoBehaviour {
	private SkinnedMeshRenderer renderer;
	private Material mat;
	private float originalAlpha;
	private void Awake() {
		renderer = GetComponent<SkinnedMeshRenderer>();
		mat = new Material(renderer.material);
		renderer.material = mat;
		originalAlpha = mat.color.a;
	}

	// The bones to check the distance of
	public Transform physicsBone, ghostBone;
	// When the bones are further than this distance away, make the hand "completely" opaque
	public float distanceThreshold = .1f;

	public void Update() {
		var color = mat.color;
		color.a = Mathf.Min((physicsBone.position - ghostBone.position).magnitude / distanceThreshold, 1) * originalAlpha;
		mat.color = color;
	}
}