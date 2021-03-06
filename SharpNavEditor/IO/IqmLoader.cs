﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
//
using System.Runtime.InteropServices;
using System.Text;
//
using OpenTK;
using System.Diagnostics;

namespace SharpNavEditor.IO
{
	[FileLoader(".iqm")]
	public class IqmParser : IModelLoader
	{


		public bool SupportsPositions { get { return true; } }
		public bool SupportsTextureCoordinates { get { return false; } }
		public bool SupportsNormals { get { return false; } }
		public bool SupportsTangents { get { return false; } }
		public bool SupportsBitangents { get { return false; } }
		public bool SupportsColors { get { return false; } }
		public bool SupportsAnimation { get { return false; } }
		public bool SupportsSkeleton { get { return false; } }
		public bool SupportsIndexing { get { return false; } }

		public int CustomVertexDataTypesCount { get { return 0; } }

		
		private enum IqmAttributeType
		{
			Position = 0,
			TexCoord = 1,
			Normal = 2,
			Tangent = 3,
			BlendIndexes = 4,
			BlendWeights = 5,
			Color = 6,
			Custom = 0x10
		}

		private enum IqmDataFormat
		{
			Byte = 0,
			UnsignedByte = 1,
			Short = 2,
			UnsignedShort = 3,
			Int = 4,
			UnsignedInt = 5,
			Half = 6,
			Float = 7,
			Double = 8
		}

		private enum IqmAnimationType
		{
			Loop = 1 << 0
		}

