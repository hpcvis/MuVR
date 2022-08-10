using System;
using System.IO;
using UnityEngine;

/*  Network inputs (total 342 elems):
 * 
 *  (12) total 48
 *    0 -  11 = trajectory position X coordinate
 *   12 -  23 = trajectory position Z coordinate
 *   24 -  35 = trajectory direction X coordinate
 *   36 -  47 = trajectory direction Z coordinate
 * 
 *  (12) total 72
 *   48 -  59 = trajectory gait stand
 *   60 -  71 = trajectory gait walk
 *   72 -  83 = trajectory gait jog
 *   84 -  95 = trajectory gait crouch
 *   96 - 107 = trajectory gait jump
 *  108 - 119 = unused, always 0.0. Reason why there isn't 330 elems as in paper.
 *  
 *  (31) total 186
 *  120 - 212 = joint positions (x,y,z). Every axis is on every third place.
 *  213 - 305 = joint velocities (x,y,z). Every axis is on every third place.
 *  
 *  (12) total 36
 *  306 - 317 = trajectory height, right point
 *  318 - 329 = trajectory height, middle point
 *  330 - 341 = trajectory height, left point
 *  
 *  ----------------------------------
 *  Network outputs (total 311 elems):
 *  
 *  0 = ? trajectory position, x axis ? (1950)
 *  1 = ? trajectory position, z axis ? (1950)
 *  2 = ? trajectory direction ?        (1952)
 *  3 = change in phase
 *  4 - 7 = ? something about IK weights ? (1730)
 *  
 *  (6) total 24
 *    8 -  13 = trajectory position, x axis
 *   14 -  19 = trajectory position, z axis
 *   20 -  25 = trajectory direction, x axis
 *   26 -  31 = trajectory direction, z axis
 *  
 *  (31) total 279
 *   32 - 124 = joint positions (x,y,z). Every axis is on every third place.
 *  125 - 217 = joint velocities (x,y,z). Every axis is on every third place.
 *  218 - 310 = joint rotations (x,y,z). Every axis is on every third place.
 */

namespace PFNN {
	public class PFNN_CPU {
		private const float PI = 3.14159274f;
		private readonly string WeightsFolderPath = Path.Combine(Application.streamingAssetsPath, "PFNNWeights");

		private readonly int InputSize;
		private readonly int OutputSize;
		private readonly int NumberOfNeurons;

		public Matrix X, Y; // inputs, outputs
		private Matrix H0, H1; // hidden layers

		private Matrix Xmean, Xstd, Ymean, Ystd;
		private Matrix[] W0, W1, W2; // weights
		private Matrix[] B0, B1, B2; // biases

		public enum Mode {
			constant,
			linear,
			cubic
		}

		private readonly Mode WeightsMode;

		/// <summary>
		/// </summary>
		/// <param name="weightsType">
		///     Determine number of location along the phase space.
		///     0 = Constant method, 50 locations.
		///     1 = Linear interpolation, 10 locations.
		///     2 = Cubic Catmull-Rom spline, 4 locations.
		/// </param>
		/// <param name="inputSize"></param>
		/// <param name="outputSize"></param>
		/// <param name="numberOfNeurons"></param>
		public PFNN_CPU(
			Mode weightsType = Mode.constant,
			int inputSize = 342,
			int outputSize = 311,
			int numberOfNeurons = 512
		) {
			WeightsMode = weightsType;
			InputSize = inputSize;
			OutputSize = outputSize;
			NumberOfNeurons = numberOfNeurons;

			SetWeightsCount();
			SetLayerSize();

			LoadMeansAndStds();
			LoadWeights();
		}

