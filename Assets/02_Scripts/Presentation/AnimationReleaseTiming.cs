using UnityEngine;
namespace Codice.Presentation{
 public static class AnimationReleaseTiming{
  [System.Serializable]class Values{public float staff=.36f;public float hand=.36f;}
  static Values values;
  static Values Data{get{if(values==null){var file=Resources.Load<TextAsset>("Presentation/Human/CastTimings");values=file?JsonUtility.FromJson<Values>(file.text):new Values();}return values;}}
  public static float Staff=>Mathf.Clamp(Data.staff,.15f,.8f);
  public static float Hand=>Mathf.Clamp(Data.hand,.15f,.8f);
 }
}
