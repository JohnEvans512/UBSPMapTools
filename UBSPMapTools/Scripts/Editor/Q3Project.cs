/*
Copyright (c) 2021 John Evans

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class Q3Project : EditorWindow
{
	private static Q3Project window;
	
	private static string MaterialsPath;
	private static string ModelsPath;
	private static string SoundPath;
	private static string Q3ProjectPath;
	
	private static bool updateMaterials = true;
	private static bool updateModels = true;
	private static bool updateSounds = true;
	
	public struct MD3Surface
	{
		public Vector3[] vertices;
		public int[] triangles;
		public Vector2[] uvs;
		public Vector3[] normals;
		public string texture;
		public int size;
		public string name;
	}
	
	public struct SurfaceVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
		public int baseIndex;
		public bool Matches (SurfaceVertex vtx)
		{
			return (position == vtx.position && normal == vtx.normal && uv == vtx.uv && baseIndex == vtx.baseIndex);
		}
	}

	const float ImportScale = 0.025f;
	const int MAX_SURF_INDICES = 12000;
	
	[MenuItem("Tools/BSP/Q3Project")]
	public static void Init()
	{
		window = Q3Project.CreateInstance<Q3Project>();
		window.titleContent = new GUIContent("Q3 Project");
		BSPCommon.LoadSettings();
		Q3ProjectPath = BSPCommon.Q3ProjectPath;
		MaterialsPath = BSPCommon.MaterialsPath;
		ModelsPath = BSPCommon.ModelsPath;
		SoundPath = BSPCommon.SoundPath;
		window.ShowUtility();
	}
	
	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(20));
		EditorGUILayout.LabelField("Q3 Project Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			Q3ProjectPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Materials Path", "Assets", ""));
		}
		EditorGUILayout.Space();
		if (GUILayout.Button("Save Defaults", GUILayout.Width(100)))
		{
			BSPCommon.Q3ProjectPath = Q3ProjectPath;
			BSPCommon.MaterialsPath = MaterialsPath;
			BSPCommon.ModelsPath = ModelsPath;
			BSPCommon.SoundPath = SoundPath;
			BSPCommon.SaveSettings();
			Debug.Log("Saved Settings: bsp_settings.txt");
		}
		EditorGUILayout.EndHorizontal();
		Q3ProjectPath = EditorGUILayout.TextField(Q3ProjectPath);

		EditorGUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(20));
		EditorGUILayout.LabelField("Materials Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			MaterialsPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Materials Path", "Assets", ""));
		}
		updateMaterials = EditorGUILayout.Toggle("Update Materials:", updateMaterials);
		EditorGUILayout.EndHorizontal();
		MaterialsPath = EditorGUILayout.TextField(MaterialsPath);
		
		EditorGUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(20));
		EditorGUILayout.LabelField("Models Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			ModelsPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Models Path", "Assets", ""));
		}
		updateModels = EditorGUILayout.Toggle("Update Models:", updateModels);
		EditorGUILayout.EndHorizontal();
		ModelsPath = EditorGUILayout.TextField(ModelsPath);

		EditorGUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(20));
		EditorGUILayout.LabelField("Sound Path:", GUILayout.Width(100));
		if (GUILayout.Button("Browse", GUILayout.Width(57)))
		{
			SoundPath = BSPCommon.ConvertPath(EditorUtility.OpenFolderPanel("Sound Path", "Assets", ""));
		}
		updateSounds = EditorGUILayout.Toggle("Update Sounds:", updateSounds);
		EditorGUILayout.EndHorizontal();		
		SoundPath = EditorGUILayout.TextField(SoundPath);
		
		EditorGUILayout.BeginHorizontal(GUILayout.Width(500), GUILayout.Height(27));
		if (GUILayout.Button("Update Q3 Project", GUILayout.Width(330), GUILayout.Height(25)))
		{
			UpdateProject();
		}
		EditorGUILayout.EndHorizontal();		
	}
	
	static void UpdateProject ()
	{
		if (updateMaterials) ProcessMaterials(MaterialsPath);
		if (updateModels) ProcessModels(ModelsPath);
		if (updateSounds) ProcessSounds(SoundPath);
		window.ShowNotification(new GUIContent("Finished updating"));
		UnityEngine.Debug.Log("Q3Project: Finished updating.");
	}
	
	static void ProcessMaterials (string folder_path)
	{
		string[] files = Directory.GetFiles(folder_path);
		for (int i = 0; i < files.Length; i++)
		{
			if (BSPCommon.StringEndsWith(files[i], ".mat"))
			{
				string material_path = files[i].Replace("\\", "/");
				string texture_path = Q3ProjectPath+"/baseq3/textures/"+material_path.Substring(MaterialsPath.Length + 1, material_path.Length - MaterialsPath.Length - 5);
				BSPCommon.CreateHierarchy(Path.GetDirectoryName(texture_path));
				if (!File.Exists(texture_path+".jpg") && !File.Exists(texture_path+".tga"))
				{
					SaveTexture(material_path, texture_path);
				}
			}
		}
		string[] folders = Directory.GetDirectories(folder_path);
		for (int i3 = 0; i3 < folders.Length; i3++)
		{
			ProcessMaterials(folders[i3]);
		}
	}
	
	static void ProcessModels (string folder_path)
	{
		string[] files = Directory.GetFiles(folder_path);
		for (int i = 0; i < files.Length; i++)
		{
			if (BSPCommon.StringEndsWith(files[i], ".fbx") || BSPCommon.StringEndsWith(files[i], ".prefab"))
			{
				string model_path = files[i].Replace("\\", "/");
				string md3_path = Q3ProjectPath+"/baseq3/models/"+model_path.Substring(ModelsPath.Length + 1, BSPCommon.RemoveExtension(model_path).Length - ModelsPath.Length - 1)+".md3";
				BSPCommon.CreateHierarchy(Path.GetDirectoryName(md3_path));
				if (!File.Exists(md3_path))
				{
					SaveMD3(model_path, md3_path);
				}
			}
		}
		string[] folders = Directory.GetDirectories(folder_path);
		for (int i3 = 0; i3 < folders.Length; i3++)
		{
			ProcessModels(folders[i3]);
		}		
	}
	
	static void ProcessSounds (string folder_path)
	{
		string[] files = Directory.GetFiles(folder_path);
		for (int i = 0; i < files.Length; i++)
		{
			if (BSPCommon.StringEndsWith(files[i], ".wav"))
			{
				string input_path = files[i].Replace("\\", "/");
				string output_path = Q3ProjectPath+"/baseq3/sound/"+input_path.Substring(SoundPath.Length + 1, input_path.Length - SoundPath.Length - 1);
				BSPCommon.CreateHierarchy(Path.GetDirectoryName(output_path));
				if (!File.Exists(output_path))
				{
					FileUtil.CopyFileOrDirectory(input_path, output_path);
				}
			}
		}
		string[] folders = Directory.GetDirectories(folder_path);
		for (int i3 = 0; i3 < folders.Length; i3++)
		{
			ProcessSounds(folders[i3]);
		}		
	}

	public static void SaveTexture (string mtl_path, string tex_path)
	{
		Material input_material = (Material)AssetDatabase.LoadAssetAtPath(mtl_path, typeof(Material));
		Texture2D input_texture = (Texture2D)input_material.mainTexture;
		if (input_texture == null) return;
		UBSPTextureUtility.TextureCheckIn(input_texture);
		Texture2D output_texture = UBSPTextureUtility.ResizeTextureSharp(input_texture, input_texture.width / 2, input_texture.height / 2, 1.0f);
		if (input_material.HasProperty("_Mode"))
		{
			if (input_material.GetInt("_Mode") > 0)
			{
				UBSPTextureUtility.WriteTGA(tex_path+".tga", output_texture);
			}
			else
			{
				UBSPTextureUtility.WriteJPG(tex_path+".jpg", output_texture, 80);
			}
		}
		else
		{
			UBSPTextureUtility.WriteJPG(tex_path+".jpg", output_texture, 80);
		}
		DestroyImmediate(output_texture);
		UBSPTextureUtility.TextureCheckOut(input_texture);
	}
	
	public static void SaveMD3 (string prefab_path, string md3_path)
	{
		GameObject model_object = (GameObject)AssetDatabase.LoadAssetAtPath(prefab_path, typeof(GameObject));
		if (model_object == null) return;
		MeshFilter mf1 = model_object.GetComponentInChildren<MeshFilter>();
		Renderer r1 = model_object.GetComponentInChildren<Renderer>();
		if (mf1 == null || r1 == null) return;
		Mesh object_mesh = mf1.sharedMesh;
		if (object_mesh == null) return;
		Vector3 mesh_center = object_mesh.bounds.center;
		Vector3 origin_offset = new Vector3(Mathf.Floor(mesh_center.x / ImportScale) * ImportScale, Mathf.Floor(mesh_center.y / ImportScale) * ImportScale, Mathf.Floor(mesh_center.z / ImportScale) * ImportScale);
		Vector3[] mesh_vertices = object_mesh.vertices;
		int[] mesh_triangles = object_mesh.triangles;
		Vector2[] mesh_uvs = object_mesh.uv;
		Vector3[] mesh_normals = object_mesh.normals;
		int submeshCount = object_mesh.subMeshCount;
		int[][] submeshes = new int[submeshCount][];
		for (int i = 0; i < submeshCount; i++)
		{
			submeshes[i] = object_mesh.GetTriangles(i);
		}
		// Clamp vertices, MD3 uses Int16 vectors for vertex positions, so each vertex can be in range from -12.75 to 12.75
		for (int i = 0; i < mesh_vertices.Length; i++)
		{
			mesh_vertices[i] -= origin_offset;
			if (mesh_vertices[i].x > 12.75f) mesh_vertices[i].x = 12.75f;
			else if (mesh_vertices[i].x < -12.75f) mesh_vertices[i].x = -12.75f;
			if (mesh_vertices[i].y > 12.75f) mesh_vertices[i].y = 12.75f;
			else if (mesh_vertices[i].y < -12.75f) mesh_vertices[i].y = -12.75f;
			if (mesh_vertices[i].z > 12.75f) mesh_vertices[i].z = 12.75f;
			else if (mesh_vertices[i].z < -12.75f) mesh_vertices[i].z = -12.75f;
		}
		List<MD3Surface> md3_surfaces = new List<MD3Surface>();
		int all_surfaces_size = 0;
		for (int i = 0; i < submeshCount; i++)
		{
			if (submeshes[i].Length < MAX_SURF_INDICES) // One submesh, one surface
			{
				SurfaceVertex[] surface_vertices = new SurfaceVertex[MAX_SURF_INDICES];
				int surface_vtx_count = 0;
				SurfaceVertex current_vtx = new SurfaceVertex();
				int tri_index_count = submeshes[i].Length;
				MD3Surface current_surface = new MD3Surface();
				current_surface.triangles = new int[tri_index_count];
				for (int i2 = 0; i2 < tri_index_count; i2++)
				{
					current_vtx.position = mesh_vertices[submeshes[i][i2]];
					current_vtx.normal = mesh_normals[submeshes[i][i2]];
					current_vtx.uv = mesh_uvs[submeshes[i][i2]];
					current_vtx.baseIndex = submeshes[i][i2];
					bool match = false;
					for (int i3 = 0; i3 < surface_vtx_count; i3++)
					{
						if (current_vtx.Matches(surface_vertices[i3]))
						{
							current_surface.triangles[i2] = i3;
							match = true;
							break;
						}
					}
					if (!match)
					{
						surface_vertices[surface_vtx_count] = current_vtx;
						current_surface.triangles[i2] = surface_vtx_count;
						surface_vtx_count++;
					}
				}
				current_surface.vertices = new Vector3[surface_vtx_count];
				current_surface.normals = new Vector3[surface_vtx_count];
				current_surface.uvs = new Vector2[surface_vtx_count];
				current_surface.size = surface_vtx_count * 16 + current_surface.triangles.Length * 4 + 176; // Size of future Surface data block in MD3
				current_surface.texture = r1.sharedMaterials[i].name;
				current_surface.name = "surface"+md3_surfaces.Count.ToString();
				for (int i2 = 0; i2 < surface_vtx_count; i2++)
				{
					current_surface.vertices[i2] = surface_vertices[i2].position;
					current_surface.normals[i2] = surface_vertices[i2].normal;
					current_surface.uvs[i2] = surface_vertices[i2].uv;
				}
				md3_surfaces.Add(current_surface);
				all_surfaces_size += current_surface.size;
			}
			else // Split submesh
			{
				int submesh_surf_count = submeshes[i].Length / MAX_SURF_INDICES;
				if (submeshes[i].Length % MAX_SURF_INDICES > 0) submesh_surf_count++;
				int tri_index_i = 0;
				for (int i2 = 0; i2 < submesh_surf_count; i2++)
				{
					SurfaceVertex[] surface_vertices = new SurfaceVertex[MAX_SURF_INDICES];
					int surface_vtx_count = 0;
					SurfaceVertex current_vtx = new SurfaceVertex();
					int tri_index_count = submeshes[i].Length;
					MD3Surface current_surface = new MD3Surface();
					List<int> current_surf_tri_index_list = new List<int>();
					for (int i3 = 0; i3 < MAX_SURF_INDICES; i3++)
					{
						if (tri_index_i == tri_index_count) break;
						current_vtx.position = mesh_vertices[submeshes[i][tri_index_i]];
						current_vtx.normal = mesh_normals[submeshes[i][tri_index_i]];
						current_vtx.uv = mesh_uvs[submeshes[i][tri_index_i]];
						current_vtx.baseIndex = submeshes[i][tri_index_i];
						bool match = false;
						for (int i5 = 0; i5 < surface_vtx_count; i5++)
						{
							if (current_vtx.Matches(surface_vertices[i5]))
							{
								current_surf_tri_index_list.Add(i5);
								match = true;
								break;
							}
						}
						if (!match)
						{
							surface_vertices[surface_vtx_count] = current_vtx;
							current_surf_tri_index_list.Add(surface_vtx_count);
							surface_vtx_count++;
						}
						tri_index_i++;
					}
					current_surface.triangles = current_surf_tri_index_list.ToArray();
					current_surface.vertices = new Vector3[surface_vtx_count];
					current_surface.normals = new Vector3[surface_vtx_count];
					current_surface.uvs = new Vector2[surface_vtx_count];
					current_surface.size = surface_vtx_count * 16 + current_surface.triangles.Length * 4 + 176; // Size of future Surface data block in MD3
					current_surface.texture = r1.sharedMaterials[i].name;
					current_surface.name = "surface"+md3_surfaces.Count.ToString();
					for (int i3 = 0; i3 < surface_vtx_count; i3++)
					{
						current_surface.vertices[i3] = surface_vertices[i3].position;
						current_surface.normals[i3] = surface_vertices[i3].normal;
						current_surface.uvs[i3] = surface_vertices[i3].uv;
					}
					md3_surfaces.Add(current_surface);
					all_surfaces_size += current_surface.size;
				}
			}
		}
		int file_size = all_surfaces_size + 164;
		int surface_count = md3_surfaces.Count;
		string model_name = BSPCommon.GetPathInFolder(md3_path, "baseq3");
		string model_folder = Path.GetDirectoryName(model_name);		
		FileStream fs1 = new FileStream(md3_path, FileMode.Create, FileAccess.Write);
		BinaryWriter bw1 = new BinaryWriter(fs1, System.Text.Encoding.UTF8);
		bw1.Seek(0, SeekOrigin.Begin);
		char[] signature = new char[] {'I', 'D', 'P', '3'};
		bw1.Write(signature);
		int version = 15;
		bw1.Write(version);
		char[] model_name_full = new char[64];
		for (int i = 0; i < 64; i++)
		{
			if (i < model_name.Length)
			{
				model_name_full[i] = model_name[i];
			}
			else
			{
				model_name_full[i] = '\0';
			}
		}
		bw1.Write(model_name_full);
		int zeroInt = 0;
		int oneInt = 1;
		bw1.Write(zeroInt); // flags 0
		bw1.Write(oneInt); // frames
		bw1.Write(zeroInt); // tags 0
		bw1.Write(surface_count); // surfaces
		bw1.Write(zeroInt); // skins 0
		int frameOffset = 108;
		bw1.Write(frameOffset);
		int tagsOffset = 164;
		bw1.Write(tagsOffset);
		int surfaceOffset = 164;
		bw1.Write(surfaceOffset);
		bw1.Write(file_size); // file size bytes
		// Frame
		Vector3 bounds_min = UnityToQ3(object_mesh.bounds.min - origin_offset);
		bw1.Write(bounds_min.x);
		bw1.Write(bounds_min.y);
		bw1.Write(bounds_min.z);
		Vector3 bounds_max = UnityToQ3(object_mesh.bounds.max - origin_offset);
		bw1.Write(bounds_max.x);
		bw1.Write(bounds_max.y);
		bw1.Write(bounds_max.z);
		float zeroFloat = 0;
		bw1.Write(zeroFloat);
		bw1.Write(zeroFloat);
		bw1.Write(zeroFloat);
		bw1.Write(bounds_max.magnitude);
		char[] frame_name = new char[] {'f', 'r', 'a', 'm', 'e', '1', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0'};
		bw1.Write(frame_name);		
		// Surfaces
		for (int i7 = 0; i7 < surface_count; i7++)
		{
			bw1.Write(signature);
			string surface_name = md3_surfaces[i7].name;
			char[] surface_name_full = new char[64];
			for (int i = 0; i < 64; i++)
			{
				if (i < surface_name.Length)
				{
					surface_name_full[i] = surface_name[i];
				}
				else
				{
					surface_name_full[i] = '\0';
				}
			}
			bw1.Write(surface_name_full);
			bw1.Write(zeroInt); // flags 0
			bw1.Write(oneInt); // frames
			bw1.Write(oneInt); // shaders
			bw1.Write(md3_surfaces[i7].vertices.Length); // vertices
			int triangle_count = md3_surfaces[i7].triangles.Length / 3;
			bw1.Write(triangle_count);
			int triangle_offset = 176;
			bw1.Write(triangle_offset);
			int shader_offset = 108;
			bw1.Write(shader_offset);
			int uvs_offset = triangle_offset + triangle_count * 12;
			bw1.Write(uvs_offset);
			int vtx_offset = uvs_offset + md3_surfaces[i7].vertices.Length * 8;
			bw1.Write(vtx_offset);
			bw1.Write(md3_surfaces[i7].size);
			// Shader
			string texture_name = model_folder+"/"+md3_surfaces[i7].texture+".tga";
			char[] texture_name_full = new char[64];
			for (int i = 0; i < 64; i++)
			{
				if (i < texture_name.Length)
				{
					texture_name_full[i] = texture_name[i];
				}
				else
				{
					texture_name_full[i] = '\0';
				}
			}
			bw1.Write(texture_name_full);
			bw1.Write(i7); // Shader index
			for (int i8 = 0; i8 < md3_surfaces[i7].triangles.Length; i8++)
			{
				bw1.Write(md3_surfaces[i7].triangles[i8]);
			}
			for (int i8 = 0; i8 < md3_surfaces[i7].uvs.Length; i8++)
			{
				bw1.Write(md3_surfaces[i7].uvs[i8].x);
				bw1.Write(1.0f - md3_surfaces[i7].uvs[i8].y);
			}
			for (int i8 = 0; i8 < md3_surfaces[i7].vertices.Length; i8++)
			{
				Vector3 md3_vector = UnityToQ3(md3_surfaces[i7].vertices[i8]);
				short v = (short)(md3_vector.x * 64.0f);
				bw1.Write(v);
				v = (short)(md3_vector.y * 64.0f);
				bw1.Write(v);
				v = (short)(md3_vector.z * 64.0f);
				bw1.Write(v);
				byte[] nrm = NormalToLatLong(UnityToQ3NoScale(md3_surfaces[i7].normals[i8]));
				bw1.Write(nrm);
			}
		}
		bw1.Close();
		fs1.Close();
		for (int i10 = 0; i10 < r1.sharedMaterials.Length; i10++)
		{
			SaveModelTexture(r1.sharedMaterials[i10], Q3ProjectPath+"/baseq3/"+model_folder+"/"+r1.sharedMaterials[i10].name+".tga");
		}
	}
	
	public static void SaveModelTexture (Material input_mtl, string output_path)
	{
		Texture2D input_texture = (Texture2D)input_mtl.mainTexture;
		if (input_texture == null) return;
		UBSPTextureUtility.TextureCheckIn(input_texture);
		int tx_max_size = Mathf.Max(input_texture.width, input_texture.height) / 4;
		if (tx_max_size > 512) tx_max_size = 512;
		if (tx_max_size < 128) tx_max_size = 128;
		Texture2D output_texture = UBSPTextureUtility.ClampTextureSize(input_texture, tx_max_size);
		UBSPTextureUtility.WriteTGA(output_path, output_texture);
		DestroyImmediate(output_texture);
		UBSPTextureUtility.TextureCheckOut(input_texture);
	}
	
	static byte[] NormalToLatLong (Vector3 normal)
	{
		byte[] bytes = new byte[2];
		if (normal.x == 0 && normal.y == 0)
		{
			if ( normal.z > 0 ) 
			{
				bytes[0] = 0;
				bytes[1] = 0;
			} 
			else 
			{
				bytes[0] = 128;
				bytes[1] = 0;
			}
		}
		else 
		{
			byte a = (byte)(Mathf.Atan2(normal.y, normal.x) * (255.0f / 360.0f) * Mathf.Rad2Deg);
			a &= 0xff;
			byte b = (byte)(Mathf.Acos(normal.z) * (255.0f / 360.0f) * Mathf.Rad2Deg);
			b &= 0xff;
			bytes[0] = b;
			bytes[1] = a;
		}
		return bytes;
	}
	
	static Vector3 UnityToQ3 (Vector3 input)
	{
		return new Vector3(-input.x, -input.z, input.y) * (1.0f / ImportScale);
	}
	
	static Vector3 UnityToQ3NoScale (Vector3 input)
	{
		return new Vector3(-input.x, -input.z, input.y);
	}
	
	static Vector3 Q3ToUnity (Vector3 input)
	{
		return new Vector3(-input.x, input.z, -input.y) * ImportScale;
	}
}
