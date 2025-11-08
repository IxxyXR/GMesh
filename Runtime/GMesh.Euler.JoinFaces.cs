// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
	/// <summary>
	/// Joins two adjacent faces by removing their shared edge.
	/// The faces are merged into face0, and face1 is invalidated.
	/// </summary>
	/// <param name="face0Index">Index of first face (will receive merged result)</param>
	/// <param name="face1Index">Index of second face (will be invalidated)</param>
	/// <returns>True if successful, false if faces don't share an edge or aren't 2-manifold</returns>
	public bool JoinFacesAndDeleteEdge(int face0Index, int face1Index)
	{
		var face0 = GetFace(face0Index);
		var face1 = GetFace(face1Index);

		if (!face0.IsValid || !face1.IsValid)
			return false;

		// Find the shared edge between the two faces
		var sharedEdgeIndex = FindSharedEdge(face0Index, face1Index);
		if (sharedEdgeIndex == UnsetIndex)
			return false; // Faces don't share an edge

		var sharedEdge = GetEdge(sharedEdgeIndex);

		// Get the loops on the shared edge for both faces
		var loop0OnEdge = GetLoop(sharedEdge.BaseLoopIndex);
		var loop1OnEdge = GetLoop(loop0OnEdge.NextRadialLoopIndex);

		// Ensure we have the correct face assignments
		if (loop0OnEdge.FaceIndex != face0Index)
		{
			var temp = loop0OnEdge;
			loop0OnEdge = loop1OnEdge;
			loop1OnEdge = temp;
		}

		if (loop0OnEdge.FaceIndex != face0Index || loop1OnEdge.FaceIndex != face1Index)
			return false; // Edge doesn't connect these faces

		// Verify this is a 2-manifold edge (only two faces share it)
		if (loop0OnEdge.NextRadialLoopIndex != loop1OnEdge.Index || 
		    loop1OnEdge.NextRadialLoopIndex != loop0OnEdge.Index)
			return false; // More than 2 faces share this edge

		// Remove the shared edge loops from their face cycles and join the remaining loops
		JoinFacesInternal_MergeLoops(face0Index, face1Index, loop0OnEdge, loop1OnEdge);

		// Remove edge from vertex disk cycles
		RemoveEdgeFromDiskCycle(sharedEdge.AVertexIndex, sharedEdge);
		RemoveEdgeFromDiskCycle(sharedEdge.OVertexIndex, sharedEdge);

		// Invalidate the shared edge and loops
		InvalidateLoop(loop0OnEdge.Index);
		InvalidateLoop(loop1OnEdge.Index);
		InvalidateEdge(sharedEdgeIndex);

		// Invalidate face1 (face0 now contains all loops)
		InvalidateFace(face1Index);

		// Update face0's element count
		face0 = GetFace(face0Index);
		face0.ElementCount = face0.ElementCount + face1.ElementCount - 2; // -2 for removed loops
		SetFace(face0);

		return true;
	}

	/// <summary>
	/// Finds an edge shared by two faces.
	/// </summary>
	private int FindSharedEdge(int face0Index, int face1Index)
	{
		var face0 = GetFace(face0Index);
		var loopIndex = face0.FirstLoopIndex;
		var elementCount = face0.ElementCount;

		for (var i = 0; i < elementCount; i++)
		{
			var loop = GetLoop(loopIndex);
			var edge = GetEdge(loop.EdgeIndex);
			
			// Check if this edge's radial loops include a loop from face1
			var radialLoop = GetLoop(loop.NextRadialLoopIndex);
			if (radialLoop.FaceIndex == face1Index)
				return edge.Index;

			loopIndex = loop.NextLoopIndex;
		}

		return UnsetIndex;
	}

	private void JoinFacesInternal_MergeLoops(int face0Index, int face1Index, Loop loop0OnEdge, Loop loop1OnEdge)
	{
		// Get the loops before and after the shared edge loops
		var loop0Prev = GetLoop(loop0OnEdge.PrevLoopIndex);
		var loop0Next = GetLoop(loop0OnEdge.NextLoopIndex);
		var loop1Prev = GetLoop(loop1OnEdge.PrevLoopIndex);
		var loop1Next = GetLoop(loop1OnEdge.NextLoopIndex);

		// Connect face0's prev loop to face1's next loop
		loop0Prev.NextLoopIndex = loop1Next.Index;
		loop1Next.PrevLoopIndex = loop0Prev.Index;

		// Connect face1's prev loop to face0's next loop
		loop1Prev.NextLoopIndex = loop0Next.Index;
		loop0Next.PrevLoopIndex = loop1Prev.Index;

		SetLoop(loop0Prev);
		SetLoop(loop0Next);
		SetLoop(loop1Prev);
		SetLoop(loop1Next);

		// Update all loops from face1 to point to face0
		var currentLoopIndex = loop1Next.Index;
		do
		{
			if (currentLoopIndex == loop1OnEdge.Index)
				break; // Skip the loop we're deleting

			var currentLoop = GetLoop(currentLoopIndex);
			currentLoop.FaceIndex = face0Index;
			SetLoop(currentLoop);
			currentLoopIndex = currentLoop.NextLoopIndex;
		} while (currentLoopIndex != loop1Next.Index);

		// Update face0's FirstLoopIndex if it was the deleted loop
		var face0 = GetFace(face0Index);
		if (face0.FirstLoopIndex == loop0OnEdge.Index)
		{
			face0.FirstLoopIndex = loop0Next.Index;
			SetFace(face0);
		}
	}
	}
}
