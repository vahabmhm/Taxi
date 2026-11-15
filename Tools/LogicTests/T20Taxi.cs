using System; using System.Linq; using UnityEngine; using EndlessCarChase; using EndlessCarChase.Modes; using EndlessCarChase.Levels;
// Tests for the taxi game: the street grid and routes, traffic lights, fares and ratings, the police stars,
// the seats of the cars, the shift and level rules
public static partial class T {
  static bool OnStreet(Vector3 p){ float s=TaxiCity.Spacing; return Mathf.Abs(p.x-Mathf.Round(p.x/s)*s)<TaxiCity.CrossHalf+0.01f || Mathf.Abs(p.z-Mathf.Round(p.z/s)*s)<TaxiCity.CrossHalf+0.01f; }

  static void TestTaxiSpots(){
    Check(Mathf.Approximately(TaxiCity.Spacing, 42), "street spacing ( city kit: 3 cells of 14 )");
    var rnd = new System.Random(5); bool allGood = true;
    for (int i=0;i<300;i++){
      var p = new Vector3((float)(rnd.NextDouble()*800-400),0,(float)(rnd.NextDouble()*800-400));
      var spot = TaxiCity.SpotNear(p, i%2==0);
      Vector2Int c;
      bool ok = OnStreet(spot.zone) && !TaxiCity.InCrossroad(spot.zone, out c)
        && Mathf.Abs(Vector3.Distance(spot.stand, spot.zone) - (TaxiCity.SidewalkOffset-TaxiCity.LaneOffset)) < 0.01f
        && Vector3.Dot(spot.toStreet, spot.zone-spot.stand) > 0 && Mathf.Abs(Vector3.Dot(spot.toStreet, spot.streetDirection)) < 0.01f;
      // Far enough from the crossroads for a car to stop in the ring
      float along = spot.streetDirection.z!=0 ? spot.zone.z : spot.zone.x;
      float local = along - Mathf.Floor(along/42)*42;
      ok = ok && local >= TaxiCity.CrossHalf+3.9f && local <= 42-TaxiCity.CrossHalf-3.9f;
      if (!ok){ allGood=false; Console.WriteLine("  bad spot near "+p.x+","+p.z+" zone "+spot.zone.x+","+spot.zone.z); break; }
    }
    Check(allGood, "passenger spots: on a street, off the crossroads, sidewalk beside the lane");
  }

  static void TestTaxiCrossroadDistance(){
    Vector2Int c;
    float d = TaxiCity.DistanceToNextCrossroad(new Vector3(3,0,10), Vector3.forward, out c);
    Check(Mathf.Abs(d-25)<0.01f && c==new Vector2Int(0,1), "next crossroad ahead along +Z ( entry line at 35 )");
    d = TaxiCity.DistanceToNextCrossroad(new Vector3(3,0,36), Vector3.forward, out c);
    Check(c==new Vector2Int(0,2), "inside a crossroad the next one counts");
    d = TaxiCity.DistanceToNextCrossroad(new Vector3(10,0,-3), Vector3.left, out c);
    Check(Mathf.Abs(d-3)<0.01f && c==new Vector2Int(0,0), "along -X");
    Check(TaxiCity.InCrossroad(new Vector3(42,0,-41), out c) && c==new Vector2Int(1,-1) && !TaxiCity.InCrossroad(new Vector3(42,0,-30), out c), "crossroad test");
  }

  // Drives a point along the waypoints ( turning at once ) and reports whether it reaches the target on the streets
  static bool Drive(Vector3 start, Vector3 target, out int steps){
    Vector3 p = start; steps = 0;
    for (; steps<4000; steps++){
      if (Vector3.Distance(p, target) < 1.5f) return true;
      Vector3 w = TaxiCity.NextWaypoint(p, target);
      Vector3 dir = w-p; dir.y=0;
      if (dir.magnitude < 0.01f) return false;
      p += dir.normalized * Mathf.Min(1, dir.magnitude);
      if (!OnStreet(p)) return false;
    }
    return false;
  }

