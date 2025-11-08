// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Moves (snaps) all vertex positions to an imaginary grid given by gridSize.
		/// For example, if gridSize is 0.01f all vertices are snapped to the nearest 1cm coordinate.
		/// </summary>
		/// <param name="gridSize"></param>
		public void SnapVerticesToGrid(float gridSize)
		{
			for (var i = 0; i < ValidVertexCount; i++)
			{
				var vertex = GetVertex(i);
				if (vertex.IsValid)
				{
					vertex.SnapPosition(gridSize);
					SetVertex(vertex);
				}
			}
		}

	/// <summary>
	/// Flips the face by reversing its loop winding order.
	/// This effectively reverses the face normal direction.
	/// </summary>
	/// <param name="faceIndex">Index of the face to flip</param>
	public void FlipFace(int faceIndex)
	{
		var face = GetFace(faceIndex);
		if (!face.IsValid)
			throw new ArgumentException($"Face {faceIndex} is invalid", nameof(faceIndex));

		// Traverse all loops and swap their prev/next pointers
		var loopIndex = face.FirstLoopIndex;
		var elementCount = face.ElementCount;
		for (var i = 0; i < elementCount; i++)
		{
			var loop = GetLoop(loopIndex);
			
			// Swap prev and next to reverse winding order
			var temp = loop.PrevLoopIndex;
			loop.PrevLoopIndex = loop.NextLoopIndex;
			loop.NextLoopIndex = temp;
			
			SetLoop(loop);
			loopIndex = loop.PrevLoopIndex; // Use prev since we just swapped
		}
	}

		private (int, int) GetBaseEdgeDiskCycleIndices(int vertexIndex)
		{
			// get prev/next edge from vertex base edge
			var vertex = GetVertex(vertexIndex);
			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			return baseEdge.GetDiskCycleIndices(vertexIndex);
		}
	}
}