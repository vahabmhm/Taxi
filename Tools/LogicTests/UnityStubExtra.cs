// Additions to the fake Unity API ( see UnityStub.cs ), for APIs used after the stub was first written
namespace UnityEngine {
public class MaterialPropertyBlock { public void SetColor(string n, Color c){} public void SetFloat(string n, float f){} public void Clear(){} }
}
namespace UnityEngine {
public static class Handheld { public static void Vibrate(){} }
}
namespace UnityEngine {
public static class ColorUtility { public static string ToHtmlStringRGBA(Color c){ return c.r+","+c.g+","+c.b+","+c.a; } public static string ToHtmlStringRGB(Color c){ return c.r+","+c.g+","+c.b; } }
}
namespace UnityEngine { public static class SystemInfo { public static int systemMemorySize = 4096; } }
