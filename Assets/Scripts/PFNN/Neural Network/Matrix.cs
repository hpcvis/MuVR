using System;

public class Matrix {
	private readonly float[] data;

	public Matrix(int rows, int columns = 1) { // If only rows are specified, then this class acts like a vector (one-dimensional matrix)
		this.Rows = rows;
		this.Columns = columns;
		data = new float[rows * columns];
	}

	public Matrix(float[,] data) : this(data.GetLength(0), data.GetLength(1)) {
		var index = 0;
		for (var row = 0; row < Rows; row++)
		for (var column = 0; column < Columns; column++) {
			this.data[index] = data[row, column];
			++index;
		}
	}

	public Matrix(Matrix mat) : this(mat.Rows, mat.Columns) {
		for (var i = 0; i < data.Length; i++) data[i] = mat.data[i];
	}

	public float this[int row, int column] {
		get => data[row * Columns + column];
		set => data[row * Columns + column] = value;
	}

	/// <summary>
	///     Returns first element in selected row. It should be used for vertical one-dimensional matrices.
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public float this[int row] {
		get => data[row * Columns + 0];
		set => data[row * Columns + 0] = value;
	}

	public int Rows { get; }
	public int Columns { get; }
	public (int, int) Dimensions => (Rows, Columns);

	public static Matrix operator+(Matrix mat1, Matrix mat2) {
		if (!mat1.HasSameDimensions(mat2)) throw new InvalidMatrixDimensionsException("Adding not possible. Matrix dimensions do not match.");
		var result = new Matrix(mat1.Rows, mat2.Columns);

		for (var i = 0; i < mat1.data.Length; i++) result.data[i] = mat1.data[i] + mat2.data[i];
		return result;
	}

	public static Matrix operator-(Matrix mat1, Matrix mat2) {
		if (!mat1.HasSameDimensions(mat2)) throw new InvalidMatrixDimensionsException("Adding not possible. Matrix dimensions do not match.");
		var result = new Matrix(mat1.Rows, mat2.Columns);

		for (var i = 0; i < mat1.data.Length; i++) result.data[i] = mat1.data[i] - mat2.data[i];
		return result;
	}

	public bool HasSameDimensions(Matrix mat) => Rows == mat.Rows && Columns == mat.Columns;

	public static Matrix operator *(Matrix mat1, Matrix mat2) {
		Matrix result;
		if (mat1.AreMatricesSameSizeAndVertical(mat2)) { // Fake matrix multiplying.
			result = new Matrix(mat1.Rows);

			for (var i = 0; i < mat1.Rows; i++) 
				result[i, 0] = mat1[i, 0] * mat2[i, 0];
			return result;
		}

		if (!mat1.IsMultiplicationPossible(mat2)) throw new InvalidMatrixDimensionsException("Multiplying not possible. First matrix column size is not same as second matrix rows.");
		result = new Matrix(mat1.Rows, mat2.Columns);

		for (var i = 0; i < mat1.Rows; i++) 
			MultiplyRow(i, mat1, mat2, ref result);
		return result;
	}

	public static Matrix operator*(Matrix mat, float scaler) {
		var result = new Matrix(mat.Rows, mat.Columns);
		for (var i = 0; i < mat.Rows; i++)
		for (var j = 0; j < mat.Columns; j++)
			result[i, j] = mat[i, j] * scaler;
		return result;
	}
	public static Matrix operator*(float scaler, Matrix mat) => mat * scaler;

	public bool AreMatricesSameSizeAndVertical(Matrix mat) => Rows == mat.Rows && Columns == 1 && mat.Columns == 1;

	public bool IsMultiplicationPossible(Matrix mat) => Columns == mat.Rows;

	public static void MultiplyRow(int row, Matrix mat1, Matrix mat2, ref Matrix resultMat) {
		var mat1Index = row * mat1.Columns;

		for (var column = 0; column < resultMat.Columns; column++) {
			float result = 0;
			var mat2Index = column;

			for (var i = 0; i < mat1.Columns; i++) {
				result += mat1.data[mat1Index + i] * mat2.data[mat2Index];
				mat2Index += mat2.Columns;
			}

			resultMat[row, column] = result;
		}
	}

	/* 
	 * !!!  Use with caution  !!!
	 * This is not 'real' matrix division simply because there is no defined process for dividing a matrix by another matrix.
	 * This operation should be used only for vertical matrices of same size (Nx1) to divide each element of mat_1 with mat_2.
	 */
	public static Matrix operator /(Matrix mat1, Matrix mat2) {
		if (!mat1.IsMatrixVertical() || !mat2.IsMatrixVertical())
			throw new InvalidMatrixDimensionsException(
				"Both matrices must be vertical. " +
				"This operation should be used only for vertical matrices of same size (Nx1) to divide each element of mat_1 with mat_2."
			);

		var result = new Matrix(mat1.Rows);

		for (var i = 0; i < mat1.Rows; i++) result[i, 0] = mat1[i, 0] / mat2[i, 0];
		return result;
	}

	private bool IsMatrixVertical() => Columns == 1;

	/// <summary>
	///     Exponential Linear Unit (ELU), activation function mostly used in Neural Networks.
	/// </summary>
	public void ELU() {
		for (var i = 0; i < data.Length; i++) data[i] = (float)(Math.Max(data[i], 0) + Math.Exp(Math.Min(data[i], 0)) - 1);
	}
	
	public static Matrix Linear(Matrix y0, Matrix y1, float mu) => (1.0f-mu) * y0 + (mu) * y1;
	public static Matrix Cubic(Matrix y0, Matrix y1, Matrix y2, Matrix y3, float mu) {
		return (-0.5f * y0 + 1.5f * y1 - 1.5f * y2 + 0.5f * y3) * mu * mu * mu +
		       (y0 - 2.5f * y1 + 2.0f * y2 - 0.5f * y3) * mu * mu +
		       (-0.5f * y0 + 0.5f * y2) * mu +
		       y1;
	}
}

public class InvalidMatrixDimensionsException : InvalidOperationException {
	public InvalidMatrixDimensionsException() { }

	public InvalidMatrixDimensionsException(string message)
		: base(message) { }

	public InvalidMatrixDimensionsException(string message, Exception inner)
		: base(message, inner) { }
}