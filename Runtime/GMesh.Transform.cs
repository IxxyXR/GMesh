// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		public const float DefaultScale = 1f;

		private float3 _pivot = float3.zero;

		public float3 Pivot { get => _pivot; set => _pivot = value; }

		/// <summary>
		/// Applies the transformation to all vertices with the Pivot as the center.
		/// </summary>
		/// <param name="transform"></param>
		public void ApplyTransform(in Transform transform) => Transform.Apply(_data, transform, _pivot);

		/// <summary>
		/// GMesh transform representation.
		/// </summary>
		[BurstCompile] [StructLayout(LayoutKind.Sequential)] [Serializable]
		public struct Transform
		{
			[Tooltip("Offset from origin (0,0,0)")]
			public float3 Translation;
			[Tooltip("Rotation is in degrees (Euler)")]
			public float3 Rotation;
			[Tooltip("Scale is self-explanatory")]
			public float3 Scale;


		internal static void Apply(in GraphData data, in Transform t, in float3 pivot)
		{
			var rigidTransform = t.AsRigidTransform();
			var vCount = data.Vertices.Length;
			for (var i = 0; Hint.Likely(i < vCount); i++)
			{
				var vertex = data.GetVertex(i);
				var vPos = vertex.Position;
				
				// Transform relative to pivot:
				// 1. Move to pivot origin
				var localPos = vPos - pivot;
				// 2. Apply rotation and scale
				localPos = math.transform(rigidTransform, localPos) * t.Scale;
				// 3. Move back from pivot origin
				vertex.Position = localPos + pivot;
				
				data.SetVertex(vertex);
			}
		}

			public Transform(float3 translation, float3 rotation, float3 scale)
			{
				Translation = translation;
				Rotation = rotation;
				Scale = scale;
			}

			public RigidTransform AsRigidTransform() => new(quaternion.Euler(math.radians(Rotation)), Translation);
		}
	}
}