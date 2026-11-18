using System; using System.Linq; using UnityEngine; using EndlessCarChase; using EndlessCarChase.Modes; using EndlessCarChase.Levels;
// Tests for the variety update: the driver rank, the follow ride rules, the ride request phone and the city events
public static partial class T {
  static TaxiPassenger MakePassenger(TaxiPassengerKind kind, Vector3 zone){
    var p = new GameObject("P").AddComponent<TaxiPassenger>();
    p.kind = kind; p.state = TaxiPassengerState.Waiting;
    p.pickup = new TaxiSpot{ zone = zone, stand = zone, streetDirection = Vector3.forward };
    p.dropoff = new TaxiSpot{ zone = zone + new Vector3(0,0,200), stand = zone, streetDirection = Vector3.forward };
    p.patience = p.patienceMax = 40;
    return p;
  }
  static void CleanTaxi(){ foreach (var p in TaxiPassenger.All.ToArray()) UnityEngine.Object.Destroy(p.gameObject); TaxiPassenger.All.Clear(); TaxiCab.All.Clear(); }

  static void TestTaxiDriverRank(){
    Check(TaxiDriverRank.Current==0 && TaxiDriverRank.Rides==0 && TaxiDriverRank.Rating==0, "new player: first rank, no rides");
    Check(TaxiDriverRank.RankFor(14,5)==0 && TaxiDriverRank.RankFor(15,3)==1 && TaxiDriverRank.RankFor(15,2.9f)==0, "rank 1 needs 15 rides and a 3.0 rating");
    Check(TaxiDriverRank.RankFor(250,4.5f)==4 && TaxiDriverRank.RankFor(250,4.4f)==3 && TaxiDriverRank.RankFor(49,5)==1, "higher ranks need both rides and rating");
    for (int i=1;i<TaxiDriverRank.Ranks.Length;i++) Check(TaxiDriverRank.Ranks[i].rides>TaxiDriverRank.Ranks[i-1].rides && TaxiDriverRank.Ranks[i].fareBonus>TaxiDriverRank.Ranks[i-1].fareBonus && !string.IsNullOrEmpty(TaxiDriverRank.Ranks[i].perk), "rank "+i+" asks more and gives more");
    Check(!TaxiDriverRank.FollowUnlocked && TaxiDriverRank.ExtraPatience==0 && TaxiDriverRank.FareMultiplier==1, "no perks at the start");
    int risen=-1, events=0; Action<int> onUp = r=>{ events++; risen=r; }; TaxiDriverRank.RankUp += onUp;
    for (int i=0;i<14;i++) TaxiDriverRank.RecordRide(4);
    Check(TaxiDriverRank.Current==0 && events==0, "14 rides: not yet");
    int got = TaxiDriverRank.RecordRide(4);
    Check(got==1 && events==1 && risen==1 && TaxiDriverRank.FollowUnlocked, "15th good ride: rank 1, follow passengers open");
    Check(Mathf.Abs(TaxiDriverRank.Rating-4)<0.001f && TaxiDriverRank.FormatRating(4.26f)=="۴٫۳", "rating average and Persian format");
    Restart();
    Check(TaxiDriverRank.Rides==15 && TaxiDriverRank.Current==1, "rank and rides survive a restart");
    for (int i=0;i<60;i++) TaxiDriverRank.RecordRide(1);
    Check(ECCSave.Data.driverRatings.Count==TaxiDriverRank.RatingWindow && Mathf.Abs(TaxiDriverRank.Rating-1)<0.001f, "only the latest 50 ratings count");
    Check(TaxiDriverRank.Current==1 && events==1, "bad rides lower the rating, never the rank");
    Check(TaxiDriverRank.NextGoal().Contains("امتیاز") && TaxiDriverRank.Progress < 1, "the next rank asks for a better rating");
    for (int i=0;i<50;i++) TaxiDriverRank.RecordRide(5);
    Check(TaxiDriverRank.Current==3 && TaxiDriverRank.ExtraPatience>0 && TaxiDriverRank.VipIntervalMultiplier<1 && TaxiDriverRank.FareMultiplier>1.05f && !TaxiDriverRank.GoldenSign, "125 rides at 5 stars: rank 3, its perks and the ones below");
    TaxiDriverRank.RankUp -= onUp;
    Check(TaxiRankUI.MenuLine().Contains(TaxiDriverRank.CurrentRank.title), "menu line shows the rank");
    TaxiRankUI.Open(); Check(TaxiRankUI.IsOpen, "rank popup opens");
  }

