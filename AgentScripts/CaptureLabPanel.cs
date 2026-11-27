using System.Threading.Tasks;using UnityEngine;
public static class CaptureLabPanel{public static async Task<string> Run(){ScreenCapture.CaptureScreenshot("D:/67/CindersBackups/2026-11-07-lab-aggression/panel.png");await Task.Delay(1200);return "captured";}}
