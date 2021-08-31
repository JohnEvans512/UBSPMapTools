using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class UBSPOBJExport : UnityEngine.Object
{
	private static Material[] unique_mtls;
	private struct TriArray
	{
		public int[] triangles;
	}
	
	[MenuItem("Tools/BSP/Export OBJ")]
	static void ExportObjWS ()
	{
		Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);
		if (selection.Length < 1)
		{
			return;
		}
		else if (selection.Length > 1)
		{
			ExportMultipleWS();
		}
		else
		{
			ExportSingleWS();
		}
		selection = null;
	}

	static void ExportSingleWS ()
	{
		Transform obj_transform = Selection.activeTransform;
		MeshFilter mf1 = obj_transform.GetComponentInChildren<MeshFilter>();
		if (mf1 == null)
		{
			return;
		}
		MeshRenderer mr1 = obj_transform.GetComponentInChildren<MeshRenderer>();
		if (mr1 == null)
		{
			return;
		}
		Mesh mesh1 = mf1.sharedMesh;
		Vector3[] mesh_vertices = mesh1.vertices;
		Vector3[] mesh_normals = mesh1.normals;
		Vector2[] mesh_uvs = mesh1.uv;
		int[] mesh_triangles = mesh1.triangles;
		int triangle_count = mesh_triangles.Length / 3;
		int submesh_count = mesh1.subMeshCount;
		Material[] mesh_materials = mr1.sharedMaterials;
		if (mesh_materials.Length != submesh_count)
		{
			Debug.LogWarning("Material count doesn't match submesh count");
		}
		int line_offset = 0;
		string[] file_lines = new string[mesh_vertices.Length + mesh_normals.Length + mesh_uvs.Length + triangle_count + 2 + submesh_count];
		string mesh_name = mesh1.name;
		string output_path = EditorUtility.SaveFilePanel("Save obj file", "", mesh_name, "obj");
		if (string.IsNullOrEmpty(output_path))
		{
			return;
		}
		
		file_lines[0] = string.Concat("o", " ", mesh_name);
		line_offset++;
		for (int i = 0; i < mesh_vertices.Length; i++)
		{
			file_lines[i + line_offset] = string.Concat("v", " ", (-(obj_transform.TransformPoint(mesh_vertices[i]).x - obj_transform.position.x) * 1.0f).ToString(), " ", ((obj_transform.TransformPoint(mesh_vertices[i]).y - obj_transform.position.y) * 1.0f).ToString(), " ", ((obj_transform.TransformPoint(mesh_vertices[i]).z - obj_transform.position.z) * 1.0f).ToString());
		}
		line_offset += mesh_vertices.Length;
		for (int i = 0; i < mesh_uvs.Length; i++)
		{
			file_lines[i + line_offset] = string.Concat("vt", " ", (mesh_uvs[i].x * 1.0f).ToString(), " ", (mesh_uvs[i].y * 1.0f).ToString());
		}
		line_offset += mesh_uvs.Length;
		// for (int i = 0; i < mesh_normals.Length; i++)
		// {
			// file_lines[i + line_offset] = string.Concat("vn", " ", (-mesh_normals[i].x * 1.0f).ToString(), " ", (mesh_normals[i].y * 1.0f).ToString(), " ", (mesh_normals[i].z * 1.0f).ToString());
		// }
		// line_offset += mesh_normals.Length; // Don't export normals
		file_lines[line_offset] = "s off";
		line_offset++;
		for (int i = 0; i < submesh_count; i++)
		{
			file_lines[line_offset] = string.Concat("usemtl", " ", mesh_materials[i].name);
			line_offset++;
			int[] submesh_triangles = mesh1.GetTriangles(i);
			for (int i2 = 0; i2 < submesh_triangles.Length; i2 += 3)
			{
				file_lines[line_offset] = string.Concat("f", " ", (submesh_triangles[i2 + 2] + 1).ToString(), "/", (submesh_triangles[i2 + 2] + 1).ToString(), " ", (submesh_triangles[i2 + 1] + 1).ToString(), "/", (submesh_triangles[i2 + 1] + 1).ToString(), " ", (submesh_triangles[i2] + 1).ToString(), "/", (submesh_triangles[i2] + 1).ToString());
				line_offset++;
			}
		}
		File.WriteAllLines(output_path, file_lines, System.Text.Encoding.UTF8);		
		Debug.Log("Export OBJ single.");
	}

	static void ExportMultipleWS ()
	{
		Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);
		GameObject[] model_objects = new GameObject[selection.Length];
		MeshFilter[] meshFilters = new MeshFilter[selection.Length];
		MeshRenderer[] meshRenderers = new MeshRenderer[selection.Length];
		for (int i = 0; i < selection.Length; i++)
		{
			model_objects[i] = selection[i] as GameObject;
			meshFilters[i] = model_objects[i].GetComponentInChildren<MeshFilter>();
			meshRenderers[i] = model_objects[i].GetComponentInChildren<MeshRenderer>();
		}
		int new_mesh_vtx_count = 0;
		int new_mesh_tri_count = 0;
		int vtx_offset = 0;
		int tri_offset = 0;
		int all_mtls_count = 0;
		int unique_mtls_count = 0;
		int current_sm_count = 0;
		
		for (int i = 0; i < selection.Length; i++)
		{
			Material[] current_mesh_mtls = meshRenderers[i].sharedMaterials;
			current_sm_count = meshFilters[i].sharedMesh.subMeshCount;
			if (current_sm_count != current_mesh_mtls.Length)
			{
				Debug.LogError(model_objects[i] + " Submesh count doesn't match material count");
				return;
			}
			all_mtls_count += current_sm_count;
		}
		
		Vector3 midpoint = Vector3.zero;
		GameObject OriginObject = GameObject.Find("Origin");
		if (OriginObject != null)
		{
			midpoint = OriginObject.transform.position;
		}
		else
		{
			for (int i = 0; i < selection.Length; i++)
			{
				midpoint += model_objects[i].transform.position;
			}
			midpoint /= selection.Length;
			midpoint.x = SnapFloat(midpoint.x, 0.5f);
			midpoint.y = SnapFloat(midpoint.y, 0.5f);
			midpoint.z = SnapFloat(midpoint.z, 0.5f);
		}

		Material[] all_materials = new Material[all_mtls_count];
		all_mtls_count = 0;
		for (int i = 0; i < selection.Length; i++)
		{
			Material[] current_mesh_mtls = meshRenderers[i].sharedMaterials;
			current_sm_count = meshFilters[i].sharedMesh.subMeshCount;
			for (int i8 = 0; i8 < current_sm_count; i8++)
			{
				all_materials[i8 + all_mtls_count] = current_mesh_mtls[i8];
			}
			all_mtls_count += current_sm_count;
		}
		unique_mtls = all_materials.Distinct().ToArray();
		unique_mtls_count = unique_mtls.Length;

	for (int i = 0; i < selection.Length; i++)
		{
			new_mesh_vtx_count += meshFilters[i].sharedMesh.vertexCount;
			new_mesh_tri_count += meshFilters[i].sharedMesh.triangles.Length;
		}
		current_sm_count = 0;
		Vector3[] new_mesh_vertices = new Vector3[new_mesh_vtx_count];
		Vector3[] new_mesh_normals = new Vector3[new_mesh_vtx_count];
		Vector2[] new_mesh_uvs = new Vector2[new_mesh_vtx_count];
		int[] new_mesh_triangles = new int[new_mesh_tri_count];
		TriArray[] out_triangle_arrays = new TriArray[unique_mtls.Length];
		int[] new_mesh_mtls = new int[new_mesh_tri_count / 3];
		for (int i = 0; i < selection.Length; i++)
		{
			int current_vtx_count = meshFilters[i].sharedMesh.vertexCount;
			Vector3[] current_mesh_vertices = meshFilters[i].sharedMesh.vertices;
			Vector3[] current_mesh_normals = meshFilters[i].sharedMesh.normals;
			Vector2[] current_mesh_uvs = meshFilters[i].sharedMesh.uv;
			int[] current_mesh_triangles = meshFilters[i].sharedMesh.triangles;
			current_sm_count = meshFilters[i].sharedMesh.subMeshCount;
			TriArray[] current_tri_arrays = new TriArray[current_sm_count];
			int current_tri_count = current_mesh_triangles.Length / 3;
			for (int i20 = 0; i20 < current_sm_count; i20++)
			{
				current_tri_arrays[i20].triangles = meshFilters[i].sharedMesh.GetTriangles(i20);
			}
			for (int i2 = 0; i2 < current_vtx_count; i2++)
			{
				new_mesh_vertices[vtx_offset + i2] = model_objects[i].transform.TransformPoint(current_mesh_vertices[i2]) - midpoint;
				new_mesh_normals[vtx_offset + i2] = model_objects[0].transform.InverseTransformDirection(model_objects[i].transform.TransformDirection(current_mesh_normals[i2]));
				new_mesh_uvs[vtx_offset + i2] = current_mesh_uvs[i2];
			}
			for (int i3 = 0; i3 < current_tri_count; i3++)
			{
				new_mesh_triangles[tri_offset + i3 * 3] = current_mesh_triangles[i3 * 3] + vtx_offset;
				new_mesh_triangles[tri_offset + (i3 * 3 + 1)] = current_mesh_triangles[i3 * 3 + 1] + vtx_offset;
				new_mesh_triangles[tri_offset + (i3 * 3 + 2)] = current_mesh_triangles[i3 * 3 + 2] + vtx_offset;
				new_mesh_mtls[(tri_offset / 3) + i3] = GetTriangleMaterial(meshFilters[i], meshRenderers[i], current_mesh_triangles[i3 * 3], current_mesh_triangles[i3 * 3 + 1], current_mesh_triangles[i3 * 3 + 2]);
			}
			vtx_offset += current_vtx_count;
			tri_offset += current_tri_count * 3;
		}
		int[] submesh_tri_counts = new int[unique_mtls_count];
		for (int i23 = 0; i23 < submesh_tri_counts.Length; i23++)
		{
			submesh_tri_counts[i23] = 0;
		}
		for (int i21 = 0; i21 < new_mesh_mtls.Length; i21++)
		{
			for (int i22 = 0; i22 < unique_mtls_count; i22++)
			{
				if (new_mesh_mtls[i21] == i22)
				{
					submesh_tri_counts[i22]++;
				}
			}
		}
		for (int i24 = 0; i24 < submesh_tri_counts.Length; i24++)
		{
			out_triangle_arrays[i24].triangles = new int[submesh_tri_counts[i24] * 3];
		}
		for (int i23 = 0; i23 < submesh_tri_counts.Length; i23++)
		{
			submesh_tri_counts[i23] = 0;
		}
		for (int i25 = 0; i25 < new_mesh_mtls.Length; i25++)
		{
			for (int i22 = 0; i22 < unique_mtls_count; i22++)
			{
				if (new_mesh_mtls[i25] == i22)
				{
					out_triangle_arrays[i22].triangles[submesh_tri_counts[i22]] = new_mesh_triangles[i25 * 3];
					out_triangle_arrays[i22].triangles[submesh_tri_counts[i22] + 1] = new_mesh_triangles[i25 * 3 + 1];
					out_triangle_arrays[i22].triangles[submesh_tri_counts[i22] + 2] = new_mesh_triangles[i25 * 3 + 2];
					submesh_tri_counts[i22] += 3;
				}
			}
		}
		Mesh mesh2 = new Mesh();
		mesh2.vertices = new_mesh_vertices;
		mesh2.triangles = new_mesh_triangles;
		mesh2.subMeshCount = unique_mtls_count;
		for (int i12 = 0; i12 < unique_mtls_count; i12++)
		{
			mesh2.SetTriangles(out_triangle_arrays[i12].triangles, i12);
		}		
		mesh2.normals = new_mesh_normals;
		mesh2.uv = new_mesh_uvs;
		mesh2.RecalculateTangents();
		mesh2.RecalculateBounds();
		mesh2.name = "new_mesh";
		
		Vector3[] mesh_vertices = mesh2.vertices;
		Vector3[] mesh_normals = mesh2.normals;
		Vector2[] mesh_uvs = mesh2.uv;
		int[] mesh_triangles = mesh2.triangles;
		int triangle_count = mesh_triangles.Length / 3;
		int submesh_count = mesh2.subMeshCount;
		int line_offset = 0;
		string[] file_lines = new string[mesh_vertices.Length + mesh_normals.Length + mesh_uvs.Length + triangle_count + 2 + submesh_count];
		string mesh_name = mesh2.name;
		string output_path = EditorUtility.SaveFilePanel("Save obj file", "", mesh_name, "obj");
		if (string.IsNullOrEmpty(output_path))
		{
			return;
		}
		
		file_lines[0] = string.Concat("o", " ", mesh_name);
		line_offset++;
		for (int i = 0; i < mesh_vertices.Length; i++)
		{
			file_lines[i + line_offset] = string.Concat("v", " ", (-mesh_vertices[i].x * 1.0f).ToString(), " ", (mesh_vertices[i].y * 1.0f).ToString(), " ", (mesh_vertices[i].z * 1.0f).ToString());
		}
		line_offset += mesh_vertices.Length;
		for (int i = 0; i < mesh_uvs.Length; i++)
		{
			file_lines[i + line_offset] = string.Concat("vt", " ", (mesh_uvs[i].x * 1.0f).ToString(), " ", (mesh_uvs[i].y * 1.0f).ToString());
		}
		line_offset += mesh_uvs.Length;
		// for (int i = 0; i < mesh_normals.Length; i++)
		// {
			// file_lines[i + line_offset] = string.Concat("vn", " ", (-mesh_normals[i].x * 1.0f).ToString(), " ", (mesh_normals[i].y * 1.0f).ToString(), " ", (mesh_normals[i].z * 1.0f).ToString());
		// }
		// line_offset += mesh_normals.Length; // Don't export normals
		file_lines[line_offset] = "s off";
		line_offset++;
		for (int i = 0; i < submesh_count; i++)
		{
			file_lines[line_offset] = string.Concat("usemtl", " ", unique_mtls[i].name);
			line_offset++;
			int[] submesh_triangles = mesh2.GetTriangles(i);
			for (int i2 = 0; i2 < submesh_triangles.Length; i2 += 3)
			{
				file_lines[line_offset] = string.Concat("f", " ", (submesh_triangles[i2 + 2] + 1).ToString(), "/", (submesh_triangles[i2 + 2] + 1).ToString(), " ", (submesh_triangles[i2 + 1] + 1).ToString(), "/", (submesh_triangles[i2 + 1] + 1).ToString(), " ", (submesh_triangles[i2] + 1).ToString(), "/", (submesh_triangles[i2] + 1).ToString());
				line_offset++;
			}
		}
		File.WriteAllLines(output_path, file_lines, System.Text.Encoding.UTF8);		
		unique_mtls = null;
		model_objects = null;
		meshFilters = null;
		meshRenderers = null;
		DestroyImmediate(mesh2);
		Debug.Log("Export OBJ multiple.");
	}
	
	static int GetTriangleMaterial (MeshFilter mf, MeshRenderer mr, int v1, int v2, int v3)
	{
		Material[] mesh_materials = mr.sharedMaterials;
		int sm_count = mf.sharedMesh.subMeshCount;
		TriArray[] sm_arrays = new TriArray[sm_count];
		for (int i = 0; i < sm_count; i++)
		{
			sm_arrays[i].triangles = mf.sharedMesh.GetTriangles(i);
		}
		for (int i2 = 0; i2 < sm_count; i2++)
		{
			for (int i3 = 0; i3 < sm_arrays[i2].triangles.Length / 3; i3++)
			{
				if (v1 == sm_arrays[i2].triangles[i3 * 3] && v2 == sm_arrays[i2].triangles[i3 * 3 + 1] && v3 == sm_arrays[i2].triangles[i3 * 3 + 2])
				{
					return System.Array.IndexOf(unique_mtls, mesh_materials[i2]);
				}
			}
		}
		return 0;
	}

	static float SnapFloat (float v1, float snap)
	{
		float m1 = v1 / snap;
		if (v1 > 0.0f)
		{
			if (v1 > snap * ((float)((int)m1)) + (snap * 0.5f))
			{
				return snap * (float)((int)m1) + snap;
			}
			else
			{
				return snap * (float)((int)m1);
			}
		}
		else
		{
			if (v1 < snap * ((float)((int)m1)) - (snap * 0.5f))
			{
				return snap * (float)((int)m1) - snap;
			}
			else
			{
				return snap * (float)((int)m1);
			}			
		}
	}
}