  static void TestTaxiFollowGating(){
    var locked = ECCEndlessMode.ShiftRules(120,2,70,false); var early = ECCEndlessMode.ShiftRules(10,2,70,true); var open = ECCEndlessMode.ShiftRules(120,2,70,true);
    int f=(int)TaxiPassengerKind.Follow;
    Check(locked.kindWeights.Length>f && locked.kindWeights[f]==0, "no follow passengers before the rank opens them");
    Check(early.kindWeights[f]==0 && open.kindWeights[f]>0, "follow passengers after the first half minute");
    Check(open.rideRequests && !ECCLevelMode.TaxiRulesFor(new EndlessCarChase.Levels.ECCLevelData{ objective=ECCObjectiveType.Passengers, target=3 }).rideRequests, "the request phone is for the shift");
    Check(TaxiPassenger.FareMultiplier(TaxiPassengerKind.Follow) > TaxiPassenger.FareMultiplier(TaxiPassengerKind.Tourist) && !string.IsNullOrEmpty(TaxiPassenger.GetName(TaxiPassengerKind.Follow)), "a follow ride pays well");
  }

  static void TestTaxiFollowTracker(){
    var t = new TaxiFollowTracker();
    Check(t.Step(1,30)==TaxiFollowTracker.Result.None && t.Step(1,30)==TaxiFollowTracker.Result.None, "a good distance: nothing happens");
    t.Step(1,5); var r = t.Step(0.7f,5);
    Check(r==TaxiFollowTracker.Result.Spotted && t.TimesSpotted==1, "too close for 1.6 s: seen");
    Check(t.Step(1,5)==TaxiFollowTracker.Result.None && t.Step(1,5)==TaxiFollowTracker.Result.None, "not seen again right away");
    for (int i=0;i<4;i++) t.Step(1,90);
    Check(!t.IsLost && t.LoseTimeLeft<=1.01f, "far for 4 s: still followed, a warning");
    t.Step(1,30); for (int i=0;i<4;i++) t.Step(1,90);
    Check(!t.IsLost, "coming back closer resets the lose timer");
    Check(t.Step(1.1f,90)==TaxiFollowTracker.Result.Lost && t.IsLost, "far for 5 s: lost");
    Check(t.Step(1,5)==TaxiFollowTracker.Result.None, "nothing after it's lost");
  }

