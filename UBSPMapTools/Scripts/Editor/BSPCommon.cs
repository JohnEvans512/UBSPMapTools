using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BSPCommon : UnityEngine.Object
{
	// Settings
	const string settings_file = "Assets/UBSPMapTools/Scripts/Editor/ubsp_settings.txt";
	public static string Q3ProjectPath = "";
	public static string MaterialsPath = "Assets/Materials";
	public static string ModelsPath = "Assets/Models";
	public static string SoundPath = "Assets/Sounds";
	public static string ModelPrefabsPath = "Assets/ModelPrefabs";
	public static float MaxMeshSurfaceArea = 3000.0f;
	public static float UV2Padding = 0.02f;
	public static bool hasSettings = false;

	public static bool StringStartsWith (string a, string b)
	{
		int aLen = a.Length;
		int bLen = b.Length;
		int ap = 0;
		int bp = 0;
		while (ap < aLen && bp < bLen && a [ap] == b [bp])
		{
			ap++;
			bp++;
		}
		return (bp == bLen && aLen >= bLen) || (ap == aLen && bLen >= aLen);
	}

	public static bool StringEndsWith (string a, string b)
	{
		int ap = a.Length - 1;
		int bp = b.Length - 1;
		while (ap >= 0 && bp >= 0 && a [ap] == b [bp])
		{
			ap--;
			bp--;
		}
		return (bp < 0 && a.Length >= b.Length) || (ap < 0 && b.Length >= a.Length);
	}

	public static string RemoveExtension (string path)
	{
		int p = path.Length - 1;
		while (p > 0)
		{
			if (path[p] == '.') break;
			p--;
		}
		if (p == 0) return path;
		return path.Substring(0, p);
	}

	public static string GetParentFolder (string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		int p = path.Length - 1;
		if (StringEndsWith(path, "/") || StringEndsWith(path, "\\"))
		{
			p--;
		}
		while (p > 0)
		{
			if (path[p] == '/' || path[p] == '\\')
			{
				break;
			}
			p--;
		}
		if (p == 0)
		{
			return null;
		}
		return path.Substring(0, p);
	}

	public static string GetParentFolder (string path, string folder)
	{
		if (StringEndsWith(path, folder))
			return path.Substring(0, path.IndexOf("/"+folder));
		if (StringStartsWith(path, folder))
			return null;
		return path.Substring(0, path.IndexOf("/"+folder+"/"));
	}

	public static void CreateFolders (string folders)
	{
		if (!StringStartsWith(folders, "Assets/"))
		{
			Debug.LogError("CreateFolders only works with Assets");
			return;
		}
		char[] path_split = new char[] {'/', '\\'};
		string[] path_folders = folders.Split(path_split);
		string path = path_folders[0];
		for (int i = 1; i < path_folders.Length; i++)
		{
			if (!Directory.Exists(path+"/"+path_folders[i]))
			{
				AssetDatabase.CreateFolder(path, path_folders[i]);
			}
			path = path+"/"+path_folders[i];
		}
		path_split = null;
		path_folders = null;
		path = null;
	}

	public static void CreateHierarchy (string folders)
	{
		char[] path_split = new char[] {'/', '\\'};
		string[] path_folders = folders.Split(path_split);
		string path = path_folders[0];
		for (int i = 1; i < path_folders.Length; i++)
		{
			if (!Directory.Exists(path+"/"+path_folders[i]))
			{
				Directory.CreateDirectory(path+"/"+path_folders[i]);
			}
			path = path+"/"+path_folders[i];
		}
		path_split = null;
		path_folders = null;
		path = null;
	}

	public static string GetPathInFolder (string path, string folder)
	{
		int i = path.IndexOf(folder+"/");
		int l = folder.Length;
		return path.Substring(i + l + 1, path.Length - i - l - 1);
	}

	public static void SaveSettings ()
	{
		string[] settings = new string[7];
		settings[0] = Q3ProjectPath;
		settings[1] = MaterialsPath;
		settings[2] = ModelsPath;
		settings[3] = SoundPath;
		settings[4] = ModelPrefabsPath;
		settings[5] = MaxMeshSurfaceArea.ToString();
		settings[6] = UV2Padding.ToString();
		File.WriteAllLines(settings_file, settings, System.Text.Encoding.UTF8);
		settings = null;
	}
	
	public static bool LoadSettings ()
	{
		if (hasSettings) return true;
		if (!File.Exists(settings_file)) return false;
		string[] settings = File.ReadAllLines(settings_file);
		Q3ProjectPath = settings[0];
		MaterialsPath = settings[1];
		ModelsPath = settings[2];
		SoundPath = settings[3];
		ModelPrefabsPath = settings[4];
		MaxMeshSurfaceArea = float.Parse(settings[5]);
		UV2Padding = float.Parse(settings[6]);
		hasSettings = true;
		settings = null;
		return true;
	}

	public static string ConvertPath (string input_path)
	{
		if (string.IsNullOrEmpty(input_path))
		{
			return "Assets";
		}
		else
		{
			if (input_path.Contains("Assets"))
			{
				return input_path.Substring(input_path.IndexOf("Assets"));
			}
			else
			{
				return input_path;
			}
		}
	}
}