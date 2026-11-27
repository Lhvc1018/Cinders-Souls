using UnityEditor;
using UnityEngine;

namespace RPGCharacterAnims
{
	static class SetupInputLayers
    {
        [MenuItem("Tools/Warrior Animation/Input and Tag Presets")]
        static void ShowInstructions()
        {
            SetupMessageWindow window = ( SetupMessageWindow )EditorWindow.GetWindow(typeof(SetupMessageWindow), true, "Load Input and Tag Presets");
			window.maxSize = new Vector2(315f, 130f);
			window.minSize = window.maxSize;
		}
	}

	public class SetupMessageWindow:EditorWindow
	{
		void OnGUI()
		{
			EditorGUILayout.LabelField("Before attempting to use the Warrior Animation Mecaim Animation Packs, you must first ensure that the layers and inputs are correctly defined.  There is an included Input.preset and Layer.preset which contains all the settings that you can load in.\n \nYou can remove this message by deleting the SetupInputLayers.cs script in the Editor folder.", EditorStyles.wordWrappedLabel);
		}
	}
}