  static void TestTaxiRideRequests(){
    CleanTaxi();
    var session = new GameObject("S").AddComponent<TaxiSession>();
    var a = MakePassenger(TaxiPassengerKind.Normal, new Vector3(3,0,20));
    var b = MakePassenger(TaxiPassengerKind.Tourist, new Vector3(3,0,60));
    var follow = MakePassenger(TaxiPassengerKind.Follow, new Vector3(3,0,30));
    Check(session.ToggleAccept(b) && b.accepted && session.Accepted==b && b.patience>=40+TaxiSession.AcceptedPatience-0.01f, "accept: the passenger waits longer");
    Check(session.ToggleAccept(a) && a.accepted && !b.accepted && session.Accepted==a, "one accepted ride at a time");
    Check(session.ToggleAccept(a) && !a.accepted && session.Accepted==null, "tapping again lets it go");
    session.ToggleAccept(b);
    var list = TaxiRideRequestsUI.Requests(session, Vector3.zero, 3);
    Check(list.Count==3 && list[0]==b && list[1]==a, "the accepted request first, then the nearest");
    // A rival ignores the accepted passenger and the follow passenger
    var rivalCar = new GameObject("R").AddComponent<ECCCar>(); rivalCar.transform.position = new Vector3(3,0,0);
    var rival = rivalCar.gameObject.AddComponent<TaxiRival>(); rival.car = rivalCar; rival.seats = 1;
    Check(session.BestPassengerFor(rival)==a, "rivals leave the accepted ride and the follow ride alone");
    var playerCar = new GameObject("D").AddComponent<ECCCar>(); playerCar.transform.position = new Vector3(3,0,0);
    var driver = playerCar.gameObject.AddComponent<TaxiDriver>(); driver.car = playerCar; driver.seats = 2;
    Check(session.BestPassengerFor(driver)==b, "the player's guide goes to the accepted ride");
    float plain = session.FareFor(driver, a), bonus = session.FareFor(driver, b);
    Check(Mathf.Abs(bonus/plain - TaxiSession.AcceptedFareBonus)<0.001f && session.FareFor(rival, b)==1, "an accepted ride pays the bonus, only to the player");
    follow.followLost = true;
    Check(Mathf.Abs(session.FareFor(driver, follow)/plain - TaxiSession.FollowLostFare)<0.001f, "a lost follow ride pays half");
    Check(TaxiRideRequestsUI.ExpectedFare(session, a) > 0 && TaxiRideRequestsUI.ExpectedFare(session, b) > TaxiRideRequestsUI.ExpectedFare(session, a), "expected fares on the phone");
    Check(TaxiRideRequestsUI.RowsFor(1080,1920)==3 && TaxiRideRequestsUI.RowsFor(1080,2400)==3 && TaxiRideRequestsUI.RowsFor(1536,2048)==1, "the phone fits short screens");
    var family = MakePassenger(TaxiPassengerKind.Family, new Vector3(3,0,80));
    typeof(TaxiSession).GetProperty("Player").SetValue(session, driver);
    Check(!session.CanAccept(family) && !session.ToggleAccept(family) && session.Accepted==b, "a family that doesn't fit can't be accepted");
    b.state = TaxiPassengerState.Leaving; int gaveUp=0; Action<TaxiPassenger> onGone = p=>gaveUp++; TaxiSession.AcceptedGaveUp += onGone;
    typeof(TaxiSession).GetMethod("CheckAccepted", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(session,null);
    TaxiSession.AcceptedGaveUp -= onGone;
    Check(session.Accepted==null && gaveUp==1, "an accepted passenger who gave up is reported once");
    CleanTaxi();
  }

  static void TestTaxiCityEvents(){
    bool neverRepeats = true; var seen = new System.Collections.Generic.HashSet<TaxiCityEventType>();
    foreach (TaxiCityEventType last in Enum.GetValues(typeof(TaxiCityEventType))) for (float roll=0; roll<=1; roll+=0.05f){ var n = TaxiCityEvents.PickNext(last, roll); if (n==last || n==TaxiCityEventType.None) neverRepeats=false; seen.Add(n); }
    Check(neverRepeats && seen.Count==4, "events: never the same twice in a row, all four happen");
    Check(Enum.GetValues(typeof(TaxiCityEventType)).Cast<TaxiCityEventType>().Where(e=>e!=TaxiCityEventType.None).All(e=>!string.IsNullOrEmpty(TaxiCityEvents.GetName(e)) && !string.IsNullOrEmpty(TaxiCityEvents.GetDescription(e))), "every event has a Persian name and text");
    var w = ECCEndlessMode.ShiftRules(60,2,70,true).kindWeights; var tour = (float[])w.Clone();
    TaxiCityEvents.ModifyWeights(TaxiCityEventType.TouristTour, tour);
    Check(tour[(int)TaxiPassengerKind.Tourist] > w[(int)TaxiPassengerKind.Tourist]*3 && tour[(int)TaxiPassengerKind.Vip] > 0, "tourist tour: many tourists and a VIP or two");
    var r = (float[])w.Clone(); TaxiCityEvents.ModifyWeights(TaxiCityEventType.RushHour, r); Check(r.SequenceEqual(w), "other events keep the passengers");
    var ev = new GameObject("E").AddComponent<TaxiCityEvents>();
    Check(ev.Active==TaxiCityEventType.None && ev.FineMultiplier==1 && ev.FareMultiplier==1, "no event at the start");
    ev.Tick(ev.firstDelay+0.1f);
    Check(ev.Active!=TaxiCityEventType.None && ev.TimeLeft>0, "the first event after the first delay");
    ev.Begin(TaxiCityEventType.Crackdown);
    Check(ev.FineMultiplier==2 && ev.HeatMultiplier>1 && ev.FareMultiplier==1, "crackdown: double fines");
    ev.Begin(TaxiCityEventType.RushHour);
    Check(ev.FareMultiplier==1.5f && ev.FineMultiplier==1, "rush hour: fares x1.5");
    ev.Tick(ev.duration+0.1f);
    Check(ev.Active==TaxiCityEventType.None && ev.FareMultiplier==1 && ev.Last==TaxiCityEventType.RushHour, "the event ends after its time");
    // ( The pause between two events is random; the stub's Random.Range always gives 0, so it isn't checked here )
    UnityEngine.Object.Destroy(ev.gameObject);
  }
}
public static partial class T {
  static void TestTaxiSharedRide(){
    var from = new Vector3(3,0,0);
    Check(TaxiSharedRide.AreCompatible(from, new Vector3(3,0,300), new Vector3(45,0,320)), "two destinations the same way: shared");
    Check(!TaxiSharedRide.AreCompatible(from, new Vector3(3,0,300), new Vector3(3,0,-300)), "opposite ways: not shared");
    Check(!TaxiSharedRide.AreCompatible(from, new Vector3(300,0,0), new Vector3(3,0,300)), "at a right angle: not shared");
    CleanTaxi();
    var carObj = new GameObject("D").AddComponent<ECCCar>(); carObj.transform.position = from;
    var driver = carObj.gameObject.AddComponent<TaxiDriver>(); driver.car = carObj; driver.seats = 2;
    var a = MakePassenger(TaxiPassengerKind.Normal, from); a.dropoff.zone = new Vector3(3,0,300); a.state = TaxiPassengerState.Riding; driver.riding.Add(a);
    var b = MakePassenger(TaxiPassengerKind.Normal, new Vector3(3,0,20)); b.dropoff.zone = new Vector3(45,0,330);
    var c = MakePassenger(TaxiPassengerKind.Normal, new Vector3(3,0,20)); c.dropoff.zone = new Vector3(3,0,-280);
    Check(TaxiSharedRide.FitsRiding(driver, b) && !TaxiSharedRide.FitsRiding(driver, c), "the phone and the hint know who goes the same way");
    var f = MakePassenger(TaxiPassengerKind.Follow, new Vector3(3,0,20)); f.dropoffHidden = true;
    Check(!TaxiSharedRide.FitsRiding(driver, f), "a follow ride is never shared");
    b.state = TaxiPassengerState.Riding; driver.riding.Add(b);
    Check(TaxiSharedRide.OnBoarded(driver, b) && a.shared && b.shared, "boarding the second one makes a shared ride");
    var session = new GameObject("S").AddComponent<TaxiSession>();
    var lone = MakePassenger(TaxiPassengerKind.Normal, from);
    Check(Mathf.Abs(session.FareFor(driver, b)/session.FareFor(driver, lone) - TaxiSharedRide.FareBonus) < 0.001f, "shared riders pay the bonus");
    driver.riding.Clear(); CleanTaxi();
  }

