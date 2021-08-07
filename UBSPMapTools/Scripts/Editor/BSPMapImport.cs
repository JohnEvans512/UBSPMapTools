#define INFO_PLAYER_START
#define CUSTOM_ENTITIES
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BSPMapImport : EditorWindow
{
	private static BSPMapImport window;
	
	private static string MaterialsPath;
	private static string ModelsPath;
	private static string SoundPath;
	private static float UV2Padding;
	private static bool loadEntities = true;
	private static bool splitMesh = true;
	private static float MaxMeshSurfaceArea;
	private static float smoothing_angle = 15;
	
	const float ImportScale = 0.025f;
	
	private static string current_folder = "";
	
	
	static TexInfo[] tex_infos;
	static Plane[] bsp_planes;
	static BSPNode[] bsp_nodes;
	static BSPLeaf[] bsp_leaves;
	static int[] bsp_leaffaces;
	
	static BSPModel[] bsp_models;
	static BSPVertex[] bsp_vertices;
	static BSPFace[] bsp_faces;
	static int[] bsp_mesh_vertices;
	static List<BSPEntity> bsp_entities;
	
	static int node_mesh_count = 0;
	
	static bool[] free_faces;
	
	static List<Material> map_materials;
	
	static UnwrapParam uw1;
	
	public struct BSPEntity
	{
		public string[] kv_entries;
		public byte entry_count;
		public string GetString (string key)
		{
			for (byte i = 0; i < entry_count; i++)
			{
				if (kv_entries[i] == key)
				{
					if (i + 1 < entry_count)
					return kv_entries[i + 1];
					break;
				}
			}
			return "";
		}
		public int GetInt (string key, int dv)
		{
			for (byte i = 0; i < entry_count; i++)
			{
				if (kv_entries[i] == key)
				{
					if (i + 1 < entry_count)
					{
						if (kv_entries[i + 1].Contains("."))
						{
							return (int)float.Parse(kv_entries[i + 1]);
						}
						else
						{
							return int.Parse(kv_entries[i + 1]);
						}
					}
					break;
				}
			}
			return dv;
		}
		public float GetFloat (string key, float dv)
		{
			for (byte i = 0; i < entry_count; i++)
			{
				if (kv_entries[i] == key)
				{
					if (i + 1 < entry_count)
					return float.Parse(kv_entries[i + 1]);
					break;
				}
			}
			return dv;
		}
		public Vector3 GetVector3 (string key)
		{
			for (byte i = 0; i < entry_count; i++)
			{
				if (kv_entries[i] == key)
				{
					if (i + 1 < entry_count)
					{
						string value3 = kv_entries[i + 1];
						char[] buf = new char[16];
						string[] vector_values = new string[5];
						int ch_i = 0;
						int str_i = 0;
						bool sp = true;
						for (int i2 = 0; i2 <value3.Length; i2++)
						{
							if (value3[i2] == ' ')
							{
								if (!sp)
								{
									vector_values[str_i] = new string(buf, 0, ch_i);
									str_i++;
									ch_i = 0;
									sp = true;
								}
							}
							else
							{
								buf[ch_i] = value3[i2];
								ch_i++;
								sp = false;
							}
						}
						vector_values[str_i] = new string(buf, 0, ch_i);
						str_i++;
						ch_i = 0;
						if (str_i < 3)
						{
							Debug.LogError("Insuficient parameters for Vector3 "+this.GetString("classname")+"->"+key);
							return Vector3.zero;
						}
						return new Vector3(float.Parse(vector_values[0]), float.Parse(vector_values[1]), float.Parse(vector_values[2]));
					}
					break;
				}
			}			
			return Vector3.zero;
		}
		public Color32 GetColor32 (string key)
		{
			for (byte i = 0; i < entry_count; i++)
			{
				if (kv_entries[i] == key)
				{
					if (i + 1 < entry_count)
					{
						string value3 = kv_entries[i + 1];
						char[] buf = new char[16];
						string[] color_values = new string[5];
						int ch_i = 0;
						int str_i = 0;
						bool sp = true;
						for (int i2 = 0; i2 <value3.Length; i2++)
						{
							if (value3[i2] == ' ')
							{
								if (!sp)
								{
									color_values[str_i] = new string(buf, 0, ch_i);
									str_i++;
									ch_i = 0;
									sp = true;
								}
							}
							else
							{
								buf[ch_i] = value3[i2];
								ch_i++;
								sp = false;
							}
						}
						color_values[str_i] = new string(buf, 0, ch_i);
						str_i++;
						ch_i = 0;
						if (str_i < 3)
						{
							Debug.LogError("Insuficient parameters for Color32 "+this.GetString("classname")+"->"+key);
							return new Color32(0, 0, 0, 255);
						}
						return new Color32((byte)float.Parse(color_values[0]), (byte)float.Parse(color_values[1]), (byte)float.Parse(color_values[2]), 255);
					}
					break;
				}
			}			
			return new Color32(0, 0, 0, 255);
		}
	}
	
	public struct TexInfo
	{
		public string name;
		public int flags;
		public int contents;
	}

	public struct BSPVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Color32 color;
		public Vector2 tex_coord;
		public Vector2 lm_coord;
	}

	public struct BSPFace
	{
		public int tex_index;
		public int effect;
		public int type; // 1=polygon, 2=patch, 3=mesh, 4=billboard
		public int vertex;
		public int n_vtx;
		public int mesh_vertex;
		public int n_mesh_vtx;
		public int lm_index;
		public int lm_start_x;
		public int lm_start_y;
		public int lm_size_x;
		public int lm_size_y;
		public Vector3 lm_origin;
		public Vector3 lm_vector1;
		public Vector3 lm_vector2;
		public Vector3 normal;
		public int size_x;
		public int size_y;
	}

	public struct BSPModel
	{
		public Vector3 mins;
		public Vector3 maxs;
		public int face;
		public int n_faces;
		public int brush;
		public int n_brushes;
	}

	public struct InfoOrigin
	{
		public Vector3 position;
		public string modelName;
	}

	public struct BrushModelEntity
	{
		public int modelIndex;
		public int entityIndex;
		public string name;
		public string parent;
		public byte collision;
		public float lm_scale;
		public float smoothing;
		public string cubemapName;
		public bool isStatic;
		public bool isMaster;
		public bool isInstance;
		public Vector3 origin;
	}
	
	public struct MeshVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
		public int baseIndex;
	}

	public struct SubMesh
	{
		public int texId;
		public List<int> triangles;
	}
	
	public struct ObjectStringPair
	{
		public GameObject object1;
		public string string1;
		public ObjectStringPair (GameObject obj, string str1)
		{
			object1 = obj;
			string1 = str1;
		}
	}

	public struct BSPNode
	{
		public int planeIndex;
		public int child1; // negative numbers are leafs
		public int child2;
		public int[] mins; // 3
		public int[] maxs; // 3
		public void GetFaces (List<int> receiver)
		{
			if (child1 < 0)
			{
				bsp_leaves[-child1 - 1].GetFaces(receiver);
			}
			else
			{
				bsp_nodes[child1].GetFaces(receiver);
			}
			if (child2 < 0)
			{
				bsp_leaves[-child2 - 1].GetFaces(receiver);
			}
			else
			{
				bsp_nodes[child2].GetFaces(receiver);
			}
		}
		public void GetSurfaceArea (ref float receiver, List<int> face_list)
		{
			if (child1 < 0)
			{
				bsp_leaves[-child1 - 1].GetSurfaceArea(ref receiver, face_list);
			}
			else
			{
				bsp_nodes[child1].GetSurfaceArea(ref receiver, face_list);
			}
			if (child2 < 0)
			{
				bsp_leaves[-child2 - 1].GetSurfaceArea(ref receiver, face_list);
			}
			else
			{
				bsp_nodes[child2].GetSurfaceArea(ref receiver, face_list);
			}
		}
	}
	
	public struct BSPLeaf
	{
		public int visCluster;
		public int portalArea;
		public int[] mins; // 3
		public int[] maxs; // 3
		public int leafFace;
		public int n_leafFaces;
		public int leafBrush;
		public int n_leafBrushes;
		public void GetFaces (List<int> receiver)
		{
			int faceOffset = leafFace;
			int face_min = bsp_models[0].face;
			int face_max = face_min + bsp_models[0].n_faces;
			for (int i = 0; i < n_leafFaces; i++)
			{
				if (bsp_leaffaces[faceOffset] >= face_min && bsp_leaffaces[faceOffset] <= face_max)
				{
					if (!tex_infos[bsp_faces[bsp_leaffaces[faceOffset]].tex_index].name.Contains("nodraw") && free_faces[bsp_leaffaces[faceOffset]])
					{
						receiver.Add(bsp_leaffaces[faceOffset]);
						free_faces[bsp_leaffaces[faceOffset]] = false;
					}
				}
				faceOffset++;
			}
		}
		public void GetSurfaceArea (ref float receiver, List<int> face_list)
		{
			int faceOffset = leafFace;
			int face_min = bsp_models[0].face;
			int face_max = face_min + bsp_models[0].n_faces;
			for (int i = 0; i < n_leafFaces; i++)
			{
				if (bsp_leaffaces[faceOffset] >= face_min && bsp_leaffaces[faceOffset] <= face_max)
				{
					if (!(tex_infos[bsp_faces[bsp_leaffaces[faceOffset]].tex_index].name.Contains("nodraw")) && !face_list.Contains(bsp_leaffaces[faceOffset]))
					{
						int offset2 = bsp_faces[bsp_leaffaces[faceOffset]].mesh_vertex;
						for (int i2 = 0; i2 < bsp_faces[bsp_leaffaces[faceOffset]].n_mesh_vtx; i2 += 3)
						{
							receiver += GetTriangleArea(bsp_vertices[bsp_mesh_vertices[offset2] + bsp_faces[bsp_leaffaces[faceOffset]].vertex].position, bsp_vertices[bsp_mesh_vertices[offset2 + 1] + bsp_faces[bsp_leaffaces[faceOffset]].vertex].position, bsp_vertices[bsp_mesh_vertices[offset2 + 2] + bsp_faces[bsp_leaffaces[faceOffset]].vertex].position);
							offset2 += 3;
						}
						face_list.Add(bsp_leaffaces[faceOffset]);
					}
				}
				faceOffset++;
			}
		}
	}
	
	[MenuItem("Tools/BSP/Import Map")]
	public static void Init()
	{
		window = BSPMapImport.CreateInstance<BSPMapImport>();
		window.titleContent = new GUIContent("Import BSP");
		BSPCommon.LoadSettings();
		MaterialsPath = BSPCommon.MaterialsPath;
		ModelsPath = BSPCommon.ModelsPath;
		SoundPath = BSPCommon.SoundPath;
		MaxMeshSurfaceArea = BSPCommon.MaxMeshSurfaceArea;
		UV2Padding = BSPCommon.UV2Padding;
		window.ShowUtility();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(20));
		EditorGUILayout.LabelField("Materials Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			MaterialsPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Materials Path", "Assets", ""));
		}
		EditorGUILayout.Space();
		if (GUILayout.Button("Save Defaults", GUILayout.Width(100)))
		{
			BSPCommon.MaterialsPath = MaterialsPath;
			BSPCommon.ModelsPath = ModelsPath;
			BSPCommon.SoundPath = SoundPath;
			BSPCommon.UV2Padding = UV2Padding;
			BSPCommon.MaxMeshSurfaceArea = MaxMeshSurfaceArea;
			BSPCommon.SaveSettings();
			Debug.Log("Saved Settings: bsp_settings.txt");
		}
		EditorGUILayout.EndHorizontal();
		MaterialsPath = EditorGUILayout.TextField(MaterialsPath);
		EditorGUILayout.BeginHorizontal(GUILayout.Width(200), GUILayout.Height(20));
		EditorGUILayout.LabelField("Models Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			ModelsPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Models Path", "Assets", ""));
		}
		EditorGUILayout.EndHorizontal();
		ModelsPath = EditorGUILayout.TextField(ModelsPath);
		
		EditorGUILayout.BeginHorizontal(GUILayout.Width(200), GUILayout.Height(20));
		EditorGUILayout.LabelField("Sound Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			SoundPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Sound Path", "Assets", ""));
		}
		EditorGUILayout.EndHorizontal();
		SoundPath = EditorGUILayout.TextField(SoundPath);
		loadEntities = EditorGUILayout.Toggle("Load Entities", loadEntities);
		UV2Padding = EditorGUILayout.FloatField("UV2 Padding", UV2Padding);
		smoothing_angle = EditorGUILayout.FloatField("Smoothing Angle", smoothing_angle);
		splitMesh = EditorGUILayout.Toggle("Split Main Mesh", splitMesh);
		if (splitMesh)
		{
			MaxMeshSurfaceArea = EditorGUILayout.FloatField("Max Surface Area", MaxMeshSurfaceArea);
		}
		EditorGUILayout.BeginHorizontal(GUILayout.Width(500), GUILayout.Height(27));
		if (GUILayout.Button("Import BSP", GUILayout.Width(164), GUILayout.Height(25)))
		{
			ImportBsp();
		}
		if (GUILayout.Button("Import MAP", GUILayout.Width(164), GUILayout.Height(25)))
		{
			ImportMap();
		}
		EditorGUILayout.EndHorizontal();
	}
	
	static void ImportBsp ()
	{
		string input_path = EditorUtility.OpenFilePanel("Import BSP", current_folder, "bsp");
		if (string.IsNullOrEmpty(input_path))
		{
			return;
		}
		current_folder = Path.GetDirectoryName(input_path);
		LoadBSP(input_path);
	}
	
	static void ImportMap ()
	{
		string input_path = EditorUtility.OpenFilePanel("Import MAP", current_folder, "map");
		if (string.IsNullOrEmpty(input_path))
		{
			return;
		}
		current_folder = Path.GetDirectoryName(input_path);
		if (BSPCommon.StringEndsWith(input_path, ".map"))
		{
			string game_dir = BSPCommon.GetParentFolder(Path.GetDirectoryName(input_path));
			string tools_path = BSPCommon.GetParentFolder(game_dir)+"/Tools";			
			string exepath1 = tools_path+"/q3map2.exe";
			string exepath2 = tools_path+"/q3map.exe";
			string compiler_path = "";
			if (File.Exists(exepath1))
			{
				compiler_path = exepath1;
			}
			else if (File.Exists(exepath2))
			{
				compiler_path = exepath2;
			}
			else 
			{
				EditorUtility.DisplayDialog("Error", "PROJECT_PATH/Tools/q3map.exe not found", "OK");
				return;
			}
			System.Diagnostics.ProcessStartInfo args1 = new System.Diagnostics.ProcessStartInfo (compiler_path, input_path);
			args1.WorkingDirectory = game_dir;
			System.Diagnostics.Process q3map = System.Diagnostics.Process.Start(args1);
			q3map.WaitForExit(30000);
			q3map.Close();
			input_path = BSPCommon.RemoveExtension(input_path)+".bsp";
			if (!File.Exists(input_path))
			{
				Debug.LogError("Map compiler did not generate a BSP file.");
				return;
			}
			LoadBSP(input_path);
			string df = input_path;
			if (File.Exists(df)) File.Delete(df);
			df = BSPCommon.RemoveExtension(df)+".srf";
			if (File.Exists(df)) File.Delete(df);
			df = BSPCommon.RemoveExtension(df)+".prt";
			if (File.Exists(df)) File.Delete(df);
			df = BSPCommon.RemoveExtension(df)+".jmx";
			if (File.Exists(df)) File.Delete(df);
			df = BSPCommon.RemoveExtension(df)+".bak";
			if (File.Exists(df)) File.Delete(df);
			df = BSPCommon.RemoveExtension(df)+".max";
			if (File.Exists(df)) File.Delete(df);
		}
	}
	
	static void LoadBSP (string bsp_path)
	{
		FileStream fs1 = new FileStream(bsp_path, FileMode.Open, FileAccess.Read);
		BinaryReader br1 = new BinaryReader(fs1, System.Text.Encoding.UTF8);
		br1.BaseStream.Position = 0;
		string signature = new string(br1.ReadChars(4));
		int version = br1.ReadInt32();
		if (signature != "IBSP" || version != 46) Debug.LogWarning("BSP Importer: Unknown signature or version.");

		int entities_offset = br1.ReadInt32();
		int entities_length = br1.ReadInt32(); // 16
		int textures_offset = br1.ReadInt32();
		int textures_length = br1.ReadInt32(); // 24
		
		int planes_offset = br1.ReadInt32();
		int planes_length = br1.ReadInt32();
		int nodes_offset = br1.ReadInt32();
		int nodes_length = br1.ReadInt32();
		int leaves_offset = br1.ReadInt32();
		int leaves_length = br1.ReadInt32();
		int leaffaces_offset = br1.ReadInt32();
		int leaffaces_length = br1.ReadInt32();
		
		br1.BaseStream.Seek(64, SeekOrigin.Begin);
		int models_offset = br1.ReadInt32();
		int models_length = br1.ReadInt32();
		br1.BaseStream.Seek(88, SeekOrigin.Begin); // Vertices
		int vertices_offset = br1.ReadInt32();
		int vertices_length = br1.ReadInt32();
		int m_vertices_offset = br1.ReadInt32();
		int m_vertices_length = br1.ReadInt32();
		br1.BaseStream.Seek(112, SeekOrigin.Begin); // Faces
		int faces_offset = br1.ReadInt32();
		int faces_length = br1.ReadInt32();
		//int lms_offset = br1.ReadInt32();
		//int lms_length = br1.ReadInt32();

		int vertex_count = vertices_length / 44;
		int m_vertex_count = m_vertices_length / 4;
		int face_count = faces_length / 104;
		//int lms_count = lms_length / 49152;
		int tex_count = textures_length / 72;
		int model_count = models_length / 40;
		
		int planes_count = planes_length / 16;
		int nodes_count = nodes_length / 36;
		int leaves_count = leaves_length / 48;
		int leaffaces_count = leaffaces_length / 4;

		tex_infos = new TexInfo[tex_count];
		bsp_planes = new Plane[planes_count];
		bsp_nodes = new BSPNode[nodes_count];
		bsp_leaves = new BSPLeaf[leaves_count];
		bsp_leaffaces = new int[leaffaces_count];
		
		bsp_models = new BSPModel[model_count];
		bsp_vertices = new BSPVertex[vertex_count];
		bsp_faces = new BSPFace[face_count];
		bsp_mesh_vertices = new int[m_vertex_count];
		bsp_entities = new List<BSPEntity>();

		br1.BaseStream.Seek(textures_offset, SeekOrigin.Begin);
		for (int i = 0; i < tex_count; i++)
		{
			tex_infos[i].name = new string(br1.ReadChars(64));
			tex_infos[i].name = tex_infos[i].name.Replace("\0", "");
			tex_infos[i].flags = br1.ReadInt32();
			tex_infos[i].contents = br1.ReadInt32();
		}

		br1.BaseStream.Seek(planes_offset, SeekOrigin.Begin);
		for (int i = 0; i < planes_count; i++)
		{
			Vector3 plane_normal = new Vector3(0, 0, 0);
			plane_normal.x = -br1.ReadSingle();
			plane_normal.z = -br1.ReadSingle();
			plane_normal.y = br1.ReadSingle();
			bsp_planes[i].normal = plane_normal;
			bsp_planes[i].distance = br1.ReadSingle() * ImportScale;
		}
		
		br1.BaseStream.Seek(leaves_offset, SeekOrigin.Begin);
		for (int i = 0; i < leaves_count; i++)
		{
			bsp_leaves[i].visCluster = br1.ReadInt32();
			bsp_leaves[i].portalArea = br1.ReadInt32();
			bsp_leaves[i].mins = new int[3];
			bsp_leaves[i].mins[0] = br1.ReadInt32();
			bsp_leaves[i].mins[1] = br1.ReadInt32();
			bsp_leaves[i].mins[2] = br1.ReadInt32();
			bsp_leaves[i].maxs = new int[3];
			bsp_leaves[i].maxs[0] = br1.ReadInt32();
			bsp_leaves[i].maxs[1] = br1.ReadInt32();
			bsp_leaves[i].maxs[2] = br1.ReadInt32();
			bsp_leaves[i].leafFace = br1.ReadInt32();
			bsp_leaves[i].n_leafFaces = br1.ReadInt32();
			bsp_leaves[i].leafBrush = br1.ReadInt32();
			bsp_leaves[i].n_leafBrushes = br1.ReadInt32();
		}

		br1.BaseStream.Seek(nodes_offset, SeekOrigin.Begin);
		for (int i = 0; i < nodes_count; i++)
		{
			bsp_nodes[i].planeIndex = br1.ReadInt32();
			bsp_nodes[i].child1 = br1.ReadInt32();
			bsp_nodes[i].child2 = br1.ReadInt32();
			bsp_nodes[i].mins = new int[3];
			bsp_nodes[i].mins[0] = br1.ReadInt32();
			bsp_nodes[i].mins[1] = br1.ReadInt32();
			bsp_nodes[i].mins[2] = br1.ReadInt32();
			bsp_nodes[i].maxs = new int[3];
			bsp_nodes[i].maxs[0] = br1.ReadInt32();
			bsp_nodes[i].maxs[1] = br1.ReadInt32();
			bsp_nodes[i].maxs[2] = br1.ReadInt32();
		}

		br1.BaseStream.Seek(leaffaces_offset, SeekOrigin.Begin);
		for(int i = 0; i < leaffaces_count; i++)
		{
			bsp_leaffaces[i] = br1.ReadInt32();
		}

		br1.BaseStream.Seek(models_offset, SeekOrigin.Begin);
		for (int i = 0; i < model_count; i++)
		{
			bsp_models[i].mins = new Vector3(0, 0, 0);
			bsp_models[i].mins.x = -br1.ReadSingle() * ImportScale;
			bsp_models[i].mins.z = -br1.ReadSingle() * ImportScale;
			bsp_models[i].mins.y = br1.ReadSingle() * ImportScale;
			bsp_models[i].maxs = new Vector3(0, 0, 0);
			bsp_models[i].maxs.x = -br1.ReadSingle() * ImportScale;
			bsp_models[i].maxs.z = -br1.ReadSingle() * ImportScale;
			bsp_models[i].maxs.y = br1.ReadSingle() * ImportScale;
			bsp_models[i].face = br1.ReadInt32();
			bsp_models[i].n_faces = br1.ReadInt32();
			bsp_models[i].brush = br1.ReadInt32();
			bsp_models[i].n_brushes = br1.ReadInt32();
		}

		br1.BaseStream.Seek(vertices_offset, SeekOrigin.Begin);
		for (int i = 0; i < vertex_count; i++)
		{
			bsp_vertices[i].position = new Vector3(0, 0, 0);
			bsp_vertices[i].position.x = -br1.ReadSingle() * ImportScale;
			bsp_vertices[i].position.z = -br1.ReadSingle() * ImportScale;
			bsp_vertices[i].position.y = br1.ReadSingle() * ImportScale;
			bsp_vertices[i].tex_coord = new Vector2(0, 0);
			bsp_vertices[i].tex_coord.x = br1.ReadSingle();
			bsp_vertices[i].tex_coord.y = -br1.ReadSingle();
			bsp_vertices[i].lm_coord = new Vector2(0, 0);
			bsp_vertices[i].lm_coord.x = br1.ReadSingle();
			bsp_vertices[i].lm_coord.y = -br1.ReadSingle();
			bsp_vertices[i].normal = new Vector3(0, 0, 0);
			bsp_vertices[i].normal.x = -br1.ReadSingle();
			bsp_vertices[i].normal.z = -br1.ReadSingle();
			bsp_vertices[i].normal.y = br1.ReadSingle();
			bsp_vertices[i].normal.Normalize();
			bsp_vertices[i].color = new Color32(0, 0, 0, 0);
			bsp_vertices[i].color.r = br1.ReadByte();
			bsp_vertices[i].color.g = br1.ReadByte();
			bsp_vertices[i].color.b = br1.ReadByte();
			bsp_vertices[i].color.a = br1.ReadByte();
		}

		br1.BaseStream.Seek(faces_offset, SeekOrigin.Begin);
		for (int i = 0; i < face_count; i++)
		{
			bsp_faces[i].tex_index = br1.ReadInt32();
			bsp_faces[i].effect = br1.ReadInt32();
			bsp_faces[i].type = br1.ReadInt32();
			bsp_faces[i].vertex = br1.ReadInt32();
			bsp_faces[i].n_vtx = br1.ReadInt32();
			bsp_faces[i].mesh_vertex = br1.ReadInt32();
			bsp_faces[i].n_mesh_vtx = br1.ReadInt32();
			bsp_faces[i].lm_index = br1.ReadInt32();
			bsp_faces[i].lm_start_x = br1.ReadInt32();
			bsp_faces[i].lm_start_y = br1.ReadInt32();
			bsp_faces[i].lm_size_x = br1.ReadInt32();
			bsp_faces[i].lm_size_y = br1.ReadInt32();
			bsp_faces[i].lm_origin = new Vector3(0, 0, 0);
			bsp_faces[i].lm_origin.x = br1.ReadSingle();
			bsp_faces[i].lm_origin.y = br1.ReadSingle();
			bsp_faces[i].lm_origin.z = br1.ReadSingle();
			bsp_faces[i].lm_vector1 = new Vector3(0, 0, 0);
			bsp_faces[i].lm_vector1.x = br1.ReadSingle();
			bsp_faces[i].lm_vector1.y = br1.ReadSingle();
			bsp_faces[i].lm_vector1.z = br1.ReadSingle();
			bsp_faces[i].lm_vector2 = new Vector3(0, 0, 0);
			bsp_faces[i].lm_vector2.x = br1.ReadSingle();
			bsp_faces[i].lm_vector2.y = br1.ReadSingle();
			bsp_faces[i].lm_vector2.z = br1.ReadSingle();
			bsp_faces[i].normal = new Vector3(0, 0, 0);
			bsp_faces[i].normal.x = -br1.ReadSingle();
			bsp_faces[i].normal.z = -br1.ReadSingle();
			bsp_faces[i].normal.y = br1.ReadSingle();
			bsp_faces[i].size_x = br1.ReadInt32();
			bsp_faces[i].size_y = br1.ReadInt32();
		}

		br1.BaseStream.Seek(m_vertices_offset, SeekOrigin.Begin);
		for (int i = 0; i < m_vertex_count; i++)
		{
			bsp_mesh_vertices[i] = br1.ReadInt32();
		}

		br1.BaseStream.Seek(entities_offset, SeekOrigin.Begin);
		char c1 = 'a';
		char[] buf = new char[256];
		byte buf_i = 0;
		byte str_i = 0;
		bool readString = false;
		BSPEntity current_bsp_entity = new BSPEntity();
		while (br1.BaseStream.Position < entities_offset + entities_length - 1)
		{
			c1 = br1.ReadChar();
			if (readString)
			{
				if (c1 == '\"')
				{
					current_bsp_entity.kv_entries[str_i] = new string(buf, 0, buf_i);
					current_bsp_entity.entry_count++;
					str_i++;
					buf_i = 0;
					readString = false;
				}
				else
				{
					buf[buf_i] = c1;
					buf_i++;
				}
			}
			else
			{
				if (c1 == '{')
				{
					current_bsp_entity.kv_entries = new string[50];
					current_bsp_entity.entry_count = 0;
					str_i = 0;
					buf_i = 0;
				}
				else if (c1 == '\"')
				{
					readString = true;
				}
				else if (c1 == '}')
				{
					bsp_entities.Add(current_bsp_entity);
				}
			}
		}
		
		List<InfoOrigin> info_origins = new List<InfoOrigin>();
		InfoOrigin current_info_origin = new InfoOrigin();
		List<BrushModelEntity> brush_model_entities = new List<BrushModelEntity>();
		BrushModelEntity current_brush_model_entity = new BrushModelEntity();

		for (int i = 0; i < bsp_entities.Count; i++)
		{
			if (bsp_entities[i].GetString("classname") == "info_origin")
			{
				current_info_origin.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				current_info_origin.modelName = bsp_entities[i].GetString("name");
				info_origins.Add(current_info_origin);
			}
		}

		for (int i = 0; i < bsp_entities.Count; i++)
		{
			if (IsBrushEntity(bsp_entities[i].GetString("classname")))
			{
				string model_value = bsp_entities[i].GetString("model");
				current_brush_model_entity.modelIndex = int.Parse(model_value.Substring(1, model_value.Length - 1));
				current_brush_model_entity.entityIndex = i;
				current_brush_model_entity.name = bsp_entities[i].GetString("name");
				current_brush_model_entity.parent = bsp_entities[i].GetString("parent");
				current_brush_model_entity.collision = (byte)bsp_entities[i].GetInt("collision", 1);
				current_brush_model_entity.lm_scale = bsp_entities[i].GetFloat("lm_scale", 1.0f);
				current_brush_model_entity.smoothing = bsp_entities[i].GetFloat("smoothing", 30.0f);
				current_brush_model_entity.cubemapName = bsp_entities[i].GetString("cubemap");
				int spawn_flags = bsp_entities[i].GetInt("spawnflags", 0);
				current_brush_model_entity.isStatic = ((spawn_flags & 0x00000001) > 0);
				current_brush_model_entity.isMaster = ((spawn_flags & 0x00000002) > 0);
				current_brush_model_entity.isInstance = ((spawn_flags & 0x00000004) > 0);
				current_brush_model_entity.origin = (bsp_models[current_brush_model_entity.modelIndex].mins + bsp_models[current_brush_model_entity.modelIndex].maxs) * 0.5f;
				for (int i2 = 0; i2 < info_origins.Count; i2++)
				{
					if (current_brush_model_entity.name == info_origins[i2].modelName)
					{
						current_brush_model_entity.origin = info_origins[i2].position;
						break;
					}
				}
				brush_model_entities.Add(current_brush_model_entity);
			}
		}
		br1.Close();
		fs1.Close();
		
		uw1 = new UnwrapParam();
		uw1.hardAngle = 70.0f;
		uw1.angleError = 8.0f;
		uw1.areaError = 15.0f;
		uw1.packMargin = UV2Padding;
		
		node_mesh_count = 0;
		
		map_materials = new List<Material>();
		
		free_faces = new bool[bsp_faces.Length];
		for (int i = 0; i < free_faces.Length; i++)
		{
			free_faces[i] = true;
		}
		
		if (!splitMesh)
		{
			CreateMesh(0, "bsp_main", false, Vector3.zero, 1);
		}
		else
		{
			List<int> unique_face_list = new List<int>();
			ProcessNode(0, unique_face_list);
		}
		brush_model_entities.Sort(SortBSPModels);
		List<int> face_indices = new List<int>();
		
		List<ObjectStringPair> transform_children = new List<ObjectStringPair>();
		List<ObjectStringPair> cubemap_users = new List<ObjectStringPair>();
		List<ObjectStringPair> map_elevators = new List<ObjectStringPair>();
		List<ObjectStringPair> target_users = new List<ObjectStringPair>();
		List<AudioClip> map_sounds = new List<AudioClip>();
		//#if CUSTOM_ENTITIES
		//List<Entities.PathTrack> scene_path_tracks = new List<Entities.PathTrack>();
		//List<string> path_track_links = new List<string>();
		//#endif
		
		int brush_model_count = 0;

		Vector3 model_origin;
		string model_name;
		string cubemap_name;
		string parent;
		byte collider_type;
		float lightmap_scale;
		float normal_smoothing;
		bool is_static;
		int be_entity_index;

		for (int i15 = 0; i15 < brush_model_entities.Count; i15++)
		{
			model_origin = brush_model_entities[i15].origin;
			model_name = brush_model_entities[i15].name;
			if (string.IsNullOrEmpty(model_name))
			{
				model_name = "brush_model"+brush_model_count.ToString();
				brush_model_count++;
			}
			parent = brush_model_entities[i15].parent;
			cubemap_name = brush_model_entities[i15].cubemapName;
			collider_type = brush_model_entities[i15].collision;
			lightmap_scale = brush_model_entities[i15].lm_scale;
			normal_smoothing = brush_model_entities[i15].smoothing;
			is_static = brush_model_entities[i15].isStatic;
			be_entity_index = brush_model_entities[i15].entityIndex;
			bool loop1 = true;
			
			for (int i = 0; i < bsp_models[brush_model_entities[i15].modelIndex].n_faces; i++)
			{
				if (!tex_infos[bsp_faces[bsp_models[brush_model_entities[i15].modelIndex].face + i].tex_index].name.Contains("nodraw"))
				{
					face_indices.Add(bsp_models[brush_model_entities[i15].modelIndex].face + i);
				}
			}
			while (loop1)
			{
				if (i15 + 1 < brush_model_entities.Count)
				{
					if (!string.IsNullOrEmpty(brush_model_entities[i15 + 1].name) && brush_model_entities[i15 + 1].name == brush_model_entities[i15].name && !brush_model_entities[i15 + 1].isInstance)
					{
						for (int i = 0; i < bsp_models[brush_model_entities[i15 + 1].modelIndex].n_faces; i++)
						{
							if (!tex_infos[bsp_faces[bsp_models[brush_model_entities[i15 + 1].modelIndex].face + i].tex_index].name.Contains("nodraw"))
							{
								face_indices.Add(bsp_models[brush_model_entities[i15 + 1].modelIndex].face + i);
							}
						}
						if (brush_model_entities[i15 + 1].isMaster)
						{
							model_origin = brush_model_entities[i15 + 1].origin;
							is_static = brush_model_entities[i15 + 1].isStatic;
							lightmap_scale = brush_model_entities[i15 + 1].lm_scale;
							normal_smoothing = brush_model_entities[i15 + 1].smoothing;
							be_entity_index = brush_model_entities[i15 + 1].entityIndex;
						}
						i15++;
					}
					else
					{
						loop1 = false;
					}
				}
				else
				{
					loop1 = false;
				}
			}
			if (face_indices.Count < 1) continue;

			List<MeshVertex> current_mesh_vertices = new List<MeshVertex>();
			List<int> current_mesh_tris = new List<int>();
			MeshVertex current_mesh_vertex = new MeshVertex();
			
			int model_face_count = face_indices.Count;
			
			List<SubMesh> mesh_submeshes = new List<SubMesh>();
			SubMesh current_submesh = new SubMesh();
			int current_submesh_index = 0;
			
			for (int i = 0; i < model_face_count; i++)
			{
				bool match = false;
				for (int i2 = 0; i2 < mesh_submeshes.Count; i2++)
				{
					if (bsp_faces[face_indices[i]].tex_index == mesh_submeshes[i2].texId)
					{
						current_submesh_index = i2;
						match = true;
						break;
					}
				}
				if (!match)
				{
					current_submesh_index = mesh_submeshes.Count;
					current_submesh.texId = bsp_faces[face_indices[i]].tex_index; 
					current_submesh.triangles = new List<int>();
					mesh_submeshes.Add(current_submesh);
				}
				
				int vtx_offset = bsp_faces[face_indices[i]].mesh_vertex;
				for (int i2 = 0; i2 < bsp_faces[face_indices[i]].n_mesh_vtx; i2++)
				{
					int local_vtx_index = 0;
					current_mesh_vertex.position = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].position - model_origin;
					current_mesh_vertex.normal = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].normal;
					current_mesh_vertex.uv = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].tex_coord;
					current_mesh_vertex.baseIndex = bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex;
					bool match2 = false;
					for (int i5 = 0; i5 < current_mesh_vertices.Count; i5++)
					{
						if (current_mesh_vertex.baseIndex == current_mesh_vertices[i5].baseIndex)
						{
							local_vtx_index = i5;
							match2 = true;
							break;
						}
					}
					if (!match2)
					{
						local_vtx_index = current_mesh_vertices.Count;
						current_mesh_vertices.Add(current_mesh_vertex);
					}
					current_mesh_tris.Add(local_vtx_index);
					mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);
					vtx_offset++;
				}
			}
			int mesh_vertex_count = current_mesh_vertices.Count;
			Mesh mesh1 = new Mesh();
			Vector3[] output_vertices2 = new Vector3[mesh_vertex_count];
			Vector3[] output_normals2 = new Vector3[mesh_vertex_count];
			Vector2[] output_uvs2 = new Vector2[mesh_vertex_count];
			for (int i = 0; i < mesh_vertex_count; i++)
			{
				output_vertices2[i] = current_mesh_vertices[i].position;
				output_normals2[i] = current_mesh_vertices[i].normal;
				output_uvs2[i] = current_mesh_vertices[i].uv;
			}		
			
			int[] mesh_triangles2 = current_mesh_tris.ToArray();
			mesh1.vertices = output_vertices2;
			mesh1.triangles = mesh_triangles2;
			mesh1.subMeshCount = mesh_submeshes.Count;
			
			for (int i = 0; i < mesh_submeshes.Count; i++)
			{
				mesh1.SetTriangles(mesh_submeshes[i].triangles.ToArray(), i);
			}
			mesh1.normals = output_normals2;
			mesh1.uv = output_uvs2;		
			mesh1.RecalculateBounds();
			mesh1.RecalculateTangents();
			mesh1.name = model_name+"_m";
			if (normal_smoothing > 0) AverageMeshNormals(mesh1, normal_smoothing);
			int material_count = mesh_submeshes.Count;
			GameObject model_object = new GameObject(model_name);
			model_object.transform.position = model_origin;
			model_object.AddComponent<MeshFilter>().mesh = mesh1;
			Material[] mesh_materials = new Material[material_count];
			string material_path;
			for (int i7 = 0; i7 < material_count; i7++)
			{
				material_path = MaterialsPath+"/"+BSPCommon.GetPathInFolder(tex_infos[mesh_submeshes[i7].texId].name, "textures")+".mat";
				Material current_material = null;
				for (int i8 = 0; i8 < map_materials.Count; i8++)
				{
					if (AssetDatabase.GetAssetPath(map_materials[i8]) == material_path)
					{
						current_material = map_materials[i8];
						break;
					}
				}
				if (current_material == null)
				{
					if (File.Exists(material_path))
					{
						current_material = (Material)AssetDatabase.LoadAssetAtPath(material_path, typeof(Material));
						map_materials.Add(current_material);
					}
					else
					{
						current_material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
						Debug.LogWarning("Map material "+material_path+" not found.");					
					}
				}
				mesh_materials[i7] = current_material;
			}
			MeshRenderer mr1 = model_object.AddComponent<MeshRenderer>();
			mr1.materials = mesh_materials;
			if (collider_type == 1)
			{
				model_object.AddComponent<MeshCollider>().sharedMesh = CollisionMesh(mesh1);
			}
			else if (collider_type == 2)
			{
				model_object.AddComponent<BoxCollider>();
			}
			else if (collider_type == 3)
			{
				model_object.AddComponent<SphereCollider>();
			}
			if (is_static)
			{
				Unwrapping.GenerateSecondaryUVSet(mesh1, uw1);
				SerializedObject s_obj = new SerializedObject(mr1);
				s_obj.FindProperty("m_ScaleInLightmap").floatValue = lightmap_scale;
				s_obj.ApplyModifiedProperties();
				s_obj = null;
				model_object.isStatic = true;
			}
			loop1 = true;
			while (loop1)
			{
				if (i15 + 1 < brush_model_entities.Count)
				{
					if (!string.IsNullOrEmpty(brush_model_entities[i15 + 1].name) && brush_model_entities[i15 + 1].name == brush_model_entities[i15].name)
					{
						GameObject model_instance = GameObject.Instantiate(model_object, brush_model_entities[i15 + 1].origin, Quaternion.identity);
						if (!string.IsNullOrEmpty(brush_model_entities[i15 + 1].parent)) transform_children.Add(new ObjectStringPair(model_instance, brush_model_entities[i15 + 1].parent));
						if (!string.IsNullOrEmpty(brush_model_entities[i15 + 1].cubemapName)) cubemap_users.Add(new ObjectStringPair(model_instance, brush_model_entities[i15 + 1].cubemapName));
						i15++;
					}
					else
					{
						loop1 = false;
					}
				}
				else
				{
					loop1 = false;
				}
			}

			if (!string.IsNullOrEmpty(parent)) transform_children.Add(new ObjectStringPair(model_object, parent));
			if (!string.IsNullOrEmpty(cubemap_name)) cubemap_users.Add(new ObjectStringPair(model_object, cubemap_name));
			face_indices.Clear();
			#if CUSTOM_ENTITIES // Additional setup for custom brush entities like doors and buttons.
			string be_classname = bsp_entities[be_entity_index].GetString("classname");
			int be_flags = bsp_entities[be_entity_index].GetInt("spawnflags", 0);
			string sound_name = "";
			string sound_path = "";
			string targetname = bsp_entities[be_entity_index].GetString("target");
			AudioClip clip2 = null;
			switch (be_classname)
			{
				case "brush_door_r":
				UBSPEntities.UBSPDoorRotating door1 = model_object.AddComponent<UBSPEntities.UBSPDoorRotating>();
				door1.axis = (UBSPEntities.UBSPDoorRotating.Axis)bsp_entities[be_entity_index].GetInt("axis", 1);
				door1.speed = bsp_entities[be_entity_index].GetFloat("speed", 90.0f);
				door1.angle = bsp_entities[be_entity_index].GetFloat("angle", 89.0f);
				door1.closeDelay = bsp_entities[be_entity_index].GetFloat("delay", 0);
				door1.CCW = ((be_flags & 0x00000008) > 0);
				door1.autoDir = ((be_flags & 0x00000010) > 0);
				door1.soundVolume = bsp_entities[be_entity_index].GetFloat("volume", 0.8f);
				if (!string.IsNullOrEmpty(targetname)) target_users.Add(new ObjectStringPair(model_object, targetname));
				sound_name = bsp_entities[be_entity_index].GetString("movesnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						door1.moveSound = clip2;
					}
				}
				sound_name = bsp_entities[be_entity_index].GetString("closesnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						door1.closeSound = clip2;
					}
				}
				model_object.isStatic = false;
				break;
				
				case "brush_door_s":
				UBSPEntities.UBSPDoorSliding door2 = model_object.AddComponent<UBSPEntities.UBSPDoorSliding>();
				door2.axis = (UBSPEntities.UBSPDoorSliding.Axis)bsp_entities[be_entity_index].GetInt("axis", 1);
				door2.speed = bsp_entities[be_entity_index].GetFloat("speed", 1.0f);
				door2.closeDelay = bsp_entities[be_entity_index].GetFloat("delay", 0);
				door2.distance = bsp_entities[be_entity_index].GetFloat("distance", 1.0f);
				door2.leap = bsp_entities[be_entity_index].GetFloat("leap", 0.01f);
				door2.startOpen = ((be_flags & 0x00000008) > 0);
				door2.autoDistance = ((be_flags & 0x00000010) > 0); // 16
				door2.reverse = ((be_flags & 0x00000020) > 0); // 32
				door2.soundVolume = bsp_entities[be_entity_index].GetFloat("volume", 0.8f);
				if (!string.IsNullOrEmpty(targetname)) target_users.Add(new ObjectStringPair(model_object, targetname));
				sound_name = bsp_entities[be_entity_index].GetString("movesnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						door2.moveSound = clip2;
					}
				}
				sound_name = bsp_entities[be_entity_index].GetString("closesnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						door2.closeSound = clip2;
					}
				}
				model_object.isStatic = false;
				break;
				
				case "brush_rotating":
				UBSPEntities.UBSPRotating cr1 = model_object.AddComponent<UBSPEntities.UBSPRotating>();
				cr1.axis = (UBSPEntities.UBSPRotating.Axis)bsp_entities[be_entity_index].GetInt("axis", 1);
				cr1.speed = bsp_entities[be_entity_index].GetFloat("speed", 90.0f);
				cr1.soundVolume = bsp_entities[be_entity_index].GetFloat("volume", 0.8f);
				if (!string.IsNullOrEmpty(targetname)) target_users.Add(new ObjectStringPair(model_object, targetname));
				sound_name = bsp_entities[be_entity_index].GetString("sound");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						cr1.sound = clip2;
					}
				}
				model_object.isStatic = false;
				break;				
				
				case "brush_rigidbody":
				MeshCollider rbmc = model_object.GetComponentInChildren<MeshCollider>();
				if (rbmc != null) rbmc.convex = true;
				Rigidbody rb1 = model_object.AddComponent<Rigidbody>();
				rb1.mass = bsp_entities[be_entity_index].GetFloat("mass", 5.0f);
				model_object.isStatic = false;
				break;
				
				case "brush_elevator":
				UBSPEntities.UBSPElevator e1 = model_object.AddComponent<UBSPEntities.UBSPElevator>();
				if ((be_flags & 0x00000010) > 0) // Add TriggerParent
				{
					GameObject pt_object = new GameObject(model_name+"_pt");
					pt_object.transform.position = model_object.transform.position;
					pt_object.transform.parent = model_object.transform;
					BoxCollider bc_e = pt_object.AddComponent<BoxCollider>();
					bc_e.isTrigger = true;
					Bounds mesh_bounds1 = model_object.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
					bc_e.center = (mesh_bounds1.max + mesh_bounds1.min) * 0.5f + Vector3.up * 0.1f;
					bc_e.size = (mesh_bounds1.max + Vector3.up * 0.1f) - mesh_bounds1.min;
					pt_object.AddComponent<UBSPEntities.UBSPTriggerParent>();
				}
				string e_points = bsp_entities[be_entity_index].GetString("points");
				if (e_points.Length > 1) map_elevators.Add(new ObjectStringPair(model_object, e_points));
				e1.speed = bsp_entities[be_entity_index].GetFloat("speed", 2.0f);
				e1.startLevel = bsp_entities[be_entity_index].GetInt("startlevel", 0);
				e1.soundVolume = bsp_entities[be_entity_index].GetFloat("volume", 0.8f);
				e1.directControl = ((be_flags & 0x00000008) > 0);
				if (!string.IsNullOrEmpty(targetname)) target_users.Add(new ObjectStringPair(model_object, targetname));
				sound_name = bsp_entities[be_entity_index].GetString("movesnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						e1.moveSound = clip2;
					}
				}
				sound_name = bsp_entities[be_entity_index].GetString("stopsnd");
				if (sound_name.Length > 1)
				{
					sound_path = SoundPath+"/"+BSPCommon.GetPathInFolder(sound_name, "sound");
					clip2 = null;
					for (int i2 = 0; i2 < map_sounds.Count; i2++)
					{
						if (sound_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
						{
							clip2 = map_sounds[i2];
							break;
						}
					}
					if (clip2 == null)
					{
						if (File.Exists(sound_path))
						{
							clip2 = (AudioClip)AssetDatabase.LoadAssetAtPath(sound_path, typeof(AudioClip));
							map_sounds.Add(clip2);
						}
						else
						{
							Debug.LogWarning("Sound "+sound_path+" not found.");
						}
					}
					if (clip2 != null)
					{
						e1.stopSound = clip2;
					}
				}				
				model_object.isStatic = false;
				break;
			}
			#endif
		}
		
		List<GameObject> map_models = new List<GameObject>();
		
		string entity_class_name;
		string entity_name;
		string game_object_name;
		string parent_name;
		int spawn_flags2;
		for (int i = 0; i < bsp_entities.Count; i++)
		{
			entity_class_name = bsp_entities[i].GetString("classname");
			switch (entity_class_name)
			{
				case "model":
				string md3_path = bsp_entities[i].GetString("model");
				if (string.IsNullOrEmpty(md3_path) || !md3_path.Contains("models")) break;
				string model_path = ModelsPath+"/"+BSPCommon.GetPathInFolder(md3_path, "models");
				model_path = model_path.Replace(".md3", "");
				string model_ext1 = model_path+".fbx";
				string model_ext2 = model_path+".prefab";
				string asset_path;
				GameObject model_object1 = null;
				for (int i2 = 0; i2 < map_models.Count; i2++)
				{
					asset_path = AssetDatabase.GetAssetPath(map_models[i2]);
					if (asset_path == model_ext1 || asset_path == model_ext2)
					{
						model_object1 = map_models[i2];
						break;
					}
				}
				if (model_object1 == null)
				{
					if (File.Exists(model_ext1))
					{
						model_object1 = (GameObject)AssetDatabase.LoadAssetAtPath(model_ext1, typeof(GameObject));
						map_models.Add(model_object1);
					}
					else if (File.Exists(model_ext2))
					{
						model_object1 = (GameObject)AssetDatabase.LoadAssetAtPath(model_ext2, typeof(GameObject));
						map_models.Add(model_object1);
					}
				}
				if (model_object1 == null) break;
				MeshFilter mf1 = model_object1.GetComponentInChildren<MeshFilter>();
				if (mf1 == null) break;
				Vector3 mesh_center = mf1.sharedMesh.bounds.center;
				Vector3 origin_offset = new Vector3(Mathf.Floor(mesh_center.x / ImportScale) * ImportScale, Mathf.Floor(mesh_center.y / ImportScale) * ImportScale, Mathf.Floor(mesh_center.z / ImportScale) * ImportScale);
				Vector3 md3_origin = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				Vector3 angles = bsp_entities[i].GetVector3("angles");
				Vector3 origin = md3_origin - origin_offset;
				GameObject md3_object = (GameObject)PrefabUtility.InstantiatePrefab(model_object1);
				md3_object.transform.position = origin;
				md3_object.transform.rotation = Quaternion.identity;
				if (angles.x != 0) md3_object.transform.RotateAround(md3_origin, Vector3.forward, angles.x);
				if (angles.y != 0) md3_object.transform.RotateAround(md3_origin, Vector3.up, -angles.y);
				if (angles.z != 0) md3_object.transform.RotateAround(md3_origin, Vector3.right, angles.z);
				spawn_flags2 = bsp_entities[i].GetInt("spawnflags", 0);
				if ((spawn_flags2 & 0x00000001) > 0)
				{
					md3_object.isStatic = true;
					MeshRenderer mr2 = md3_object.GetComponentInChildren<MeshRenderer>();
					SerializedObject s_obj = new SerializedObject(mr2);
					s_obj.FindProperty("m_ScaleInLightmap").floatValue = bsp_entities[i].GetFloat("lm_scale", 1.0f);
					s_obj.ApplyModifiedProperties();
					s_obj = null;
				}
				parent_name = bsp_entities[i].GetString("parent");
				cubemap_name = bsp_entities[i].GetString("cubemap");
				entity_name = bsp_entities[i].GetString("name");
				if (!string.IsNullOrEmpty(entity_name)) md3_object.name = entity_name;
				if (!string.IsNullOrEmpty(parent_name)) transform_children.Add(new ObjectStringPair(md3_object, parent_name));
				if (!string.IsNullOrEmpty(cubemap_name)) cubemap_users.Add(new ObjectStringPair(md3_object, cubemap_name));
				break;
				
				case "env_sound":
				string snd_path = bsp_entities[i].GetString("wavname");
				if (string.IsNullOrEmpty(snd_path) || !snd_path.Contains("sound")) break;
				string clip_path = SoundPath+"/"+BSPCommon.GetPathInFolder(snd_path, "sound");
				AudioClip clip1 = null;
				for (int i2 = 0; i2 < map_sounds.Count; i2++)
				{
					if (clip_path == AssetDatabase.GetAssetPath(map_sounds[i2]))
					{
						clip1 = map_sounds[i2];
						break;
					}
				}
				if (clip1 == null)
				{
					if (File.Exists(clip_path))
					{
						clip1 = (AudioClip)AssetDatabase.LoadAssetAtPath(clip_path, typeof(AudioClip));
						map_sounds.Add(clip1);
					}
					else
					{
						Debug.LogWarning("Sound "+clip_path+" not found.");
					}
				}
				if (clip1 == null) break;
				entity_name = bsp_entities[i].GetString("name");
				game_object_name = string.IsNullOrEmpty(entity_name) ? clip1.name : entity_name;
				GameObject snd_obj = new GameObject(game_object_name);
				snd_obj.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				AudioSource src1 = snd_obj.AddComponent<AudioSource>();
				src1.clip = clip1;
				src1.volume = Mathf.Clamp01(bsp_entities[i].GetFloat("volume", 1.0f));
				src1.spatialBlend = Mathf.Clamp01(bsp_entities[i].GetFloat("blend", 1.0f));
				spawn_flags2 = bsp_entities[i].GetInt("spawnflags", 0);
				src1.playOnAwake = ((spawn_flags2 & 0x00000001) > 0);
				src1.loop = ((spawn_flags2 & 0x00000002) > 0);
				float range = bsp_entities[i].GetFloat("range", 10.0f) * 1.2f;
				src1.maxDistance = range;
				src1.minDistance = range * 0.1f;
				parent_name = bsp_entities[i].GetString("parent");
				if (!string.IsNullOrEmpty(parent_name)) transform_children.Add(new ObjectStringPair(snd_obj, parent_name));
				break;
				
				case "light_point":
				entity_name = bsp_entities[i].GetString("name");
				game_object_name = string.IsNullOrEmpty(entity_name) ? "LightPoint" : entity_name;
				GameObject light_obj = new GameObject(game_object_name);
				light_obj.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				Light light1 = light_obj.AddComponent<Light>();
				light1.type = LightType.Point;
				int light_mode = bsp_entities[i].GetInt("mode", 0);
				switch (light_mode)
				{
					case 0:
					light1.lightmapBakeType = LightmapBakeType.Realtime;
					break;
					case 1:
					light1.lightmapBakeType = LightmapBakeType.Mixed;
					break;
					case 2:
					light1.lightmapBakeType = LightmapBakeType.Baked;
					break;					
				}
				light1.color = bsp_entities[i].GetColor32("_color");
				light1.intensity = bsp_entities[i].GetFloat("intensity", 1.0f);
				light1.range = bsp_entities[i].GetFloat("range", 10.0f);
				spawn_flags2 = bsp_entities[i].GetInt("spawnflags", 0);
				if ((spawn_flags2 & 0x00000001) > 0) light_obj.tag = "staticlight";
				parent_name = bsp_entities[i].GetString("parent");
				if (!string.IsNullOrEmpty(parent_name)) transform_children.Add(new ObjectStringPair(light_obj, parent_name));
				break;
				
				case "env_cubemap":
				entity_name = bsp_entities[i].GetString("name");
				game_object_name = string.IsNullOrEmpty(entity_name) ? "Cubemap" : entity_name;
				GameObject cmap_obj = new GameObject(game_object_name);
				cmap_obj.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				ReflectionProbe cmap_ref = cmap_obj.AddComponent<ReflectionProbe>();
				int ref_resolution = bsp_entities[i].GetInt("resolution", 2);
				switch (ref_resolution)
				{
					case 0:
					cmap_ref.resolution = 32;
					break;
					case 1:
					cmap_ref.resolution = 64;
					break;
					case 2:
					cmap_ref.resolution = 128;
					break;
					case 3:
					cmap_ref.resolution = 256;
					break;
					case 4:
					cmap_ref.resolution = 512;
					break;
				}
				spawn_flags2 = bsp_entities[i].GetInt("spawnflags", 0);
				cmap_ref.hdr = ((spawn_flags2 & 0x00000001) > 0);
				cmap_ref.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
				break;
				
				case "info_transform":
				entity_name = bsp_entities[i].GetString("name");
				if (string.IsNullOrEmpty(entity_name)) break;
				GameObject transform_obj = new GameObject(entity_name);
				transform_obj.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				break;
				
				#if INFO_PLAYER_START
				case "info_player_start":
				GameObject player_object = new GameObject("Player");
				player_object.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				float player_rotation = bsp_entities[i].GetFloat("angle", 0);
				if (player_rotation == 0) player_rotation = bsp_entities[i].GetVector3("angles").y;
				player_object.transform.eulerAngles = new Vector3(0, -90.0f - player_rotation, 0);
				CharacterController controller1 = player_object.AddComponent<CharacterController>();
				controller1.height = 1.8f;
				controller1.radius = 0.35f;
				Camera player_camera = (Camera)FindObjectOfType(typeof(Camera));
				if (player_camera == null)
				{
					GameObject camera_object = new GameObject("PlayerCamera");
					camera_object.transform.position = player_object.transform.position + new Vector3(0, 0.7f, 0);
					camera_object.transform.rotation = player_object.transform.rotation;					
					player_camera = camera_object.AddComponent<Camera>();
					#if !UNITY_2017_2_OR_NEWER
					camera_object.AddComponent<GUILayer>();
					#endif
					camera_object.AddComponent<FlareLayer>();
					camera_object.AddComponent<AudioListener>();
				}
				player_camera.fieldOfView = 75.0f;
				player_camera.nearClipPlane = 0.1f;
				player_camera.depth = -1.0f;
				player_object.AddComponent<UBSPEntities.UBSPPlayer>().playerCamera = player_camera;
				player_object.tag = "Player";
				break;
				#endif
				
				#if CUSTOM_ENTITIES
				case "brush_trigger":
				entity_name = bsp_entities[i].GetString("name");
				game_object_name = string.IsNullOrEmpty(entity_name) ? "Trigger" : entity_name;
				string model_id = bsp_entities[i].GetString("model");
				int model_index = int.Parse(model_id.Substring(1, model_id.Length - 1));
				Vector3 trigger_origin = (bsp_models[model_index].mins + bsp_models[model_index].maxs) * 0.5f;
				Vector3 trigger_size = bsp_models[model_index].maxs - bsp_models[model_index].mins;
				GameObject trigger_object = new GameObject(game_object_name);
				trigger_object.transform.position = trigger_origin;
				BoxCollider bc_t = trigger_object.AddComponent<BoxCollider>();
				bc_t.isTrigger = true;
				bc_t.center = new Vector3(0, 0, 0);
				bc_t.size = trigger_size;
				UBSPEntities.UBSPTrigger t1 = trigger_object.AddComponent<UBSPEntities.UBSPTrigger>();
				spawn_flags2 = bsp_entities[i].GetInt("spawnflags", 0);
				t1.once = ((spawn_flags2 & 0x00000008) > 0);
				t1.partialMatch = ((spawn_flags2 & 0x00000010) > 0);
				t1.restrictName = bsp_entities[i].GetString("restrict");
				break;
				
				//case "path_track":
				//entity_name = bsp_entities[i].GetString("name");
				//string next_track = bsp_entities[i].GetString("next");
				//game_object_name = string.IsNullOrEmpty(entity_name) ? "PathTrack" : entity_name;
				//GameObject path_track_obj = new GameObject(game_object_name);
				//path_track_obj.transform.position = Q3ToUnity(bsp_entities[i].GetVector3("origin"));
				//Entities.PathTrack path_track = path_track_obj.AddComponent<Entities.PathTrack>();
				//path_track.speed = bsp_entities[i].GetFloat("speed", 0);
				//scene_path_tracks.Add(path_track);
				//path_track_links.Add(next_track);
				//break;
				#endif
			}
		}
		
		for (int i = 0; i < cubemap_users.Count; i++)
		{
			cubemap_users[i].object1.GetComponentInChildren<MeshRenderer>().probeAnchor = GameObject.Find(cubemap_users[i].string1).transform;
		}

		for (int i = 0; i < transform_children.Count; i++)
		{
			transform_children[i].object1.transform.parent = GameObject.Find(transform_children[i].string1).transform;
		}
		
		#if CUSTOM_ENTITIES
		for (int i = 0; i < map_elevators.Count; i++)
		{
			string[] point_names = map_elevators[i].string1.Split(' ');
			List<Transform> elevator_points = new List<Transform>();
			for (int i20 = 0; i20 < point_names.Length; i20++)
			{
				if (!string.IsNullOrEmpty(point_names[i20]))
				{
					GameObject e_point_obj;
					e_point_obj = GameObject.Find(point_names[i20]);
					if (e_point_obj != null)
					{
						elevator_points.Add(e_point_obj.transform);
					}
				}
			}
			if (elevator_points.Count > 0)
			{
				map_elevators[i].object1.GetComponentInChildren<UBSPEntities.UBSPElevator>().levels = elevator_points.ToArray();
			}
		}
		
		for (int i = 0; i < target_users.Count; i++)
		{
			GameObject target_object = GameObject.Find(target_users[i].string1);
			if (target_object != null)
			{
				UBSPEntities.UBSPBaseActivator a1 = target_object.GetComponentInChildren<UBSPEntities.UBSPBaseActivator>();
				if (a1 != null) target_users[i].object1.GetComponentInChildren<UBSPEntities.UBSPBaseActivator>().target = a1;
			}
		}

		//for (int i = 0; i < scene_path_tracks.Count; i++)
		//{
			//if (!string.IsNullOrEmpty(path_track_links[i])) scene_path_tracks[i].next = GameObject.Find(path_track_links[i]).GetComponent<Entities.PathTrack>();
		//}
		//scene_path_tracks = null;
		//path_track_links = null;		
		#endif
		
		bsp_entities = null;
		cubemap_users = null;
		transform_children = null;
		map_elevators = null;
		map_materials = null;
		map_models = null;
		map_sounds = null;
		target_users = null;
		tex_infos = null;
		bsp_planes = null;
		bsp_nodes = null;
		bsp_leaves = null;
		bsp_leaffaces = null;
		bsp_models = null;
		bsp_vertices = null;
		bsp_faces = null;
		bsp_mesh_vertices = null;
		bsp_entities = null;
	}
	
	static void ProcessNode (int node_id, List<int> face_list1)
	{
		bool build_model = false;
		float surf_area = 0;
		if (node_id < 0) // Leaf
		{
			build_model = true;
		}
		else
		{
			bsp_nodes[node_id].GetSurfaceArea(ref surf_area, face_list1);
			if (surf_area < MaxMeshSurfaceArea && surf_area > 0)
			{
				build_model = true;
			}
			else if (surf_area > 0)
			{
				List<int> face_list2 = new List<int>();
				ProcessNode(bsp_nodes[node_id].child1, face_list2);
				ProcessNode(bsp_nodes[node_id].child2, face_list2);
			}
		}
		if (build_model)
		{
			Vector3 mins = Vector3.zero;
			Vector3 maxs = Vector3.zero;
			if (node_id < 0)
			{
				mins = new Vector3(-((float)bsp_leaves[-node_id - 1].mins[0] * ImportScale), (float)bsp_leaves[-node_id - 1].mins[2] * ImportScale, -((float)bsp_leaves[-node_id - 1].mins[1] * ImportScale));
				maxs = new Vector3(-((float)bsp_leaves[-node_id - 1].maxs[0] * ImportScale), (float)bsp_leaves[-node_id - 1].maxs[2] * ImportScale, -((float)bsp_leaves[-node_id - 1].maxs[1] * ImportScale));
			}
			else
			{
				mins = new Vector3(-((float)bsp_nodes[node_id].mins[0] * ImportScale), (float)bsp_nodes[node_id].mins[2] * ImportScale, -((float)bsp_nodes[node_id].mins[1] * ImportScale));
				maxs = new Vector3(-((float)bsp_nodes[node_id].maxs[0] * ImportScale), (float)bsp_nodes[node_id].maxs[2] * ImportScale, -((float)bsp_nodes[node_id].maxs[1] * ImportScale));				
			}
			CreateMesh(node_id, "bsp_main", true, (mins + maxs) * 0.5f, 1);
		}
	}
	
	static void CreateMesh (int source_id, string model_name, bool from_node, Vector3 origin, byte collider_type)
	{
		List<int> face_indices = new List<int>();
		if (from_node)
		{
			if (source_id < 0) bsp_leaves[-source_id - 1].GetFaces(face_indices);
			else bsp_nodes[source_id].GetFaces(face_indices);
		}
		else
		{
			for (int i = 0; i < bsp_models[source_id].n_faces; i++)
			{
				if (!tex_infos[bsp_faces[bsp_models[source_id].face + i].tex_index].name.Contains("nodraw"))
				{
					face_indices.Add(bsp_models[source_id].face + i);
				}
			}
		}
		if (face_indices.Count < 1) return;
		List<MeshVertex> current_mesh_vertices = new List<MeshVertex>();
		List<int> current_mesh_tris = new List<int>();
		MeshVertex current_mesh_vertex = new MeshVertex();
		
		int model_face_count = face_indices.Count;
		
		List<SubMesh> mesh_submeshes = new List<SubMesh>();
		SubMesh current_submesh = new SubMesh();
		int current_submesh_index = 0;
		
		for (int i = 0; i < model_face_count; i++)
		{
			bool match = false;
			for (int i2 = 0; i2 < mesh_submeshes.Count; i2++)
			{
				if (bsp_faces[face_indices[i]].tex_index == mesh_submeshes[i2].texId)
				{
					current_submesh_index = i2;
					match = true;
					break;
				}
			}
			if (!match)
			{
				current_submesh_index = mesh_submeshes.Count;
				current_submesh.texId = bsp_faces[face_indices[i]].tex_index; 
				current_submesh.triangles = new List<int>();
				mesh_submeshes.Add(current_submesh);
			}
			
			int vtx_offset = bsp_faces[face_indices[i]].mesh_vertex;
			for (int i2 = 0; i2 < bsp_faces[face_indices[i]].n_mesh_vtx; i2++)
			{
				int local_vtx_index = 0;
				current_mesh_vertex.position = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].position - origin;
				current_mesh_vertex.normal = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].normal;
				current_mesh_vertex.uv = bsp_vertices[bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex].tex_coord;
				current_mesh_vertex.baseIndex = bsp_mesh_vertices[vtx_offset] + bsp_faces[face_indices[i]].vertex;
				bool match2 = false;
				for (int i5 = 0; i5 < current_mesh_vertices.Count; i5++)
				{
					if (current_mesh_vertex.baseIndex == current_mesh_vertices[i5].baseIndex)
					{
						local_vtx_index = i5;
						match2 = true;
						break;
					}
				}
				if (!match2)
				{
					local_vtx_index = current_mesh_vertices.Count;
					current_mesh_vertices.Add(current_mesh_vertex);
				}
				current_mesh_tris.Add(local_vtx_index);
				mesh_submeshes[current_submesh_index].triangles.Add(local_vtx_index);
				vtx_offset++;
			}
		}
		int mesh_vertex_count = current_mesh_vertices.Count;
		Mesh mesh1 = new Mesh();
		Vector3[] output_vertices2 = new Vector3[mesh_vertex_count];
		Vector3[] output_normals2 = new Vector3[mesh_vertex_count];
		Vector2[] output_uvs2 = new Vector2[mesh_vertex_count];
		for (int i = 0; i < mesh_vertex_count; i++)
		{
			output_vertices2[i] = current_mesh_vertices[i].position;
			output_normals2[i] = current_mesh_vertices[i].normal;
			output_uvs2[i] = current_mesh_vertices[i].uv;
		}		
		
		int[] mesh_triangles2 = current_mesh_tris.ToArray();
		mesh1.vertices = output_vertices2;
		mesh1.triangles = mesh_triangles2;
		mesh1.subMeshCount = mesh_submeshes.Count;
		
		for (int i = 0; i < mesh_submeshes.Count; i++)
		{
			mesh1.SetTriangles(mesh_submeshes[i].triangles.ToArray(), i);
		}
		mesh1.normals = output_normals2;
		mesh1.uv = output_uvs2;		
		mesh1.RecalculateBounds();
		mesh1.RecalculateTangents();
		string object_name = "";
		if (from_node)
		{
			object_name = model_name+node_mesh_count.ToString();
			node_mesh_count++;
		}
		else
		{
			object_name = model_name;
		}
		mesh1.name = object_name+"_m";
		if (smoothing_angle > 0) AverageMeshNormals(mesh1, smoothing_angle);
		int material_count = mesh_submeshes.Count;
		GameObject model_object = new GameObject(object_name);
		model_object.transform.position = origin;
		model_object.AddComponent<MeshFilter>().mesh = mesh1;
		Material[] mesh_materials = new Material[material_count];
		string material_path;
		for (int i7 = 0; i7 < material_count; i7++)
		{
			material_path = MaterialsPath+"/"+BSPCommon.GetPathInFolder(tex_infos[mesh_submeshes[i7].texId].name, "textures")+".mat";
			Material current_material = null;
			for (int i8 = 0; i8 < map_materials.Count; i8++)
			{
				if (AssetDatabase.GetAssetPath(map_materials[i8]) == material_path)
				{
					current_material = map_materials[i8];
					break;
				}
			}
			if (current_material == null)
			{
				if (File.Exists(material_path))
				{
					current_material = (Material)AssetDatabase.LoadAssetAtPath(material_path, typeof(Material));
					map_materials.Add(current_material);
				}
				else
				{
					current_material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
					Debug.LogWarning("Map material "+material_path+" not found.");					
				}
			}
			mesh_materials[i7] = current_material;
		}
		model_object.AddComponent<MeshRenderer>().materials = mesh_materials;
		if (collider_type == 1)
		{
			model_object.AddComponent<MeshCollider>().sharedMesh = CollisionMesh(mesh1);
		}
		else if (collider_type == 2)
		{
			model_object.AddComponent<BoxCollider>();
		}
		else if (collider_type == 3)
		{
			model_object.AddComponent<SphereCollider>();
		}
		Unwrapping.GenerateSecondaryUVSet(mesh1, uw1);
		model_object.isStatic = true;
	}
	
	static float GetTriangleArea (Vector3 v1, Vector3 v2, Vector3 v3)
	{
		float a = Vector3.Distance(v1, v2);
		float b = Vector3.Distance(v2, v3);
		float c = Vector3.Distance(v3, v1);
		float s = (a + b + c) * 0.5f;
		return (float)System.Math.Sqrt(s * (s - a) * (s - b) * (s - c));
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

	static void AverageMeshNormals (Mesh mesh1, float max_angle = 45.0f)
	{
		Vector3[] mesh_vertices = mesh1.vertices;
		Vector3[] mesh_normals = mesh1.normals;
		Vector3[] new_normals = new Vector3[mesh_vertices.Length];
		Vector3 current_normal = Vector3.zero;
		int group_count = 0;
		for (int i = 0; i < mesh_vertices.Length; i++)
		{
			current_normal = Vector3.zero;
			group_count = 0;
			current_normal += mesh_normals[i];
			group_count++;
			for (int i2 = 0; i2 < mesh_vertices.Length; i2++)
			{
				if (Vector3.Distance(mesh_vertices[i], mesh_vertices[i2]) < 0.01f && i != i2)
				{
					mesh_vertices[i2] = mesh_vertices[i];
					if (Vector3.Angle(mesh_normals[i], mesh_normals[i2]) < max_angle)
					{
						current_normal += mesh_normals[i2];
						group_count++;
						
					}
				}
			}
			if (group_count < 2)
			{
				new_normals[i] = mesh_normals[i];
			}
			else
			{
				current_normal /= (float)group_count;
				new_normals[i] = current_normal.normalized;
			}
		}
		mesh1.vertices = mesh_vertices;
		mesh1.normals = new_normals;
		mesh1.RecalculateBounds();
		mesh1.RecalculateTangents();
	}	

	static int SortBSPModels (BrushModelEntity brush_model1, BrushModelEntity brush_model2)
	{
		if (brush_model1.name == brush_model2.name)
		{			
			if (!brush_model1.isInstance && brush_model2.isInstance)
			{
				return -1;
			}
			if (brush_model1.isInstance && !brush_model2.isInstance)
			{
				return 1;
			}
			return 0;
		}
		return -1;
	}
	
	static bool IsBrushEntity (string entity)
	{
		if (entity == "brush_model") return true;
		if (entity == "brush_door_r") return true;
		if (entity == "brush_door_s") return true;
		if (entity == "brush_rotating") return true;
		if (entity == "brush_rigidbody") return true;
		if (entity == "brush_elevator") return true;
		return false;
	}
	
	static Vector3 Q3ToUnity (Vector3 input)
	{
		return new Vector3(-input.x, input.z, -input.y) * ImportScale;
	}
	
	static Vector3 Q3ToUnityNoScale (Vector3 input)
	{
		return new Vector3(-input.x, input.z, -input.y);
	}
}
