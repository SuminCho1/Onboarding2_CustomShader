using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
   private MaterialEditor _editor;
   private Object[] _materials;
   private MaterialProperty[] _properties;
   private bool _showPresets;
   
   private bool Clipping
   {
      set => SetProperty("_Clipping", "_CLIPPING", value);
   }
   
   private bool PremultiplyAlpha {
      set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
   }

   private BlendMode SrcBlend {
      set => SetProperty("_SrcBlend", (float)value);
   }

   private BlendMode DstBlend {
      set => SetProperty("_DstBlend", (float)value);
   }

   private bool ZWrite {
      set => SetProperty("_ZWrite", value ? 1f : 0f);
   }
   
   private RenderQueue RenderQueue {
      set {
         foreach (Material material in _materials) {
            material.renderQueue = (int)value;
         }
      }
   }
   
   public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
   {
      base.OnGUI(materialEditor, properties);
      _editor = materialEditor;
      _materials = materialEditor.targets;
      _properties = properties;
      
      EditorGUILayout.Space();
      _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
      if (_showPresets)
      {
         OpaquePreset();
         ClipPreset();
         FadePreset();
         TransparentPreset();
      }
   }

   private bool PresetButton(string name)
   {
      if (GUILayout.Button(name))
      {
         _editor.RegisterPropertyChangeUndo(name);
         return true;
      }

      return false;
   }

   private void OpaquePreset()
   {
      if (PresetButton("Opaque"))
      {
         Clipping = false;
         PremultiplyAlpha = false;
         SrcBlend = BlendMode.One;
         DstBlend = BlendMode.Zero;
         ZWrite = true;
         RenderQueue = RenderQueue.Geometry;
      }
   }

   private void ClipPreset()
   {
      if (PresetButton("Clip"))
      {
         Clipping = true;
         PremultiplyAlpha = false;
         SrcBlend = BlendMode.One;
         DstBlend = BlendMode.Zero;
         ZWrite = true;
         RenderQueue = RenderQueue.AlphaTest;
      }
   }

   private void FadePreset()
   {
      if (PresetButton("Fade"))
      {
         Clipping = false;
         PremultiplyAlpha = false;
         SrcBlend = BlendMode.SrcAlpha;
         DstBlend = BlendMode.OneMinusSrcAlpha;
         ZWrite = false;
         RenderQueue = RenderQueue.Transparent;
      }
   }

   private void TransparentPreset()
   {
      if (PresetButton("Transparent"))
      {
         Clipping = false;
         PremultiplyAlpha = true;
         SrcBlend = BlendMode.One;
         DstBlend = BlendMode.OneMinusSrcAlpha;
         ZWrite = false;
         RenderQueue = RenderQueue.Transparent;
      }
   }
   
   private void SetProperty(string name, float value)
   {
      FindProperty(name, _properties).floatValue = value;
   }

   private void SetProperty(string name, string keyword, bool value)
   {
      SetProperty(name, value ? 1f : 0f);
      SetKeyword(keyword, value);
   }

   private void SetKeyword(string keyword, bool enabled)
   {
      if (enabled)
      {
         foreach(Material material in _materials)
            material.EnableKeyword(keyword);
      }
      else
      {
         foreach(Material material in _materials)
            material.DisableKeyword(keyword);
      }
   }
}
