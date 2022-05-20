using UnityEngine;

public class XRRigInputLinker : MonoBehaviour {
	private void Start() {
		SyncTransform[] syncs = GetComponentsInChildren<SyncTransform>();
		
		var parent = transform.parent;
		for(int i = 0; i < syncs.Length; i++)
			syncs[i].target = parent.GetChild(i).transform;
	}
}