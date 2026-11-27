using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class VisualReview
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public static string Size(int width,int height)
    {
        var asm=typeof(Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        var group=type.GetProperty("currentGroup",Flags).GetValue(singleton);var gt=group.GetType();
        int count=(int)gt.GetMethod("GetTotalCount",Flags).Invoke(group,null),index=-1;
        for(int i=0;i<count;i++)
        {
            var size=gt.GetMethod("GetGameViewSize",Flags).Invoke(group,new object[]{i});var st=size.GetType();
            if((int)st.GetProperty("width",Flags).GetValue(size)==width&&(int)st.GetProperty("height",Flags).GetValue(size)==height){index=i;break;}
        }
        if(index<0)
        {
            var sizeType=asm.GetType("UnityEditor.GameViewSize");var enumType=asm.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.ToObject(enumType,1),width,height,"Cinders review "+width+"x"+height},null);
            gt.GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});index=count;
        }
        var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        var property=view.GetType().GetProperty("selectedSizeIndex",Flags);
        if(SessionState.GetInt("CindersOriginalView",-1)<0)SessionState.SetInt("CindersOriginalView",(int)property.GetValue(view));
        property.SetValue(view,index);view.Repaint();return "Game View "+width+"x"+height;
    }
    public static string Restore()
    {
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));int index=SessionState.GetInt("CindersOriginalView",-1);
        if(index>=0)view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,index);SessionState.EraseInt("CindersOriginalView");return "Game View restored";
    }
    public static string TextAudit()
    {
        Canvas.ForceUpdateCanvases();
        var texts=UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>();
        return string.Join("\n",texts.Where(t=>t.isActiveAndEnabled&&t.isTextOverflowing).Select(t=>t.name+" | "+t.text));
    }
}
