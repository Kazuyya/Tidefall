/*
 * Copyright (c) 2023 MiniGames
 *
 * Check out how to use it here.
 * https://www.youtube.com/channel/UCrLZAN_rgpW7i84gDAHHH1g
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using UnityEngine;

namespace Tiny
{
	public enum TrailStopMode
	{
		/// <summary>Freeze position only; trail keeps updating (shrinks) for the duration, then disappears.</summary>
		ShrinkThenHide,
		/// <summary>Freeze position and trail; fade out over the duration, then disappear.</summary>
		FreezeAndFadeOut
	}

	public class Trail : MonoBehaviour
	{
		[SerializeField, Tooltip("The material to apply to the trail.")]
		private Material material = null;

		[SerializeField, Tooltip("Define the lifetime of a point in the trail, in seconds.")]
		private float duration = 0.1f;

		[SerializeField, Tooltip("Increase this value to make the trail corners appear rounder.")]
		private int corner = 1;

		[SerializeField, Tooltip("Enable this to connect the first and last positions of the line, and form a closed loop.")]
		private bool loop = false;

		[SerializeField, Tooltip("The array of Vector3 points to connect.")]
		private Vector3[] points = new Vector3[] { new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f) };

		[SerializeField, Tooltip("Id for AttackDataSO Trail Effects (enable/disable at normalized time). Must match entry in attack.")]
		private string trailId = "";

		[SerializeField, Tooltip("If true, trail does not run until StartTrail() is called (e.g. on attack).")]
		private bool startInactive = true;

		[SerializeField, Tooltip("How long the frozen trail stays visible after StopTrail(). 0 = never destroy.")]
		private float frozenTrailLifetime = 2f;

		[SerializeField, Tooltip("Set true to start trail, false to stop (see Stop Mode).")]
		private bool enableTrail = false;

		[SerializeField, Tooltip("Mode 1: Freeze position only, trail keeps updating (shrinking) for Frozen Trail Lifetime then hide. Mode 2: Freeze position and trail, fade out over Frozen Trail Lifetime then hide.")]
		private TrailStopMode stopMode = TrailStopMode.ShrinkThenHide;

		[NonSerialized] bool _trailActive = false;
		[NonSerialized] GameObject trailGo = null;
		[NonSerialized] Mesh mesh = null;

		[NonSerialized] Vector3[] vertices = null;
		[NonSerialized] Transform cacheTM = null;

		[NonSerialized] int lastSegmentCount = -1;
		[NonSerialized] int lastCorner = -1;
		[NonSerialized] int pointCount = -1;
		[NonSerialized] float toCornerT = 0f;

		Coroutine update = null;

		/// <summary>
		/// The array of Vector3 points to connect.
		/// </summary>
		public Vector3[] Points {
			get{ return points; }
			set{ points = value; }
		}

		/// <summary>
		/// Enable this to connect the first and last positions of the line, and form a closed loop.
		/// </summary>
		public bool Loop {	get { return loop && points.Length >= 3; }	}

		/// <summary>
		/// Removes all points from the TrailRenderer. Useful for restarting a trail from a new position.
		/// </summary>
		public void Clear()
		{
			if (!enabled || pointCount <= 1 || !trailGo)
				return;

			if (update != null)
				StopCoroutine(update);

			ClearVertices();

			update = StartCoroutine(PhysicsUpdate());
		}

		/// <summary>Trail id for AttackDataSO Trail Effects (must match entry in attack).</summary>
		public string TrailId { get => trailId; set => trailId = value ?? ""; }

		/// <summary>Stop mode when StopTrail is called (ShrinkThenHide or FreezeAndFadeOut). Can be set from code before StartTrail.</summary>
		public TrailStopMode StopMode { get => stopMode; set => stopMode = value; }
		/// <summary>How long the frozen trail stays visible after StopTrail. Can be set from code before StartTrail.</summary>
		public float FrozenTrailLifetime { get => frozenTrailLifetime; set => frozenTrailLifetime = value; }

		/// <summary>
		/// When true, trail is drawing and following this transform. When false, trail is stopped (frozen in place, then after Frozen Trail Lifetime the frozen copy is removed and position can update again).
		/// </summary>
		public bool EnableTrail
		{
			get => enableTrail;
			set => enableTrail = value;
		}

		/// <summary>
		/// Start drawing the trail (e.g. when attack starts). Trail will follow this transform until StopTrail().
		/// </summary>
		public void StartTrail()
		{
			if (!trailGo)
				return;
			enableTrail = true;
			_trailActive = true;
			trailGo.SetActive(true);
			if (vertices == null || pointCount <= 1)
				Initialize((int)(duration / Time.fixedDeltaTime));
			else
				Clear();
		}

		/// <summary>
		/// Stop drawing the trail. The current trail freezes in place (does not follow the weapon anymore).
		/// A copy of the trail mesh is left in the scene and destroyed after frozenTrailLifetime (e.g. 2s), then position can update again.
		/// </summary>
		public void StopTrail()
		{
			enableTrail = false;
			_trailActive = false;
			if (update != null)
			{
				StopCoroutine(update);
				update = null;
			}
			if (trailGo != null && vertices != null && pointCount > 1)
			{
				CreateFrozenTrailCopy();
				trailGo.SetActive(false);
			}
		}

		private void CreateFrozenTrailCopy()
		{
			Mesh sourceMesh = trailGo.GetComponent<MeshFilter>().sharedMesh;
			Mesh frozenMesh = new Mesh { name = "Frozen Trail" };
			frozenMesh.vertices = sourceMesh.vertices;
			frozenMesh.uv = sourceMesh.uv;
			frozenMesh.SetIndices(sourceMesh.GetIndices(0), MeshTopology.Triangles, 0);
			frozenMesh.RecalculateBounds();

			GameObject frozenGo = new GameObject(name + "FrozenTrail", typeof(MeshFilter), typeof(MeshRenderer));
			frozenGo.GetComponent<MeshFilter>().sharedMesh = frozenMesh;
			frozenGo.layer = gameObject.layer;
			MeshRenderer r = frozenGo.GetComponent<MeshRenderer>();
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			if (stopMode == TrailStopMode.ShrinkThenHide)
			{
				r.material = material;
				if (frozenTrailLifetime > 0f)
					StartCoroutine(ShrinkFrozenTrail(frozenGo, frozenMesh, frozenTrailLifetime));
				else
					Destroy(frozenGo);
			}
			else
			{
				Material fadeMat = new Material(material);
				r.material = fadeMat;
				if (frozenTrailLifetime > 0f)
					StartCoroutine(FadeOutFrozenTrail(frozenGo, fadeMat, frozenTrailLifetime));
				else
					Destroy(frozenGo);
			}
		}

		private IEnumerator ShrinkFrozenTrail(GameObject frozenGo, Mesh frozenMesh, float dur)
		{
			Vector3[] verts = frozenMesh.vertices;
			int ptCount = pointCount;
			if (ptCount <= 0) { Destroy(frozenGo); yield break; }
			int step = ptCount;
			int numRows = verts.Length / step;
			if (numRows <= 1)
			{
				Destroy(frozenMesh);
				Destroy(frozenGo);
				yield break;
			}
			int shiftsNeeded = numRows - 1;
			YieldInstruction wait = new WaitForFixedUpdate();
			float endTime = Time.time + dur;
			int shiftsDone = 0;
			while (shiftsDone < shiftsNeeded && Time.time < endTime)
			{
				yield return wait;
				Array.Copy(verts, 0, verts, step, verts.Length - step);
				frozenMesh.vertices = verts;
				frozenMesh.RecalculateBounds();
				shiftsDone++;
			}
			float remaining = endTime - Time.time;
			if (remaining > 0f)
				yield return new WaitForSeconds(remaining);
			Destroy(frozenMesh);
			Destroy(frozenGo);
		}

		private IEnumerator FadeOutFrozenTrail(GameObject frozenGo, Material fadeMat, float dur)
		{
			float elapsed = 0f;
			string colorProp = fadeMat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
			Color c = fadeMat.GetColor(colorProp);
			float startA = c.a;
			while (elapsed < dur)
			{
				elapsed += Time.deltaTime;
				c.a = Mathf.Lerp(startA, 0f, elapsed / dur);
				fadeMat.SetColor(colorProp, c);
				yield return null;
			}
			Destroy(fadeMat);
			Destroy(frozenGo);
		}

		private void Start()
		{
			cacheTM = transform;

			trailGo = new GameObject(name + "Trail", typeof(MeshFilter), typeof(MeshRenderer));
			DontDestroyOnLoad(trailGo);

			mesh = new Mesh { name = "Trail Effect" };
			mesh.MarkDynamic();
			trailGo.GetComponent<MeshFilter>().sharedMesh = mesh;
			trailGo.layer = gameObject.layer;

			MeshRenderer meshRenderer = trailGo.GetComponent<MeshRenderer>();
			meshRenderer.material = material;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			if (!startInactive)
			{
				enableTrail = true;
				_trailActive = true;
				Initialize((int)(duration / Time.fixedDeltaTime));
			}
			else
				trailGo.SetActive(false);
		}

		private void OnDestroy()
		{
			if (mesh != null)
				DestroyImmediate(mesh);
			mesh = null;

			if (trailGo != null)
				DestroyImmediate(trailGo);
			trailGo = null;
		}

		private void OnEnable()
		{
			if (trailGo == null)
				return;
			if (startInactive)
			{
				trailGo.SetActive(false);
				return;
			}
			trailGo.SetActive(true);
			Initialize((int)(duration / Time.fixedDeltaTime));
		}

		private void OnDisable()
		{
			if (trailGo)
				trailGo.SetActive(false);

			if (update != null)
				StopCoroutine(update);
			update = null;
		}

		private void SetVerticesAndCorner()
		{
			int nextSegmentPoint = pointCount + (pointCount * corner);

			Array.Copy(vertices, 0, vertices, nextSegmentPoint, vertices.Length - nextSegmentPoint);

			TransformVertices();

			int next2 = nextSegmentPoint * 2;
			int next3 = nextSegmentPoint * 3;

			for (int x = -1; ++x < pointCount;)
			{
				Vector3 a = vertices[x];
				Vector3 b = vertices[x + nextSegmentPoint];
				Vector3 c = vertices[x + next2];
				Vector3 d = vertices[x + next3];

				for (int n = -1, index = pointCount + x; ++n < corner; index += pointCount)
				{
					float t = (n + 1) * toCornerT;
					vertices[index] = CatmullRomSpline(a, a, b, c, t);
					vertices[index + nextSegmentPoint] = CatmullRomSpline(a, b, c, d, t);
				}
			}
		}

		private void SetVertices()
		{
			Array.Copy(vertices, 0, vertices, pointCount, vertices.Length - pointCount);
			TransformVertices();
		}

		private IEnumerator PhysicsUpdate()
		{
			YieldInstruction wait = new WaitForFixedUpdate();

			Action action = corner > 0 ? SetVerticesAndCorner : SetVertices;

			while (true)
			{
				yield return wait;
				action();
				cacheTM.hasChanged = false;
			}
		}

		private void Update()
		{
			if (enableTrail == _trailActive)
				return;
			_trailActive = enableTrail;
			if (_trailActive)
				StartTrail();
			else
				StopTrail();
		}

		private void LateUpdate()
		{
			if (vertices == null || !trailGo.activeSelf)
				return;
			if (cacheTM.hasChanged)
				TransformVertices();

			mesh.vertices = vertices;
			mesh.RecalculateBounds();
		}

		private void TransformVertices()
		{
			Matrix4x4 localToWorldMatrix = cacheTM.localToWorldMatrix;
			for (int i = -1; ++i < pointCount;)
				vertices[i] = localToWorldMatrix.MultiplyPoint3x4(points[i]);
		}

		private void ClearVertices()
		{
			TransformVertices();

			for (int i = pointCount; i < vertices.Length; i += pointCount)
				Array.Copy(vertices, 0, vertices, i, pointCount);
		}		

		private void Initialize(int segment)
		{
			int corner = segment >= 3 ? this.corner : 0;

			if (lastSegmentCount == segment && pointCount == points.Length && lastCorner == corner)
			{
				ClearVertices();

				update = StartCoroutine(PhysicsUpdate());
				return;
			}

			pointCount = points.Length;
			lastCorner = corner;
			lastSegmentCount = segment;

			if (pointCount <= 1)
			{
				mesh.Clear();
				return;
			}

			int segmentAndCorner = segment + (segment * corner);

			Vector2[] uvs = new Vector2[pointCount * (segmentAndCorner + 1)];

			bool isLoop = Loop;

			int[] indexs = new int[(isLoop ? pointCount : pointCount - 1) * 6 * segmentAndCorner];

			Vector2 uv = new Vector2();

			int endPoint = pointCount - 1;

			float invSegment = 1f / segment;
			float invEnd = 1f / endPoint;
			toCornerT = 1f / (corner + 1);

			for (int y = -1, i = -1; ++y <= segment;)
			{
				uv.y = y * invSegment;
				for (int x = -1; ++x < pointCount;)
				{
					uv.x = x * invEnd;
					uvs[++i] = uv;
				}

				if (y == segment)
					continue;

				for (int n = -1; ++n < corner;)
				{
					uv.y = Mathf.Lerp(y * invSegment, (y + 1) * invSegment, (n + 1) * toCornerT);

					for (int x = -1; ++x < pointCount;)
					{
						uv.x = x * invEnd;
						uvs[++i] = uv;
					}
				}
			}

			int index = 0;
			int lineCount = isLoop ? endPoint+1 : endPoint;

			for (int y = -1; ++y < segmentAndCorner;)
			{
				int beginIndex = y * pointCount;
				int nextIndex = y * pointCount;
				if (isLoop)
					beginIndex += endPoint;
				else
					nextIndex += 1;

				for (int x = -1; ++x < lineCount; index += 6, beginIndex = nextIndex++)
				{
					indexs[index + 0] = beginIndex;
					indexs[index + 1] = beginIndex + pointCount;
					indexs[index + 2] = nextIndex;					
					indexs[index + 3] = nextIndex;
					indexs[index + 4] = beginIndex + pointCount;
					indexs[index + 5] = nextIndex + pointCount;					
				}
			}

			vertices = new Vector3[uvs.Length];
			ClearVertices();

			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.SetIndices(indexs, MeshTopology.Triangles, 0);

			update = StartCoroutine(PhysicsUpdate());
		}

		/// <summary>
		/// p1 과 p2 사이에 곡선을 생성한다.
		/// 
		/// t == 0 일 때 p1을, t == 1 일 때 p2를 리턴한다.
		/// </summary>
		static Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			float t2 = t * t;
			float t3 = t2 * t;
			return 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
		}
	}
}