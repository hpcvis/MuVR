using MuVR.Utility;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PFNN {
	public class Controller : MonoBehaviour {
		protected PFNN_CPU network;
		protected Transform mainCamera;

		// How often (in seconds) the network should be asked for another frame
		protected const float ResetTime = 1f / 60;
		// Actual time until the network should be asked for another frame 
		protected float time = ResetTime;

		public Vector3 initialWorldPosition;

		protected Transform characterBody;
		protected Transform trajectoryPath;

		public GameObject framePointPrefab;
		public GameObject framePointInvisiblePrefab;
		public bool visualizeTrajectory; // NOTE: Changing this value at runtime has no effect!
		public GameObject jointPrefab;
		public GameObject jointInvisiblePrefab;
		public bool visualizeJoints; // NOTE: Changing this value at runtime has no effect!
		public float phase;

		// Character Joints stuff 
		[Header("Joints")]
		public int jointsNumber = 31;

		public float strafeAmount;
		public float strafeTarget;
		public float crouchedAmount;
		public float crouchedTarget;
		public float responsive;

		// Enum providing names and IDs for all of the various types of joints
		public enum JointType {
			None = ~0,
			Hips = 0,
			RHipJoint,
			RightUpLeg,
			RightLeg,
			RightFoot,
			RightToeBase,
			LHipJoint,
			LeftUpLeg,
			LeftLeg,
			LeftFoot,
			LeftToeBase,
			LowerBack,
			Spine,
			Spine1,
			Neck,
			Neck1,
			Head,
			RightShoulder,
			RightArm,
			RightForeArm,
			RightHand,
			RightFingerBase,
			RThumb,
			LeftHandIndex1,
			LeftShoulder,
			LeftArm,
			LeftForeArm,
			LeftHand,
			LeftFingerBase,
			LThumb,
			RightHandIndex,
		}

		public struct JointsComponents {
			public Vector3 position;
			public Vector3 velocity;

			public Quaternion rotation;

			public GameObject jointPoint;
			public JointType jointType;
			public string jointName => jointType.ToString();
		}

		public JointsComponents[] joints;

		// Trajectory values
		[Header("Trajectory")]
		[Range(0, 1f)]
		public float scaleFactor = 0.04f; // 0.06f

		protected float oppositeScaleFactor;

		public int numberOfTrajectoryProjections = 12;
		public int trajectoryLength;

		[Tooltip("Distance of right and left from middle trajectory point.")]
		public float sidePointsOffset = 25f;

		[Tooltip("Layer index which should be ignored when measuring height.")]
		public int layerMask;

		public float heightOrigin;

		protected Vector3 targetDirection;
		protected Vector3 targetVelocity;

		public struct TrajectoryComponents {
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 direction;
			public float height;
			public float gaitStand;
			public float gaitWalk;
			public float gaitJog;
			public float gaitCrouch;
			public float gaitJump;
			public float gaitBump;

			public GameObject framePoint;
		}

		public TrajectoryComponents[] points;

		protected Utils.WallPoints[] terrainWalls;
		public float wallWidth = 1.5f;
		public float wallVal = 1.1f;
		// The maximum length of the shadow between forward from hip and forward from foot where a slope is no longer considered a wall
		public float autoWallShadowLength = 4;

		protected int[] JointParents = {
			-1, // Hips 0
			0, // RHip 1
			1, // RThigh 2
			2, // RShin 3
			3, // RAnkle 4
			4, // RToes 5
			0, // LHip 6
			6, // LThigh 7
			7, // LShin 8
			8, // LAnkle 9
			9, // LToes 10
			0, // Spine 11
			11, // Spine1 12
			12, // Spine2 13
			13, // Neck 14
			14, // Head 15
			15, // HeadTop 16
			12, // RCollar 17
			17, // RArm 18
			18, // RForearm 19
			19, // RWrist 20
			20, // RHand 21
			21, // RFingers 22
			22, // RWrist2 23
			12, // LCollar 24
			24, // LArm 25
			25, // LForearm 26
			26, // LWrist 27
			27, // RHand 28
			28, // RFingers 29
			29 // RWrist2 30
		};

		// Use this for initialization
		protected virtual void Awake() {
			GetAllWalls();

			characterBody = gameObject.transform.GetChild(1);
			InitializeJoints();

			trajectoryPath = gameObject.transform.GetChild(2);
			InitializeTrajectory();

			layerMask = 1 << layerMask;
			layerMask = ~layerMask;

			oppositeScaleFactor = 1 / scaleFactor;

			initialWorldPosition = new Vector3(
				transform.position.x,
				transform.position.y,
				transform.position.z);

			network = new PFNN_CPU();
			mainCamera = gameObject.transform.GetChild(0);

			ResetCharacter();
		}

		// Update is called once per frame
		protected virtual void Update() {
			// Only invoke the neural network 60 times per second (if the framerate is low enough, we may invoke the network twice)
			while (time <= 0) {
				UpdateNetworkInput(ref network.X);
				network.Compute(phase);
				BuildLocalTransforms(network.Y);

				// display stuff (TODO: Refactor)
				DisplayTrajectory();
				DisplayJoints();

				PostVisualisationCalculation(network.Y);
				UpdatePhase(network.Y);

				// Reset the timer (While standing don't calculate several frames)
				if (IsStanding()) time = ResetTime;
				else time += ResetTime;
			}

			time -= Time.deltaTime;
		}

		// Move the character in the direction relative to the given coordinate basis
		protected void MoveCharacter(Vector2 basis, Vector2 direction, float sprinting = 0, float strafing = 0) {
			var newTargetDirection = Vector3.Normalize(new Vector3(basis.x, 0, basis.y));

			UpdateStrafe(strafing);
			UpdateTargetDirectionAndVelocity(newTargetDirection, direction.x, direction.y, sprinting);
			UpdateGait(sprinting);
			PredictFutureTrajectory();

			//Jumps();
			Walls();

			UpdateRotation();
			UpdateHeights();
		}
		// Move the character in the direction relative to the camera's coordinate basis
		protected void MoveCharacter(Vector2 direction, float sprinting = 0, float strafing = 0) {
			MoveCharacter(new Vector2(mainCamera.forward.x, mainCamera.forward.z), direction, sprinting, strafing);
		}

		// Move the character to a particular point (No pathfinding is done, pathfinding must be performed externally)
		protected void MoveCharacterTo(Vector3 point, float sprinting = 0, float strafing = 0, float targetDistance = 0) {
			var movementSpeed = 2.5f + 2.5f * sprinting;
			
			var direction = (point - transform.position).normalized;
			var direction2D = new Vector2(direction.x, direction.z);
			direction2D = (point - (transform.position + direction * movementSpeed)).sqrMagnitude > targetDistance * targetDistance ? direction2D : Vector2.zero;
			MoveCharacter(Vector2.up, direction2D, sprinting, strafing);
		}

		protected void ResetCharacter() {
			network.Reset();
			Reset(initialWorldPosition, network.Y);
		}

		protected void GetAllWalls() {
			var terrainWalls = FindObjectsOfType<PFNN.Wall>();
			this.terrainWalls = new Utils.WallPoints[terrainWalls.Length + 1]; // The last wall slot is used for walls automatically found in the environment

			for (var i = 0; i < terrainWalls.Length; i++)
				this.terrainWalls[i] = terrainWalls[i].Points;
		}

		protected void InitializeJoints() {
			phase = 0;

			strafeAmount = 0;
			strafeTarget = 0;
			crouchedAmount = 0;
			crouchedTarget = 0;
			responsive = 0;

			joints = new JointsComponents[jointsNumber];

			InstantiateJoints();
		}

		protected void InstantiateJoints() {
			for (var i = 0; i < jointsNumber; i++) {
				var newJoint = Instantiate(
					visualizeJoints ? jointPrefab : jointInvisiblePrefab,
					new Vector3(transform.position.x, 0.5f, transform.position.z),
					Quaternion.identity,
					characterBody
				);
				newJoint.name = "Joint_" + i;
				joints[i].jointPoint = newJoint;
				joints[i].jointType = (JointType)i;
			}
		}

		protected void InitializeTrajectory() {
			trajectoryLength = numberOfTrajectoryProjections * 10;
			points = new TrajectoryComponents[trajectoryLength];

			targetDirection = Vector3.forward;
			targetVelocity = new Vector3();

			InstantiateTrajectoryPoints();
		}

		protected void InstantiateTrajectoryPoints() {
			for (var i = 0; i < trajectoryLength; i += 10) {
				var newPoint = Instantiate(
					visualizeTrajectory ? framePointPrefab : framePointInvisiblePrefab,
					new Vector3(transform.position.x, transform.position.y, transform.position.z + (-6f + i / 10f)),
					Quaternion.identity,
					trajectoryPath
				);
				newPoint.name = "FramePoint_" + i / 10;

				points[i].framePoint = newPoint;
			}
		}

		protected void Reset(Vector3 initialPosition, Matrix Y) {
			var rootPosition = new Vector3(initialPosition.x, GetHeightSample(initialPosition), initialPosition.z);
			var rootRotation = new Quaternion();

			for (var i = 0; i < jointsNumber; i++) {
				var oPosition = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 0;
				var oVelocity = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 1;
				var oRotation = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 2;

				var position = rootRotation * new Vector3(
									Y[oPosition + i * 3 + 0], 
									Y[oPosition + i * 3 + 1], 
									Y[oPosition + i * 3 + 2]
								) + rootPosition;
				var velocity = rootRotation * new Vector3(
					Y[oVelocity + i * 3 + 0],
					Y[oVelocity + i * 3 + 1],
					Y[oVelocity + i * 3 + 2]
				);
				var rotation = rootRotation * Utils.QuaternionExponent(new Vector3( // Possible error (1061)
					Y[oRotation + i * 3 + 0],
					Y[oRotation + i * 3 + 1],
					Y[oRotation + i * 3 + 2]
				));

				joints[i].position = position;
				joints[i].velocity = velocity;
				joints[i].rotation = rotation;
			}

			for (var i = 0; i < trajectoryLength; i++) {
				points[i].position = rootPosition;
				points[i].rotation = rootRotation;
				points[i].direction = Vector3.forward; //new Vector3(0, 0, 1f);
				points[i].height = rootPosition.y;
				points[i].gaitStand = 0;
				points[i].gaitWalk = 0;
				points[i].gaitJog = 0;
				points[i].gaitCrouch = 0;
				points[i].gaitJump = 0;
				points[i].gaitBump = 0;
			}

			phase = 0;
		}

		public void UpdateStrafe(float strafe) {
			strafeTarget = strafe;
			strafeAmount = Mathf.Lerp(strafeAmount, strafeTarget, Utils.extraStrafeSmooth);
		}

		public void UpdateTargetDirectionAndVelocity(Vector3 newTargetDirection, float axisX, float axisY, float rightTrigger) {
			var newTargetRotation = Quaternion.AngleAxis(
				Mathf.Atan2(newTargetDirection.x, newTargetDirection.z) * Mathf.Rad2Deg,
				Vector3.up
			);

			var movementSpeed = 2.5f + 2.5f * rightTrigger;

			var newTargetVelocity = movementSpeed * (newTargetRotation * new Vector3(axisX, 0, axisY));
			targetVelocity = Vector3.Lerp(targetVelocity, newTargetVelocity, Utils.extraVelocitySmooth);

			var targetVelocityDirection = targetVelocity.magnitude < 1e-05 ? targetDirection : targetVelocity.normalized;

			newTargetDirection = Utils.MixDirections(targetVelocityDirection, newTargetDirection, strafeAmount);
			targetDirection = Utils.MixDirections(targetDirection, newTargetDirection, Utils.extraDirectionSmooth);

			crouchedAmount = Mathf.Lerp(crouchedAmount, crouchedTarget, Utils.extraCrouchedSmooth);

			Debug.DrawRay(transform.position, targetDirection * 10, Color.red);
			Debug.DrawRay(transform.position, targetVelocityDirection * 10, Color.green);
		}

		public void UpdateGait(float sprinting) {
			if (targetVelocity.magnitude < 0.1f) { // Standing still
				var standAmount = 1f - Mathf.Clamp01(targetVelocity.magnitude / 0.1f);

				points[trajectoryLength / 2].gaitStand = Mathf.Lerp(points[trajectoryLength / 2].gaitStand, standAmount, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitWalk = Mathf.Lerp(points[trajectoryLength / 2].gaitWalk, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJog = Mathf.Lerp(points[trajectoryLength / 2].gaitJog, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitCrouch = Mathf.Lerp(points[trajectoryLength / 2].gaitCrouch, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJump = Mathf.Lerp(points[trajectoryLength / 2].gaitJump, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitBump = Mathf.Lerp(points[trajectoryLength / 2].gaitBump, 0, Utils.extraGaitSmooth);
			} else if (crouchedAmount > 0.1f) { // Crouch
				points[trajectoryLength / 2].gaitStand = Mathf.Lerp(points[trajectoryLength / 2].gaitStand, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitWalk = Mathf.Lerp(points[trajectoryLength / 2].gaitWalk, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJog = Mathf.Lerp(points[trajectoryLength / 2].gaitJog, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitCrouch = Mathf.Lerp(points[trajectoryLength / 2].gaitCrouch, crouchedAmount, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJump = Mathf.Lerp(points[trajectoryLength / 2].gaitJump, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitBump = Mathf.Lerp(points[trajectoryLength / 2].gaitBump, 0, Utils.extraGaitSmooth);
			} else if (sprinting != 0) { // Jog - 546 wuuuut??
				points[trajectoryLength / 2].gaitStand = Mathf.Lerp(points[trajectoryLength / 2].gaitStand, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitWalk = Mathf.Lerp(points[trajectoryLength / 2].gaitWalk, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJog = Mathf.Lerp(points[trajectoryLength / 2].gaitJog, 1f, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitCrouch = Mathf.Lerp(points[trajectoryLength / 2].gaitCrouch, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJump = Mathf.Lerp(points[trajectoryLength / 2].gaitJump, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitBump = Mathf.Lerp(points[trajectoryLength / 2].gaitBump, 0, Utils.extraGaitSmooth);
			} else { // Walk
				points[trajectoryLength / 2].gaitStand = Mathf.Lerp(points[trajectoryLength / 2].gaitStand, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitWalk = Mathf.Lerp(points[trajectoryLength / 2].gaitWalk, 1f, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJog = Mathf.Lerp(points[trajectoryLength / 2].gaitJog, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitCrouch = Mathf.Lerp(points[trajectoryLength / 2].gaitCrouch, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitJump = Mathf.Lerp(points[trajectoryLength / 2].gaitJump, 0, Utils.extraGaitSmooth);
				points[trajectoryLength / 2].gaitBump = Mathf.Lerp(points[trajectoryLength / 2].gaitBump, 0, Utils.extraGaitSmooth);
			}
		}

		public void PredictFutureTrajectory() {
			var positionsBlend = new Vector3[trajectoryLength];
			positionsBlend[trajectoryLength / 2] = points[trajectoryLength / 2].position;

			CalculateAutomaticWall();

			for (var i = trajectoryLength / 2 + 1; i < trajectoryLength; i++) {
				var biasPosition = Mathf.Lerp(0.5f, 1f, strafeAmount); // On both variables will come character response check (569)
				var biasDirection = Mathf.Lerp(2f, 0.5f, strafeAmount);

				var scalePosition = 1f - Mathf.Pow(1f - (float)(i - trajectoryLength / 2) / (trajectoryLength / 2), biasPosition);
				var scaleDirection = 1f - Mathf.Pow(1f - (float)(i - trajectoryLength / 2) / (trajectoryLength / 2), biasDirection);

				positionsBlend[i] = positionsBlend[i - 1] + Vector3.Lerp(
					points[i].position - points[i - 1].position,
					targetVelocity,
					scalePosition);

				// Collide with walls
				var trajectoryPoint = new Vector2(positionsBlend[i].x * scaleFactor, positionsBlend[i].z * scaleFactor);
				for (var j = 0; j < terrainWalls.Length; j++) {
					if ((trajectoryPoint - (terrainWalls[j].wallStart + terrainWalls[j].wallEnd) / 2f).magnitude >
					    (terrainWalls[j].wallStart - terrainWalls[j].wallEnd).magnitude)
						continue;
					
					var segmentPoint = Utils.SegmentNearest(terrainWalls[j].wallStart, terrainWalls[j].wallEnd, trajectoryPoint);
					var segmentDistance = (segmentPoint - trajectoryPoint).magnitude;

					if (!(segmentDistance < wallWidth + wallVal)) continue;
					var point0 = (wallWidth + 0) * (trajectoryPoint - segmentPoint).normalized + segmentPoint;
					var point1 = (wallWidth + wallVal) * (trajectoryPoint - segmentPoint).normalized + segmentPoint;
					var point = Vector2.Lerp(point0, point1, Mathf.Clamp01(segmentDistance - wallWidth) / wallVal);

					positionsBlend[i].x = point.x * oppositeScaleFactor;
					positionsBlend[i].z = point.y * oppositeScaleFactor;
				}

				points[i].direction = Utils.MixDirections(points[i].direction, targetDirection, scaleDirection);

				points[i].height = points[trajectoryLength / 2].height;

				points[i].gaitStand = points[trajectoryLength / 2].gaitStand;
				points[i].gaitWalk = points[trajectoryLength / 2].gaitWalk;
				points[i].gaitJog = points[trajectoryLength / 2].gaitJog;
				points[i].gaitCrouch = points[trajectoryLength / 2].gaitCrouch;
				points[i].gaitJump = points[trajectoryLength / 2].gaitJump;
				points[i].gaitBump = points[trajectoryLength / 2].gaitBump;
			}

			for (var i = trajectoryLength / 2 + 1; i < trajectoryLength; i++) points[i].position = positionsBlend[i];

			// crouch stuff
		}

		public void Jumps() {
			for (var i = trajectoryLength / 2 + 1; i < trajectoryLength; i++) {
				points[i].gaitJump = 0;

				points[i].gaitJump = Mathf.Max(
					points[i].gaitJump,
					1f - Mathf.Clamp01(3f / 5f)
				);
			}
		}

		private Coroutine removeWallCoroutine;
		protected void CalculateAutomaticWall() {
			var hips = GetJoint(JointType.Hips).jointPoint;
			var forward = hips.transform.forward;
			var right = hips.transform.right;
			forward.y = 0;
			var upPosition = hips.transform.position;
			
			var setWall = false;
			if (Physics.Raycast(new Ray(upPosition, forward), out var upHit, autoWallShadowLength * 2 + wallWidth, layerMask)) {
				var downPosition = upPosition;
				downPosition.y = (GetJoint(JointType.RightFoot).jointPoint.transform.position.y + GetJoint(JointType.LeftFoot).jointPoint.transform.position.y) / 2;
				if (Physics.Raycast(new Ray(downPosition, forward), out var downHit, autoWallShadowLength * 2 + wallWidth, layerMask)) {
					var closer = Utils.CloserToPoint(downHit.point, upHit.point, (upPosition + downPosition) / 2);
					var shadow = Vector3.Project(upHit.point - downHit.point, forward).sqrMagnitude;

					if (shadow < autoWallShadowLength * autoWallShadowLength) {
						setWall = true;

						var start = closer + right - forward * wallWidth;
						var end = closer - right - forward * wallWidth;
						terrainWalls[^1].wallStart = new Vector2(start.x, start.z);
						terrainWalls[^1].wallEnd = new Vector2(end.x, end.z);
#if UNITY_EDITOR
						Debug.DrawLine(start, end, Color.green);
#endif
					}
				}
			}

			if (setWall) return;
			if (removeWallCoroutine is not null) StopCoroutine(removeWallCoroutine);
			removeWallCoroutine = StartCoroutine(Timer.Start(() => {
				terrainWalls[^1].wallStart = new Vector2(Mathf.Infinity, Mathf.Infinity);
				terrainWalls[^1].wallEnd = new Vector2(Mathf.Infinity, Mathf.Infinity);
				removeWallCoroutine = null;
			}, .1f));
		}

		public void Walls() {
			for (var i = 0; i < trajectoryLength; i++) {
				points[i].gaitBump = 0;
				for (var j = 0; j < terrainWalls.Length; j++) {
					var trajectoryPoint = new Vector2(points[i].position.x * scaleFactor, points[i].position.z * scaleFactor);
					var segmentPoint = Utils.SegmentNearest(terrainWalls[j].wallStart, terrainWalls[j].wallEnd, trajectoryPoint);

					var segmentDistance = (segmentPoint - trajectoryPoint).magnitude;
					points[i].gaitBump = Mathf.Max(points[i].gaitBump, 1f - Mathf.Clamp01((segmentDistance - wallWidth) / wallVal));
				}
			}
		}

		public void UpdateRotation() {
			for (var i = 0; i < trajectoryLength; i++)
				points[i].rotation = Quaternion.AngleAxis(
					Mathf.Atan2(points[i].direction.x, points[i].direction.z) * Mathf.Rad2Deg,
					Vector3.up);
		}

		public void UpdateHeights() {
			for (var i = trajectoryLength / 2; i < trajectoryLength; i++) points[i].position.y = GetHeightSample(points[i].position);

			points[trajectoryLength / 2].height = 0;
			for (var i = 0; i < trajectoryLength; i += 10) points[trajectoryLength / 2].height += points[i].position.y / (trajectoryLength / 10f);
		}

		public float GetHeightSample(Vector3 position) {
			position.Scale(new Vector3(scaleFactor, 0, scaleFactor));
			position.y = heightOrigin;

			if (!Physics.Raycast(position, Vector3.down, out var hit, Mathf.Infinity, layerMask)) return 0;
			return hit.transform.CompareTag("Terrain") ? (heightOrigin - hit.distance) * oppositeScaleFactor : 0;
		}

		public void UpdateNetworkInput(ref Matrix X) {
			var rootPosition = new Vector3(
				points[trajectoryLength / 2].position.x,
				points[trajectoryLength / 2].height,
				points[trajectoryLength / 2].position.z);

			var rootRotation = points[trajectoryLength / 2].rotation;

			var w = trajectoryLength / 10;

			// Trajectory position and direction
			for (var i = 0; i < trajectoryLength; i += 10) {
				var position = Quaternion.Inverse(rootRotation) * (points[i].position - rootPosition);
				var direction = Quaternion.Inverse(rootRotation) * points[i].direction;

				X[w * 0 + i / 10] = position.x;
				X[w * 1 + i / 10] = position.z;

				X[w * 2 + i / 10] = direction.x;
				X[w * 3 + i / 10] = direction.z;
			}

			// Trajectory gaits
			for (var i = 0; i < trajectoryLength; i += 10) {
				X[w * 4 + i / 10] = points[i].gaitStand;
				X[w * 5 + i / 10] = points[i].gaitWalk;
				X[w * 6 + i / 10] = points[i].gaitJog;
				X[w * 7 + i / 10] = points[i].gaitCrouch;
				X[w * 8 + i / 10] = points[i].gaitJump;
				X[w * 9 + i / 10] = 0;
			}

			// Joint previous position, velocity and rotation
			var previousRootPosition = new Vector3(
				points[trajectoryLength / 2 - 1].position.x,
				points[trajectoryLength / 2 - 1].height,
				points[trajectoryLength / 2 - 1].position.z);

			var previousRootRotation = points[trajectoryLength / 2 - 1].rotation;

			var o = trajectoryLength / 10 * 10;
			for (var i = 0; i < jointsNumber; i++) {
				var pos = Quaternion.Inverse(previousRootRotation) * (joints[i].position - previousRootPosition);
				var prv = Quaternion.Inverse(previousRootRotation) * joints[i].velocity;

				X[o + jointsNumber * 3 * 0 + i * 3 + 0] = pos.x;
				X[o + jointsNumber * 3 * 0 + i * 3 + 1] = pos.y;
				X[o + jointsNumber * 3 * 0 + i * 3 + 2] = pos.z;

				X[o + jointsNumber * 3 * 1 + i * 3 + 0] = prv.x;
				X[o + jointsNumber * 3 * 1 + i * 3 + 1] = prv.y;
				X[o + jointsNumber * 3 * 1 + i * 3 + 2] = prv.z;
			}

			// Trajectory heights
			o += jointsNumber * 3 * 2;
			for (var i = 0; i < trajectoryLength; i += 10) {
				var positionRight = points[i].position + points[i].rotation * new Vector3(sidePointsOffset, 0, 0);
				var positionLeft = points[i].position + points[i].rotation * new Vector3(-sidePointsOffset, 0, 0);

				X[o + w * 0 + i / 10] = GetHeightSample(positionRight) - rootPosition.y;
				X[o + w * 1 + i / 10] = points[i].position.y - rootPosition.y;
				X[o + w * 2 + i / 10] = GetHeightSample(positionLeft) - rootPosition.y;
			}
		}

		public void BuildLocalTransforms(Matrix Y) {
			var rootPosition = new Vector3(
				points[trajectoryLength / 2].position.x,
				points[trajectoryLength / 2].height,
				points[trajectoryLength / 2].position.z);

			var rootRotation = points[trajectoryLength / 2].rotation;

			for (var i = 0; i < jointsNumber; i++) {
				var oPosition = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 0;
				var oVelocity = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 1;
				var oRotation = 8 + trajectoryLength / 2 / 10 * 4 + jointsNumber * 3 * 2;

				var position = rootRotation * new Vector3(
					               Y[oPosition + i * 3 + 0],
					               Y[oPosition + i * 3 + 1],
					               Y[oPosition + i * 3 + 2])
				               + rootPosition;
				var velocity = rootRotation * new Vector3(
					Y[oVelocity + i * 3 + 0],
					Y[oVelocity + i * 3 + 1],
					Y[oVelocity + i * 3 + 2]);
				var rotation = rootRotation * Utils.QuaternionExponent( // Possible error (1061)
					new Vector3(
						Y[oRotation + i * 3 + 0],
						Y[oRotation + i * 3 + 1],
						Y[oRotation + i * 3 + 2]));

				joints[i].position = Vector3.Lerp(joints[i].position + velocity, position, Utils.extraJointSmooth);
				joints[i].velocity = velocity;
				joints[i].rotation = rotation;
			}
		}

		public void PostVisualisationCalculation(Matrix Y) {
			// Update past trajectory
			for (var i = 0; i < trajectoryLength / 2; i++) {
				points[i].position = points[i + 1].position;
				points[i].rotation = points[i + 1].rotation;
				points[i].direction = points[i + 1].direction;
				points[i].height = points[i + 1].height;
				points[i].gaitStand = points[i + 1].gaitStand;
				points[i].gaitWalk = points[i + 1].gaitWalk;
				points[i].gaitJog = points[i + 1].gaitJog;
				points[i].gaitCrouch = points[i + 1].gaitCrouch;
				points[i].gaitJump = points[i + 1].gaitJump;
				points[i].gaitBump = points[i + 1].gaitBump;
			}

			// Update current trajectory
			var standAmount = GetStandAmount();

			var trajectoryUpdate = points[trajectoryLength / 2].rotation * new Vector3(Y[0], 0, Y[1]);
			points[trajectoryLength / 2].position += standAmount * trajectoryUpdate;

			points[trajectoryLength / 2].direction = Quaternion.AngleAxis(standAmount * -Y[2, 0] * Mathf.Rad2Deg, Vector3.up)
			                                         * points[trajectoryLength / 2].direction;

			points[trajectoryLength / 2].rotation = Quaternion.AngleAxis(
				Mathf.Atan2(points[trajectoryLength / 2].direction.x,
					points[trajectoryLength / 2].direction.z)
				* Mathf.Rad2Deg, Vector3.up);

			// Collide with walls
			for (var j = 0; j < terrainWalls.Length; j++) {
				var trajectoryPoint = new Vector2(points[trajectoryLength / 2].position.x * scaleFactor, points[trajectoryLength / 2].position.z * scaleFactor);
				var segmentPoint = Utils.SegmentNearest(terrainWalls[j].wallStart, terrainWalls[j].wallEnd, trajectoryPoint);

				var segmentDistance = (segmentPoint - trajectoryPoint).magnitude;

				if (!(segmentDistance < wallWidth + wallVal)) continue;
				var point0 = (wallWidth + 0) * (trajectoryPoint - segmentPoint).normalized + segmentPoint;
				var point1 = (wallWidth + wallVal) * (trajectoryPoint - segmentPoint).normalized + segmentPoint;
				var point = Vector2.Lerp(point0, point1, Mathf.Clamp01((segmentDistance - wallWidth) / wallVal));

				points[trajectoryLength / 2].position.x = point.x * oppositeScaleFactor;
				points[trajectoryLength / 2].position.z = point.y * oppositeScaleFactor;
			}

			// Update future trajectory
			var w = trajectoryLength / 2 / 10;
			for (var i = trajectoryLength / 2 + 1; i < trajectoryLength; i++) {
				var m = (i - trajectoryLength / 2 / 10) % 1f;

				points[i].position.x = (1 - m) * Y[8 + w * 0 + i / 10 - w] + m * Y[8 + w * 0 + i / 10 - (w + 1)];
				points[i].position.z = (1 - m) * Y[8 + w * 1 + i / 10 - w] + m * Y[8 + w * 1 + i / 10 - (w + 1)];
				points[i].direction.x = (1 - m) * Y[8 + w * 2 + i / 10 - w] + m * Y[8 + w * 2 + i / 10 - (w + 1)];
				points[i].direction.z = (1 - m) * Y[8 + w * 3 + i / 10 - w] + m * Y[8 + w * 3 + i / 10 - (w + 1)];

				points[i].position = points[trajectoryLength / 2].rotation * points[i].position + points[trajectoryLength / 2].position;
				points[i].direction = Vector3.Normalize(points[trajectoryLength / 2].rotation * points[i].direction);
				points[i].rotation = Quaternion.AngleAxis(
					Mathf.Atan2(points[i].direction.x, points[i].direction.z) * Mathf.Rad2Deg,
					Vector3.up);
			}
		}

		public void UpdatePhase(Matrix Y) {
			phase = (phase + (GetStandAmount() * 0.9f + 0.1f) * (2f * Mathf.PI) * Y[3, 0]) % (2f * Mathf.PI);
		}

		protected float GetStandAmount() => Mathf.Pow(1f - points[trajectoryLength / 2].gaitStand, 0.25f);
		public bool IsStanding() => points[trajectoryLength / 2].gaitStand > .9f;

		public void DisplayTrajectory() {
			// Middle point
			for (var i = 0; i < trajectoryLength; i += 10) {
				var posCenter = -points[i].position;
				posCenter.Scale(new Vector3(scaleFactor, scaleFactor, scaleFactor));

				points[i].framePoint.transform.localPosition = -transform.position - posCenter;

				if (i / 10 == 6) transform.position = -posCenter;
			}

			// Left and right point
			for (var i = 0; i < trajectoryLength; i += 10) {
				// left
				var posLeft = Vector3.up + points[i].rotation * new Vector3(-sidePointsOffset * scaleFactor, 0, 0);
				points[i].framePoint.transform.GetChild(0).localPosition = posLeft;

				// right
				var posRight = Vector3.up + points[i].rotation * new Vector3(sidePointsOffset * scaleFactor, 0, 0);
				points[i].framePoint.transform.GetChild(2).localPosition = posRight;
			}

			// Direction arrow
			for (var i = 0; i < trajectoryLength; i += 10) {
				var angle = Quaternion.AngleAxis(
					Mathf.Atan2(points[i].direction.x, points[i].direction.z) * Mathf.Rad2Deg,
					Vector3.up); //new Vector3(0, 1f, 0));
				angle *= new Quaternion(0.7f, 0, 0, 0.7f);

				points[i].framePoint.transform.GetChild(1).localRotation = angle;
			}
		}

		public void DisplayJoints() {
			for (var i = 0; i < jointsNumber; i++) {
				var JointName = joints[i].jointName;
				var position = joints[i].position;
				position.Scale(new Vector3(scaleFactor, scaleFactor, scaleFactor));

				joints[i].jointPoint.transform.localPosition = new Vector3(
					transform.position.x - position.x,
					-(transform.position.y - position.y), // because of that weird 180 degree rotation about the vertical axis in the character model?
					transform.position.z - position.z);

				joints[i].jointPoint.transform.rotation = joints[i].rotation;
			}
		}

		public void Crouch() => crouchedTarget = crouchedTarget == 0 ? 1f : 0;

		public ref JointsComponents GetJoint(JointType type) => ref joints[(int)type];
	}
}