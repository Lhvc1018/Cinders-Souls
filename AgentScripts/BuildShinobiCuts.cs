using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BuildShinobiCuts
{
    const string Root = "Assets/Resources/Presentation/Human/";
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "Shinobi.controller");
        Install(controller, "KatanaSlashA", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx");
        Install(controller, "KatanaSlashB", "Assets/03_Models/Mixamo/GreatSword/great sword slash (3).fbx");
        AssetDatabase.SaveAssets();
        return "Installed humanoid cuts with native foot goals on Shinobi";
    }

    static void Install(AnimatorController controller, string state, string source)
    {
        var native = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().First(c => c.isHumanMotion && !c.name.StartsWith("__"));
        var clip = UnityEngine.Object.Instantiate(native);
        clip.name = "Shinobi_" + state;
        AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        settings.keepOriginalOrientation = true;
        settings.heightFromFeet = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var dest = Root + clip.name + ".anim";
        var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);
        if (old)
        {
            EditorUtility.CopySerialized(clip, old);
            UnityEngine.Object.DestroyImmediate(clip);
            clip = old;
        }
        else AssetDatabase.CreateAsset(clip, dest);
        var machine = controller.layers[0].stateMachine;
        var action = machine.states.First(s => s.state.name == state).state;
        action.motion = clip;
        action.iKOnFeet = false;
        action.speedParameterActive = true;
        action.speedParameter = "ActionSpeed";
        var preview = machine.states.FirstOrDefault(s => s.state.name == "Preview_" + clip.name).state ?? machine.AddState("Preview_" + clip.name);
        preview.motion = clip;
        EditorUtility.SetDirty(clip);
        EditorUtility.SetDirty(action);
        EditorUtility.SetDirty(preview);
        EditorUtility.SetDirty(controller);
    }
}
