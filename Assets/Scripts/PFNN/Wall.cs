using UnityEngine;

namespace PFNN {
	public class Wall : MonoBehaviour {
		public Utils.WallPoints Points { get; protected set; }

		// Use this for initialization
		private void Awake() {
			var wallStart = transform.GetChild(0).transform.position;
			var wallEnd = transform.GetChild(1).transform.position;

			Utils.WallPoints points;
			points.wallStart = new Vector2(wallStart.x, wallStart.z);
			points.wallEnd = new Vector2(wallEnd.x, wallEnd.z);
			Points = points;
		}

#if UNITY_EDITOR
		private void Update() {
			Debug.DrawLine(new Vector3(Points.wallStart.x, 1, Points.wallStart.y), new Vector3(Points.wallEnd.x, 1, Points.wallEnd.y), Color.green);
		}
#endif
	}
}
