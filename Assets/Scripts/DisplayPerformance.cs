using UnityEngine;

public class DisplayPerformance : MonoBehaviour {
	public int targetFrameRate = 60;
	private float nextDisplayTime = 0f;
	private readonly MovingAverage average = new(100);

	private uint frames = 0;

	private void Update() {
#if UNITY_SERVER
        Application.targetFrameRate = targetFrameRate;

        frames++;
        if (Time.time < nextDisplayTime)
            return;

        average.ComputeAverage(frames);
        frames = 0;
        //Update display twice a second.
        nextDisplayTime = Time.time + 1f;

        double avgFrameRate = average.Average;
        //Performance lost.
        double lost = avgFrameRate / (double)targetFrameRate;
        lost = (1d - lost);

        //Replace this with the equivalent of your networking solution.
        int clientCount = 0;
        // int clientCount = InstanceFinder.ServerManager.Clients.Count;

        Debug.Log($"Average {lost:f3} performance lost ({avgFrameRate:f2}) with {clientCount} clients.");
#elif UNITY_EDITOR
		//Max out editor frames to test client side scalability.
		Application.targetFrameRate = 9999;
#else
        /* Limit client frame rate to 15
         * so your computer doesn't catch fire when opening
         * hundreds of clients. */
        Application.targetFrameRate = 15;
#endif
	}
}