  static void TestTaxiRoutes(){
    var rnd = new System.Random(11); int reached=0, total=200; long sum=0;
    for (int i=0;i<total;i++){
      var a = TaxiCity.SpotNear(new Vector3((float)(rnd.NextDouble()*600-300),0,(float)(rnd.NextDouble()*600-300)), true).zone;
      var b = TaxiCity.SpotNear(new Vector3((float)(rnd.NextDouble()*600-300),0,(float)(rnd.NextDouble()*600-300)), rnd.Next(2)==0).zone;
      int steps;
      if (Drive(a,b,out steps)) { reached++; sum+=steps; }
      else Console.WriteLine("  route failed "+a.x+","+a.z+" -> "+b.x+","+b.z);
    }
    Check(reached==total, "every route along the streets reaches its target ("+reached+"/"+total+")");
    Check(TaxiCity.NextWaypoint(new Vector3(3,0,10), new Vector3(3,0,30))==new Vector3(3,0,30), "same street: straight to the target");
  }

  static void TestTaxiTrafficLights(){
    var c = new Vector2Int(3,-2); int green=0, red=0, both=0;
    for (float t=0;t<64;t+=0.25f){
      var z = TaxiTrafficLights.LightAt(c,true,t,16,2); var x = TaxiTrafficLights.LightAt(c,false,t,16,2);
      if (z==TaxiLight.Green) green++; if (z==TaxiLight.Red) red++;
      if (z==TaxiLight.Green && x==TaxiLight.Green) both++;
    }
    Check(both==0, "the two streets of a crossroad never have green together");
    Check(Mathf.Abs(green-96)<=2 && Mathf.Abs(red-128)<=2, "6 s green, 2 s yellow, 8 s red in a 16 s cycle");
    bool differ = Enumerable.Range(0,20).Any(i=>TaxiTrafficLights.LightAt(new Vector2Int(i,0),true,5,16,2)!=TaxiTrafficLights.LightAt(new Vector2Int(i+1,0),true,5,16,2));
    Check(differ, "crossroads are not all in step");
  }

  static void TestTaxiFares(){
    Check(TaxiPassenger.Rate(TaxiPassengerKind.Normal,0,false,2,2)==5, "smooth ride with perfect stops: 5 stars");
    Check(TaxiPassenger.Rate(TaxiPassengerKind.Normal,2,false,1,1)==3, "two bumps: 3 stars");
    Check(TaxiPassenger.Rate(TaxiPassengerKind.Grumpy,2,false,1,1)==2, "a grumpy passenger minds the bumps more");
    Check(TaxiPassenger.Rate(TaxiPassengerKind.Hurry,0,true,1,1)==3, "late costs two stars");
    Check(TaxiPassenger.Rate(TaxiPassengerKind.Normal,9,true,0,0)==1, "never below 1 star");
    int normal = TaxiPassenger.Fare(TaxiPassengerKind.Normal,200,3,1,1,false);
    Check(TaxiPassenger.Fare(TaxiPassengerKind.Normal,400,3,1,1,false) > normal, "longer rides pay more");
    Check(TaxiPassenger.Fare(TaxiPassengerKind.Vip,200,3,1,1,false) >= normal*3, "VIP pays a lot more");
    Check(TaxiPassenger.Fare(TaxiPassengerKind.Normal,200,5,2,2,false) > TaxiPassenger.Fare(TaxiPassengerKind.Normal,200,3,1,1,false), "good rating and perfect stops tip");
    Check(TaxiPassenger.Fare(TaxiPassengerKind.Hurry,200,3,1,1,false) > TaxiPassenger.Fare(TaxiPassengerKind.Hurry,200,3,1,1,true), "on time pays the hurry tip");
    Check(TaxiPassenger.Fare(TaxiPassengerKind.Normal,200,1,0,0,false) < normal, "an unhappy passenger pays less");
    Check(TaxiCab.StopQuality(0.5f)==2 && TaxiCab.StopQuality(2)==1 && TaxiCab.StopQuality(3)==0, "stop quality by the distance to the middle");
    Check(!string.IsNullOrEmpty(TaxiPassenger.GetName(TaxiPassengerKind.Family)) && TaxiPassenger.RideDistance(TaxiPassengerKind.Tourist).x > TaxiPassenger.RideDistance(TaxiPassengerKind.Normal).y * 0.9f, "names and ride lengths");
  }