		private void SetWeightsCount() {
			switch (WeightsMode) {
				case Mode.constant:
					W0 = new Matrix[50];
					W1 = new Matrix[50];
					W2 = new Matrix[50];
					B0 = new Matrix[50];
					B1 = new Matrix[50];
					B2 = new Matrix[50];
					break;
				case Mode.linear:
					W0 = new Matrix[10];
					W1 = new Matrix[10];
					W2 = new Matrix[10];
					B0 = new Matrix[10];
					B1 = new Matrix[10];
					B2 = new Matrix[10];
					break;
				case Mode.cubic:
					W0 = new Matrix[4];
					W1 = new Matrix[4];
					W2 = new Matrix[4];
					B0 = new Matrix[4];
					B1 = new Matrix[4];
					B2 = new Matrix[4];
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void LoadMeansAndStds() {
			ReadDataFromFile(out Xmean, InputSize, "Xmean.bin");
			ReadDataFromFile(out Xstd, InputSize, "Xstd.bin");
			ReadDataFromFile(out Ymean, OutputSize, "Ymean.bin");
			ReadDataFromFile(out Ystd, OutputSize, "Ystd.bin");
		}

		public void LoadWeights() {
			int j;
			switch (WeightsMode) {
				case Mode.constant:
					for (var i = 0; i < 50; i++) {
						ReadDataFromFile(out W0[i], NumberOfNeurons, InputSize, $"W0_{i:000}.bin");
						ReadDataFromFile(out W1[i], NumberOfNeurons, NumberOfNeurons, $"W1_{i:000}.bin");
						ReadDataFromFile(out W2[i], OutputSize, NumberOfNeurons, $"W2_{i:000}.bin");

						ReadDataFromFile(out B0[i], NumberOfNeurons, $"b0_{i:000}.bin");
						ReadDataFromFile(out B1[i], NumberOfNeurons, $"b1_{i:000}.bin");
						ReadDataFromFile(out B2[i], OutputSize, $"b2_{i:000}.bin");
					}

					break;
				case Mode.linear:
					for (var i = 0; i < 10; i++) {
						j = i * 5;

						ReadDataFromFile(out W0[i], NumberOfNeurons, InputSize, $"W0_{j:000}.bin");
						ReadDataFromFile(out W1[i], NumberOfNeurons, NumberOfNeurons, $"W1_{j:000}.bin");
						ReadDataFromFile(out W2[i], OutputSize, NumberOfNeurons, $"W2_{j:000}.bin");

						ReadDataFromFile(out B0[i], NumberOfNeurons, $"b0_{j:000}.bin");
						ReadDataFromFile(out B1[i], NumberOfNeurons, $"b1_{j:000}.bin");
						ReadDataFromFile(out B2[i], OutputSize, $"b2_{j:000}.bin");
					}

					break;
				case Mode.cubic:
					for (var i = 0; i < 4; i++) {
						j = (int)(i * 12.5);

						ReadDataFromFile(out W0[i], NumberOfNeurons, InputSize, $"W0_{j:000}.bin");
						ReadDataFromFile(out W1[i], NumberOfNeurons, NumberOfNeurons, $"W1_{j:000}.bin");
						ReadDataFromFile(out W2[i], OutputSize, NumberOfNeurons, $"W2_{j:000}.bin");

						ReadDataFromFile(out B0[i], NumberOfNeurons, $"b0_{j:000}.bin");
						ReadDataFromFile(out B1[i], NumberOfNeurons, $"b1_{j:000}.bin");
						ReadDataFromFile(out B2[i], OutputSize, $"b2_{j:000}.bin");
					}

					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ReadDataFromFile(out Matrix item, int rows, string fileName) {
			item = new Matrix(rows);

			var fullPath = Path.Combine(WeightsFolderPath, fileName);
			if (!File.Exists(fullPath)) return;
			using var reader = new BinaryReader(File.Open(fullPath, FileMode.Open));
			for (var i = 0; i < rows; i++) {
				var value = reader.ReadSingle();
				item[i] = value;
			}
		}

		private void ReadDataFromFile(out Matrix item, int rows, int columns, string fileName) {
			item = new Matrix(rows, columns);

			var fullPath = Path.Combine(WeightsFolderPath, fileName);
			if (!File.Exists(fullPath)) return;
			using var reader = new BinaryReader(File.Open(fullPath, FileMode.Open));
			for (var i = 0; i < rows; i++)
			for (var j = 0; j < columns; j++) {
				var value = reader.ReadSingle();
				item[i, j] = value;
			}
		}

		private void SetLayerSize() {
			X = new Matrix(InputSize);
			Y = new Matrix(OutputSize);

			H0 = new Matrix(NumberOfNeurons);
			H1 = new Matrix(NumberOfNeurons);
		}

		/// <summary>
		///     Main function for computing Neural Network result.
		/// </summary>
		/// <param name="p">Phase value.</param>
		public void Compute(float p) {
			int pIndex0;

			X = (X - Xmean) / Xstd;

			switch (WeightsMode) {
				case Mode.constant:
					pIndex0 = (int)(p / (2 * PI) * 50);

					// Layer 1
					H0 = W0[pIndex0] * X + B0[pIndex0];
					H0.ELU();

					// Layer 2
					H1 = W1[pIndex0] * H0 + B1[pIndex0];
					H1.ELU();

					// Layer 3, network output
					Y = W2[pIndex0] * H1 + B2[pIndex0];
					break;

				case Mode.linear:
					break;

				case Mode.cubic:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			Y = Y * Ystd + Ymean;
		}

		public void Reset() {
			Y = Ymean;
		}
	}
}