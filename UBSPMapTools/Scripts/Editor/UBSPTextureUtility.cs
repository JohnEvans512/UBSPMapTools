// TextureCheckIn
// TextureCheckOut
// GetTexturesFolder
// WritePNG
// WriteJPG
// WriteTGA
// WriteBMP
// LoadTexture
// LoadTGA
// LoadBMP
// ClampTextureSize
// ResizeTextureNPOT
// IsPowerOfTwo
// GetLowerPowerOfTwo
// SharpenTexture
// ResizeTexture
// ResizeTextureSharp
// ResizeTextureSharpNM
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class UBSPTextureUtility : UnityEngine.Object
{
	private const string Assets = "Assets";
	private const string Materials = "Materials";
	private const string materials = "materials";
	private const string Textures = "Textures";
	private const string textures = "textures";
	
	private const string pathEmpty = "Path is empty";
	private const string refIsNull = "Reference is NULL";
	
	private const float sharpen = 0.95f;
	private const float sharpen_normal = 0.75f;

	public static void TextureCheckIn (Texture2D input_texture)
	{
		if (input_texture == null)
		{
			return;
		}
		if (!AssetDatabase.Contains(input_texture))
		{
			return;
		}
		string asset_path = AssetDatabase.GetAssetPath(input_texture);
		TextureImporter tx_importer1 = AssetImporter.GetAtPath(asset_path) as TextureImporter;
		if (tx_importer1.textureCompression == TextureImporterCompression.Uncompressed && tx_importer1.isReadable)
		{
			return;
		}
		tx_importer1.isReadable = true;
		tx_importer1.textureCompression = TextureImporterCompression.Uncompressed;
		AssetDatabase.ImportAsset(asset_path);
	}

	public static void TextureCheckOut (Texture2D input_texture)
	{
		if (input_texture == null)
		{
			return;
		}
		if (!AssetDatabase.Contains(input_texture))
		{
			return;
		}
		string asset_path = AssetDatabase.GetAssetPath(input_texture);
		TextureImporter tx_importer1 = AssetImporter.GetAtPath(asset_path) as TextureImporter;
		if (tx_importer1.textureCompression == TextureImporterCompression.Compressed && !tx_importer1.isReadable)
		{
			return;
		}
		tx_importer1.isReadable = false;
		tx_importer1.textureCompression = TextureImporterCompression.Compressed;
		AssetDatabase.ImportAsset(asset_path);
	}
	
	public static string GetTexturesFolder (string asset_path, bool create = true)
	{
		string textures_folder = "";
		string asset_folder = Path.GetDirectoryName(asset_path);
		if (!asset_folder.ToLower().Contains(materials))
		{
			textures_folder = BSPCommon.RemoveExtension(asset_path)+"_t";
			if (create)
			{
				BSPCommon.CreateFolders(textures_folder);
				return textures_folder;
			}
			else
			{
				if (Directory.Exists(textures_folder))
				{
					return textures_folder;
				}
				return null;
			}
		}
		if (BSPCommon.StringEndsWith(asset_folder.ToLower(), materials))
		{
			// Top level
			if (asset_path.Contains(Materials))
			{
				textures_folder = BSPCommon.GetParentFolder(asset_folder, Materials)+"/"+Textures+"/"+Path.GetFileNameWithoutExtension(asset_path)+"_t";
			}
			else
			{
				textures_folder = BSPCommon.GetParentFolder(asset_folder, materials)+"/"+textures+"/"+Path.GetFileNameWithoutExtension(asset_path)+"_t";
			}
		}
		else
		{
			if (asset_path.Contains(Materials))
			{
				textures_folder = BSPCommon.GetParentFolder(asset_folder, Materials)+"/"+Textures+"/"+BSPCommon.RemoveExtension(BSPCommon.GetPathInFolder(asset_path, Materials))+"_t";
			}
			else
			{
				textures_folder = BSPCommon.GetParentFolder(asset_folder, materials)+"/"+textures+"/"+BSPCommon.RemoveExtension(BSPCommon.GetPathInFolder(asset_path, materials))+"_t";
			}
		}
		if (create)
		{
			BSPCommon.CreateFolders(textures_folder);
			return textures_folder;
		}
		else
		{
			if (Directory.Exists(textures_folder))
			{
				return textures_folder;
			}
		}
		return null;
	}

	public static void WritePNG (string output_path, Texture2D input_texture)
	{
		byte[] output_bytes = input_texture.EncodeToPNG();
		File.WriteAllBytes(output_path, output_bytes);
		output_bytes = null;
	}

	public static void WriteJPG (string output_path, Texture2D input_texture, int quality = 80)
	{
		byte[] output_bytes = input_texture.EncodeToJPG(quality);
		File.WriteAllBytes(output_path, output_bytes);
		output_bytes = null;
	}

	public static void WriteTGA (string output_path, Texture2D input_texture)
	{
		if (input_texture == null)
		{
			Debug.LogError(refIsNull);
			return;
		}
		if (string.IsNullOrEmpty(output_path))
		{
			Debug.LogError(pathEmpty);
			return;
		}
		FileStream fs1 = new FileStream(output_path, FileMode.Create, FileAccess.Write);
		BinaryWriter bw1 = new BinaryWriter(fs1, System.Text.Encoding.UTF8);
		bw1.Seek(0, SeekOrigin.Begin);
		Color32[] pixels = input_texture.GetPixels32();
		byte[] tga_header = new byte[] {0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		byte[] tga_footer = new byte[] {0, 0, 0, 0, 0, 0, 0, 0, 84, 82, 85, 69, 86, 73, 83, 73, 79, 78, 45, 88, 70, 73, 76, 69, 46, 0};
		short width = (short)input_texture.width;
		short height = (short)input_texture.height;
		byte bit_depth = 24;
		if (input_texture.format != TextureFormat.RGB24) bit_depth = 32;
		bw1.Write(tga_header);
		bw1.Write(width);
		bw1.Write(height);
		bw1.Write(bit_depth);
		bw1.Write((byte)0x00);
		for (int i = 0; i < pixels.Length; i++)
		{
			bw1.Write(pixels[i].b);
			bw1.Write(pixels[i].g);
			bw1.Write(pixels[i].r);
			if (bit_depth == 32) bw1.Write(pixels[i].a);
		}
		bw1.Write(tga_footer);
		bw1.Close();
		fs1.Close();
		pixels = null;
		tga_header = null;
		tga_footer = null;
	}

	public static void WriteBMP (string output_path, Texture2D input_texture)
	{
		if (input_texture == null)
		{
			Debug.LogError(refIsNull);
			return;
		}
		if (string.IsNullOrEmpty(output_path))
		{
			Debug.LogError(pathEmpty);
			return;
		}
		FileStream fs1 = new FileStream(output_path, FileMode.Create, FileAccess.Write);
		BinaryWriter bw1 = new BinaryWriter(fs1, System.Text.Encoding.UTF8);
		bw1.Seek(0, SeekOrigin.Begin);
		Color32[] pixels = input_texture.GetPixels32();
		int width = input_texture.width;
		int height = input_texture.height;
		byte[] block1 = new byte[] {0, 0, 0, 0, 54, 0, 0, 0, 40, 0, 0, 0};
		byte[] block2 = new byte[] {18, 11, 0, 0, 18, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		int channels = 3;
		short bit_depth = 24;
		if (input_texture.format != TextureFormat.RGB24)
		{
			channels = 4;
			bit_depth = 32;
		}
		int line_size_r = width * channels;
		int r = line_size_r % 4;
		int line_size = line_size_r;
		if (r > 0)
		{
			line_size = line_size_r - r + 4;
			r = line_size - line_size_r;
		}
		int data_size = line_size * height;
		int file_size = data_size + 54;
		bw1.Write((byte)0x42);
		bw1.Write((byte)0x4d);
		bw1.Write(file_size);
		bw1.Write(block1);
		bw1.Write(width);
		bw1.Write(height);
		bw1.Write((byte)0x01);
		bw1.Write((byte)0x00);
		bw1.Write(bit_depth);
		bw1.Write((byte)0x00);
		bw1.Write((byte)0x00);
		bw1.Write((byte)0x00);
		bw1.Write((byte)0x00);
		bw1.Write(data_size);
		bw1.Write(block2);
		int pixels_i = 0;
		for (int i = 0; i < height; i++)
		{
			for (int i2 = 0; i2 < width; i2++)
			{
				bw1.Write(pixels[pixels_i].b);
				bw1.Write(pixels[pixels_i].g);
				bw1.Write(pixels[pixels_i].r);
				if (bit_depth == 32) bw1.Write(pixels[pixels_i].a);
				pixels_i++;
			}
			if (r > 0)
			{
				for (int i3 = 0; i3 < r; i3++)
				{
					bw1.Write((byte)0x00);
				}
			}
		}
		bw1.Close();
		fs1.Close();
		pixels = null;
		block1 = null;
		block2 = null;
	}

	public static Texture2D LoadTexture (string image_path, bool load_alpha = true, bool linear = false)
	{
		if (BSPCommon.StringEndsWith(image_path, ".tga") || BSPCommon.StringEndsWith(image_path, ".TGA"))
		{
			return LoadTGA(image_path, load_alpha, linear);
		}
		else if (BSPCommon.StringEndsWith(image_path, ".bmp") || BSPCommon.StringEndsWith(image_path, ".BMP"))
		{
			return LoadBMP(image_path, load_alpha, linear);
		}
		else if (BSPCommon.StringEndsWith(image_path, ".png") || BSPCommon.StringEndsWith(image_path, ".PNG"))
		{
			FileStream fs1 = new FileStream(image_path, FileMode.Open, FileAccess.Read);
			BinaryReader reader1 = new BinaryReader(fs1);
			PNGFile png = PNGTools.ReadPNGFile(reader1);
			for (int i = png.chunks.Count - 1; i >= 0; i--)
			{
				PNGChunk c = png.chunks[i];
				if (c.type != EChunkType.IHDR && c.type != EChunkType.IDAT && c.type != EChunkType.IEND)
				{
					png.chunks.RemoveAt(i);
				}
			}
			reader1.Close();
			fs1.Close();
			MemoryStream m_output = new MemoryStream();
			BinaryWriter writer1 = new BinaryWriter(m_output);
			PNGTools.WritePNGFile(png, writer1);
			byte[] png_bytes = m_output.ToArray();
			writer1.Close();
			m_output.Close();
			Texture2D output_texture = new Texture2D(32, 32, TextureFormat.RGB24, true, linear);
			#if UNITY_2017_1_OR_NEWER
			ImageConversion.LoadImage(output_texture, png_bytes, false);
			#else
			output_texture.LoadImage(png_bytes);
			#endif
			png_bytes = null;
			return output_texture;
		}
		else
		{
			Texture2D output_texture = new Texture2D(32, 32, TextureFormat.RGB24, true, linear);
			#if UNITY_2017_1_OR_NEWER
			ImageConversion.LoadImage(output_texture, File.ReadAllBytes(image_path), false);
			#else
			output_texture.LoadImage(File.ReadAllBytes(image_path));
			#endif
			return output_texture;
		}
	}

	public static Texture2D LoadTGA (string image_path, bool load_alpha = true, bool linear = false)
	{
		if (string.IsNullOrEmpty(image_path))
		{
			Debug.LogError(pathEmpty);
			return null;
		}
		FileStream fs1 = new FileStream(image_path, FileMode.Open, FileAccess.Read);
		BinaryReader br1 = new BinaryReader(fs1, System.Text.Encoding.UTF8);
		br1.BaseStream.Position = 0;
		br1.BaseStream.Seek(2, SeekOrigin.Begin);
		if (br1.ReadByte() == 10)
		{
			Debug.LogError("Compessed TGA are not suppopted at the moment.");
			br1.Close();
			fs1.Close();		
			return null;
		}
		br1.BaseStream.Seek(12, SeekOrigin.Begin);
		int width = (int)br1.ReadInt16();
		int height = (int)br1.ReadInt16();
		byte bit_depth = br1.ReadByte();
		br1.BaseStream.Seek(18, SeekOrigin.Begin);
		int pixel_count = width * height;
		Color32[] pixels = new Color32[pixel_count];
		for (int i = 0; i < pixel_count; i++)
		{
			if (bit_depth == 8)
			{
				pixels[i].b = br1.ReadByte();
				pixels[i].g = pixels[i].b;
				pixels[i].r = pixels[i].b;
				pixels[i].a = 255;
			}
			else
			{
				pixels[i].b = br1.ReadByte();
				pixels[i].g = br1.ReadByte();
				pixels[i].r = br1.ReadByte();
				if (bit_depth == 32) pixels[i].a = br1.ReadByte();
				else pixels[i].a = 255;
			}
		}
		Texture2D output_texture = null;
		if (bit_depth == 32 && load_alpha) output_texture = new Texture2D(width, height, TextureFormat.RGBA32, true, linear);
		else output_texture = new Texture2D(width, height, TextureFormat.RGB24, true, linear);
		output_texture.SetPixels32(pixels);
		output_texture.Apply(true, false);
		pixels = null;
		br1.Close();
		fs1.Close();
		return output_texture;
	}

	public static Texture2D LoadBMP (string image_path, bool load_alpha = true, bool linear = false)
	{
		if (string.IsNullOrEmpty(image_path))
		{
			Debug.LogError(pathEmpty);
			return null;
		}
		FileStream fs1 = new FileStream(image_path, FileMode.Open, FileAccess.Read);
		BinaryReader br1 = new BinaryReader(fs1, System.Text.Encoding.UTF8);
		br1.BaseStream.Seek(18, SeekOrigin.Begin);
		int width = br1.ReadInt32();
		int height = br1.ReadInt32();
		br1.BaseStream.Seek(2, SeekOrigin.Current);
		short bit_depth = br1.ReadInt16();
		int channels = bit_depth / 8;
		int pixel_count = width * height;
		int line_size_r = width * channels;
		int r = line_size_r % 4;
		int line_size = line_size_r;
		if (r > 0)
		{
			line_size = line_size_r - r + 4;
			r = line_size - line_size_r;
		}
		Color32[] pixels = new Color32[pixel_count];
		int pixels_i = 0;
		br1.BaseStream.Seek(54, SeekOrigin.Begin);
		for (int i = 0; i < height; i++)
		{
			for (int i2 = 0; i2 < width; i2++)
			{
				pixels[pixels_i].b = br1.ReadByte();
				pixels[pixels_i].g = br1.ReadByte();
				pixels[pixels_i].r = br1.ReadByte();
				if (bit_depth == 32) pixels[pixels_i].a = br1.ReadByte();
				else pixels[pixels_i].a = 255;
				pixels_i++;
			}
			if (r > 0)
			{
				br1.BaseStream.Seek(r, SeekOrigin.Current);
			}
		}
		Texture2D output_texture = null;
		if (bit_depth == 32 && load_alpha) output_texture = new Texture2D(width, height, TextureFormat.RGBA32, true, linear);
		else output_texture = new Texture2D(width, height, TextureFormat.RGB24, true, linear);
		output_texture.SetPixels32(pixels);
		output_texture.Apply(true, false);
		pixels = null;
		br1.Close();
		fs1.Close();
		return output_texture;
	}
	
	public static Texture2D ClampTextureSize (Texture2D texture, int size, bool normal = false)
	{
		if (texture.width > size || texture.height > size)
		{
			Texture2D output_texture = texture;
			if (!IsPowerOfTwo(output_texture.width) || !IsPowerOfTwo(output_texture.height))
			{
				output_texture = ResizeTexture(texture, GetLowerPowerOfTwo(texture.width - 1), GetLowerPowerOfTwo(texture.height - 1));
				texture = output_texture;
				if (!(output_texture.width > size) && !(output_texture.height > size))
				{
					return SharpenTexture(output_texture, normal ? sharpen_normal : sharpen, normal);
				}
			}
			while (output_texture.width > size || output_texture.height > size)
			{
				if (output_texture.width / 2 > size || output_texture.height / 2 > size)
				{
					output_texture = ResizeTexture(texture, texture.width / 2, texture.height / 2);
				}
				else
				{
					if (normal)
					{
						output_texture = ResizeTextureSharpNM(texture, texture.width / 2, texture.height / 2, sharpen_normal);
					}
					else
					{
						output_texture = ResizeTextureSharp(texture, texture.width / 2, texture.height / 2, sharpen);
					}
				}
				texture = output_texture;
			}
			return output_texture;
		}
		return texture;
	}

	public static Texture2D ResizeTextureNPOT (Texture2D texture, int size_x, int size_y, bool normal = false)
	{
		int new_size_x = 0;
		int new_size_y = 0;
		if (texture.width > size_x || texture.height > size_y)
		{
			Texture2D output_texture = texture;
			
			while (output_texture.width > size_x || output_texture.height > size_y)
			{
				if (output_texture.width > size_x * 2 || output_texture.height > size_y * 2)
				{
					if (output_texture.width > size_x * 2) new_size_x = output_texture.width / 2;
					else new_size_x = size_x;
					if (output_texture.height > size_y * 2) new_size_y = output_texture.height / 2;
					else new_size_y = size_y;
					output_texture = ResizeTexture(texture, new_size_x, new_size_y);
				}
				else
				{
					if (normal)
					{
						output_texture = ResizeTextureSharpNM(texture, size_x, size_y, sharpen_normal);
					}
					else
					{
						output_texture = ResizeTextureSharp(texture, size_x, size_y, sharpen);
					}
				}
				texture = output_texture;
			}
			return output_texture;
		}
		return texture;		
	}

	public static bool IsPowerOfTwo (int number)
	{
		return number > 0 && (number & (number - 1)) == 0;
	}

	public static int GetLowerPowerOfTwo (int number)
	{
		int output = 8192;
		while (output > number && number > 2)
		{
			output /= 2;
		}
		return output;
	}
	
	public static Texture2D SharpenTexture (Texture2D texture, float strength, bool normal = false)
	{
		int texture_width = texture.width;
		int texture_height = texture.height;		
		Texture2D output_texture;
		if (texture.format == TextureFormat.RGBA32)
		{
			output_texture = new Texture2D(texture_width, texture_height, TextureFormat.RGBA32, false, false);
		}
		else
		{
			output_texture = new Texture2D(texture_width, texture_height, TextureFormat.RGB24, false, false);
		}
		float[,] filter = new float[,]
		{ 
			{0.0f, -0.125f, 0.0f},
			{-0.125f, 1.5f, -0.125f},
			{0.0f, -0.125f, 0.0f}
		};
		float bias = 1.0f - strength;
		
		Color pixel_color1 = Color.clear;
		Color pixel_color2 = Color.clear;
		Color pixel_color3 = Color.clear;
		Vector2 normal_vector = Vector2.zero;
		for (int i = 0; i < texture_height; i++)
		{
			for (int i2 = 0; i2 < texture_width; i2++)
			{
				pixel_color1 = Color.clear;
				pixel_color2 = texture.GetPixel(i2, i);
				for (int filterX = 0; filterX < 3; filterX++)
				{
					for (int filterY = 0; filterY < 3; filterY++)
					{
						pixel_color1 += texture.GetPixel(i2 + filterX - 1, i + filterY - 1) * filter[filterX, filterY];
					}
				}
				pixel_color3 = pixel_color1 * strength + pixel_color2 * bias;
				if (normal)
				{
					normal_vector.x = (pixel_color3.r - 0.5f) * 2.0f;
					normal_vector.y = (pixel_color3.g - 0.5f) * 2.0f;
					pixel_color3.b = 0.5f + Mathf.Sqrt(1.0f - Mathf.Clamp(normal_vector.x * normal_vector.x + normal_vector.y * normal_vector.y, 0.0f, 1.0f)) * 0.5f;
				}
				output_texture.SetPixel(i2, i, pixel_color3);
			}
		}
		output_texture.Apply(false, false);
		return output_texture;
	}
	
	public static Texture2D ResizeTexture (Texture2D input_texture, int width, int height)
	{
		Texture2D output_texture;
		if (input_texture.format == TextureFormat.RGBA32)
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
		}
		else
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
		}
		float half_pixel_w = 1.0f / (float)(width * 4);
		float half_pixel_h = 1.0f / (float)(height * 4);
		Color pixel_color1 = Color.clear;
		for (int i = 0; i < height; i++)
		{
			for (int i2 = 0; i2 < width; i2++)
			{
				pixel_color1 = input_texture.GetPixelBilinear(((float)i2 / (float)width) + half_pixel_w, ((float)i / (float)height) + half_pixel_h);
				output_texture.SetPixel(i2, i, pixel_color1);
			}
		}
		output_texture.Apply(false, false);
		return output_texture;
	}

	public static Texture2D ResizeTextureSharp (Texture2D input_texture, int width, int height, float strength = 1.0f)
	{
		Texture2D output_texture;
		if (input_texture.format == TextureFormat.RGBA32)
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
		}
		else
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
		}
		float[,] filter = new float[,]
		{ 
			{0.0f, -0.125f, 0.0f},
			{-0.125f, 1.5f, -0.125f},
			{0.0f, -0.125f, 0.0f}
		};
		float bias = 1.0f - strength;
		float half_pixel_w = 1.0f / (float)(width * 4);
		float half_pixel_h = 1.0f / (float)(height * 4);
		Color pixel_color1 = Color.clear;
		Color pixel_color2 = Color.clear;
		for (int i = 0; i < height; i++)
		{
			for (int i2 = 0; i2 < width; i2++)
			{
				pixel_color1 = Color.clear;
				pixel_color2 = input_texture.GetPixelBilinear(((float)i2 / (float)width) + half_pixel_w, ((float)i / (float)height) + half_pixel_h);
				for (int filterX = 0; filterX < 3; filterX++)
				{
					for (int filterY = 0; filterY < 3; filterY++)
					{
						pixel_color1 += input_texture.GetPixelBilinear(((float)(i2 + filterX - 1) / (float)width) + half_pixel_w, ((float)(i + filterY - 1) / (float)height) + half_pixel_h) * filter[filterX, filterY];
					}
				}
				output_texture.SetPixel(i2, i, pixel_color1 * strength + pixel_color2 * bias);
			}
		}
		output_texture.Apply(false, false);
		return output_texture;
	}

	public static Texture2D ResizeTextureSharpNM (Texture2D input_texture, int width, int height, float strength = 1.0f)
	{
		Texture2D output_texture;
		if (input_texture.format == TextureFormat.RGBA32)
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
		}
		else
		{
			output_texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
		}
		float[,] filter = new float[,]
		{ 
			{0.0f, -0.125f, 0.0f},
			{-0.125f, 1.5f, -0.125f},
			{0.0f, -0.125f, 0.0f}
		};
		float bias = 1.0f - strength;
		float half_pixel_w = 1.0f / (float)(width * 4);
		float half_pixel_h = 1.0f / (float)(height * 4);
		Color pixel_color1 = Color.clear;
		Color pixel_color2 = Color.clear;
		Color pixel_color3 = Color.clear;
		Vector2 normal_vector = Vector2.zero;
		for (int i = 0; i < height; i++)
		{
			for (int i2 = 0; i2 < width; i2++)
			{
				pixel_color1 = Color.clear;
				pixel_color2 = input_texture.GetPixelBilinear(((float)i2 / (float)width) + half_pixel_w, ((float)i / (float)height) + half_pixel_h);
				for (int filterX = 0; filterX < 3; filterX++)
				{
					for (int filterY = 0; filterY < 3; filterY++)
					{
						pixel_color1 += input_texture.GetPixelBilinear(((float)(i2 + filterX - 1) / (float)width) + half_pixel_w, ((float)(i + filterY - 1) / (float)height) + half_pixel_h) * filter[filterX, filterY];
					}
				}
				pixel_color3 = pixel_color1 * strength + pixel_color2 * bias;
				normal_vector.x = (pixel_color3.r - 0.5f) * 2.0f;
				normal_vector.y = (pixel_color3.g - 0.5f) * 2.0f;
				pixel_color3.b = 0.5f + Mathf.Sqrt(1.0f - Mathf.Clamp(normal_vector.x * normal_vector.x + normal_vector.y * normal_vector.y, 0.0f, 1.0f)) * 0.5f;
				output_texture.SetPixel(i2, i, pixel_color3);
			}
		}
		output_texture.Apply(false, false);
		return output_texture;
	}

	////////////////////////////////// PNG Utility //////////////////////////////////
    public enum EChunkType : uint
    {
        IHDR = 0x49484452,
        sRGB = 0x73524742,
        gAMA = 0x67414D41,
        cHRM = 0x6348524D,
        pHYs = 0x70485973,
        IDAT = 0x49444154,
        IEND = 0x49454E44,
    }

    public class PNGFile
    {
        public static ulong m_Signature = 0x89504E470D0A1A0AU;
        public ulong Signature;
        public List<PNGChunk> chunks;
        public int FindChunk(EChunkType aType, int aStartIndex = 0)
        {
            if (chunks == null)
                return -1;
            for(int i = aStartIndex; i < chunks.Count; i++)
            {
                if (chunks[i].type == aType)
                    return i;
            }
            return -1;
        }
    }

    public class PNGChunk
    {
        public uint length;
        public EChunkType type;
        public byte[] data;
        public uint crc;
        public uint CalcCRC()
        {
            var crc = PNGTools.UpdateCRC(0xFFFFFFFF, (uint)type);
            crc = PNGTools.UpdateCRC(crc, data);
            return crc ^ 0xFFFFFFFF;
        }
    }

    public class PNGTools
    {
        static uint[] crc_table = new uint[256];
        static PNGTools()
        {
            for (int n = 0; n < 256; n++)
            {
                uint c = (uint)n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) > 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crc_table[n] = c;
            }
        }

        public static uint UpdateCRC(uint crc, byte aData)
        {
            return crc_table[(crc ^ aData) & 0xff] ^ (crc >> 8);
        }

        public static uint UpdateCRC(uint crc, uint aData)
        {
            crc = crc_table[(crc ^ ((aData >> 24) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ ((aData >> 16) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ ((aData >> 8) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ (aData & 0xFF)) & 0xff] ^ (crc >> 8);
            return crc;
        }


        public static uint UpdateCRC(uint crc, byte[] buf)
        {
            for (int n = 0; n < buf.Length; n++)
                crc = crc_table[(crc ^ buf[n]) & 0xff] ^ (crc >> 8);
            return crc;
        }

        public static uint CalculateCRC(byte[] aBuf)
        {
            return UpdateCRC(0xffffffff, aBuf) ^ 0xffffffff;
        }
        public static List<PNGChunk> ReadChunks(BinaryReader aReader)
        {
            var res = new List<PNGChunk>();
            while (aReader.BaseStream.Position < aReader.BaseStream.Length - 4)
            {
                var chunk = new PNGChunk();
                chunk.length = ReadUInt32BE(aReader);
                if (aReader.BaseStream.Position >= aReader.BaseStream.Length - 4 - chunk.length)
                    break;
                res.Add(chunk);
                chunk.type = (EChunkType)ReadUInt32BE(aReader);
                chunk.data = aReader.ReadBytes((int)chunk.length);
                chunk.crc = ReadUInt32BE(aReader);

                uint crc = chunk.CalcCRC();

                if ((chunk.crc ^ crc) != 0)
                    Debug.Log("Chunk CRC wrong. Got 0x" + chunk.crc.ToString("X8") + " expected 0x" + crc.ToString("X8"));
                if (chunk.type == EChunkType.IEND)
                    break;
            }
            return res;
        }

        public static PNGFile ReadPNGFile(BinaryReader aReader)
        {
            if (aReader == null || aReader.BaseStream.Position >= aReader.BaseStream.Length - 8)
                return null;
            var file = new PNGFile();
            file.Signature = ReadUInt64BE(aReader);
            file.chunks = ReadChunks(aReader);
            return file;
        }
        public static void WritePNGFile(PNGFile aFile, BinaryWriter aWriter)
        {
            WriteUInt64BE(aWriter, PNGFile.m_Signature);
            foreach (var chunk in aFile.chunks)
            {
                WriteUInt32BE(aWriter, (uint)chunk.data.Length);
                WriteUInt32BE(aWriter, (uint)chunk.type);
                aWriter.Write(chunk.data);
                WriteUInt32BE(aWriter, chunk.crc);
            }
        }

        public static void SetPPM(PNGFile aFile, uint aXPPM, uint aYPPM)
        {
            int pos = aFile.FindChunk(EChunkType.pHYs);
            PNGChunk chunk;
            if (pos > 0)
            {
                chunk = aFile.chunks[pos];
                if (chunk.data == null || chunk.data.Length < 9)
                    throw new System.Exception("PNG: pHYs chunk data size is too small. It should be at least 9 bytes");
            }
            else
            {
                chunk = new PNGChunk();
                chunk.type = EChunkType.pHYs;
                chunk.length = 9;
                chunk.data = new byte[9];
                aFile.chunks.Insert(1, chunk);
            }
            var data = chunk.data;
            data[0] = (byte)((aXPPM >> 24) & 0xFF);
            data[1] = (byte)((aXPPM >> 16) & 0xFF);
            data[2] = (byte)((aXPPM >> 8) & 0xFF);
            data[3] = (byte)((aXPPM) & 0xFF);

            data[4] = (byte)((aYPPM >> 24) & 0xFF);
            data[5] = (byte)((aYPPM >> 16) & 0xFF);
            data[6] = (byte)((aYPPM >> 8) & 0xFF);
            data[7] = (byte)((aYPPM) & 0xFF);

            data[8] = 1;
            chunk.crc = chunk.CalcCRC();
        }

        public static byte[] ChangePPM(byte[] aPNGData, uint aXPPM, uint aYPPM)
        {
            PNGFile file;
            using (var stream = new MemoryStream(aPNGData))
            using (var reader = new BinaryReader(stream))
            {
                file = ReadPNGFile(reader);
            }
            SetPPM(file, aXPPM, aYPPM);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WritePNGFile(file, writer);
                return stream.ToArray();
            }
        }
        public static byte[] ChangePPI(byte[] aPNGData, float aXPPI, float aYPPI)
        {
            return ChangePPM(aPNGData, (uint)(aXPPI * 39.3701f), (uint)(aYPPI * 39.3701f));
        }
    }

	public static uint ReadUInt32BE(BinaryReader aReader)
	{
		return ((uint)aReader.ReadByte() << 24) | ((uint)aReader.ReadByte() << 16) | ((uint)aReader.ReadByte() << 8) | ((uint)aReader.ReadByte());
	}

	public static ulong ReadUInt64BE(BinaryReader aReader)
	{
		return (ulong)ReadUInt32BE(aReader)<<32 | ReadUInt32BE(aReader);
	}

	public static void WriteUInt32BE(BinaryWriter aWriter, uint aValue)
	{
		aWriter.Write((byte)((aValue >> 24) & 0xFF));
		aWriter.Write((byte)((aValue >> 16) & 0xFF));
		aWriter.Write((byte)((aValue >> 8 ) & 0xFF));
		aWriter.Write((byte)((aValue      ) & 0xFF));
	}

	public static void WriteUInt64BE(BinaryWriter aWriter, ulong aValue)
	{
		WriteUInt32BE(aWriter, (uint)(aValue >> 32));
		WriteUInt32BE(aWriter, (uint)(aValue));
	}
}