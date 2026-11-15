using System; using UnityEngine; using EndlessCarChase;
// Car toughness: police health follows the player's ram damage and the chapter; the boss takes several rams
public static partial class T {
  static ECCCar Cop(float health, int damage = 1){ var c = new GameObject("SedanCop").AddComponent<ECCCar>(); c.health = health; c.damage = damage; return c; }
  static void TestPoliceToughness(){
    ECCGameController gc; ECCPowerUpManager manager; MakeChaseWithBar(out gc, out manager);
    typeof(ECCGameController).GetProperty("Instance").SetValue(null, gc);
    var cfg = ECCEconomyConfig.Instance;
    Check(cfg.policeHealthPerPlayerDamage==1.5f && cfg.policeHealthPerChapter==0.15f && cfg.playerHealthMultiplier==2.5f && cfg.bossHitsOfPlayerDamage==8, "defaults: 1.5x player damage, +15% per chapter, player x2.5, boss 8 rams");
    gc.playerObject.damage = 6;
    var sedan = Cop(2); ECCCopVariant.Toughen(sedan, 0);
    Check(Mathf.Approximately(sedan.health, 9), "chapter 1: a 2-health police car gets 1.5 x 6 = 9 ( about two rams of 6 )");
    var strong = Cop(20); ECCCopVariant.Toughen(strong, 0);
    Check(Mathf.Approximately(strong.health, 20), "never lowers a tougher car");
    var ch2 = Cop(2); ECCCopVariant.Toughen(ch2, 1); var ch3 = Cop(2); ECCCopVariant.Toughen(ch3, 2);
    Check(Mathf.Approximately(ch2.health, 9*1.15f) && Mathf.Approximately(ch3.health, 9*1.3f), "chapter 2 +15%, chapter 3 +30%");
    var late = Cop(2); ECCCopVariant.Toughen(late, 20);
    Check(Mathf.Approximately(late.health, 18), "chapter bonus capped at double");
    gc.playerObject.damage = 1; var weak = Cop(2); ECCCopVariant.Toughen(weak, 0);
    Check(Mathf.Approximately(weak.health, 2), "starter car ( damage 1 ): police keep their own health");
    gc.playerObject.damage = 6; var heavy = Cop(2); ECCCopVariant.Toughen(heavy, 0); ECCCopVariant.Apply(heavy, ECCCopType.Heavy);
    Check(Mathf.Approximately(heavy.health, 22.5f), "variants scale from the toughened health ( heavy x2.5 )");
    var boss = Cop(40, 4); // the van after Apply(Boss): 4 x 10
    ECCCopVariant.Toughen(boss, 0); ECCCopVariant.ToughenBoss(boss);
    Check(boss.health >= 48, "boss takes at least 8 rams of the player's car");
  }
}
