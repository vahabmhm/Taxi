# UNITY MASTER DEVELOPMENT AGENT
## Thirteen Integrated Features — Sequential Implementation, Persian UI, RTL Support

You are a senior Unity developer, gameplay systems architect, mobile UI/UX designer, game-economy designer, and advertising integration engineer. Inspect the actual Unity project and implement the thirteen systems below. Preserve existing gameplay, assets, progression, and player saves. Deliver working implementations, not pseudocode or unconnected scripts.

# GLOBAL RULES

1. Analyze before editing. Identify Unity version, scenes, prefabs, gameplay, save systems, installed SDKs, fonts, UI frameworks, and existing features.
2. Reuse and extend working implementations rather than duplicating managers, wallets, save services, UI screens, or ad services.
3. Keep all changes compatible with the project's Unity version, architecture, screen sizes, and art direction.
4. Do not change unrelated functionality, invent SDK APIs or ad credentials, erase player progress, or silently overwrite user changes.
5. New player-facing text MUST be Persian; detailed RTL instructions below override any existing-language convention. Preserve existing older texts unless integration requires a change.
6. Prefer configurable ScriptableObjects/settings, shared services, reliable persistence, event-driven UI updates, and lightweight mobile animations.
7. Implement and connect code, scene objects, prefabs, UI elements, assets, and event handlers, where environment access permits. Document any Unity Editor/manual-only step precisely.
8. Never claim a feature or test is complete unless it has actually been implemented or executed. Final reports must be in Persian.

# MANDATORY PERSIAN LANGUAGE AND RTL SUPPORT

All NEW player-facing text across ALL THIRTEEN tasks MUST be in natural, fluent Persian (Farsi), regardless of the original game's language. This requirement applies to page titles, missions, challenges, instructions, buttons, status labels, notifications, power-ups, reward results, NoAds messages, catalog labels, and error messages.

- Make new UI screens properly right-to-left (RTL), with correct reading order, alignment, padding, and layout flow.
- Use a Persian-capable font. Reuse an existing compatible font when possible; if missing, use a properly licensed compatible alternative and document any import steps.
- Verify Arabic/Persian shaping and joining, bidirectional text, mixed Persian/Latin words, numeric values, icon placement, and TextMeshPro/UI rendering.
- Use project-supported RTL/shaping solutions where necessary, avoiding unnecessary new packages. Do not reverse strings naively.
- Use appropriate alignment and anchoring without automatically mirroring decorative graphics or spinning wheel direction.
- Ensure numerals, coin icons, values, and buttons render clearly on various mobile aspect ratios.
- Keep technical identifiers, SDK names, package names, and code symbols in their original spelling; UI should not expose English placeholders.
- Progress summaries and final developer reports must also be written in Persian.

Required new UI wording (adapt minor phrasing only for natural Persian):

| English meaning | Mandatory Persian UI text |
| --- | --- |
| Today's Missions | مأموریت‌های امروز |
| Daily Challenge | چالش روز |
| Start Today's Challenge | شروع چالش امروز |
| Not Today | امروز چالش نمی‌خوام |
| Not Completed | تکمیل نشده |
| Ready to Claim | آماده دریافت |
| Claimed | دریافت شده |
| You Won! | برنده شدی! |
| Other Games | سایر بازی‌ها |
| Lucky Wheel | گردونه شانس |
| Spin | چرخاندن گردونه |
| Watch Video | تماشای ویدئو |
| Double Reward | دوبرابر کردن جایزه |
| No Ads Available | تبلیغی موجود نیست |

# MAIN MENU VISUAL DESIGN (APPLIES TO ALL TASKS)

Several tasks add main-menu entries (Daily Missions, Other Games, Lucky Wheel, Login Rewards, Weekly League, coin balance). The main menu must stay clean, readable and attractive despite these additions.

## Fewer icons, clean layout

- Keep the main menu **visually light**: do not put a large decorative icon on every button and do not fill the screen with icon grids or floating icons.
- Buttons are **text-first** (Persian label), with **at most one small, simple icon** per button only where it truly helps recognition. Badges (Task 2), lock state (Task 13) and the coin balance (Task 1) are functional indicators and are allowed.
- Keep one clearly dominant primary action (e.g., Play / «شروع بازی»). Secondary entries use a consistent, slightly smaller style and must not compete with the primary button.
- Do not add new decorative icons, stickers or background clutter to the main menu. Reuse existing art; remove or simplify redundant icons only when this does not break existing functionality or scene bindings, and document what was changed.
- Keep the layout non-overlapping, safe-area aware and scrollable if needed (see Final Integration Audit).

## Glossy buttons with a light-sweep (shine) effect

