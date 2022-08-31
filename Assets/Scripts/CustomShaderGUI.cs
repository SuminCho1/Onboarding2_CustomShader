using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
   private enum ShadowMode
   {
      On, Clip, Dither, Off
   }
   
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

   private ShadowMode Shadows
   {
      set
      {
         if (SetProperty("_Shadows", (float)value))
         {
            SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
            SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
         }
      }
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
      EditorGUI.BeginChangeCheck();
      
      base.OnGUI(materialEditor, properties);
      _editor = materialEditor;
      _materials = materialEditor.targets;
      _properties = properties;

      BakedEmission();
      
      EditorGUILayout.Space();
      _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
      if (_showPresets)
      {
         OpaquePreset();
         ClipPreset();
         FadePreset();
         TransparentPreset();
      }
      
      if(EditorGUI.EndChangeCheck())
         SetShaderCasterPass();
   }

   private void BakedEmission()
   {
      EditorGUI.BeginChangeCheck();
      _editor.LightmapEmissionProperty();

      if (EditorGUI.EndChangeCheck())
      {
         foreach (Material m in _editor.targets)
            m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
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
   
   private bool SetProperty(string name, float value)
   {
      var property = FindProperty(name, _properties, false);
      if (property != null)
      {
         property.floatValue = value;
         return true;
      }

      return false;
   }

   private void SetProperty(string name, string keyword, bool value)
   {
      if (SetProperty(name, value ? 1f : 0f)) 
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

   private void SetShaderCasterPass()
   {
      var shadows = FindProperty("_Shadows", _properties, false);

      if (shadows == null || shadows.hasMixedValue)
         return;

      bool enabled = shadows.floatValue < (float)ShadowMode.Off;
      foreach(Material m in _materials)
         m.SetShaderPassEnabled("ShadowCaster", enabled);
   }
}