		/// <summary>
		/// The header at the top of every IQM file.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		private struct IqmHeader
		{
			/// <summary>
			/// The identifier for an IQM file - "INTERQUAKEMODEL\0".
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
			public string Magic;

			/// <summary>
			/// The version of the file. Must be at version 2 to work.
			/// </summary>
			public uint Version;

			/// <summary>
			/// Size of the file.
			/// </summary>
			public uint FileSize;

			/// <summary>
			/// File flags. There's nothing in the specification about the meaning of this field.
			/// </summary>
			public uint Flags;

			public uint TextCount;
			public uint TextOffset;

			public uint MeshesCount;
			public uint MeshesOffset;

			public uint VertexArraysCount;
			public uint VertexesCount;
			public uint VertexArraysOffset;

			public uint TrianglesCount;
			public uint TrianglesOffset;
			public uint AdjacencyOffset;

			public uint JointsCount;
			public uint JointsOffset;

			public uint PosesCount;
			public uint PosesOffset;

			public uint AnimationsCount;
			public uint AnimationsOffset;

			public uint FramesCount;
			public uint FrameChannelsCount;
			public uint FramesOffset;
			public uint BoundsOffset;

			public uint CommentCount;
			public uint CommentOffset;

			public uint ExtensionsCount;
			public uint ExtensionsOffset;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmHeader)); } }

			public void Parse(byte[] byteMe)
			{
				Magic = Encoding.UTF8.GetString(byteMe, 0, 16);
				Version = BitConverter.ToUInt32(byteMe, 4 * 4);
				FileSize = BitConverter.ToUInt32(byteMe, 5 * 4);
				Flags = BitConverter.ToUInt32(byteMe, 6 * 4);
				TextCount = BitConverter.ToUInt32(byteMe, 7 * 4);
				TextOffset = BitConverter.ToUInt32(byteMe, 8 * 4);
				MeshesCount = BitConverter.ToUInt32(byteMe, 9 * 4);
				MeshesOffset = BitConverter.ToUInt32(byteMe, 10 * 4);

				VertexArraysCount = BitConverter.ToUInt32(byteMe, 11 * 4);
				VertexesCount = BitConverter.ToUInt32(byteMe, 12 * 4);
				VertexArraysOffset = BitConverter.ToUInt32(byteMe, 13 * 4);

				TrianglesCount = BitConverter.ToUInt32(byteMe, 14 * 4);
				TrianglesOffset = BitConverter.ToUInt32(byteMe, 15 * 4);
				AdjacencyOffset = BitConverter.ToUInt32(byteMe, 16 * 4);

				JointsCount = BitConverter.ToUInt32(byteMe, 17 * 4);
				JointsOffset = BitConverter.ToUInt32(byteMe, 18 * 4);

				PosesCount = BitConverter.ToUInt32(byteMe, 19 * 4);
				PosesOffset = BitConverter.ToUInt32(byteMe, 20 * 4);

				AnimationsCount = BitConverter.ToUInt32(byteMe, 21 * 4);
				AnimationsOffset = BitConverter.ToUInt32(byteMe, 22 * 4);

				FramesCount = BitConverter.ToUInt32(byteMe, 23 * 4);
				FrameChannelsCount = BitConverter.ToUInt32(byteMe, 24 * 4);
				FramesOffset = BitConverter.ToUInt32(byteMe, 25 * 4);
				BoundsOffset = BitConverter.ToUInt32(byteMe, 26 * 4);

				CommentCount = BitConverter.ToUInt32(byteMe, 27 * 4);
				CommentOffset = BitConverter.ToUInt32(byteMe, 28 * 4);

				ExtensionsCount = BitConverter.ToUInt32(byteMe, 29 * 4);
				ExtensionsOffset = BitConverter.ToUInt32(byteMe, 30 * 4);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmMesh
		{
			public uint Name;
			public uint Material;

			public uint FirstVertex;
			public uint VertexesCount;

			public uint FirstTriangle;
			public uint TrianglesCount;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmMesh)); } }

			public void Parse(byte[] byteMe)
			{
				Name = BitConverter.ToUInt32(byteMe, 0);
				Material = BitConverter.ToUInt32(byteMe, 1 * 4);

				FirstVertex = BitConverter.ToUInt32(byteMe, 2 * 4);
				VertexesCount = BitConverter.ToUInt32(byteMe, 3 * 4);

				FirstTriangle = BitConverter.ToUInt32(byteMe, 4 * 4);
				TrianglesCount = BitConverter.ToUInt32(byteMe, 5 * 4);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmTriangle
		{
			public uint Vertex0;
			public uint Vertex1;
			public uint Vertex2;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmTriangle)); } }

			public void Parse(byte[] byteMe)
			{
				Vertex0 = BitConverter.ToUInt32(byteMe, 0);
				Vertex1 = BitConverter.ToUInt32(byteMe, 1 * 4);
				Vertex2 = BitConverter.ToUInt32(byteMe, 2 * 4);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmAdjacency
		{
			public uint Triangle0;
			public uint Triangle1;
			public uint Triangle2;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmAdjacency)); } }

			public void Parse(byte[] byteMe)
			{
				Triangle0 = BitConverter.ToUInt32(byteMe, 0);
				Triangle1 = BitConverter.ToUInt32(byteMe, 1 * 4);
				Triangle2 = BitConverter.ToUInt32(byteMe, 2 * 4);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmJoint
		{
			public uint Name;
			public int Parent;

			public Vector3 Translate;
			public Quaternion Rotate;
			public Vector3 Scale;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmJoint)); } }

			public void Parse(byte[] byteMe)
			{
				Name = BitConverter.ToUInt32(byteMe, 0);
				Parent = BitConverter.ToInt32(byteMe, 1 * 4);
				Translate = new Vector3(BitConverter.ToSingle(byteMe, 2 * 4), BitConverter.ToSingle(byteMe, 3 * 4), BitConverter.ToSingle(byteMe, 4 * 4));
				Rotate = new Quaternion(BitConverter.ToSingle(byteMe, 5 * 4), BitConverter.ToSingle(byteMe, 6 * 4), BitConverter.ToSingle(byteMe, 7 * 4), BitConverter.ToSingle(byteMe, 8 * 4));
				Scale = new Vector3(BitConverter.ToSingle(byteMe, 9 * 4), BitConverter.ToSingle(byteMe, 10 * 4), BitConverter.ToSingle(byteMe, 11 * 4));
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmPose
		{
			public int Parent;
			public uint Mask;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
			public float[] ChannelOffset;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
			public float[] ChannelScale;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmPose)); } }

			public void Parse(byte[] byteMe)
			{
				Parent = BitConverter.ToInt32(byteMe, 0);
				Mask = BitConverter.ToUInt32(byteMe, 1 * 4);
				for (int x = 0; x < 10; x++)
				{
					ChannelOffset[x] = BitConverter.ToSingle(byteMe, (1 + x) * 4);
					ChannelScale[x] = BitConverter.ToSingle(byteMe, (11 + x) * 4);
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmAnimation
		{
			public uint Name;

			public uint FirstFrame;
			public uint FramesCount;

			public float Framerate;

			public uint Flags;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmAnimation)); } }

			public void Parse(byte[] byteMe)
			{
				Name = BitConverter.ToUInt32(byteMe, 0);
				FirstFrame = BitConverter.ToUInt32(byteMe, 1 * 4);
				FramesCount = BitConverter.ToUInt32(byteMe, 2 * 4);
				Framerate = BitConverter.ToSingle(byteMe, 3 * 4);
				Flags = BitConverter.ToUInt32(byteMe, 4 * 4);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmVertexArray
		{
			public IqmAttributeType Type;
			public uint Flags;
			public IqmDataFormat Format;
			public uint Size;
			public uint Offset;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmVertexArray)); } }

			public void Parse(byte[] byteMe)
			{
				Type = (IqmAttributeType)BitConverter.ToInt32(byteMe, 0);
				Flags = BitConverter.ToUInt32(byteMe, 1 * 4);
				Format = (IqmDataFormat)BitConverter.ToInt32(byteMe, 2 * 4);
				Size = BitConverter.ToUInt32(byteMe, 3 * 4);
				Offset = BitConverter.ToUInt32(byteMe, 4 * 4);

			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct IqmBounds
		{
			public Vector3 BBMin;
			public Vector3 BBMax;

			public float XYRadius;
			public float Radius;

			public static int SizeInBytes { get { return Marshal.SizeOf(typeof(IqmBounds)); } }

			public void Parse(byte[] byteMe)
			{
				BBMin = new Vector3(BitConverter.ToSingle(byteMe, 0), BitConverter.ToSingle(byteMe, 1 * 4), BitConverter.ToSingle(byteMe, 2 * 4));
				BBMax = new Vector3(BitConverter.ToSingle(byteMe, 3 * 4), BitConverter.ToSingle(byteMe, 4 * 4), BitConverter.ToSingle(byteMe, 5 * 4));
				XYRadius = BitConverter.ToSingle(byteMe, 6 * 4);
				Radius = BitConverter.ToSingle(byteMe, 7 * 4);
			}
		}

		//public static Drawable Parse(string path)
		public IModelData LoadModel(string path)
		{
			var position = new List<Vector3>();
			var pos = new List<float>();
			var norm = new List<float>();

			if (!File.Exists(path))
				throw new ArgumentException("The file \"" + path + "\" does not exist.", "path");

			List<string> text;
			List<IqmMesh> meshes;
			List<IqmVertexArray> vertexArrays;
			List<IqmTriangle> triangles;
			List<IqmAdjacency> adjacency;
			List<IqmJoint> joints;
			List<IqmPose> poses;
			List<IqmAnimation> animations;
			//TODO matrix3x4 for frames.
			IqmBounds bounds;

			//List<VertexAttribute> attributes = new List<VertexAttribute>();

			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				using (BinaryReader reader = new BinaryReader(fs))
				{
					//make sure the file is long enough to parse a header before we check the other requirements.
					if (fs.Length < IqmHeader.SizeInBytes)
						throw new ArgumentException("The file \"" + path + "\" is not an IQM file.", "path");

					//parse the header and check for magic number match, file size match, and version match.
					//IqmHeader header = ParseHeader(reader);
					byte[] headerData = reader.ReadBytes(IqmHeader.SizeInBytes);
					IqmHeader header = new IqmHeader(); 
					header.Parse(headerData);
					if (header.Magic != "INTERQUAKEMODEL\0")
						throw new ArgumentException("The file \"" + path + "\" is not an IQM file.", "path");

					if (header.FileSize != fs.Length)
						throw new ArgumentException("The file \"" + path + "\" is invalid or corrupted. The file size doesn't match the size reported in the header.");
						//throw new DataCorruptionException("The file \"" + path + "\" is invalid or corrupted. The file size doesn't match the size reported in the header.");

					long positionStore = reader.BaseStream.Position;
					reader.BaseStream.Position = 0;
					byte[] fileData = reader.ReadBytes((int)fs.Length);
					reader.BaseStream.Position = positionStore;
					if (header.Version != 2)
						throw new ArgumentException("The file \"" + path + "\" is using a different version of the IQM specification. Make sure to compile the model as version 2.");
						//throw new OutdatedClientException("The file \"" + path + "\" is using a different version of the IQM specification. Make sure to compile the model as version 2.");

					//Parse text
					if (header.TextOffset != 0)
						text = ParseText(reader, (int)header.TextOffset, (int)header.TextCount);
					else
						text = new List<string>();

					//Parse meshes
					if (header.MeshesOffset != 0)
						meshes = ParseMeshes(reader, (int)header.MeshesOffset, (int)header.MeshesCount);
					else
						meshes = new List<IqmMesh>();

					//Parse vertexarrays
					if (header.VertexArraysOffset != 0)
						vertexArrays = ParseVertexArrays(reader, (int)header.VertexArraysOffset, (int)header.VertexArraysCount);
					else
						vertexArrays = new List<IqmVertexArray>();

					//Parse triangles
					if (header.TrianglesOffset != 0)
						triangles = ParseTriangles(reader, (int)header.TrianglesOffset, (int)header.TrianglesCount);
					else
						triangles = new List<IqmTriangle>();

					//Parse adjacency
					if (header.AdjacencyOffset != 0)
						adjacency = ParseAdjacency(reader, (int)header.AdjacencyOffset, (int)header.TrianglesCount);
					else
						adjacency = new List<IqmAdjacency>();

					//Parse joints
					if (header.JointsOffset != 0)
						joints = ParseJoints(reader, (int)header.JointsOffset, (int)header.JointsCount);
					else
						joints = new List<IqmJoint>();

					//Parse poses
					if (header.PosesOffset != 0)
						poses = ParsePoses(reader, (int)header.PosesOffset, (int)header.PosesCount);
					else
						poses = new List<IqmPose>();

					//Parse animations
					if (header.AnimationsOffset != 0)
						animations = ParseAnimations(reader, (int)header.AnimationsOffset, (int)header.AnimationsCount);
					else
						animations = new List<IqmAnimation>();

					//Parse frames

					//Parse bounds
					if (header.BoundsOffset != 0)
						bounds = ParseBounds(reader, (int)header.BoundsOffset);

					//Parse vertices
					for (int i = 0; i < header.VertexArraysCount; i++)
					{
						IqmVertexArray va = vertexArrays[i];

						if ((int)va.Type > 3) //HACK no animation stuff set up yet.
							continue;
					}


					uint triOff, arrOff, vertOff, normVertOff;
					float x,y,z;
					float xNorm, yNorm, zNorm;
					Boolean normExists = false;
					int normVertIndex = 2; // standard location, automaticly set to actual
					if (header.VertexArraysCount > 1)
					{
						for (int i = 0; i < header.VertexArraysCount; i++) 
						{
							if (vertexArrays[i].Type == IqmAttributeType.Normal) //if type = normal
							{
								normVertIndex = i;
								normExists = true;
							}
						}
					}
					for (int m = 0; m < header.MeshesCount; m++)
					{
						triOff = meshes[m].FirstTriangle;
						arrOff = meshes[m].FirstVertex;
						vertOff = vertexArrays[0].Offset;
						
						for (int t = 0; t < meshes[m].TrianglesCount; t++)
						{
							x = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3);
							y = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3 + 4);
							z = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3 + 8);
							
							pos.Add(x);
							pos.Add(y);
							pos.Add(z);

							x = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3);
							y = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3 + 4);
							z = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3 + 8);

							pos.Add(x);
							pos.Add(y);
							pos.Add(z);

							x = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3);
							y = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3 + 4);
							z = BitConverter.ToSingle(fileData, (int)vertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3 + 8);

							
							pos.Add(x);
							pos.Add(y);
							pos.Add(z);

							if (normExists)
							{
								normVertOff = vertexArrays[normVertIndex].Offset;

								xNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3);
								yNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3 + 4);
								zNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex2) * 4 * 3 + 8);

								norm.Add(xNorm);
								norm.Add(yNorm);
								norm.Add(zNorm);

								xNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3);
								yNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3 + 4);
								zNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex1) * 4 * 3 + 8);

								norm.Add(xNorm);
								norm.Add(yNorm);
								norm.Add(zNorm);

								xNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3);
								yNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3 + 4);
								zNorm = BitConverter.ToSingle(fileData, (int)normVertOff + (int)(triangles[(int)triOff + t].Vertex0) * 4 * 3 + 8);

								norm.Add(xNorm);
								norm.Add(yNorm);
								norm.Add(zNorm);
							}

							
						}
					}
				   
					return new IqmData((pos.Count != 0 ? pos.ToArray() : null), (norm.Count != 0 ? norm.ToArray() : null)); 
					
				}
			}
		}
		/*
		private unsafe static IqmHeader ParseHeader(BinaryReader reader)
		{
			byte[] headerData = reader.ReadBytes(IqmHeader.SizeInBytes);

			fixed (byte* headerPtr = headerData)
				return PtrToStructure<IqmHeader>(headerPtr);
		}
		*/
		private static List<string> ParseText(BinaryReader reader, int position, int count)
		{
			List<string> text = new List<string>();
			char[] tempCharToBuildString = new char[count];

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte nextByte = reader.ReadByte();
				tempCharToBuildString[i] = (char)nextByte;

				if (nextByte == 0)
				{
					text.Add(new string(tempCharToBuildString));
				}
			}

			return text;
		}

		private static List<IqmMesh> ParseMeshes(BinaryReader reader, int position, int count)
		{
			List<IqmMesh> meshes = new List<IqmMesh>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] meshData = reader.ReadBytes(IqmMesh.SizeInBytes);
				IqmMesh meh = new IqmMesh();
				meh.Parse(meshData);
				meshes.Add(meh);
			}

			return meshes;
		}

		private static List<IqmVertexArray> ParseVertexArrays(BinaryReader reader, int position, int count)
		{
			List<IqmVertexArray> vertexArrays = new List<IqmVertexArray>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] vertData = reader.ReadBytes(IqmVertexArray.SizeInBytes);
				IqmVertexArray vertArr = new IqmVertexArray();
				vertArr.Parse(vertData);
				vertexArrays.Add(vertArr);
			}

			return vertexArrays;
		}

		private static List<IqmTriangle> ParseTriangles(BinaryReader reader, int position, int count)
		{
			List<IqmTriangle> triangles = new List<IqmTriangle>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] vertData = reader.ReadBytes(IqmTriangle.SizeInBytes);
				IqmTriangle tri = new IqmTriangle();
				tri.Parse(vertData);
				triangles.Add(tri);
			}

			return triangles;
		}

		private static List<IqmAdjacency> ParseAdjacency(BinaryReader reader, int position, int count)
		{
			List<IqmAdjacency> adjacency = new List<IqmAdjacency>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] adjData = reader.ReadBytes(IqmAdjacency.SizeInBytes);
				IqmAdjacency adj = new IqmAdjacency();
				adj.Parse(adjData);
				adjacency.Add(adj);
			}

			return adjacency;
		}

		private static List<IqmJoint> ParseJoints(BinaryReader reader, int position, int count)
		{
			List<IqmJoint> joints = new List<IqmJoint>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] jointData = reader.ReadBytes(IqmJoint.SizeInBytes);
				IqmJoint joi = new IqmJoint();
				joi.Parse(jointData);
				joints.Add(joi);
			}

			return joints;
		}

		private static List<IqmPose> ParsePoses(BinaryReader reader, int position, int count)
		{
			List<IqmPose> poses = new List<IqmPose>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] poseData = reader.ReadBytes(IqmPose.SizeInBytes);
				IqmPose pose = new IqmPose();
				pose.Parse(poseData);
				poses.Add(pose);
			}

			return poses;
		}

		private static List<IqmAnimation> ParseAnimations(BinaryReader reader, int position, int count)
		{
			List<IqmAnimation> animations = new List<IqmAnimation>(count);

			reader.BaseStream.Position = position;

			for (int i = 0; i < count; i++)
			{
				byte[] aniData = reader.ReadBytes(IqmAnimation.SizeInBytes);
				IqmAnimation ani = new IqmAnimation();
				ani.Parse(aniData);
				animations.Add(ani);
			}

			return animations;
		}

		private static IqmBounds ParseBounds(BinaryReader reader, int position)
		{
			reader.BaseStream.Position = position;

			byte[] boundData = reader.ReadBytes(IqmBounds.SizeInBytes);
			IqmBounds bound = new IqmBounds();
			bound.Parse(boundData);

			return bound;
		}
		
		private static int SizeOfIqmFormat(IqmDataFormat format)
		{
			switch (format)
			{
				case IqmDataFormat.Byte:
				case IqmDataFormat.UnsignedByte:
					return 1;
				case IqmDataFormat.Half:
				case IqmDataFormat.Short:
				case IqmDataFormat.UnsignedShort:
					return 2;
				case IqmDataFormat.Float:
				case IqmDataFormat.Int:
				case IqmDataFormat.UnsignedInt:
					return 4;
				case IqmDataFormat.Double:
					return 8;
				default:
					return 0;
			}
		}
	}
}
