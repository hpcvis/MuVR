using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Timer {
	public delegate void VoidDel();
	
	public static IEnumerator Start(VoidDel toRun, float duration = 3) {
		//to whatever you want
		float start = Time.time;
		while (Time.time - start < duration) yield return null;
		toRun();
	}
}
