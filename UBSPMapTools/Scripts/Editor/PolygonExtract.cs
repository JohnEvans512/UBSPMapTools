using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PolygonExtract : EditorWindow
{
	public struct TriangleVector
	{
		public Vector3 v1;
		public Vector3 v2;
		public Vector3 v3;
		public TriangleVector (Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
		{
			v1 = vertex1;
			v2 = vertex2;
			v3 = vertex3;
		}
	}
	public struct TriangleInt
	{
		public int v1;
		public int v2;
		public int v3;
		public TriangleInt (int vertex1, int vertex2, int vertex3)
		{
			v1 = vertex1;
			v2 = vertex2;
			v3 = vertex3;
		}
	}
	public struct MeshVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
		public bool Matches (MeshVertex ref_vtx)
		{
			return (Vector3.Distance(position, ref_vtx.position) < 0.01f && Vector3.Angle(normal, ref_vtx.normal) < 1.0f && Vector2.Distance(uv, ref_vtx.uv) < 0.01f);
		}
	}
	public struct SubMesh
	{
		public int mtlId;
		public List<int> triangles;
	}

	static PolygonExtract window;
	static Color32 selection_color = new Color32(150, 150, 255, 70);
	static bool mouse_drag = false;
	static bool active = false;
	static List<GameObject> input_objects;
	static List<List<TriangleInt>> object_tri_groups;
	static List<TriangleVector> highlight_triangles;
	static Vector3[] triangle_vertices;
	static int[] mesh_triangles;
	static Vector3[] mesh_vertices;
	static Event event1;
	static Color32 button_color1 = new Color32(150, 150, 255, 255);
	static GameObject m_object;
	static Transform obj_transform;
	static int triangle_count;
	static TriangleInt[] s_triangles;
	static Mesh mesh1;
	static bool autoCubemap = true;
	static float UV2Padding = 0.02f;
	static UnwrapParam uw1;
	
	[MenuItem("Tools/BSP/PolygonExtract")]
	public static void Init()
	{
		window = GetWindow<PolygonExtract>();
		#if UNITY_2019_1_OR_NEWER
		SceneView.duringSceneGui += OnScene;
		#else
		SceneView.onSceneGUIDelegate += OnScene;
		#endif		
		input_objects = new List<GameObject>();
		object_tri_groups = new List<List<TriangleInt>>();
		highlight_triangles = new List<TriangleVector>();
		triangle_vertices = new Vector3[3];
		obj_transform = null;
		triangle_count = 0;
	}

	void OnGUI()
	{
		if (active)
		{
			GUI.backgroundColor = button_color1;
			if (GUILayout.Button("Active", GUILayout.Width(328), GUILayout.Height(30)))
			{
				active = false;
			}
		}
		else
		{
			GUI.backgroundColor = Color.white;
			if (GUILayout.Button("Activate", GUILayout.Width(328), GUILayout.Height(30)))
			{
				active = true;
			}
		}
		GUI.backgroundColor = Color.white;
		if (GUILayout.Button("Build Mesh", GUILayout.Width(328), GUILayout.Height(30)))
		{
			BuildMesh();
		}
		
		if (GUILayout.Button("Clear Selection", GUILayout.Width(328), GUILayout.Height(30)))
		{
			input_objects.Clear();
			highlight_triangles.Clear();
			object_tri_groups.Clear();
		}
		autoCubemap = EditorGUILayout.Toggle("Assign Cubemap", autoCubemap);
		UV2Padding = EditorGUILayout.FloatField("UV2 Padding", UV2Padding);
		if (input_objects != null)
		{
			for (int i = 0; i < input_objects.Count; i++)
			{
				EditorGUILayout.LabelField(input_objects[i].name);
			}
		}
	}

	static void OnScene(SceneView sceneview)
	{
		int controlID = GUIUtility.GetControlID(sceneview.GetHashCode(), FocusType.Passive);
		event1 = Event.current;
		if (active)
		{
			
			if (event1.type == EventType.MouseDown && event1.button == 0)
			{
				GUIUtility.hotControl = controlID;
				SelectFace(event1.control);
				GUIUtility.hotControl = 0;
			}
			else if (event1.type == EventType.MouseDown && event1.button == 1)
			{
				mouse_drag = false;
			}
			else if (event1.type == EventType.MouseDrag)
			{
				mouse_drag = true;
			}
			else if (event1.type == EventType.MouseUp && event1.button == 1)
			{
				if (!mouse_drag)
				{
					DeselectFace(event1.control);
				}
				mouse_drag = false;
			}
		}
		Handles.color = selection_color;
		for (int i = 0; i < highlight_triangles.Count; i++)
		{
			triangle_vertices[0] = highlight_triangles[i].v1;
			triangle_vertices[1] = highlight_triangles[i].v2;
			triangle_vertices[2] = highlight_triangles[i].v3;
			Handles.DrawAAConvexPolygon(triangle_vertices);
		}
	}

	static void SelectFace (bool all_faces)
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(event1.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 1000))
		{
			GameObject obj1 = hit.collider.gameObject;
			int tri_group_index = 0;
			bool match = false;
			for (int i = 0; i < input_objects.Count; i++)
			{
				if (obj1 == input_objects[i])
				{
					tri_group_index = i;
					match = true;
					break;
				}
			}
			if (!match)
			{
				tri_group_index = input_objects.Count;
				input_objects.Add(obj1);
				object_tri_groups.Add(new List<TriangleInt>());
				window.Repaint();
			}
			if (obj1 != m_object)
			{
				MeshFilter mf1 = obj1.GetComponentInChildren<MeshFilter>();
				if (mf1 == null)
				{
					return;
				}
				Mesh mesh1 = mf1.sharedMesh;
				obj_transform = obj1.transform;
				mesh_vertices = mesh1.vertices;
				mesh_triangles = mesh1.triangles;
				triangle_count = mesh_triangles.Length / 3;
				s_triangles = new TriangleInt[triangle_count];
				for (int i = 0; i < triangle_count; i++)
				{
					s_triangles[i].v1 = mesh_triangles[i * 3];
					s_triangles[i].v2 = mesh_triangles[i * 3 + 1];
					s_triangles[i].v3 = mesh_triangles[i * 3 + 2];
				}
				m_object = obj1;
			}
			float rayDistance;
			float m_rayDistance = 1000.0f;
			Vector3 raycast_point;
			Vector3 contact_point = Vector3.zero;
			Plane triangle_plane = new Plane(Vector3.zero, Vector3.one, Vector3.forward);
			Vector3[] current_triangle = new Vector3[3];
			Vector3[] angle_vectors = new Vector3[3];
			float angle_sum = 0.0f;
			int triangle_id = -1;
			for (int i = 0; i < triangle_count; i++)
			{
				current_triangle[0] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3]]);
				current_triangle[1] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3 + 1]]);
				current_triangle[2] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3 + 2]]);
				triangle_plane.Set3Points(current_triangle[0], current_triangle[1], current_triangle[2]);
				if (triangle_plane.Raycast(ray, out rayDistance))
				{
					raycast_point = ray.GetPoint(rayDistance);
					angle_vectors[0] = current_triangle[0] - raycast_point;
					angle_vectors[1] = current_triangle[1] - raycast_point;
					angle_vectors[2] = current_triangle[2] - raycast_point;
					angle_sum = Vector3.Angle(angle_vectors[0], angle_vectors[1]) + Vector3.Angle(angle_vectors[1], angle_vectors[2]) + Vector3.Angle(angle_vectors[2], angle_vectors[0]);
					if (angle_sum > 359.0f)
					{
						if (triangle_plane.GetSide(ray.origin))
						{
							if (rayDistance < m_rayDistance)
							{
								triangle_id = i;
								contact_point = raycast_point;
								m_rayDistance = rayDistance;
							}
						}
					}
				}
			}
			if (triangle_id != -1)
			{
				if (all_faces)
				{
					for (int i = 0; i < triangle_count; i++)
					{
						if (!object_tri_groups[tri_group_index].Contains(s_triangles[i])) object_tri_groups[tri_group_index].Add(s_triangles[i]);
						TriangleVector current_triangle2 = new TriangleVector(obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v1]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v2]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v3]));
						if (!highlight_triangles.Contains(current_triangle2))
						{
							highlight_triangles.Add(current_triangle2);
						}						
					}
				}
				else
				{
					List<TriangleInt> current_face_triangles = new List<TriangleInt>();
					current_face_triangles.Add(s_triangles[triangle_id]);			
					for (int i = 0; i < triangle_count; i++)
					{	
						for (int i3 = 0; i3 < current_face_triangles.Count; i3++)
						{
							if (TrisConnected(s_triangles[i], current_face_triangles[i3]))
							{
								if (!current_face_triangles.Contains(s_triangles[i])) current_face_triangles.Add(s_triangles[i]);
								if (!object_tri_groups[tri_group_index].Contains(s_triangles[i])) object_tri_groups[tri_group_index].Add(s_triangles[i]);
								TriangleVector current_triangle2 = new TriangleVector(obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v1]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v2]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v3]));
								if (!highlight_triangles.Contains(current_triangle2))
								{
									highlight_triangles.Add(current_triangle2);
								}
							}
						}
					}
					current_face_triangles.Clear();
				}	
			}
		}
		HandleUtility.Repaint();
	}
	
	static void DeselectFace (bool all_faces)
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(event1.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 1000))
		{
			GameObject obj1 = hit.collider.gameObject;
			int tri_group_index = 0;
			bool match = false;
			for (int i = 0; i < input_objects.Count; i++)
			{
				if (obj1 == input_objects[i])
				{
					tri_group_index = i;
					match = true;
					break;
				}
			}
			if (!match)
			{
				tri_group_index = input_objects.Count;
				input_objects.Add(obj1);
				object_tri_groups.Add(new List<TriangleInt>());
				window.Repaint();
			}
			if (obj1 != m_object)
			{
				MeshFilter mf1 = obj1.GetComponentInChildren<MeshFilter>();
				if (mf1 == null)
				{
					return;
				}
				Mesh mesh1 = mf1.sharedMesh;
				obj_transform = obj1.transform;
				mesh_vertices = mesh1.vertices;
				mesh_triangles = mesh1.triangles;
				triangle_count = mesh_triangles.Length / 3;
				s_triangles = new TriangleInt[triangle_count];
				for (int i = 0; i < triangle_count; i++)
				{
					s_triangles[i].v1 = mesh_triangles[i * 3];
					s_triangles[i].v2 = mesh_triangles[i * 3 + 1];
					s_triangles[i].v3 = mesh_triangles[i * 3 + 2];
				}
				m_object = obj1;
			}
			float rayDistance;
			float m_rayDistance = 1000.0f;
			Vector3 raycast_point;
			Vector3 contact_point = Vector3.zero;
			Plane triangle_plane = new Plane(Vector3.zero, Vector3.one, Vector3.forward);
			Vector3[] current_triangle = new Vector3[3];
			Vector3[] angle_vectors = new Vector3[3];
			float angle_sum = 0.0f;
			int triangle_id = -1;
			for (int i = 0; i < triangle_count; i++)
			{
				current_triangle[0] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3]]);
				current_triangle[1] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3 + 1]]);
				current_triangle[2] = obj_transform.TransformPoint(mesh_vertices[mesh_triangles[i * 3 + 2]]);
				triangle_plane.Set3Points(current_triangle[0], current_triangle[1], current_triangle[2]);
				if (triangle_plane.Raycast(ray, out rayDistance))
				{
					raycast_point = ray.GetPoint(rayDistance);
					angle_vectors[0] = current_triangle[0] - raycast_point;
					angle_vectors[1] = current_triangle[1] - raycast_point;
					angle_vectors[2] = current_triangle[2] - raycast_point;
					angle_sum = Vector3.Angle(angle_vectors[0], angle_vectors[1]) + Vector3.Angle(angle_vectors[1], angle_vectors[2]) + Vector3.Angle(angle_vectors[2], angle_vectors[0]);
					if (angle_sum > 359.0f)
					{
						if (triangle_plane.GetSide(ray.origin))
						{
							if (rayDistance < m_rayDistance)
							{
								triangle_id = i;
								contact_point = raycast_point;
								m_rayDistance = rayDistance;
							}
						}
					}
				}
			}
			if (triangle_id != -1)
			{
				if (all_faces)
				{
					for (int i = 0; i < triangle_count; i++)
					{
						if (object_tri_groups[tri_group_index].Contains(s_triangles[i])) object_tri_groups[tri_group_index].Remove(s_triangles[i]);
						TriangleVector current_triangle2 = new TriangleVector(obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v1]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v2]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v3]));
						if (highlight_triangles.Contains(current_triangle2))
						{
							highlight_triangles.Remove(current_triangle2);
						}						
					}
				}
				else
				{
					List<TriangleInt> current_face_triangles = new List<TriangleInt>();
					current_face_triangles.Add(s_triangles[triangle_id]);			
					for (int i = 0; i < triangle_count; i++)
					{	
						for (int i3 = 0; i3 < current_face_triangles.Count; i3++)
						{
							if (TrisConnected(s_triangles[i], current_face_triangles[i3]))
							{
								if (!current_face_triangles.Contains(s_triangles[i])) current_face_triangles.Add(s_triangles[i]);
								if (object_tri_groups[tri_group_index].Contains(s_triangles[i])) object_tri_groups[tri_group_index].Remove(s_triangles[i]);
								TriangleVector current_triangle2 = new TriangleVector(obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v1]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v2]), obj_transform.TransformPoint(mesh_vertices[s_triangles[i].v3]));
								if (highlight_triangles.Contains(current_triangle2))
								{
									highlight_triangles.Remove(current_triangle2);
								}
							}
						}
					}
					current_face_triangles.Clear();
				}
				for (int i = 0; i < object_tri_groups.Count; i++)
				{
					if (object_tri_groups[i].Count < 1)
					{
						object_tri_groups.RemoveAt(i);
						input_objects.RemoveAt(i);
					}
				}
			}
		}
		HandleUtility.Repaint();
	}
	
	static void BuildMesh ()
	{
		int input_obj_count = input_objects.Count;
		if (input_obj_count < 1) return;
		List<Material> selection_materials = new List<Material>();
		int[][] mtl_dictionary = new int[input_obj_count][];
		Material[] object_materials = null;
		for (int i = 0; i < input_obj_count; i++)
		{
			object_materials = input_objects[i].GetComponentInChildren<MeshRenderer>().sharedMaterials;
			mtl_dictionary[i] = new int[object_materials.Length];
			for (int i2 = 0; i2 < object_materials.Length; i2++)
			{
				bool match2 = false;
				for (int i3 = 0; i3 < selection_materials.Count; i3++)
				{
					if (object_materials[i2] == selection_materials[i3])
					{
						mtl_dictionary[i][i2] = i3;
						match2 = true;
						break;
					}
				}
				if (!match2)
				{
					mtl_dictionary[i][i2] = selection_materials.Count;
					selection_materials.Add(object_materials[i2]);
				}
			}
		}
		List<GameObject> cleanupObjects = new List<GameObject>();
		uw1 = new UnwrapParam();
		uw1.hardAngle = 70.0f;
		uw1.angleError = 8.0f;
		uw1.areaError = 15.0f;
		uw1.packMargin = UV2Padding;

		List<MeshVertex> s_mesh_vertices = new List<MeshVertex>();
		MeshVertex current_mesh_vertex = new MeshVertex();
		List<int> s_mesh_triangles = new List<int>();
		Vector3[] input_mesh_vertices = null;
		Vector3[] input_mesh_normals = null;
		Vector2[] input_mesh_uvs = null;
		Mesh input_object_mesh = null;
		int input_mesh_sm_count = 0;
		int[] submesh_triangles = null;
		
		List<SubMesh> mesh_submeshes = new List<SubMesh>();
		SubMesh current_submesh = new SubMesh();
		int current_submesh_index = -1;

		for (int i = 0; i < input_obj_count; i++)
		{
			input_object_mesh = input_objects[i].GetComponentInChildren<MeshFilter>().sharedMesh;
			input_mesh_vertices = input_object_mesh.vertices;
			input_mesh_normals = input_object_mesh.normals;
			input_mesh_uvs = input_object_mesh.uv;
			input_mesh_sm_count = input_object_mesh.subMeshCount;
			for (int i2 = 0; i2 < input_mesh_sm_count; i2++)
			{
				submesh_triangles = input_object_mesh.GetTriangles(i2);
				current_submesh_index = -1;
				for (int i3 = 0; i3 < submesh_triangles.Length; i3 += 3)
				{
					for (int i5 = 0; i5 < object_tri_groups[i].Count; i5++)
					{
						if (object_tri_groups[i][i5].v1 == submesh_triangles[i3] && object_tri_groups[i][i5].v2 == submesh_triangles[i3 + 1] && object_tri_groups[i][i5].v3 == submesh_triangles[i3 + 2])
						{
							if (current_submesh_index == -1)
							{
								int mtl_index = mtl_dictionary[i][i2];
								bool match3 = false;
								for (int i7 = 0; i7 < mesh_submeshes.Count; i7++)
								{
									if (mtl_index == mesh_submeshes[i7].mtlId)
									{
										current_submesh_index = i7;
										match3 = true;
										break;
									}
								}
								if (!match3)
								{
									current_submesh_index = mesh_submeshes.Count;
									current_submesh.mtlId = mtl_index;
									current_submesh.triangles = new List<int>();
									mesh_submeshes.Add(current_submesh);
								}
							}
							
							int local_vtx_index = 0;
							current_mesh_vertex.position = input_objects[i].transform.TransformPoint(input_mesh_vertices[object_tri_groups[i][i5].v1]);
							current_mesh_vertex.normal = input_objects[i].transform.TransformDirection(input_mesh_normals[object_tri_groups[i][i5].v1]).normalized;
							current_mesh_vertex.uv = input_mesh_uvs[object_tri_groups[i][i5].v1];
							bool match5 = false;
							for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
							{
								if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
								{
									local_vtx_index = i8;
									match5 = true;
									break;
								}
							}
							if (!match5)
							{
								local_vtx_index = s_mesh_vertices.Count;
								s_mesh_vertices.Add(current_mesh_vertex);
							}
							s_mesh_triangles.Add(local_vtx_index);
							mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);

							local_vtx_index = 0;
							current_mesh_vertex.position = input_objects[i].transform.TransformPoint(input_mesh_vertices[object_tri_groups[i][i5].v2]);
							current_mesh_vertex.normal = input_mesh_normals[object_tri_groups[i][i5].v2];
							current_mesh_vertex.uv = input_mesh_uvs[object_tri_groups[i][i5].v2];
							match5 = false;
							for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
							{
								if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
								{
									local_vtx_index = i8;
									match5 = true;
									break;
								}
							}
							if (!match5)
							{
								local_vtx_index = s_mesh_vertices.Count;
								s_mesh_vertices.Add(current_mesh_vertex);
							}
							s_mesh_triangles.Add(local_vtx_index);
							mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);

							local_vtx_index = 0;
							current_mesh_vertex.position = input_objects[i].transform.TransformPoint(input_mesh_vertices[object_tri_groups[i][i5].v3]);
							current_mesh_vertex.normal = input_mesh_normals[object_tri_groups[i][i5].v3];
							current_mesh_vertex.uv = input_mesh_uvs[object_tri_groups[i][i5].v3];
							match5 = false;
							for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
							{
								if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
								{
									local_vtx_index = i8;
									match5 = true;
									break;
								}
							}
							if (!match5)
							{
								local_vtx_index = s_mesh_vertices.Count;
								s_mesh_vertices.Add(current_mesh_vertex);
							}
							s_mesh_triangles.Add(local_vtx_index);
							mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);							
						}
					}
				}
			}
		}
		
		int mesh_vertex_count = s_mesh_vertices.Count;
		Mesh mesh1 = new Mesh();
		Vector3 model_origin = Vector3.zero;
		for (int i = 0; i < mesh_vertex_count; i++)
		{
			model_origin += s_mesh_vertices[i].position;
		}
		model_origin /= mesh_vertex_count;
		Vector3[] output_vertices2 = new Vector3[mesh_vertex_count];
		Vector3[] output_normals2 = new Vector3[mesh_vertex_count];
		Vector2[] output_uvs2 = new Vector2[mesh_vertex_count];
		for (int i = 0; i < mesh_vertex_count; i++)
		{
			output_vertices2[i] = s_mesh_vertices[i].position - model_origin;
			output_normals2[i] = s_mesh_vertices[i].normal;
			output_uvs2[i] = s_mesh_vertices[i].uv;
		}
		int[] mesh_triangles2 = s_mesh_triangles.ToArray();
		mesh1.vertices = output_vertices2;
		mesh1.triangles = mesh_triangles2;
		mesh1.subMeshCount = mesh_submeshes.Count;
		for (int i = 0; i < mesh_submeshes.Count; i++)
		{
			mesh1.SetTriangles(mesh_submeshes[i].triangles.ToArray(), i);
		}
		Material[] output_materials = new Material[mesh_submeshes.Count];
		for (int i = 0; i < output_materials.Length; i++)
		{
			output_materials[i] = selection_materials[mesh_submeshes[i].mtlId];
		}
		mesh1.normals = output_normals2;
		mesh1.uv = output_uvs2;		
		mesh1.RecalculateBounds();
		mesh1.RecalculateTangents();
		int mesh_instance_id = mesh1.GetInstanceID();
		mesh1.name = "mesh"+mesh_instance_id.ToString();
		Unwrapping.GenerateSecondaryUVSet(mesh1, uw1);
		GameObject model_object = new GameObject("model"+mesh_instance_id.ToString());
		model_object.transform.position = model_origin;
		model_object.AddComponent<MeshFilter>().mesh = mesh1;
		MeshRenderer mr = model_object.AddComponent<MeshRenderer>();
		mr.materials = output_materials;
		if (autoCubemap)
		{
			mr.probeAnchor = GetBestCubemap(model_object);
		}
		model_object.AddComponent<MeshCollider>().sharedMesh = CollisionMesh(mesh1);
		model_object.isStatic = true;

		MeshFilter mf1 = null;
		MeshRenderer mr1 = null;
		for (int i = 0; i < input_obj_count; i++)
		{
			mf1 = input_objects[i].GetComponentInChildren<MeshFilter>();
			mr1 = input_objects[i].GetComponentInChildren<MeshRenderer>();

			input_object_mesh = mf1.sharedMesh;
			string input_mesh_name = input_object_mesh.name;
			input_mesh_vertices = input_object_mesh.vertices;
			input_mesh_normals = input_object_mesh.normals;
			input_mesh_uvs = input_object_mesh.uv;
			input_mesh_sm_count = input_object_mesh.subMeshCount;
			
			s_mesh_triangles.Clear();
			s_mesh_vertices.Clear();
			mesh_submeshes.Clear();
			
			
			for (int i2 = 0; i2 < input_mesh_sm_count; i2++)
			{
				submesh_triangles = input_object_mesh.GetTriangles(i2);
				current_submesh_index = -1;
				for (int i3 = 0; i3 < submesh_triangles.Length; i3 += 3)
				{
					bool in_selection = false;
					for (int i5 = 0; i5 < object_tri_groups[i].Count; i5++)
					{
						if (object_tri_groups[i][i5].v1 == submesh_triangles[i3] && object_tri_groups[i][i5].v2 == submesh_triangles[i3 + 1] && object_tri_groups[i][i5].v3 == submesh_triangles[i3 + 2])
						{
							in_selection = true;
							break;
						}
					}
					if (!in_selection)
					{
						if (current_submesh_index == -1)
						{
							int mtl_index = mtl_dictionary[i][i2];
							bool match3 = false;
							for (int i7 = 0; i7 < mesh_submeshes.Count; i7++)
							{
								if (mtl_index == mesh_submeshes[i7].mtlId)
								{
									current_submesh_index = i7;
									match3 = true;
									break;
								}
							}
							if (!match3)
							{
								current_submesh_index = mesh_submeshes.Count;
								current_submesh.mtlId = mtl_index;
								current_submesh.triangles = new List<int>();
								mesh_submeshes.Add(current_submesh);
							}
						}
						
						int local_vtx_index = 0;
						current_mesh_vertex.position = input_mesh_vertices[submesh_triangles[i3]];
						current_mesh_vertex.normal = input_mesh_normals[submesh_triangles[i3]];
						current_mesh_vertex.uv = input_mesh_uvs[submesh_triangles[i3]];
						bool match5 = false;
						for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
						{
							if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
							{
								local_vtx_index = i8;
								match5 = true;
								break;
							}
						}
						if (!match5)
						{
							local_vtx_index = s_mesh_vertices.Count;
							s_mesh_vertices.Add(current_mesh_vertex);
						}
						s_mesh_triangles.Add(local_vtx_index);
						mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);

						local_vtx_index = 0;
						current_mesh_vertex.position = input_mesh_vertices[submesh_triangles[i3 + 1]];
						current_mesh_vertex.normal = input_mesh_normals[submesh_triangles[i3 + 1]];
						current_mesh_vertex.uv = input_mesh_uvs[submesh_triangles[i3 + 1]];
						match5 = false;
						for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
						{
							if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
							{
								local_vtx_index = i8;
								match5 = true;
								break;
							}
						}
						if (!match5)
						{
							local_vtx_index = s_mesh_vertices.Count;
							s_mesh_vertices.Add(current_mesh_vertex);
						}
						s_mesh_triangles.Add(local_vtx_index);
						mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);

						local_vtx_index = 0;
						current_mesh_vertex.position = input_mesh_vertices[submesh_triangles[i3 + 2]];
						current_mesh_vertex.normal = input_mesh_normals[submesh_triangles[i3 + 2]];
						current_mesh_vertex.uv = input_mesh_uvs[submesh_triangles[i3 + 2]];
						match5 = false;
						for (int i8 = 0; i8 < s_mesh_vertices.Count; i8++)
						{
							if (current_mesh_vertex.Matches(s_mesh_vertices[i8]))
							{
								local_vtx_index = i8;
								match5 = true;
								break;
							}
						}
						if (!match5)
						{
							local_vtx_index = s_mesh_vertices.Count;
							s_mesh_vertices.Add(current_mesh_vertex);
						}
						s_mesh_triangles.Add(local_vtx_index);
						mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);
					}
				}
			}
			if (s_mesh_triangles.Count > 0)
			{
				mesh_vertex_count = s_mesh_vertices.Count;
				Mesh mesh2 = new Mesh();
				output_vertices2 = new Vector3[mesh_vertex_count];
				output_normals2 = new Vector3[mesh_vertex_count];
				output_uvs2 = new Vector2[mesh_vertex_count];
				for (int i10 = 0; i10 < mesh_vertex_count; i10++)
				{
					output_vertices2[i10] = s_mesh_vertices[i10].position;
					output_normals2[i10] = s_mesh_vertices[i10].normal;
					output_uvs2[i10] = s_mesh_vertices[i10].uv;
				}
				mesh_triangles2 = s_mesh_triangles.ToArray();
				mesh2.vertices = output_vertices2;
				mesh2.triangles = mesh_triangles2;
				mesh2.subMeshCount = mesh_submeshes.Count;
				for (int i10 = 0; i10 < mesh_submeshes.Count; i10++)
				{
					mesh2.SetTriangles(mesh_submeshes[i10].triangles.ToArray(), i10);
				}
				Material[] output_materials2 = new Material[mesh_submeshes.Count];
				for (int i10 = 0; i10 < output_materials2.Length; i10++)
				{
					output_materials2[i10] = selection_materials[mesh_submeshes[i10].mtlId];
				}
				mesh2.normals = output_normals2;
				mesh2.uv = output_uvs2;		
				mesh2.RecalculateBounds();
				mesh2.RecalculateTangents();
				mesh2.name = input_mesh_name;
				Unwrapping.GenerateSecondaryUVSet(mesh2, uw1);
				mf1.mesh = mesh2;
				mr1.materials = output_materials2;
				MeshCollider mc1 = input_objects[i].GetComponentInChildren<MeshCollider>();
				if (mc1 != null) mc1.sharedMesh = CollisionMesh(mesh2);
			}
			else
			{
				cleanupObjects.Add(input_objects[i]);
			}
		}
		if (cleanupObjects.Count > 0)
		{
			for (int i15 = 0; i15 < cleanupObjects.Count; i15++)
			{
				GameObject d1 = cleanupObjects[i15];
				cleanupObjects[i15] = null;
				DestroyImmediate(d1);
			}
		}
		cleanupObjects = null;
		input_objects.Clear();
		highlight_triangles.Clear();
		object_tri_groups.Clear();
		active = false;
		selection_materials = null;
		object_materials = null;
		mesh1 = null;
		m_object = null;
	}
	
	static bool TrisConnected (TriangleInt t1, TriangleInt t2)
	{
		if (t1.v1 == t2.v1) return true;
		if (t1.v1 == t2.v2) return true;
		if (t1.v1 == t2.v3) return true;
		if (t1.v2 == t2.v1) return true;
		if (t1.v2 == t2.v2) return true;
		if (t1.v2 == t2.v3) return true;
		if (t1.v3 == t2.v1) return true;
		if (t1.v3 == t2.v2) return true;
		if (t1.v3 == t2.v3) return true;
		return false;
	}
	
	static Transform GetBestCubemap (GameObject model)
	{
		ReflectionProbe[] scene_cubemaps = FindObjectsOfType(typeof(ReflectionProbe)) as ReflectionProbe[];
		if (scene_cubemaps.Length < 2) return null;
		Transform rcm = scene_cubemaps[0].transform;
		for (int i = 0; i < scene_cubemaps.Length; i++)
		{
			if ((CubemapVisible(model, scene_cubemaps[i].transform) && !CubemapVisible(model, rcm)) || ((CubemapVisible(model, scene_cubemaps[i].transform) && !CubemapVisible(model, rcm)) && (Vector3.Distance(model.transform.position, scene_cubemaps[i].transform.position) < Vector3.Distance(model.transform.position, rcm.transform.position))))
			{
				rcm = scene_cubemaps[i].transform;
			}
		}
		return rcm;
	}
	
	static bool CubemapVisible (GameObject model, Transform cubemap)
	{
		RaycastHit[] hits = Physics.RaycastAll(cubemap.position, (model.transform.position - cubemap.position).normalized, Vector3.Distance(cubemap.position, model.transform.position));
		bool visible = true;
		for (int i = 0; i < hits.Length; i++)
		{
			if (hits[i].collider.gameObject != model) visible = false;
		}
		return visible;
	}
	
	static Mesh CollisionMesh (Mesh input_mesh, float treshold = 0.01f)
	{
		Mesh output_mesh = new Mesh();
		output_mesh.name = string.Concat(input_mesh.name, "_cm");
		Vector3[] input_vertices = input_mesh.vertices;
		int input_vertex_count = input_mesh.vertexCount;
		int[] input_triangles = input_mesh.triangles;
		int[] vtx_dict = new int[input_vertex_count];
		Vector3[] new_vertices = new Vector3[1];
		new_vertices[0] = input_vertices[0];
		bool mark;
		for (int i = 0; i < input_vertex_count; i++)
		{
			mark = false;
			for (int i2 = 0; i2 < new_vertices.Length; i2++)
			{
				if (Vector3.Distance(input_vertices[i], new_vertices[i2]) < treshold)
				{
					vtx_dict[i] = i2;
					mark = true;
				}
			}
			if (!mark)
			{
				System.Array.Resize(ref new_vertices, new_vertices.Length + 1);
				new_vertices[new_vertices.Length - 1] = input_vertices[i];
				vtx_dict[i] = new_vertices.Length - 1;
			}
		}
		int[] new_triangles = new int[input_triangles.Length];
		int resize = 0;
		int i3 = 0;
		for (int i = 0; i < input_triangles.Length; i += 3)
		{
			if (vtx_dict[input_triangles[i]] == vtx_dict[input_triangles[i + 1]] || vtx_dict[input_triangles[i]] == vtx_dict[input_triangles[i + 2]] || vtx_dict[input_triangles[i + 1]] == vtx_dict[input_triangles[i + 2]])
			{
				i3 -= 3;
				resize += 3;
			}
			else
			{
				new_triangles[i3] = vtx_dict[input_triangles[i]];
				new_triangles[i3 + 1] = vtx_dict[input_triangles[i + 1]];
				new_triangles[i3 + 2] = vtx_dict[input_triangles[i + 2]];
			}
			i3 += 3;
		}
		if (resize > 0)
		{
			System.Array.Resize(ref new_triangles, new_triangles.Length - resize);
		}
		output_mesh.vertices = new_vertices;
		output_mesh.triangles = new_triangles;
		return output_mesh;
	}
	
	public void OnDestroy()
	{
		#if UNITY_2019_1_OR_NEWER
		SceneView.duringSceneGui -= OnScene;
		#else
		SceneView.onSceneGUIDelegate -= OnScene;
		#endif
		input_objects.Clear();
		highlight_triangles.Clear();
		object_tri_groups.Clear();
		active = false;
		highlight_triangles = null;
		input_objects = null;
		object_tri_groups = null;
		triangle_vertices = null;
		m_object = null;
	}
}