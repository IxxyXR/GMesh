// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
	
	/// <summary>
	/// Splits a face by creating an edge between two non-adjacent vertices.
	/// The original face is divided into two faces along the new edge.
	/// </summary>
	/// <param name="faceIndex">Index of the face to split</param>
	/// <param name="vertex0Index">Index of first vertex on the face</param>
	/// <param name="vertex1Index">Index of second vertex on the face (must not be adjacent to first)</param>
	/// <returns>Tuple of (new face index, new edge index)</returns>
	public (int, int) SplitFaceAndCreateEdge(int faceIndex, int vertex0Index, int vertex1Index)
	{
		var face = GetFace(faceIndex);
		if (!face.IsValid)
			throw new ArgumentException($"Face {faceIndex} is invalid", nameof(faceIndex));

		// Find if an edge already exists between these vertices
		var existingEdgeIndex = FindExistingEdgeIndex(vertex0Index, vertex1Index);
		if (existingEdgeIndex != UnsetIndex)
			throw new ArgumentException($"Edge already exists between vertices {vertex0Index} and {vertex1Index}");

		// Find the loops for both vertices in this face
		var loop0Index = FindLoopInFace(faceIndex, vertex0Index);
		var loop1Index = FindLoopInFace(faceIndex, vertex1Index);

		if (loop0Index == UnsetIndex || loop1Index == UnsetIndex)
			throw new ArgumentException("One or both vertices not found in face");

		// Ensure vertices are not adjacent (would create degenerate face)
		var loop0 = GetLoop(loop0Index);
		var loop1 = GetLoop(loop1Index);
		if (loop0.NextLoopIndex == loop1Index || loop0.PrevLoopIndex == loop1Index)
			throw new ArgumentException("Vertices are adjacent - cannot split face between adjacent vertices");

		// Create new edge between the vertices
		var newEdge = Edge.Create(vertex0Index, vertex1Index);
		var newEdgeIndex = AddEdge(ref newEdge);
		newEdge = GetEdge(newEdgeIndex);

		// Insert edge into disk cycles of both vertices
		InsertEdgeInDiskCycle(vertex0Index, ref newEdge);
		InsertEdgeInDiskCycle(vertex1Index, ref newEdge);
		SetEdge(newEdge);

		// Create new face
		var newFace = Face.Create(loop1Index);
		var newFaceIndex = AddFace(ref newFace);

		// Split the loop cycle and update face assignments
		SplitFaceInternal_SplitLoops(faceIndex, newFaceIndex, newEdgeIndex, loop0Index, loop1Index);

		return (newFaceIndex, newEdgeIndex);
	}

	/// <summary>
	/// Finds the loop in a face that starts with the given vertex.
	/// </summary>
	private int FindLoopInFace(int faceIndex, int vertexIndex)
	{
		var face = GetFace(faceIndex);
		var loopIndex = face.FirstLoopIndex;
		var elementCount = face.ElementCount;

		for (var i = 0; i < elementCount; i++)
		{
			var loop = GetLoop(loopIndex);
			if (loop.StartVertexIndex == vertexIndex)
				return loopIndex;
			loopIndex = loop.NextLoopIndex;
		}

		return UnsetIndex;
	}

	private void SplitFaceInternal_SplitLoops(int face0Index, int face1Index, int newEdgeIndex, int loop0Index, int loop1Index)
	{
		// Get loops
		var loop0 = GetLoop(loop0Index);
		var loop1 = GetLoop(loop1Index);

		// Create two new loops on the new edge
		var newLoop0 = Loop.Create(face0Index, newEdgeIndex, loop1.StartVertexIndex, loop0Index, loop1.PrevLoopIndex, newEdgeIndex, newEdgeIndex);
		var newLoop1 = Loop.Create(face1Index, newEdgeIndex, loop0.StartVertexIndex, loop1Index, loop0.PrevLoopIndex, newEdgeIndex, newEdgeIndex);

		var newLoop0Index = _data.ValidLoopCount;
		var newLoop1Index = newLoop0Index + 1;

		newLoop0.Index = newLoop0Index;
		newLoop1.Index = newLoop1Index;

		// Set radial loop connection (both loops share the new edge)
		newLoop0.SetRadialLoopIndices(newLoop1Index);
		newLoop1.SetRadialLoopIndices(newLoop0Index);

		_data.AddLoop(ref newLoop0);
		_data.AddLoop(ref newLoop1);

		// Update edge to point to one of its loops
		var edge = GetEdge(newEdgeIndex);
		edge.BaseLoopIndex = newLoop0Index;
		SetEdge(edge);

		// Relink loop chains
		// Face 0: loop0 -> ... -> loop0.Prev -> newLoop0 -> loop1.Prev -> ... -> back to loop0
		var loop0Prev = GetLoop(loop0.PrevLoopIndex);
		loop0Prev.NextLoopIndex = newLoop0Index;
		SetLoop(loop0Prev);

		var loop1Prev = GetLoop(loop1.PrevLoopIndex);
		loop1Prev.NextLoopIndex = loop1Index;
		SetLoop(loop1Prev);

		loop0.PrevLoopIndex = newLoop0Index;
		newLoop0.NextLoopIndex = loop1.PrevLoopIndex;
		newLoop0.PrevLoopIndex = loop0.PrevLoopIndex;

		// Face 1: loop1 -> ... -> loop1.Prev -> newLoop1 -> loop0.Prev -> ... -> back to loop1
		var loop0PrevOld = GetLoop(loop0.PrevLoopIndex);
		loop0PrevOld.NextLoopIndex = newLoop1Index;
		SetLoop(loop0PrevOld);

		loop1.PrevLoopIndex = newLoop1Index;
		newLoop1.NextLoopIndex = loop0.PrevLoopIndex;
		newLoop1.PrevLoopIndex = loop1.PrevLoopIndex;

		// Update all loops in face1 to point to the new face
		var currentLoopIndex = loop1Index;
		var face1ElementCount = 0;
		do
		{
			var currentLoop = GetLoop(currentLoopIndex);
			currentLoop.FaceIndex = face1Index;
			SetLoop(currentLoop);
			currentLoopIndex = currentLoop.NextLoopIndex;
			face1ElementCount++;
		} while (currentLoopIndex != loop1Index);

		// Update face element counts
		var face0 = GetFace(face0Index);
		var face1 = GetFace(face1Index);
		var originalCount = face0.ElementCount;
		face0.ElementCount = originalCount - face1ElementCount + 2; // +2 for new loops
		face1.ElementCount = face1ElementCount + 2;
		face1.FirstLoopIndex = loop1Index;
		SetFace(face0);
		SetFace(face1);

		SetLoop(loop0);
		SetLoop(loop1);
	}
	}
}
