using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to change the color of the Renderer (e.g. MeshRenderer) attached to the current GameObject when selected.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Renderer))]
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanSelectableRendererColor")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Selectable Renderer Color")]
	public class LeanSelectableRendererColor : LeanSelectableBehaviour
	{
		/// <summary>The default color given to the SpriteRenderer.</summary>
		[SerializeField] private Color defaultColor = Color.white;
		public Color DefaultColor { set { defaultColor = value; UpdateColor(); } get { return defaultColor; } }

				private void Awake()
				{
					// Get the default color from the renderer's material
					if (cachedRenderer == null)
						cachedRenderer = GetComponent<Renderer>();
					
					if (cachedRenderer != null && cachedRenderer.sharedMaterial != null)
					{
						var material = cachedRenderer.sharedMaterial;
						if (material.HasProperty("_BaseColor"))
							defaultColor = material.GetColor("_BaseColor");
						else if (material.HasProperty("_Color"))
							defaultColor = material.GetColor("_Color");
						else if (material.HasProperty("_MainColor"))
							defaultColor = material.GetColor("_MainColor");
					}
				}

		/// <summary>The color given to the SpriteRenderer when selected.</summary>
		[SerializeField] private Color selectedColor = Color.green;
		public Color SelectedColor { set { selectedColor = value; UpdateColor(); } get { return selectedColor; } }
		
		[System.NonSerialized]
		private Renderer cachedRenderer;

		[System.NonSerialized]
		private MaterialPropertyBlock properties;

		protected override void OnSelected(LeanSelect select)
		{
			UpdateColor();
		}

		protected override void OnDeselected(LeanSelect select)
		{
			UpdateColor();
		}

		protected override void Start()
		{
			base.Start();

			UpdateColor();
		}

		public void UpdateColor()
		{
			// Don't update colors in edit mode on prefabs
			#if UNITY_EDITOR
						if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
							return;
			#endif
			
						var color = Selectable != null && Selectable.IsSelected == true ? selectedColor : defaultColor;
			
						// Update color for this GameObject's renderer
						UpdateRendererColor(gameObject, color);
			
						// Loop through all children and update their colors
						foreach (Transform child in transform)
						{
							UpdateRendererColor(child.gameObject, color);
						}
					}

		private void UpdateRendererColor(GameObject target, Color color)
		{
			var renderer = target.GetComponent<Renderer>();
			if (renderer == null) return;

			// Create a new MaterialPropertyBlock for each call to avoid null reference
			var properties = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(properties);

			// Use sharedMaterial instead of material to avoid prefab issues
			var material = renderer.sharedMaterial;
			if (material == null) return;

			// Try different color properties for different render pipelines
			if (material.HasProperty("_BaseColor"))
				properties.SetColor("_BaseColor", color);
			else if (material.HasProperty("_Color"))
				properties.SetColor("_Color", color);
			else if (material.HasProperty("_MainColor"))
				properties.SetColor("_MainColor", color);

			renderer.SetPropertyBlock(properties);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanSelectableRendererColor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelectableRendererColor_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			var updateColor = false;

			Draw("defaultColor", ref updateColor, "The default color given to the SpriteRenderer.");
			Draw("selectedColor", ref updateColor, "The color given to the SpriteRenderer when selected.");

			if (updateColor == true)
			{
				Each(tgts, t => t.UpdateColor(), true);
			}
		}
	}
}
#endif