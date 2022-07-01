using System;
using UnityEngine;

public class MovingAverage {
	#region Public.

    /// <summary>
    ///     Average from samples favoring the most recent sample.
    /// </summary>
    public float Average { get; private set; }

	#endregion

    /// <summary>
    ///     Next index to write a sample to.
    /// </summary>
    private int writeIndex;

    /// <summary>
    ///     Collected samples.
    /// </summary>
    private readonly float[] samples;

    /// <summary>
    ///     Number of samples written. Will be at most samples size.
    /// </summary>
    private int writtenSamples;

    /// <summary>
    ///     Samples accumulated over queue.
    /// </summary>
    private float sampleAccumulator;

	public MovingAverage(int sampleSize) {
		if (sampleSize < 0)
			sampleSize = 0;
		else if (sampleSize < 2)
			Debug.LogWarning("Using a sampleSize of less than 2 will always return the most recent value as Average.");

		samples = new float[sampleSize];
	}


    /// <summary>
    ///     Computes a new windowed average each time a new sample arrives
    /// </summary>
    /// <param name="newSample"></param>
    public void ComputeAverage(float newSample) {
		if (samples.Length <= 1) {
			Average = newSample;
			return;
		}

		sampleAccumulator += newSample;
		samples[writeIndex] = newSample;

		//Increase writeIndex.
		writeIndex++;
		writtenSamples = Math.Max(writtenSamples, writeIndex);
		if (writeIndex >= samples.Length)
			writeIndex = 0;

		Average = sampleAccumulator / writtenSamples;

		/* If samples are full then drop off
		* the oldest sample. This will always be
		* the one just after written. The entry isn't
		* actually removed from the array but will
		* be overwritten next sample. */
		if (writtenSamples >= samples.Length)
			sampleAccumulator -= samples[writeIndex];
	}
}