  static void TestTaxiWishes(){
    Check(TaxiWish.PickFor(TaxiPassengerKind.Follow,0.5f)==TaxiWishType.None && TaxiWish.PickFor(TaxiPassengerKind.Hurry,0.5f)==TaxiWishType.None, "follow and hurry passengers have no wish");
    Check(TaxiWish.PickFor(TaxiPassengerKind.Grumpy,0.9f)==TaxiWishType.Gentle && TaxiWish.PickFor(TaxiPassengerKind.Tourist,0.1f)==TaxiWishType.Stop, "a grumpy one wants a gentle ride, a tourist a photo stop");
    var kinds = new System.Collections.Generic.HashSet<TaxiWishType>(); for (float r=0;r<1;r+=0.05f) kinds.Add(TaxiWish.PickFor(TaxiPassengerKind.Normal,r));
    Check(kinds.Count==3, "normal passengers ask all three");
    foreach (TaxiWishType t in new[]{TaxiWishType.Hurry,TaxiWishType.Gentle,TaxiWishType.Stop}) Check(!string.IsNullOrEmpty(TaxiWish.Quote(t,TaxiPassengerKind.Normal)) && !string.IsNullOrEmpty(TaxiWish.Explain(t)), "Persian text for "+t);
    var h = TaxiWish.Create(TaxiWishType.Hurry, 180);
    Check(Mathf.Abs(h.timeLeft-(180/TaxiWish.HurrySpeed+TaxiWish.HurryExtra))<0.01f, "hurry time from the distance left");
    h.Step(5,1,false,false); h.Arrive(); Check(h.state==TaxiWishState.Done && h.Stars==1, "hurry: in time is a star more");
    h = TaxiWish.Create(TaxiWishType.Hurry, 90); h.Step(30,1,false,false); h.Arrive(); Check(h.state==TaxiWishState.Failed && h.Stars==-1, "hurry: late is a star less");
    var g = TaxiWish.Create(TaxiWishType.Gentle, 300); for (int i=0;i<16;i++) g.Step(1,0.5f,false,false);
    Check(g.state==TaxiWishState.Done, "gentle: slow for 15 s");
    g = TaxiWish.Create(TaxiWishType.Gentle, 300); g.Step(1,0.5f,false,false); g.Step(1,1,false,false); g.Step(1,1,false,false); g.Step(1,1,false,false);
    Check(g.state==TaxiWishState.Failed, "gentle: too fast for more than 2.5 s fails");
    g = TaxiWish.Create(TaxiWishType.Gentle, 300); g.Step(1,1,false,false); g.Step(1,0.3f,false,false); Check(g.IsActive, "gentle: a short burst is forgiven");
    g.Step(0.1f,0.3f,true,false); Check(g.state==TaxiWishState.Failed, "gentle: a bump fails");
    g = TaxiWish.Create(TaxiWishType.Gentle, 300); g.Step(3,0.3f,false,false); g.Arrive(); Check(g.state==TaxiWishState.Done, "gentle: arriving gently counts");
    var s = TaxiWish.Create(TaxiWishType.Stop, 300); s.Step(1,0,false,true); s.Step(0.3f,1,false,false); s.Step(1,0,false,true);
    Check(s.IsActive, "stop: the stand must be 1.5 s in one go");
    s.Step(0.6f,0,false,true); Check(s.state==TaxiWishState.Done, "stop: standing in the ring keeps the wish");
    s = TaxiWish.Create(TaxiWishType.Stop, 300); s.Arrive(); Check(s.state==TaxiWishState.Failed, "stop: skipped on the way is a star less");
    s = TaxiWish.Create(TaxiWishType.Stop, 300); s.Step(41,1,false,false); Check(s.state==TaxiWishState.Failed, "stop: too late");
    var done = TaxiWish.Create(TaxiWishType.Gentle, 300); done.Step(16,0,false,false); done.Arrive(); Check(done.state==TaxiWishState.Done, "a settled wish stays settled");
    Check(ECCEndlessMode.ShiftRules(60,2,70,true).passengerWishes, "wishes come in the shift");
  }
}
