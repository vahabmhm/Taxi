using System; using System.Linq; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
// AGENTS.md main menu visual design: text-first buttons, one shared glossy + light-sweep style
public static partial class T {
  static void TestMenuButtonStyle(){
    var hub = MakeMenu(); ShowMenu(hub);
    var root = UnityEngine.Object.FindAllRoots().Last(r=>FindAll<Button>(r,"ButtonLeague").Count>0);
    string[] names = { "ButtonGarage","ButtonPlayLevel","ButtonLevelMap","ButtonEndless","Buttonmissions","Buttonchallenge","Buttonwheel","Buttonothergames","Buttonlogin","Buttonachievements","ButtonLeague" };
    var buttons = names.Select(n=>FindAll<Button>(root,n).FirstOrDefault()).ToList();
    Check(buttons.All(b=>b!=null && b.GetComponent<ECCMenuButtonShine>()!=null), "every main menu button uses the one shared shine style");
    Check(buttons.All(b=>FindAll<Image>(b.transform,"Gloss").Count==1 && FindAll<RectTransform>(b.transform,"ShineClip").Count==1), "gloss layer and a clip area on each");
    Check(buttons.All(b=>FindAll<Image>(b.transform,"Gloss")[0].raycastTarget==false && b.GetComponentInChildren<RectMask2D>()!=null), "nothing added blocks the hit area; the streak is clipped");
    var grid = names.Skip(4).Take(6).Select(n=>FindAll<Button>(root,n)[0]).ToList();
    Check(grid.All(b=>FindAll<Image>(b.transform,"Icon").Count==0), "secondary grid buttons are text-first ( no decorative icons )");
    Check(FindAll<Image>(FindAll<Button>(root,"ButtonEndless")[0].transform,"Icon").Count==0, "endless button: no extra icon next to its record");
    var play = FindAll<Button>(root,"ButtonPlayLevel")[0];
    Check(ECCRuntimeUI.GetLabel(play).fontSize > ECCRuntimeUI.GetLabel(grid[0]).fontSize && ECCRuntimeUI.GetLabel(play).fontSize > ECCRuntimeUI.GetLabel(FindAll<Button>(root,"ButtonEndless")[0]).fontSize, "one dominant primary action ( «شروع مرحله» )");
    // The sweep: runs, is staggered, stops while a popup covers the menu
    var shines = buttons.Select(b=>b.GetComponent<ECCMenuButtonShine>()).ToList();
    var size = ((RectTransform)play.transform).sizeDelta;
    bool anyStarted=false, allAtOnce=true;
    for (int f=0; f<60; f++){ foreach (var s in shines) s.Step(0.05f); int n = shines.Count(s=>s.IsSweeping); if (n>0) anyStarted=true; if (n>0 && n<shines.Count) allAtOnce=false; }
    Check(anyStarted && !allAtOnce, "sweeps start and are staggered between buttons");
    Check(((RectTransform)play.transform).sizeDelta == size, "the button's size is never changed");
    ECCWeeklyLeagueUI.OpenLocked(); foreach (var s in shines) s.Step(0.05f);
    Check(shines.All(s=>!s.IsSweeping), "paused while a popup covers the menu");
    ECCMenuStyleConfig.Instance.lightSweep = false; Check(!ECCMenuStyleConfig.Instance.SweepAllowed, "can be turned off ( low-end devices )"); ECCMenuStyleConfig.Instance.lightSweep = true;
    SystemInfo.systemMemorySize = 1024; Check(!ECCMenuStyleConfig.Instance.SweepAllowed, "turned off below the memory limit"); SystemInfo.systemMemorySize = 4096;
  }
}