- Main-menu buttons must look **glossy/shiny**: a subtle top highlight/gloss layer that matches the game's art direction (no harsh, cheap-looking gradients).
- Add a **light-sweep effect**: a soft diagonal light streak periodically passes across each main-menu button from one side to the other.
  - Duration of one sweep about **0.6–0.9 s**, repeated every **3–5 s** (configurable).
  - **Stagger** the sweep between buttons so they do not all flash at the same moment; the primary Play button may sweep slightly more often.
  - The streak must be clipped to the button shape (e.g., UI `Mask`/`RectMask2D` with a moving gradient image, or a lightweight UI shader compatible with the project's render pipeline). It must not leak outside the button, must not change the button's size or layout, and must not block or alter its hit area/raycasts.
- Implement this as **one reusable component/prefab style** (e.g., `MenuButtonShine`) used by every main-menu button, not separate copies per button.
- Performance: use cheap tweens/coroutines or a shader time parameter, no per-frame allocations, no heavy post-processing. Pause the effect when the menu is hidden, during popups that cover the menu, and when the app is in the background. Provide a config option to reduce/disable it on low-end devices.
- Pulse animations required by other tasks (e.g., Daily Missions badge pulse, Task 13 first-open pulse) must still work and remain visually distinct from the shine sweep.
- **When to implement:** create this shared style the first time a task adds or modifies a main-menu button (normally Task 2), apply it to the existing main-menu buttons at that time, and reuse it for every later main-menu button. Record it in `CODEX_PROGRESS.md`.

# CRITICAL: ONE TASK AT A TIME / RESUMABLE WORK

Thirteen tasks must be processed IN THIS ORDER:

1. Coin Economy
2. Daily Missions
3. Daily Challenge
4. Other Games Screen
5. Unified Adivery + Tapsell Mediation Ads
6. Professional Power-Ups
7. Daily Lucky Wheel
8. Daily Other-Game Recommendation Popup
9. Ten-Day Consecutive Login Rewards
10. Pre-Ad Loading Popup
11. Win/Loss Result Screens Upgrade
12. One-Time Bazaar / Myket Review Prompt (Android Intent)
13. Weekly Star League (Simulated Weekly Leaderboard)

**DO NOT implement multiple major tasks simultaneously.** Finish the current task to a safe, working checkpoint, run all available relevant validation, fix blocking errors, update progress, and only then proceed. This avoids leaving many partially implemented features if Codex usage, context, or execution time runs out.

Create and maintain `CODEX_PROGRESS.md` in the project root. It MUST record:
- Project discovery and current architecture.
- Each task's status: `NOT_STARTED`, `IN_PROGRESS`, `BLOCKED`, or `COMPLETE`.
- Active task and exact next action.
- Completed changes and specific scripts/scenes/prefabs modified.
- Verified tests with results; unverified Editor/device checks separately.
- Known blockers, external credentials/assets needed, and manual actions.

At the beginning of EVERY subsequent Codex session:
1. Read `AGENTS.md` and `CODEX_PROGRESS.md`.
2. Inspect actual project/repository state, including unfinished or user modifications.
3. Verify recorded task statuses against implemented code/assets.
4. Resume the earliest incomplete task from the latest safe checkpoint, rather than starting over.
5. Do not duplicate earlier work or restart completed tasks.

Commit coherent, tested changes to Git when the environment and repository rules permit. Never reset, clean, force-push, or overwrite unrelated work. If a task is blocked by inaccessible external assets/services, implement all safe testable work, mark `BLOCKED` with an exact reason, and proceed only to an independent task when no conflicts will result. Do not mislabel blocked or untested work as complete.

# INITIAL PROJECT DISCOVERY

Inspect Unity version, rendering pipeline, game loop, menus, scenes, wins/losses, gameplay events, progression, wallets, missions, challenges, power-ups, rewards, persistence, animations, localization, installed advertising SDKs, and existing other-games/wheel screens. Check whether previous `connectball` / `connectballs` project files are actually accessible in this workspace; past conversations alone do NOT imply file access. Record findings and integration plan in `CODEX_PROGRESS.md` before Task 1.

# TASK 1 — COIN ECONOMY

If the game lacks an economy, implement a centralized persistent coin system. If one exists, integrate and improve it safely rather than adding a second wallet.

Requirements:
- Persistent coin balance, add/spend/check operations, nonnegative balances, save/load, migration safety.
- Idempotent coin reward transactions with unique reward IDs to avoid double payouts.
- Observable balance changes and event-driven UI, no unnecessary per-frame polling.
- Display `[coin icon] [formatted balance]` in appropriate screens: main menu, levels/shop when relevant, daily missions, challenge, power-ups, lucky wheel.
- Reuse existing coin sprite and UI style; ensure correct Persian/RTL layout, thousands separators, and responsive placement.
- Shared configurable coin-reward amounts, connected to actual game win/progression events when appropriate.
- Reusable floating reward animation: `+X` and a coin icon, gentle scale/float/fade over approximately two seconds, balance refresh.
- Reuse current save architecture. Do not reset existing balances.

## Mandatory shared coin-balance button and rewarded-coin popup

Every screen that displays the player's coin balance MUST use **one identical reusable coin-balance button/prefab**, not separate, visually inconsistent implementations. Replace or refactor existing balance UI carefully to use the shared component without breaking its scene bindings.

Shared button appearance:
- Use one consistent container design, dimensions, spacing, colors, icon size, animation, and layout across all scenes (only adapt anchoring/safe-area placement where necessary).
- Show a recognizable **coin icon**, and immediately beside it the player's current **formatted numeric coin balance**.
- Render the balance number in the **Vizari (ویزری) font** requested for the game. First find the actual Vizari font asset in the project and use it consistently in all balance components; if it is missing, do not substitute a different font while claiming it is Vizari—record the missing font and ask the developer to supply/import it. Use a temporary clearly documented fallback only if required to keep the UI functional.
- Add a distinct **plus (+) button at the LEFT edge of the coin icon / balance container**, visually overlapping the boundary so **half of the plus button is inside the balance container and half is outside**. Keep it visible, clickable, correctly layered, and within mobile safe areas; do not clip the outside half with masks or parent layout groups.
- The balance container and the plus control must BOTH open the same coin-reward popup when tapped. Avoid click-through, double opening, and overlapping hit-target problems. For Persian RTL UI, explicitly preserve the specified left-edge plus placement rather than accidentally mirroring it to the right.
- Subscribe the component to centralized balance-change events so the number updates immediately on every screen and on scene transitions; do not duplicate wallet state or poll every frame.

Coin-reward popup:
- Open a visually consistent, Persian RTL popup explaining that **watching a rewarded video grants free coins**. Example copy: **«با تماشای ویدئو، سکه جایزه بگیر!»**.
- Clearly show the **exact number of coins offered**, a coin icon, a **«تماشای ویدئو»** action, and a close button. Display loading/unavailable states and prevent repeated taps while an ad request is pending.
- The rewarded-coin amount MUST be **configurable per game** and selected based on this game's mechanics, difficulty, progression, earning rates, and power-up prices; do not hardcode a universal value across all games. Keep the amount consistent between the UI offer and actual payout for each request.
- Route ad playback through the project's existing `adsmanagernew.cs` and the unified rewarded-video provider/fallback behavior specified in Task 5. Task 1 may build the popup and a clean integration point; complete missing provider wiring in Task 5 without creating a second ad manager.
- Award the offered coins **only after a verified rewarded-ad completion callback**; failed, skipped, cancelled, or unavailable ads never pay coins. Use centralized idempotent coin transactions, guard against duplicate SDK callbacks, save the credited balance, update all balance buttons, and show the existing `+X` coin feedback.
- If neither Adivery nor Tapsell Mediation can provide the ad, show the project's shared **NoAds** panel (or the safe documented placeholder until Task 5 makes the shared asset available). Do not fake ad completion.
- Respect any existing ad frequency limits and cooldowns; do not invent a universal daily quota unless explicitly configured for the game. Test opening from every relevant screen, visual consistency, RTL, Vizari font rendering, plus-button overlap, reward flow, duplicate callbacks, and provider-unavailable behavior.


## Mandatory coin economy balancing and reward/cost rebalance

Do not merely add coin rewards and prices independently. **Audit and rebalance the complete coin economy as one connected system** so the player earns enough to feel rewarded, but does not accumulate so many coins that spending and optional rewarded ads become irrelevant.

### 1. Audit the full economy before changing values

- Inspect every existing and planned coin source: level wins, stage progression, daily missions, Daily Challenge, daily login calendar, lucky wheel, achievements, gifts, rewarded-video coin popup, win-screen 2X bonuses, and any other reward path.
- Inspect every coin sink: power-up purchases/uses, continue or revive purchases if applicable, level skips, shop items, retries, upgrades, and any other spending path.
- Read the actual gameplay pace, average session length, level difficulty, expected daily play frequency, and the usefulness/frequency of each purchasable item.
- Identify duplicate rewards, overly generous payouts, low-cost/high-value purchases, and any infinite or easily repeatable coin farming loops.
- Record the original values and the proposed revised values in `CODEX_PROGRESS.md` (or a dedicated economy-balance table/config) so changes are transparent and tunable.

### 2. Establish a per-game economy model

- Build a **per-game configurable economy table**; do not copy identical coin values between unrelated games.
- Define a reasonable expected daily coin income for a typical active player, based on normal gameplay plus achievable missions/challenges/login rewards.
- Define the expected daily/weekly spending opportunities and prices so coins have meaningful uses throughout progression.
- Tune rewards and costs together. If a source is increased, check whether related prices or other rewards also need adjustment; if a sink becomes cheaper, check whether it causes excessive purchasing or removes the value of coins.
- Keep early-game rewards welcoming, then scale rewards and prices gradually with progression/difficulty. Avoid sudden inflation, huge unexplained reward jumps, or prices that become impossible for ordinary players.
- Preserve meaningful rewards for skill and progress. Do not reduce all gameplay rewards simply to force ad views.

### 3. Encourage approximately 1–2 optional rewarded ads per day

The intended design target is to make **about one or two rewarded videos per active day attractive and useful**, not mandatory. Use the shared rewarded-coin popup and other existing rewarded opportunities as optional top-ups.

- Set the rewarded-coin offer per game so it is noticeable and worthwhile, but not so large that one ad floods the wallet or makes normal coin sinks irrelevant.
- As an initial tuning guideline, a single rewarded-coin ad may provide roughly **10–20% of a typical active player's expected daily coin spending**, and one or two ads together should provide a helpful top-up rather than the majority of all progression income. Adjust this range based on the actual game's session length, economy and testing; it is a starting guideline, not a universal fixed rule.
- Make the offer context-aware where appropriate (for example, a player close to a useful power-up purchase may find the offer more relevant), but never misrepresent the amount or change it after the player accepts the ad.
- Do not block ordinary gameplay, essential progression, or a reasonable path to earn coins behind advertisements. A player who chooses not to watch ads must still be able to progress through normal play, though it may take longer to accumulate optional purchases.
- Do not add forced ads or automatically open rewarded ads. The player must explicitly choose to watch each rewarded video.
- Respect any existing provider/ad frequency limits. Do not introduce a hard daily ad quota unless the project already requires one or the developer explicitly configures it. The 1–2 ads/day target is an economy-design goal, not permission to force or repeatedly prompt the player.

### 4. Prevent coin inflation and preserve long-term value

- Ensure the total expected coin income from all daily systems is considered together; do not balance missions, wheel, login rewards, challenge rewards and level rewards in isolation.
- Check best-case farming behavior, repeated easy levels, retries, duplicate callbacks, repeated claims, offline clock changes, and save/load exploits.
- Use idempotent transaction IDs for every reward and reliable spend transactions for every purchase.
- Keep wallet balance nonnegative. A failed purchase must not consume coins; a failed or skipped ad must not grant coins.
- Review whether high-value purchases (especially power-ups and level skips) remain meaningful after daily rewards and ad bonuses are included.
- Avoid excessive stockpiling: tune recurring sources and useful sinks so regular play naturally creates spending decisions without making the player feel punished for saving.
- Do not silently wipe or arbitrarily devalue existing player balances. If the current economy requires migration, preserve existing balances and document a safe migration strategy.

### 5. Test and tune with representative player profiles

Create a simple economy simulation or spreadsheet/table using the game's actual configurable values and estimate at least:

- A casual player who plays briefly and watches no ads.
- A regular player who completes normal gameplay and daily rewards but watches no ads.
- A regular player who voluntarily watches about one rewarded ad per day.
- A highly active player who voluntarily watches about two rewarded ads per day.

For each profile, estimate daily coin income, typical spending, remaining balance, and how long it takes to afford common power-ups/optional skips. Adjust values if the no-ad player cannot reasonably progress or if the two-ad player accumulates excessive unused coins.

Validate the economy at early, middle and late progression. Record the final per-game values, assumptions, and any remaining tuning risks. If real player analytics are unavailable, clearly label the model as an estimate and make the values easy to tune after release; do not claim that the economy is empirically balanced without data.

Verify app restarts, scene changes, reward duplication, insufficient balance, spending and UI synchronization. Also validate the complete coin source/sink audit, per-game reward/cost rebalance, representative no-ad / one-ad / two-ad player profiles, and absence of trivial coin-farming exploits. Save the economy assumptions and final tuning values in `CODEX_PROGRESS.md`. Save progress before Task 2.

# TASK 2 — DAILY MISSIONS

Inspect existing missions first. Create or refine **exactly 25** achievable mission definitions adapted to REAL gameplay: 10 easy, 9 medium, 6 hard. Each definition includes unique stable ID, Persian title/description, difficulty, event/objective type, target, progress and configurable coin reward. Use data-driven definitions such as ScriptableObjects.

Daily selection:
- Randomly choose **4 or 5** distinct missions each day; mix easy, medium, and hard whenever valid.
- Only select mechanics currently supported and achievable for the player.
- Save selected IDs, progress, and claimed flags; maintain a stable set throughout the day.
- Use centralized calendar-day reset and reasonable clock-rollback protection. Use trusted server time when existing infrastructure provides it; local-only time cannot be fully tamper-proof.

Gameplay tracking must listen to real game events (wins, level completion, score, moves, etc.), avoid duplicate counts, and persist valid progress.

Create a Persian RTL **«مأموریت‌های امروز»** screen displaying 4–5 rows: Persian mission text, progress/goal, reward with coin icon, and three button states: **«تکمیل نشده»** (disabled), **«آماده دریافت»** (claimable), **«دریافت شده»** (disabled). Claim with centralized idempotent coin transaction, show reward animation, save and update immediately.

Add a matching Daily Missions button BELOW the other main-menu buttons, or position gracefully within a scrollable menu if necessary. Put a small top-right badge showing the exact number of completed, unclaimed missions. When count > 0, loop a subtle scale pulse WITHOUT changing neighboring button layout; stop immediately when count returns to zero. Badge hidden when zero.

Verify all 25 valid objectives, random daily selection, resets, persistence, progress, claiming, badge and pulse before Task 3.

# TASK 3 — DAILY CHALLENGE

Create one challenge definition per calendar day. **Do not automatically show the popup on the FIRST EVER app launch after installation.** Starting with the next eligible day, automatically open the popup once per day at the first safe main-menu entry; do not re-open automatically that day after dismissing.

Popup is Persian RTL and titled **«چالش روز»**; explain that completing today's challenge earns coins and show reward and two buttons **«شروع چالش امروز»** and **«امروز چالش نمی‌خوام»**. Declining closes it; accepting starts a dedicated daily challenge mode.

If the actual game supports procedural generation, generate a valid seeded randomized challenge; otherwise choose a suitable existing level. Avoid impossible or locked content; preserve normal campaign progression. Replace normal level labels temporarily with **«چالش روز»** and restore original labels afterward.

Set a configurable, achievable gameplay-specific time limit with visible countdown; handle pause/resume, backgrounding, wins, losses and timeouts safely. Default: retries allowed after failure during the same day, but a successful reward can only be collected once daily.

## Failure screen — rewarded second chance (+30 seconds)

When the player loses the daily challenge, especially when its timer expires:
- Show a Persian RTL failure screen/popup matching the game's visual style.
- Add a prominent rewarded-video button, for example **«تماشای ویدئو و دریافت ۳۰ ثانیه فرصت دوباره»**, with a video/play icon and a clear **+۳۰ ثانیه** indicator.
- Allow **at most ONE successful rewarded continuation (+30 seconds) per player per calendar day**, across all attempts of today’s challenge. This is a strict daily limit, not configurable upward. Ordinary manual retries do not reset or replenish it. Once used, hide or disable the revive button for the remainder of that calendar day, even if the challenge is lost again.
- Before showing the failure screen, preserve the **exact active challenge session** (same puzzle/level, seed, board, score, moves, progress, and any relevant gameplay state). Enter a paused, resumable state instead of destroying/restarting the level.
- If the player chooses the video option, request a Rewarded Video through the shared advertising integration (the existing `adsmanagernew.cs` entry point, unified in Task 5).
- **Only after the actual SDK issues a verified rewarded-completion callback**, close the failure popup, restore that same challenge state, add **30 seconds** to the remaining challenge time (normally 0 after a timeout), refresh the visible countdown, and resume gameplay. Do **NOT** restart or regenerate the level.
- If the failure resulted from another game-over condition, only offer continuation if the original gameplay rules can be safely resumed; restore a valid resumable game state without granting an unrelated gameplay advantage.
- If the player skips/closes the video, the ad fails, the ad isn't ready, or either ad provider cannot supply it, do **not** grant extra time or consume the revive. Follow the shared provider fallback policy (Adivery ↔ Tapsell Mediation) and display the existing **NoAds** panel if both are unavailable.
- Disable the button while an ad request is in progress. Prevent multiple ad presentations, simultaneous resumes, duplicated +30 seconds, and duplicate reward callbacks. Persist the used-revive entitlement and session state as safely as the existing architecture permits; if an app interruption cannot restore the live gameplay state, handle it gracefully and do not claim a seamless resume was implemented.
- Preserve the default ability to retry a failed daily challenge normally, without resetting today's definition or generating additional coin rewards.
- If exact in-place continuation is unsupported by the current game architecture, implement the safest feasible resumable-state integration and explicitly report the remaining limitation rather than pretending a restart is a continuation.

## Victory screen — rewarded 2X coin button

On victory:
- Show Persian **«برنده شدی!»** popup.
- Play mobile-friendly confetti.
- Show won coin amount and floating centered `+X` plus coin icon for a couple seconds.
- Credit the **base coin reward exactly once**, even if the player declines the ad or closes the screen.
- Add a top-right close button.
- Add a **prominent, clickable 2X rewarded-video button** on the victory screen, with a video icon, large **2X** label and Persian text such as **«تماشای ویدئو و دوبرابر کردن جایزه»**. Clearly display the base reward and the resulting doubled total.
- Allow **at most ONE successful 2X rewarded-video bonus per player per calendar day**, independent of the +30-second revive limit. After it is claimed, disable or replace the 2X button with a Persian claimed state for the rest of that calendar day. When pressed, use the existing rewarded advertising interface; after **verified successful rewarded video completion only**, credit an **additional bonus equal to the base reward**. Example: 100 base + 100 ad bonus = 200 total. Update the displayed balance and result feedback immediately.
- Never re-credit the base payout, never grant the 2X bonus on skipped/failed/unavailable ads, and never grant the bonus twice (including after repeated taps, scene changes, popup closing, app backgrounding or duplicate callbacks).
- While loading/showing an ad, disable the 2X button and restore it if the request fails and the bonus remains eligible; once successfully claimed, change it to an appropriate Persian claimed state or hide it.
- If the preferred provider cannot display the video, automatically try the other provider via Task 5's rotation/fallback rules. If neither is available, show **NoAds** and preserve the player's original base reward.
- Where appropriate, let the player reopen today's victory result to claim an unclaimed 2X bonus later, without replaying the base reward.

## Strict daily usage limits for both video options

- Maintain **two separate, persistent daily-use flags** keyed to the same consistent calendar-day/reset policy: `dailyChallengeReviveUsed` and `dailyChallengeDoubleRewardUsed` (or equivalent fields). Do not use one shared flag for both features.
- The player may successfully use **the +30-second second chance ONCE per day** and **the 2X victory reward ONCE per day**. Using one does not consume or prevent using the other.
- Only mark the relevant daily-use flag as consumed after the advertising SDK confirms the rewarded completion and the corresponding effect has been successfully applied and durably recorded. Guard against double callbacks and interruptions with idempotent operations / transaction IDs.
- If the ad is unavailable, skipped, cancelled, or fails, do not consume that feature's daily allowance. Let the player try again while the feature remains eligible.
- Both flags must survive app restarts and scene changes. Reset eligibility only on the next valid calendar day, using the shared daily reset logic and its clock-change safeguards. Never reset either allowance on replay, popup reopening, loss, or victory.
- If the user has already used today's revive, display a Persian exhausted state such as **«فرصت مجدد امروز استفاده شده»** and never show a second rewarded continuation for that day.
- If the user has already claimed today's 2X bonus, display a Persian claimed state such as **«جایزه دوبرابر امروز دریافت شده»** and do not show another available 2X reward that day.
- Add explicit tests for: first use succeeds; second use on the same day is blocked; using revive does not block 2X; using 2X does not block revive; app restart does not restore consumed allowances; unsuccessful ads do not consume allowances; next eligible day restores each allowance.

Use the same shared rewarded-ad entry point for **both the failure-screen +30-second continuation and the victory-screen 2X**. In Task 3, integrate with the existing ad service if available, otherwise create a clean adapter contract for Task 5; never use fake rewarded callbacks in production or import missing ad SDKs automatically. Persist installation/popup dates, challenge seed/ID, completion, base claim, 2X bonus claim, and daily rewarded-continuation usage.

Verify first-install behavior, subsequent popup schedule, challenge mode/title, timer, failure popup, verified-ad +30-second in-place continuation, unchanged gameplay/board state, ad-unavailable NoAds behavior, single-use protection, victory popup, base coin payout, verified-ad 2X button, confetti, duplicate callback resistance, persistence, and regular campaign progression before Task 4.

# TASK 4 — OTHER GAMES SCREEN

Build or improve a Persian RTL **«سایر بازی‌ها»** screen, opened through a button styled like the existing main-menu buttons.

Use a **two-column scrollable grid** with an appropriate vertical scrollbar and mobile-responsive layout. Every game card has:
1. Cafe Bazaar game icon.
2. Game title.
3. Smaller-font, short game description underneath title.
4. Tap action opening the correct Cafe Bazaar listing.

Use the **real package name** as stable identifier to match the correct Cafe Bazaar title, icon, description, and listing. Exclude the current game and remove duplicates. Look for the developer's game catalog/package names in accessible project or configuration files. **Also search the actually accessible previous `connectballs` project (or `connectball` if that is the directory name) for its Other Games list/catalog, package names, titles, descriptions, icons, and Cafe Bazaar links. Prefer adapting verified catalog entries from that project rather than rebuilding the list.** Copy only compatible data and approved assets, validate package-name-to-listing mapping, and do not assume the previous project is accessible merely because it was mentioned in a prior conversation. **Never invent package names, listings, icons, or descriptions.** If package names/catalog cannot be confirmed, create a configurable catalog data source, document missing inputs, and do not insert fictional games.

Prefer verified development-time retrieval and locally cached/approved icon assets; do not rely on undocumented fragile runtime scraping. Provide a graceful image placeholder and support long text, variable content and failed metadata retrieval. Keep scrolling smooth. Verify links, two columns, scrollbar, RTL text, and common mobile aspect ratios. Keep the verified catalog reusable for Task 8. Update progress before Task 5.

# TASK 5 — UNIFIED ADIV​​ERY + TAPSELL MEDIATION SYSTEM

Implement a **single common ad service** for both **Adivery** and **Tapsell Mediation**, supporting **Rewarded Video** and **Interstitial**. Inspect actual installed SDKs, existing managers, ad units, and documentation matched to their versions; don't invent APIs, IDs or secrets.

## Mandatory source file: adsmanagernew.cs

- **The primary file to inspect and modify is `adsmanagernew.cs`** (match filename case as appropriate for the project). This is the intended location for the game's existing ad mechanics. Search the current project's `Assets` tree and relevant assemblies for the file before creating or changing advertising architecture.
- If the file exists, **edit and extend that implementation in place**. Preserve its public methods, Unity scene/prefab references, existing call sites, ad placement rules, and save-compatible behavior wherever possible. Do not replace it with an unrelated new advertising manager.
- If it does not exist in the current project, look for the same file in an **actually accessible** previous `connectballs` project (also check `connectball` if that is the directory name). If found, copy only the required `adsmanagernew.cs` and necessary compatible local dependencies, then adapt it carefully to this project and verify references.
- If the previous project/file is inaccessible, **do not claim it was copied**. Record the missing source in `CODEX_PROGRESS.md`, report the specific blocker, and proceed only with safe, independent preparation; do not invent a purported reproduction of the original file.
- **Do NOT download, install, or import the Adivery or Tapsell Mediation SDKs yourself.** If either SDK is absent, explicitly remind the developer **in Persian** to import the missing SDK manually, name which one(s) are missing, and explain the remaining steps to finish and test integration.
- When SDKs are missing, avoid breaking compilation: isolate SDK-specific integrations behind appropriate interfaces/conditional compilation or other compatible measures. Do not create fake SDK implementations, claim ads are functional, or grant simulated ad rewards in production. Mark live SDK integration as BLOCKED until manual SDK import and configuration are completed.
- Do not modify unrelated ad scripts unless essential for wiring `adsmanagernew.cs` to existing call sites or scenes. Keep this file the central entry point for rewarded and interstitial advertising behavior.


## Rotation and fallback rules

- Initial preferred provider: **Adivery**.
- After Adivery **successfully displays** an ad, preferred provider for the next ad request becomes **Tapsell Mediation**.
- After Tapsell Mediation **successfully displays** an ad, preferred provider becomes **Adivery**.
- If the preferred provider has no ready ad, cannot load, times out, or reports failure **before presentation**, immediately try the alternate provider for that SAME request.
- If both fail or are unavailable, show the **NoAds panel**.
- Do not rotate based only on requests, unavailable ads or failed loading.
- Use a documented shared rotation state across rewarded and interstitial, unless technical SDK constraints require separate rotation state.
- Never attempt to display two full-screen ads for one request or switch after an ambiguously successful/ongoing display. Treat uncertain post-display failures conservatively.

## Rewarded ads

Use verified SDK reward/completion signals to grant rewards. No credit on open, skip, close without reward or failure. Ignore duplicate callbacks and prevent overlapping requests. Provide a shared interface for Daily Challenge 2X, Power-Ups and any existing rewarded placements. Ensure app backgrounding and popup dismissal cannot cause duplicate rewards.

## Interstitials

Respect appropriate gameplay transitions. Support the same priority, readiness check, pre-display fallback and no-double-show logic; do not add disruptive placements.

### Mandatory level-based interstitial schedule

This schedule **replaces any existing level-count frequency rule** for interstitials. Keep any existing *time-based* minimum cooldown only if it does not contradict this schedule, and document what was kept or removed. All values must be configurable (e.g., in an ad config ScriptableObject).

| Player progress (highest reached normal level) | Interstitial frequency |
| --- | --- |
| Levels 1–10 (`interstitialStartAfterLevel = 10`) | **No interstitials at all** |
| After level 10, up to level 50 | **Once every 4 completed levels** (`earlyInterval = 4`) |
| After level 50 (`lateTierStartLevel = 50`) | **Once every 3 completed levels** (`lateInterval = 3`) |

Rules:
- Count only **completed normal levels** (wins) toward the interval, using the same verified win event as Tasks 2, 11, 12 and 13. Do not count losses, Daily Challenge, tutorial levels, level skips, or duplicate win events.
- Keep a persistent counter `levelsSinceLastInterstitial`. When it reaches the current interval, mark an interstitial as **due** for the next safe transition after the win (e.g., after the win screen when the player taps Next Level or Home) — never during gameplay, never on top of the win screen's reward/2X flow, and never during the Daily Challenge.
- Reset the counter to 0 **only after an interstitial is actually displayed**. If both providers fail, **do not show the NoAds panel for interstitials** (the player did not ask for an ad); skip silently, keep it due, and try again at the next eligible transition.
- Determine the tier (none / every 4 / every 3) from the player's **highest reached normal level**, so replaying early levels after level 10 still follows the active tier. Players who update the game while already past level 10 or 50 immediately follow the correct tier (safe migration).
- Do not show an interstitial immediately after a rewarded ad the player just watched in the same transition, and never show two full-screen ads for one transition.
- The "interstitial due" state must be explicit and readable by Task 12 (review prompt deferral) and the popup coordinator.

## NoAds panel

Search the current game for an existing `NoAds` panel/image/prefab. Reuse it if found. Otherwise, locate it in an actually accessible `connectball` or `connectballs` project and safely copy/adapt its necessary visual assets and dependencies. Do not copy whole unrelated systems. If neither is accessible, create an appropriate local fallback panel using available assets and document the difference. The panel must display correctly, close safely and not trap player flow.

## Tests

Use SDK test modes where available. Test Adivery-first, alternating after successful display, automatic fallback in BOTH directions, both unavailable -> NoAds (rewarded) / silent skip (interstitial), duplicate callbacks, rewarded skips/failures/success, interstitial, backgrounding, and no concurrent full-screen ads. Also test the interstitial schedule: no interstitial through level 10; one per 4 completed levels after level 10; one per 3 completed levels after level 50; counter persists across restarts and resets only after an actual display; losses/Daily Challenge/skips do not count; migration for players already past level 10/50. Distinguish simulated tests from real provider/device tests. Update progress before Task 6.

# TASK 6 — PROFESSIONAL POWER-UPS

Analyze the game's real solving mechanics and add or improve **several genuinely useful assistance power-ups**, preferably at least three distinct ones. Options ONLY if relevant: hint, reveal correct move, remove wrong option, undo, shuffle while retaining solvability, add time, highlight, etc. Do not create decorative, nonworking buttons or impossible mechanics.

**At least three power-up types should start with one free use EACH** for new players. Persist that one-time initial grant (including safe migration for existing players), free-use counts, and consumed state. These uses should not regenerate on every restart/day unless an existing game rule specifically requires that behavior.

Activation order:
1. If this power-up has a free use, use it and decrement ONLY after successfully applying the effect.
2. Otherwise, if enough coins are available, show price clearly and spend the configured coins through the shared wallet on confirmed activation; don't charge for failed effects.
3. Otherwise offer a rewarded video using the shared advertising service from Task 5. Grant the power-up ONLY after verified successful rewarded completion.
4. If neither provider can serve a rewarded ad, display the NoAds panel. Do not spend coins/free uses or activate the power-up.

Create responsive Persian RTL power-up UI with appropriate icons, free count, coin costs, rewarded video alternative, and confirmation where needed. Match existing visual style. Keep coin prices configurable and sensible relative to the economy and difficulty.

Verify actual effects, undo/solvability when applicable, free inventory, persistence, coin spending, insufficient balance, rewarded callback, fallback NoAds and no duplicate activation. Update progress before Task 7.

# TASK 7 — DAILY LUCKY WHEEL

Create or improve a daily **«گردونه شانس»** feature: **one reward-bearing spin per eligible calendar day** for coin prizes.

First inspect current project for existing Lucky Wheel. If absent, search actually accessible older `connectball` / `connectballs` project files for wheel scripts, prefabs, art, tweening/animation, reward selection, persistence and dependencies. Reuse/adapt compatible implementation and assets; never blindly duplicate their wallet/save managers. If unavailable, implement a wheel natively in this project and document inability to reuse the old source.

Requirements:
- Main-menu button matching existing style; optional availability indicator for the daily spin.
- Persian RTL wheel panel/screen with current coin balance, prize segments, spin button, status, back/close control.
- Smooth wheel acceleration/deceleration; disable repeat taps during spinning.
- Select a reward through a controlled, configurable reward table with valid optional weights; wheel pointer and awarded prize MUST match.
- Grant reward via centralized idempotent coin transaction exactly once; display `+X` with coin icon and update balance.
- Persist last rewarded spin/day and the result; never allow replay farming on app restart.
- Use same centralized daily reset/clock-protection policy as missions and challenge.
- Restore game/menu state safely after spin; remain performant on mobile.

Verify animation/reward alignment, once-per-day restriction, save/load, day rollover, duplicate payout protection and main-menu integration.

# TASK 8 — DAILY RANDOM OTHER-GAME RECOMMENDATION POPUP

## Goal

Once per eligible calendar day, recommend **one random game** from the existing verified **«سایر بازی‌ها»** catalog through a polished, Persian, RTL popup. The purpose is to help players discover another game and install it through Cafe Bazaar.

## Data source and dependency

- Reuse the catalog, icons, titles, package names and validated Cafe Bazaar links from **Task 4**. Never create a separate or conflicting games database.
- When building or repairing the catalog, search the accessible `connectballs` / `connectball` project for its existing Other Games list first, as described in Task 4.
- Exclude the current game, unverified entries, duplicates, and games without a valid Cafe Bazaar listing.
- If no eligible, verified game exists, do **not** show an empty or fabricated popup. Record the blocker and keep normal gameplay uninterrupted.

## Timing and precedence

- Show **at most one recommendation popup per local calendar day**, with saved last-shown date and consistent shared daily-reset/clock-safeguard logic.
- **Do not show this popup automatically at the first app opening or first main-menu entry of the day.** The Daily Challenge popup from Task 3 has priority.
- An appropriate trigger is **after the player wins a normal level**, once the existing victory flow and its reward UI have finished or been dismissed. Never interrupt active gameplay or obscure a victory reward popup.
- Only trigger after the Daily Challenge popup for that day has been dealt with (started, dismissed, or otherwise resolved). Do not stack both popups.
- Do not trigger directly after the Daily Challenge victory popup; prefer a normal level win or another safe later opportunity.
- If there is no suitable trigger that day, simply skip the recommendation for that day; do not force it upon startup or create disruptive navigation.
- Record the shown state when the popup is actually displayed, not merely when it is scheduled. Reopening scenes or restarting the app must not show it again that day.

## Random selection

- Select one eligible game randomly from the Task 4 catalog.
- Prefer not to recommend the same game on consecutive recommendation days when at least two eligible games exist.
- Persist the selected game for the day once selected so reopening or interrupted UI flow does not unpredictably switch the recommendation.
- Keep random selection lightweight and nonblocking.

## Popup UI

Create a game-style-consistent, responsive, Persian RTL popup with:

- A concise Persian heading such as **«پیشنهاد بازی امروز»**.
- The actual game's recognizable Cafe Bazaar icon.
- Its actual game name.
- An optional short Persian description if verified content exists.
- A prominent button titled **«در کافه بازار نصب کنید»**.
- A clear close (X) button so players can decline without leaving the game.

Use the project's Persian-compatible fonts and correct RTL shaping. Match existing modal styles and mobile safe areas. Never display fabricated icons, game titles, or unsupported descriptions.

## Cafe Bazaar navigation

- On tapping **«در کافه بازار نصب کنید»**, open the exact verified Cafe Bazaar listing for the selected package name.
- Prefer a supported Cafe Bazaar deep link with a safe HTTPS listing fallback, using valid Unity/Android APIs already appropriate for the project.
- Handle an absent Cafe Bazaar app or failed deep link gracefully. Never send users to a guessed or unrelated listing.
- The popup must close safely; declining does not affect coin rewards, daily challenges, or gameplay progress.

## Persistence and testing

Use the shared existing save and daily reset architecture. Save the last-shown date and chosen catalog item; do not reset eligibility by reopening the app. Test:

1. No recommendation on first app open/main-menu entry of the day.
2. Task 3 Daily Challenge popup has precedence; no overlay stacking.
3. Safe display after a normal level victory.
4. Only one display per day, even with repeated wins, scene changes, or restart.
5. A new eligible recommendation on a later day.
6. Correct real icon, title, and Persian RTL button.
7. Correct Cafe Bazaar navigation and missing-app fallback.
8. Close button and no disruption to normal game progression.
9. Graceful skipping when there is no verified eligible game.

Fully integrate, validate, and document Task 8 before beginning Task 9.

---

# TASK 9 — TEN-DAY CONSECUTIVE LOGIN REWARDS

## Goal

Implement a **10-day consecutive-login reward calendar** with increasing daily rewards and a special reward on Day 10. Reuse the project's existing shared daily-reset/date service and Task 1 coin wallet. Do not create a second calendar-day manager or reset system.

## Calendar and reward design

- Create a polished, mobile-friendly, **Persian and RTL** screen/popup showing 10 clearly numbered days in an accessible calendar layout (for example, five columns by two rows, adapting to screen size).
- Each day shows its reward, coin icon or reward icon, and a clear state: **«آینده»**, **«آماده دریافت»**, or **«دریافت شده»**; highlight the current streak day.
- Days 1–9 award configurable coin amounts that **increase with the consecutive day number**. Balance the values against the game's existing economy; do not hardcode unreasonably large payouts.
- **Day 10 gives a special reward**, preferably a **temporary power-up or temporary gameplay benefit** supported by the game, using the power-up system from Task 6. Make the benefit and its valid duration configurable (e.g., a limited-time power-up entitlement). Clearly show remaining validity and how it expires; do not permanently grant a temporary reward. If the game cannot safely support a timed benefit, choose another genuinely special compatible reward, document why, and avoid inventing unusable gameplay abilities.
- All UI text and reward explanations must be natural Persian, correctly shaped and aligned RTL.

## Consecutive-day and claim rules

- A streak advances **once per distinct eligible calendar day when the player opens the game**, based on the shared day boundary policy. Multiple launches on the same day never advance it twice.
- Grant **at most one daily login reward per day**, only when the player explicitly claims it. A successful claim must be idempotent and safely persisted; prevent duplicate payouts from repeated taps, restarts, and interrupted saves.
- On consecutive-day return, advance from Day 1 through Day 10; after collecting Day 10, the next eligible consecutive day begins a new 10-day cycle at Day 1.
- If a full eligible calendar day is missed, reset the streak to Day 1 on the next login. Do not revoke already claimed coins or currently active temporary benefits. Store the prior eligible login date, current streak position, latest claim date, cycle identifier, and any temporary benefit expiry.
- Use the existing save infrastructure and clock-rollback safeguards. If trusted server time is available, prefer it; explain local-only limitations honestly.
- Ensure restarting on the same day, timezone/date changes, an app resume, or a double-click cannot skip a day or grant a second reward.

## Popup priority and timing — no overlapping popups

- **The Daily Challenge popup from Task 3 has priority on the first eligible launch/day entry.** Never open the login calendar on top of it or concurrently with another modal.
- Use a centralized popup queue or equivalent safe coordination with the Daily Challenge and Task 8 Other-Game Recommendation; only one modal may be visible at a time.
- Once the Daily Challenge popup has been dismissed or its flow safely concluded, show the login reward calendar at the next safe menu state, or provide an unobtrusive claimable entry point if the challenge starts immediately. Do not interrupt active gameplay, result animations, ad playback, or challenge gameplay.
- The Other-Game Recommendation must remain a lower-priority, later event (for example, after a normal level win) and must never overlap the challenge or login calendar.
- Do not repeatedly force-show the calendar after it has been dismissed that day; keep the daily reward claim accessible from the main menu until claimed or expired at the next reset. Distinguish popup-shown state from reward-claimed state.

## Integration

- Add a Persian-styled main-menu entry such as **«جایزه ورود روزانه»** with a discreet claim-ready indicator and correct safe-area behavior.
- Credit ordinary coin rewards through Task 1's centralized economy using stable, unique transaction IDs.
- Use the existing Task 6 power-up/benefit system for Day 10, including reliable time-based expiration; do not create duplicate inventory managers.
- Show a short reward feedback animation and update UI immediately after claiming.
- Respect all existing daily missions, challenge, wheel, and advertising behavior; the login reward must not require watching an ad.

## Validation

Verify and record the actual checks performed for:

1. Day 1 initial calendar and first claim.
2. Correct increasing rewards on Days 2–9.
3. Day 10 special entitlement activates and expires as configured.
4. Streak advances once per consecutive calendar day.
5. Missing a full calendar day resets the streak, while past payouts remain intact.
6. Correct restart at Day 1 after the completed 10-day cycle.
7. Double-click/relaunch/scene resume does not duplicate rewards.
8. Claimed and unclaimed states survive restart.
9. Calendar never overlaps the first daily Daily Challenge popup or the later Other-Game Recommendation.
10. Dismissed calendar does not block access to a still-claimable reward.
11. Persian text, icons and calendar states display correctly at common mobile aspect ratios.

Complete, validate, and record Task 9 in `CODEX_PROGRESS.md` before the final integration audit.

---

# TASK 10 — PRE-AD LOADING POPUP

## Goal

Before showing **any fullscreen advertisement flow in the game**, display a small, attractive, user-friendly loading popup for at least **2 seconds** so the user understands that the ad/video is preparing. This task must integrate with the **single advertising architecture from Task 5** and must not create a second unrelated ad-entry flow.

This requirement applies to all eligible ad-triggered flows, including but not limited to:

- Rewarded ads for Daily Challenge rewards and retry/continue.
- Rewarded ads for power-ups.
- Rewarded ads for coin gifts from the shared coin-balance popup.
- Rewarded ads for level skip when applicable.
- Interstitial ad presentations where showing a short preparation popup improves user experience and is technically appropriate.

## Popup behavior

Implement one shared reusable popup/panel such as **«ویدئو در حال بارگذاری...»** or **«تبلیغ در حال بارگذاری...»** with a friendly Persian message and appealing lightweight graphics that fit the visual style of the current game.

Requirements:

- Show the popup immediately after the player confirms or triggers an ad request and before the ad becomes visible.
- The popup must remain visible for a **minimum of 2 seconds**, even if the ad becomes ready sooner.
- If the ad is not ready after 2 seconds, keep the popup visible while the system continues its normal provider/fallback checks, but do not keep the player waiting unnecessarily after a verified failure.
- As soon as the ad is actually ready **and** the minimum 2-second display time has elapsed, close the popup and present the ad.
- If both providers fail or no ad is available, close the loading popup cleanly and proceed to the existing fallback behavior (for example, NoAds panel or a localized unavailability message), without freezing input or leaving the UI in a blocked state.
- The popup must not accidentally trigger duplicate ad requests, duplicate callbacks, duplicate rewards, or two full-screen ads for a single user action.
- Do not grant any reward merely because the loading popup appeared. Rewards still depend on the verified ad-completion rules of the underlying feature.

## UI and UX

- Use Persian and RTL text with proper shaping.
- Match the game's art direction with a pleasant loading indicator, animation, or icon.
- Keep the popup small/lightweight and centered or otherwise placed appropriately for the game's modal style.
- Prevent accidental repeated tapping on the source button while the popup is active.
- Avoid making the popup feel like an error dialog; it should communicate that the video is being prepared.
- If the request is cancelled due to failure/unavailability, restore control safely.

## Integration and validation

- Implement this as a shared service or reusable UI component, not separate duplicated popups for every feature.
- Wire it into the ad-request paths managed by `adsmanagernew.cs` and the Task 5 unified ad system.
- If `adsmanagernew.cs` is absent and must be sourced from `connectballs`, integrate the loading popup into that restored/shared flow as well.
- Do not import missing SDK packages automatically; if Adivery or Tapsell Mediation is absent, document that manual SDK import is required.

Verify and record the actual checks performed for:

1. Minimum 2-second popup display before a rewarded ad.
2. Transition from loading popup to successful rewarded ad.
3. Transition from loading popup to successful interstitial ad.
4. Correct behavior when the preferred provider is not ready and fallback is used.
5. Correct behavior when both providers fail and NoAds/fallback appears.
6. No duplicate ad requests or duplicate rewards while the popup is active.
7. Proper Persian text and attractive presentation on common mobile aspect ratios.

Complete, validate, and record Task 10 before beginning Task 11.

---

# TASK 11 — WIN / LOSS RESULT SCREENS UPGRADE

## Goal

Create or improve **attractive, graphical, game-themed win and loss screens** across the game, adapting the design to the actual genre and art style. These result screens must feel polished, mobile-friendly, and tightly integrated with the game's economy, advertising, continuation, and progression systems.

If suitable win/loss screens already exist, improve and extend them rather than replacing them unnecessarily.

## General UI requirements

- Design the screens to match the current game's visual identity, fonts, colors, characters, and animation style.
- Use Persian and RTL text for all new labels and buttons.
- Add tasteful animation and feedback without hurting performance.
- Show clear next actions and avoid confusing button overload.
- Maintain safe touch targets, mobile safe areas, and good readability.

## Win screen requirements

On a normal gameplay victory/result screen, include appropriate existing actions such as next level, replay, and home/menu, while also adding or improving reward presentation.

If the mode awards coins, display the earned coin reward clearly and integrate with the shared coin economy. Add a **prominent rewarded-video 2X button** that:

- Visually stands out and uses a noticeable **pulse animation** to attract attention.
- Clearly communicates that watching a video will double the coin reward.
- Uses the shared ad-loading popup from Task 10 before the ad appears.
- Grants the bonus only after verified rewarded-ad completion.
- Never duplicates the base reward or grants the 2X bonus more than once.
- Adapts to each game's reward/economy values rather than hardcoding one universal amount.

If a specific mode already has its own dedicated victory flow (for example Daily Challenge), integrate carefully so the general win-screen improvements do not conflict with mode-specific rules. Reuse existing compatible 2X behavior when present.

## Loss screen requirements

Create or improve an attractive loss screen and adapt the recovery option to the actual gameplay style:

- For **time-based games or stages**, offer a rewarded-ad continue option that adds a meaningful amount of extra time (for example, configurable extra seconds chosen according to the game's pacing).
- For **life/death-based games**, offer a rewarded-ad revive/continue option that lets the player resume the same stage/session safely from an appropriate point.
- For other genres, choose the most suitable continuation mechanic that fits the real gameplay, instead of forcing an incompatible pattern.

The primary continue/revive CTA on the loss screen must also have a **pulse animation** so the player notices it.

Requirements:

- Use the shared ad-loading popup from Task 10 before showing the ad.
- Grant the continue/revive only after verified rewarded-ad completion.
- Resume the current stage/session safely without corrupting progress, score, challenge state, or timers.
- Prevent duplicate revives from duplicate callbacks or repeated taps.
- Respect any mode-specific constraints; for example, do not break carefully defined Daily Challenge rules. If a mode already has a stricter dedicated continuation rule, integrate without contradiction.

## Two consecutive losses → skip level option

For eligible **level-based progression modes**, track consecutive losses **on the same stage/level**. If the player loses the same stage **twice in a row**, then on the relevant loss screen also present a **Skip Level** option.

Skip-level rules:

- Offer skipping the level for a **relatively high but configurable coin cost**, chosen according to the game's economy and difficulty so it remains meaningful.
- If the player has enough coins and chooses to skip, spend the coins safely through the shared coin wallet and advance progression appropriately.
- If the player does **not** have enough coins and taps the skip option, offer a rewarded video as an alternative skip path.
- The rewarded-ad skip path must also use the shared ad-loading popup from Task 10 and only activate after verified completion.
- Skipping must unlock or advance progression safely without creating exploit loops, duplicate completion rewards, or broken level state.
- Decide and document a sensible progression consequence (for example, unlock next level but do not grant full win rewards/stars unless the game already supports a skip-completion convention).
- Reset the consecutive-loss counter when the player wins, successfully skips, or moves to another level.
- If the game is not level-based or skipping would break progression, implement the nearest compatible alternative and document the limitation honestly.

## Integration and validation

- Reuse shared systems from Tasks 1, 5, and 10.
- Ensure win/loss screens work with the game's actual level/mode structure and do not conflict with the Daily Challenge, login rewards, or other modal flows.
- Maintain stable save behavior when coins are doubled, a revive is used, or a level is skipped.

Verify and record the actual checks performed for:

1. Attractive win screen integrated with actual game art/style.
2. Attractive loss screen integrated with actual game art/style.
3. Pulsing 2X button on win screen.
4. Verified 2X reward path with no duplicate payout.
5. Pulsing continue/revive CTA on loss screen.
6. Time-based extra-time continuation, when applicable.
7. Life/death-based revive continuation, when applicable.
8. Detection of two consecutive losses on the same level.
9. Skip-level-for-coins flow with correct coin spending.
10. Skip-level-with-rewarded-ad flow when coins are insufficient.
11. Proper reset of the consecutive-loss counter.
12. No progression corruption, duplicate rewards, or broken UI states after continue, revive, or skip.

Complete, validate, and record Task 11 before beginning Task 12.

---

# TASK 12 — ONE-TIME BAZAAR / MYKET REVIEW PROMPT (ANDROID INTENT)

## Goal

Show a review/rating prompt **only once per preserved player save**, after several normal stage victories, and **only after the player presses the existing Next Level button**. The destination market is **Cafe Bazaar OR Myket**, selected by a field already defined in `BazaarReviewManager.cs`. Do not hardcode Cafe Bazaar as the only supported store.

## Mandatory source of truth: BazaarReviewManager.cs

- Search the current Unity project for `BazaarReviewManager.cs` (allow case variations such as `bazaareweiwmanager.cs`). Inspect its actual store-selection field/enum/boolean and Android Intent-based review flow. **Respect its existing field values, serialization, Inspector configuration, and store-selection logic.** Do not assume a specific field name or value without inspecting the source.
- Keep **this manager as the primary integration point**. Reuse its existing Intent dispatch rather than replacing it with a generic external URL or creating a second review manager.
- If missing, search actually accessible older projects, including `connectballs` / `connectball`, for the same script and required assets. Copy/adapt only compatible files, preserving the store-selecting field and Intent logic. If no source is available, implement a compatible manager with a configurable Bazaar/Myket selector and Android Intent flow; document that the original code could not be found.
- **Do not silently alter the selected market.** A Bazaar selection must launch the current game's Bazaar review/listing destination; a Myket selection must launch the current game's Myket review/listing destination, using the actual Android package name and valid Intent action, data, and package details for the selected store. Do not invent market package IDs or deep-link formats; verify against accessible project logic/docs.
- Use `AndroidJavaClass`/`AndroidJavaObject` or the project's existing Unity Android bridge as appropriate, and catch `ActivityNotFoundException`/other launch errors. Use only a verified, store-appropriate fallback if available, without redirecting a Myket selection to Bazaar (or vice versa) without explicit configuration.
- For unsupported platforms (such as Unity Editor), fail gracefully and document how to test Intent behavior on an Android device.

## Eligibility and exact trigger

- Wait for several successful **normal** levels (default at least 3 distinct stage victories, configurable). Do not count tutorials, Daily Challenge victories, level skips, or duplicate victory events unless the game's existing progress logic explicitly warrants them.
- **Never show on the win screen immediately.** Check eligibility when the player **clicks Next Level after a qualifying win**, at a safe point in the level transition. Do not interrupt live gameplay, and do not add a duplicate Next Level button.
- Never show on first launch or on a loss.

## Priority — no popup/ad overlap

- If Task 8's daily Other Games recommendation is scheduled or showing on that Next Level transition, **do not display the review prompt**. Defer it to a future eligible Next Level click; do not stack it behind the recommendation.
- If an interstitial is scheduled, loading, showing, or otherwise selected for the same transition, **do not display the review prompt** even if the ad later fails. Defer it to a later eligible win/Next Level click; do not stack it behind the interstitial or NoAds fallback.
- Respect the centralized popup coordinator, Daily Challenge, and login-calendar priority. Never show more than one modal or fullscreen ad flow simultaneously.
- Use explicit scheduling/state information rather than guesses based on visual visibility or timing.

## Exactly once and persistence

- Persist one durable `reviewPromptShown` state in the existing save architecture and set it when the **review prompt is actually displayed**, not when merely eligible or postponed.
- Once shown, never show it again while that local save is preserved, whether the user taps Review, Later, or X. Dismissing the popup does not reset eligibility.
- Guard against repeat taps, duplicate callbacks, scene transitions, and restarts. Local-only saves cannot guarantee once-ever behavior after deleting app data/reinstalling; document that limitation.

## Persian RTL UI and Intent action

- Use a small polished Persian RTL popup consistent with the game's visuals.
- Set the localized store-specific copy according to the **same selection field** in `BazaarReviewManager.cs`:
  - Bazaar: **«نظرت رو در کافه‌بازار ثبت کن»** and **«ثبت نظر در کافه‌بازار»**.
  - Myket: **«نظرت رو در مایکت ثبت کن»** and **«ثبت نظر در مایکت»**.
- Include a clear **«بعداً»** or close button.
- The review button must invoke the manager's **Android Intent** implementation for the configured store, not blindly open a Bazaar URL for both stores. Return to the normal next-level navigation safely on dismiss or intent-launch attempt.

## Validation and completion

Record actual tests, including:

1. Configurable victory threshold and Next Level-only trigger.
2. No prompt before pressing Next Level, on failure, or on launch.
3. No prompt when the daily game recommendation is scheduled/displayed.
4. No prompt when interstitial is due/loading/showing, including ad fallback.
5. A deferred prompt can appear on a later eligible Next Level click.
6. Prompt shows once and stays suppressed after close, review attempt, restart, and scene changes (with save retained).
7. With Bazaar selected in the existing field, Android Intent routes to the **correct game's Bazaar** destination.
8. With Myket selected in that field, Android Intent routes to the **correct game's Myket** destination.
9. Missing target app and unsupported-platform behavior is handled safely, with only verified appropriate fallback.
10. Correct Persian/RTL labels, no duplicate manager, intact progression and player saves.

Do not mark device Intent navigation as tested unless verified on Android. Complete Task 12 and update `CODEX_PROGRESS.md` before beginning Task 13.

---

# TASK 13 — WEEKLY STAR LEAGUE (SIMULATED WEEKLY LEADERBOARD)

## Goal

Add a Persian RTL **weekly star league** («لیگ هفتگی») that ranks the real player against a set of **locally simulated competitors** by the number of stars earned during the current week. The league must motivate the player to play **at least about three levels per day** to keep up, reset every week, and show rank changes on the main menu and on the win screen.

This is an **offline, locally simulated** leaderboard: only the real player's stars are real; all other entries are generated competitors. Do not add a server, networking, accounts, or any new SDK for this feature.

## Unlock requirement — league opens at level 8

- The league is **locked until the player reaches level 8** (configurable `leagueUnlockLevel`, default **8**). "Reaches level 8" means level 8 is unlocked/available in the player's real campaign progress (i.e., level 7 has been completed). Read this from the game's existing progression data; do not create a second progress counter. Document the exact condition used.
- **Existing players** whose saved progress is already at or beyond level 8 when this update is installed are unlocked immediately on the next launch (safe migration; no progress reset).
- While locked:
  - The main-menu **«لیگ هفتگی»** button is still visible, but shows a **lock icon** instead of a rank, using the existing visual style (e.g., «لیگ هفتگی 🔒»). Do not show any rank, competitor data, or fake numbers.
  - Tapping it opens a small Persian RTL info popup (through the centralized popup coordinator, never stacked on another modal) with the text: **«وقتی به مرحله ۸ برسی، وارد لیگ هفتگی می‌شی و می‌تونی رتبه‌ات رو ببینی.»** and a single **«باشه»** button. The level number in the text must come from `leagueUnlockLevel` (rendered with Persian digits), not be hardcoded.
  - No stars are counted toward the league, no competitors are generated, and the win-screen league section is hidden.
- On the moment of unlocking:
  - Record `leagueUnlocked = true` and the unlock date/time durably, exactly once.
  - Start the player's **first league week** at that moment (see "First league week" below). Stars earned **before** unlocking are **not** added retroactively; the player starts the league at 0.
  - Show a short, non-blocking Persian message on the win screen of the level that unlocked the league (or at the next safe main-menu entry if that is safer), e.g. **«لیگ هفتگی برات باز شد! حالا رتبه‌ات رو ببین.»**. It must not be a forced modal on top of other popups and must not delay result actions.
  - Optionally show a one-time attention pulse on the main-menu league button until the player opens the league screen for the first time (stop it after first open).

## Honesty and naming rules for simulated competitors

- Competitor display names come from a configurable **name pool** that mixes three categories (approximate default mix, configurable):
  - **Common Persian boy first names (~35%)**, e.g. «علی»، «محمد»، «امیرحسین»، «رضا»، «مهدی»، «آرش»، «پارسا»، «سینا»، «حسین»، «کیان»، «امید»، «مهران».
  - **Common Persian girl first names (~35%)**, e.g. «زهرا»، «فاطمه»، «سارا»، «مریم»، «نگار»، «الناز»، «ریحانه»، «یاسمن»، «هستی»، «نازنین»، «ستاره»، «پریا».
  - **Nicknames / gamer handles (~30%)**, e.g. «ستاره‌چین»، «توپ‌باز_۲۳»، «مسافر شب»، «شیرِ میدان»، «بانوی ستاره‌ها»، «قهرمان کوچولو»، «سلطان مراحل».
  - First names may optionally get a light variation for realism (e.g., «سارا ۷۷»، «رضا_پرو»، «مریم.ک»), configurable.
- Provide at least **40 boy names, 40 girl names and 30 nicknames** in the pool so tables do not look repetitive. Keep names balanced between boys and girls, natural, respectful and family-friendly; no offensive, political, religious-sensitive or mocking names.
- **Only first names**, never full names (first name + surname), and never names/handles of celebrities, public figures, or real players. Never use real photos; use generic avatars/placeholder icons from the game's art style. Never collect or display real user data of other people.
- Names are picked **deterministically from the week seed**, with **no duplicate name in the same week's table**, and must not change after restart.
- Do not add UI copy that claims the competitors are real, online, or live players (avoid phrases such as «بازیکنان آنلاین» or «کاربران واقعی»). Neutral wording such as «رقبای این هفته» is acceptable.
- Keep a configurable flag (default off) to show a small, unobtrusive note that the league is an offline challenge, so the developer can enable it if store policy or brand requirements demand it. Document this choice in `CODEX_PROGRESS.md`.

## Star source and scoring rules

- Inspect the game's actual star system first (each normal level awards 0–3 stars; e.g., a game with 400 levels has a 1200-star campaign maximum). Reuse the existing star result; do not invent a second star calculation.
- The player's **weekly score = the number of stars earned from winning levels during the current league week, counted only after the league is unlocked.** Example: a 3-star win adds 3, a 2-star win adds 2. Losses add 0.
- Count stars only from normal level victories, through the same verified win event used by Tasks 2, 11, and 12. Do not count Daily Challenge wins, level skips (Task 11), tutorial levels, or duplicate win events unless explicitly configured.
- **Anti-farming rule (configurable, default ON):** within one league week, each level contributes at most its **best star count for that week** (max 3). Replaying a level adds only the improvement (e.g., previously 2 stars this week → now 3 stars adds +1). After the weekly reset, levels can contribute again. Without this rule, replaying the easiest level would make the league trivial; document the chosen setting.
- Use idempotent, transaction-like accounting (unique win/session ID) so app backgrounding, duplicate callbacks, or scene reloads never add stars twice. Persist through the existing save architecture.

## Week boundary and reset

- The league week runs **Saturday 00:00 to Friday 24:00** (i.e., the reset happens at **Friday night midnight**, which is the start of Saturday), matching the Iranian week.
- Use the project's shared daily-reset/date service and its clock-rollback safeguards (Tasks 2/3/9). Prefer trusted server time if the project already has it; otherwise use local device time and document that local-only time can be manipulated.
- At reset: set the player's weekly stars to 0, clear the weekly per-level best-star records, generate a new week of competitors, and store a week identifier (e.g., the date of that week's Saturday). Do not touch campaign progress, permanent stars, coins, or other features.
- If the app was closed across one or more resets, compute the correct current week on the next launch (no catch-up rewards, no double reset).

## Simulated competitor model

All values below must be configurable in a ScriptableObject/settings asset (e.g., `WeeklyLeagueConfig`).

- **League size:** configurable, default **30** entries total (29 competitors + the player).
- **Deterministic generation:** generate competitors from a stable seed (e.g., week ID + install ID) so the table is identical after restarts and does not need to run in the background. Competitor scores for any moment are calculated from: starting score + sum of daily increments for elapsed days of the week. Persist the seed and week ID; do not re-randomize on reopen.
- **Daily update:** competitor scores update **once per calendar day** (at the shared day boundary). On each new day of the week, each competitor gains a daily random amount.
- **Average daily gain:** the competitor field should gain on average about **8 stars per day** (daily gain per competitor randomly in **0–8**, with the distribution tuned so the field average is about 8/day for the leading group; configurable). This means the player needs roughly **three 3-star levels per day (≈9 stars)** to keep pace with or overtake the leading group.
- **First league week (at unlock, level 8):** when the league is unlocked for the first time (any day of the week), generate the current week with the **same rules as a normal weekly reset** (below): the player starts at 0, the leading group starts **50–80 stars ahead**, and the rest start lower. If unlocking happens mid-week, do **not** add the elapsed days' gains to competitors retroactively; competitors grow only from the unlock day onward so the new player is not buried on day one. Weekly resets before the unlock are ignored entirely. Keep this first-week profile configurable separately (e.g., `firstWeekLeaderMin/Max`) so it can be tuned later.
- **Every weekly reset after unlock (Saturday morning):** when the player first opens the game in the new week, a group of competitors (configurable, default **5**) is placed **50–80 stars ahead of the player** (random per competitor within that range). The remaining competitors start lower (configurable, default 0–40) so the player can see attainable targets above and below. During the rest of the week, each competitor gains a random **0–8 stars per day** until the week ends.
- Clamp competitor scores to a realistic maximum (never above the game's real total star count, and never above a configurable weekly cap) so the table never shows impossible values.
- Rank ties: the player ranks above competitors with the same score (configurable) to avoid demotivating ties; document the rule.

> Note for the implementer: an earlier design started first-week competitors at 321 stars; it was intentionally replaced because a new player at 0 could never catch up in the first week. Do not reintroduce a large fixed starting score for the first week.

## Main-menu entry

- Add a main-menu button styled like the existing main-menu buttons, labeled **«لیگ هفتگی»**. **After unlock** it also displays the player's **current rank**, e.g. **«رتبه ۱۲»** (Persian digits, correct RTL shaping). **Before unlock** it shows the lock state and opens the locked-info popup described in the unlock section, never the league screen.
- Update the rank and the locked/unlocked state on menu entry, after a win, on unlock, and after a weekly/day rollover through events, not per-frame polling.
- Place it without overlapping existing buttons (missions, Other Games, Lucky Wheel, login calendar); make the menu scrollable if necessary (see Final Integration Audit).
- Opening the league must not trigger any automatic popup or ad, and must respect the centralized popup coordinator (Tasks 3, 8, 9, 12).

## League screen (Persian RTL)

- Title: **«لیگ هفتگی»**, subtitle with remaining time until reset, e.g. **«پایان هفته: ۲ روز و ۵ ساعت»**.
- A scrollable ranked list showing rank number, avatar/placeholder icon, nickname, and weekly stars with a star icon. Highlight the player's own row («شما») and auto-scroll to it on open.
- Optional visual emphasis for the top 3 ranks (gold/silver/bronze) using existing art style.
- A short explanation line, e.g. **«هر ستاره‌ای که این هفته بگیری به امتیازت اضافه می‌شه. جمعه شب ساعت ۲۴ امتیازها صفر می‌شن.»**
- Close/back button; correct layout on common Android aspect ratios and safe areas.
- Do not add coin or weekly rank rewards unless explicitly configured; if added later, they must go through Task 1's idempotent wallet transactions and the Task 1 economy audit.

## Win-screen integration

Extend the existing/upgraded win screen from Task 11 (do not create a second win screen) with a compact league section that shows:

- Stars earned in this win and how many were actually added to the weekly score (e.g., **«+۳ ستاره به لیگ هفتگی»**, or **«+۱ ستاره (بهبود رکورد این هفته)»** when the anti-farming rule applies, or a neutral message when 0 were added).
- The player's new rank and the change since the **last rank the player saw**: **«رتبه‌ات ۴ پله بهتر شد ↑»** / **«رتبه‌ات ۲ پله پایین‌تر رفت ↓»** / **«رتبه‌ات تغییری نکرد»**. Because competitors update daily, the rank can drop even after a win; compute the change against the last displayed rank and store the newly displayed rank.
- A small, lightweight count-up/arrow animation; it must not delay or cover the 2X button, next-level button, or other result actions.
- Do not show this section for Daily Challenge wins, skips, modes excluded from scoring, or **while the league is still locked** (except the one-time unlock message on the unlocking win).

## Persistence

Persist via the existing save system (competitor names are derived from the week seed, not stored separately unless needed): `leagueUnlocked` flag and unlock date, first-league-week ID, unlock-message-shown flag, first-open pulse flag, week ID, week seed, player weekly stars, per-level weekly best stars, last processed win IDs (bounded), last displayed rank, and last processed day index. Restarting the app, changing scenes, or reopening the screen must never re-randomize competitors or double-count stars.

## Validation

Verify and record the actual checks performed for:

1. Before level 8: main-menu button shows the lock state with no rank; tapping it shows «وقتی به مرحله ۸ برسی، وارد لیگ هفتگی می‌شی و می‌تونی رتبه‌ات رو ببینی.» via the popup coordinator; no stars are counted and no league section appears on the win screen.
2. Reaching level 8 unlocks the league exactly once (also after restart), shows the one-time unlock message without blocking results, and existing players already past level 8 are unlocked on update.
3. First league week (unlock on any weekday): player at 0 with no retroactive pre-unlock stars, leading group 50–80 stars ahead, no retroactive competitor gains for elapsed days, rank displayed on main menu.
4. Daily competitor update happens once per calendar day and is deterministic after restart.
5. Average competitor growth ≈ 8 stars/day; three 3-star wins per day keep the player competitive.
6. A 3-star win adds exactly 3 stars; replay rule adds only improvement within the week.
7. No stars from losses, skips, Daily Challenge, or duplicate win events.
8. Reset exactly at Friday 24:00 (Saturday 00:00); player stars become 0.
9. On the first Saturday open after unlock, the configured group of competitors is 50–80 stars ahead, then grows 0–8 per day.
10. App closed across several days/weeks computes the correct current week without double reset.
11. Clock rollback does not grant extra stars, unlock the league early, or re-trigger resets incorrectly.
12. Win screen shows stars earned and correct rank change (up, down, unchanged).
13. Main-menu button rank/lock state updates without per-frame polling and does not overlap other buttons.
14. Persian/RTL text, Persian digits, lock/star icons and list scrolling render correctly on common aspect ratios.
15. No conflict with popup coordination, ads, Daily Challenge, login calendar, or the review prompt.
16. Competitor names mix boy names, girl names and nicknames per the configured ratio, contain no duplicates within a week, contain no surnames or real-person handles, and stay identical after restart.

Complete, validate, and record Task 13 in `CODEX_PROGRESS.md` before the final integration audit.

---

# FINAL INTEGRATION AUDIT

When all thirteen tasks are completed or precisely documented as blocked, review their combined behavior:

- Single shared coin wallet for missions, daily challenge, power-ups, wheel and ordinary login rewards.
- The 10-day consecutive-login streak persists safely, grants at most one claim a day, resets correctly after missed days, and Day 10 temporary reward is applied/expired correctly.
- Daily Challenge, login reward calendar, and Other-Game Recommendation have explicit non-overlapping popup priority.
- Single daily reset policy and durable per-feature date records.
- Unified Adivery/Tapsell ads with automatic fallback and NoAds.
- Shared pre-ad loading popup appears before ad playback, lasts at least 2 seconds, and hands off cleanly to successful ads or fallback behavior.
- Daily Challenge failure-screen +30-second in-place continuation, victory-screen 2X, and Power-Ups consume verified rewarded-ad callbacks correctly; no extra time or coins on failed/skipped/unavailable ads.
- Existing interstitial placements preserved and integrated where compatible.
- General win/loss result screens are upgraded, game-themed, and integrated with shared reward, revive/continue, and skip-level logic.
- The once-daily Other Games recommendation reuses Task 4 catalog, never supersedes the Daily Challenge popup, never appears at first daily entry, and opens verified Cafe Bazaar listings after a safe normal-level victory.
- Main menu buttons for missions, Other Games and Lucky Wheel do not overlap; make menu scrollable if necessary.
- Main menu stays visually light (few icons, text-first buttons, one dominant Play action), and all main-menu buttons use the shared glossy style with a staggered, clipped, lightweight light-sweep effect that pauses when hidden.
- Interstitials follow the level schedule: none through level 10, every 4 completed levels after level 10, every 3 after level 50; counter resets only after an actual display; no NoAds panel for failed interstitials.
- Notifications/pulsing remain accurate, text is Persian with proper RTL/shaping, and coin icon/number ordering works.
- All relevant screens support Android aspect ratios and safe areas.
- App resume, pauses, scene changes, failed ads, rapid taps, duplicate callbacks, restarts and daily rollover are handled.
- Before every eligible ad flow, the loading popup appears exactly once, blocks repeated taps safely, and never causes duplicate ad requests.
- On eligible level-based modes, two consecutive losses on the same level unlock the skip-level option with coin or rewarded-ad fallback.
- Bazaar/Myket review prompt uses the store-selection field and Android Intent in `BazaarReviewManager.cs`, appears once after an eligible Next Level tap, and never competes with the daily recommendation or scheduled interstitial.
- Weekly Star League stays locked until level 8 (with the Persian locked-info popup), counts only valid weekly stars after unlock, resets at Friday 24:00, keeps simulated competitors deterministic, shows rank on the main menu and rank change on the win screen, and never overlaps other modals.
- Existing game flow, progression and saved player data remain functional.
- Avoid per-frame polling, redundant event subscriptions, duplicate SDK initialization and heavy effects.

Run all available compile/static/automated tests and Unity Editor Play Mode/device checks if accessible. Mark unperformed manual checks honestly; do not claim they passed.

# REPORTING REQUIREMENTS

After **each task**, update `CODEX_PROGRESS.md` with status, changed files, actual test results, blockers, and next exact action. Provide short progress summaries in **Persian**.

At the end, report in **Persian**:
1. Unity version and game type.
2. Status of tasks 1–13 (`COMPLETE / BLOCKED / IN_PROGRESS / NOT_STARTED`).
3. What was implemented per task, including all 25 missions, their goals and coin rewards.
4. Challenge generation method, time limit, rewarded +30-second continuation, and victory 2X implementation status.
5. Interstitial level schedule (start level, intervals, counter/migration behavior), pre-ad loading popup behavior, minimum display timing, and shared integration status.
6. Win/loss result-screen upgrade status, including pulsing 2X, revive/continue, and skip-level behavior after consecutive losses.
7. One-time Bazaar/Myket review prompt eligibility, store-selection field, Android Intent behavior, Next Level trigger, `BazaarReviewManager.cs` reuse, and non-overlap verification.
8. Ten-day consecutive-login calendar, increasing reward configuration, Day 10 special reward, streak/reset logic and popup coordination status.
9. Game catalog/Cafe Bazaar status, including reuse from `connectballs` and daily recommendation popup behavior.
10. Adivery/Tapsell rewarded/interstitial integration and live-testing status.
11. `connectball` / `connectballs` reuse status for NoAds, wheel, Other Games catalog, and any shared ad manager assets.
12. Created/modified scripts, scenes, prefabs and assets, including the main-menu glossy/light-sweep button style and any icons removed or simplified.
13. Tests actually performed and test results.
14. Required manual actions, external credentials/data and any unresolved risks.
15. Weekly Star League: level-8 unlock condition and locked-state behavior, star-counting rules, competitor simulation values, weekly reset behavior, main-menu rank and win-screen rank-change status.
16. Exact next step, if anything remains.

# MOST IMPORTANT DIRECTIVE

**Implement ONE TASK AT A TIME. Finish, integrate, validate, record progress, and only then move to the next.** If interrupted by a usage or context limit, recover from `CODEX_PROGRESS.md` and actual repository state instead of restarting. All NEW player-facing UI text MUST be correctly rendered in PERSIAN with proper RTL behavior. Do not sacrifice working gameplay just to check off tasks.