  static void TestTaxiPoliceStars(){
    float[] t = {15,45,90,150,230};
    Check(TaxiPolice.StarsFor(0,t)==0 && TaxiPolice.StarsFor(14,t)==0 && TaxiPolice.StarsFor(15,t)==1 && TaxiPolice.StarsFor(100,t)==3 && TaxiPolice.StarsFor(999,t)==5, "heat to stars");
    int fine; float heat, prevHeat=0; int prevFine=0;
    TaxiPolice.GetPenalty(TaxiViolation.RedLight, out fine, out heat);
    Check(TaxiPolice.StarsFor(heat,t)==1, "one red light: one police car");
    TaxiPolice.GetPenalty(TaxiViolation.WrongWay, out prevFine, out prevHeat);
    TaxiPolice.GetPenalty(TaxiViolation.RamPolice, out fine, out heat);
    Check(heat > prevHeat && fine > prevFine, "attacking the police is worse than the wrong lane");
    Check(TaxiPolice.StarsFor(30+35+30, t)==3, "a red light, a crash and another red light: three stars");
    Check(Enum.GetValues(typeof(TaxiViolation)).Cast<TaxiViolation>().All(v=>!string.IsNullOrEmpty(TaxiPolice.GetName(v))), "every violation has a Persian name");
  }

  static void TestTaxiSeats(){
    Check(TaxiDriver.SeatsFor("Van1")==3 && TaxiDriver.SeatsFor("Kenney_SuvLuxury")==3, "vans and SUVs take a family");
    Check(TaxiDriver.SeatsFor("Kenney_Taxi")==2 && TaxiDriver.SeatsFor("Sedan")==2, "a taxi takes two");
    Check(TaxiDriver.SeatsFor("Kenney_Race")==1 && TaxiDriver.SeatsFor("Coupe")==1, "sports cars take one");
  }

  static void TestTaxiShiftAndLevelRules(){
    var early = ECCEndlessMode.ShiftRules(0,1,70); var late = ECCEndlessMode.ShiftRules(600,4,70);
    Check(early.rivals==1 && late.rivals==4 && early.kindWeights[(int)TaxiPassengerKind.Vip]==0, "shift rules: rivals, VIPs only by the timer");
    Check(late.kindWeights[(int)TaxiPassengerKind.Grumpy] > early.kindWeights[(int)TaxiPassengerKind.Grumpy], "the passengers get more demanding during the shift");
    var vip = new ECCLevelData{ objective=ECCObjectiveType.Vip, target=2, rivals=0 };
    var rules = ECCLevelMode.TaxiRulesFor(vip);
    Check(rules.vipInterval > 0 && rules.rivals >= 1 && rules.police, "VIP levels have VIPs and rivals");
    var copy = rules.Clone(); copy.kindWeights[0] = 99;
    Check(rules.kindWeights[0] != 99, "rules are copied, not shared");
    Check(ECCObjectives.IsTaxi(ECCObjectiveType.Passengers) && !ECCObjectives.IsTaxi(ECCObjectiveType.Escape), "taxi goals");
    var level = ECCLevelCatalog.Instance.chapters[0].levels[0];
    Check(level.ObjectiveDescription.Contains("مسافر"), "the first level is about passengers");
  }
}
