using UnityEditor;
public static class RefreshCombatAssets{public static string Run(){AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);return "Assets refreshed";}}
