﻿using UnityEngine;

public class WallScript : MonoBehaviour {
	private Vector2 wallStart;
	private Vector2 wallEnd;

	// Use this for initialization
	private void Start() {
		var wallStart = transform.GetChild(0).transform.position;
		var wallEnd = transform.GetChild(1).transform.position;

		this.wallStart = new Vector2(wallStart.x, wallStart.z);
		this.wallEnd = new Vector2(wallEnd.x, wallEnd.z);
	}

	public Utils.WallPoints GetWallPoints() {
		Utils.WallPoints points;
		points.wallStart = wallStart;
		points.wallEnd = wallEnd;

		return points;
	}
}