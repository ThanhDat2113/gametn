SerializationException: End of Stream encountered before parsing was completed.
System.Runtime.Serialization.Formatters.Binary.__BinaryParser.Run () (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.ObjectReader.Deserialize (System.Runtime.Remoting.Messaging.HeaderHandler handler, System.Runtime.Serialization.Formatters.Binary.__BinaryParser serParser, System.Boolean fCheck, System.Boolean isCrossAppDomain, System.Runtime.Remoting.Messaging.IMethodCallMessage methodCallMessage) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Deserialize (System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler, System.Boolean fCheck, System.Boolean isCrossAppDomain, System.Runtime.Remoting.Messaging.IMethodCallMessage methodCallMessage) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Deserialize (System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler, System.Boolean fCheck, System.Runtime.Remoting.Messaging.IMethodCallMessage methodCallMessage) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Deserialize (System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler, System.Boolean fCheck) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Deserialize (System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Deserialize (System.IO.Stream serializationStream) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
InventoryManager.LoadFromFile () (at Assets/Scripts/Inventory/InventoryManager.cs:63)
InventoryManager.LoadInventory () (at Assets/Scripts/Inventory/InventoryManager.cs:46)
InventoryManager.Awake () (at Assets/Scripts/Inventory/InventoryManager.cs:29)

[FormationManager] Đã tự động thêm nhân vật mặc định 'Eugeo' vào ô 0
UnityEngine.Debug:Log (object)
FormationManager:EnsureAtLeastOneCharacter () (at Assets/Scripts/Combat/Formation/FormationManager.cs:151)
FormationManager:Start () (at Assets/Scripts/Combat/Formation/FormationManager.cs:46)

[Quest] Started fresh quest: Khởi đầu mới
UnityEngine.Debug:Log (object)
QuestManager:StartQuest (QuestData) (at Assets/Scripts/Quest/QuestManager.cs:109)
QuestManager:Start () (at Assets/Scripts/Quest/QuestManager.cs:61)

[Quest] Step completed: Trò chuyện với Vergil
UnityEngine.Debug:Log (object)
QuestManager:CompleteCurrentStep () (at Assets/Scripts/Quest/QuestManager.cs:137)
QuestManager:OnDialogueEnded (string) (at Assets/Scripts/Quest/QuestManager.cs:119)
DialogueTrigger:OnDialogueComplete () (at Assets/Scripts/Dialogue/DialogueTrigger.cs:364)
DialogueBubbleUI:ShowSequential (DialogueLineData[],UnityEngine.Transform,System.Action,int) (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:60)
DialogueBubbleUI/<>c__DisplayClass20_0:<ShowSequential>b__0 () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:63)
DialogueBubbleUI:Hide () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:73)
DialogueBubbleUI:Update () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:40)

[FormationManager] SaveFormation: đã lưu 1 nhân vật.
UnityEngine.Debug:Log (object)
FormationManager:SaveFormation () (at Assets/Scripts/Combat/Formation/FormationManager.cs:292)
MapEnemy/<StartCombatTransition>d__4:MoveNext () (at Assets/Scripts/Combat/MapEnemy.cs:32)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
MapEnemy:OnTriggerEnter (UnityEngine.Collider) (at Assets/Scripts/Combat/MapEnemy.cs:19)

[MapEnemy] MapRoot found and deactivated.
UnityEngine.Debug:Log (object)
MapEnemy/<StartCombatTransition>d__4:MoveNext () (at Assets/Scripts/Combat/MapEnemy.cs:50)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatManager] Automatically adding CanvasGroup to planningCanvas.
UnityEngine.Debug:Log (object)
CombatManager:Awake () (at Assets/Scripts/Combat/CombatManager.cs:169)

[TestStarter] PendingFormation detected from Map. Disabling test starter.
UnityEngine.Debug:Log (object)
CombatTestStarter:Awake () (at Assets/Scripts/Combat/CombatTestStarter.cs:27)

[CombatSceneStarter] Formation: OK Enemy: Tutorial
UnityEngine.Debug:Log (object)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:15)

[Eugeo's Passive] Đã đăng ký vào sự kiện OnActionConfirmed.
UnityEngine.Debug:Log (object)
AleusPassive:Initialize (CombatUnit) (at Assets/Scripts/Data/Passives/AleusPassive.cs:16)
CombatUnit:SetPassive (PassiveAbility) (at Assets/Scripts/Combat/CombatUnit.cs:341)
CombatManager:InitializePassives (CombatUnit) (at Assets/Scripts/Combat/CombatManager.cs:318)
CombatManager:SpawnSide (System.Collections.Generic.List`1<CombatUnit>,UnityEngine.Transform[]) (at Assets/Scripts/Combat/CombatManager.cs:288)
CombatManager:SpawnUnitViews () (at Assets/Scripts/Combat/CombatManager.cs:257)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:222)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[Passive] Initialized passive 'AleusPassive' for Eugeo.
UnityEngine.Debug:Log (object)
CombatManager:InitializePassives (CombatUnit) (at Assets/Scripts/Combat/CombatManager.cs:319)
CombatManager:SpawnSide (System.Collections.Generic.List`1<CombatUnit>,UnityEngine.Transform[]) (at Assets/Scripts/Combat/CombatManager.cs:288)
CombatManager:SpawnUnitViews () (at Assets/Scripts/Combat/CombatManager.cs:257)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:222)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[Spawn] Eugeo slot6 at (-6.82, 6.50, 1.11). Final grid pos: (-6.82, 6.50, 1.11)
UnityEngine.Debug:Log (object)
CombatManager:SpawnSide (System.Collections.Generic.List`1<CombatUnit>,UnityEngine.Transform[]) (at Assets/Scripts/Combat/CombatManager.cs:293)
CombatManager:SpawnUnitViews () (at Assets/Scripts/Combat/CombatManager.cs:257)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:222)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[Spawn] Slime slot1 at (42.06, 6.50, -6.89). Final grid pos: (21.18, 6.50, -6.89)
UnityEngine.Debug:Log (object)
CombatManager:SpawnSide (System.Collections.Generic.List`1<CombatUnit>,UnityEngine.Transform[]) (at Assets/Scripts/Combat/CombatManager.cs:293)
CombatManager:SpawnUnitViews () (at Assets/Scripts/Combat/CombatManager.cs:258)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:222)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

=== COMBAT STARTED === Player:1 vs Enemy:1
UnityEngine.Debug:Log (object)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:223)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[UnitStatusManager] OnCombatStarted called
UnityEngine.Debug:Log (object)
UnitStatusManager:OnCombatStarted () (at Assets/Scripts/UI/UnitStatusManager.cs:51)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:224)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[CombatCamera] Auto-fit: Size=19.25, Center=(15.62, 8.00, -2.89), Units=2
UnityEngine.Debug:Log (object)
CombatCameraManager:AutoFitUnitsInView () (at Assets/Scripts/Camera/CombatCameraManager.cs:290)
CombatCameraManager:HandleCombatStarted () (at Assets/Scripts/Camera/CombatCameraManager.cs:407)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:224)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[Phase] None → Intro
UnityEngine.Debug:Log (object)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:22)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:226)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[IntroCamera] Intro sequence BEGAN. Camera control is now locked.
UnityEngine.Debug:Log (object)
CombatCameraManager:BeginIntroSequence () (at Assets/Scripts/Camera/CombatCameraManager.cs:519)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:345)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:332)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:StartCombat (FormationData,EnemyGroupData) (at Assets/Scripts/Combat/CombatManager.cs:226)
CombatSceneStarter:Start () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:28)

[UnitStatusManager] Combat already in phase Intro, creating slots.
UnityEngine.Debug:Log (object)
UnitStatusManager:Start () (at Assets/Scripts/UI/UnitStatusManager.cs:36)

[UnitStatusManager] OnCombatStarted called
UnityEngine.Debug:Log (object)
UnitStatusManager:OnCombatStarted () (at Assets/Scripts/UI/UnitStatusManager.cs:51)
UnitStatusManager:Start () (at Assets/Scripts/UI/UnitStatusManager.cs:37)

[SceneLoaderManager] Combat scene loaded and set active.
UnityEngine.Debug:Log (object)
SceneLoaderManager/<LoadAdditive>d__12:MoveNext () (at Assets/Scripts/Combat/SceneLoaderManager.cs:39)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[UnitStatusManager] Creating 1 slots.
UnityEngine.Debug:Log (object)
UnitStatusManager:CreateSlots () (at Assets/Scripts/UI/UnitStatusManager.cs:75)
UnitStatusManager/<CreateSlotsDelayed>d__8:MoveNext () (at Assets/Scripts/UI/UnitStatusManager.cs:64)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[UnitStatusManager] Creating 1 slots.
UnityEngine.Debug:Log (object)
UnitStatusManager:CreateSlots () (at Assets/Scripts/UI/UnitStatusManager.cs:75)
UnitStatusManager/<CreateSlotsDelayed>d__8:MoveNext () (at Assets/Scripts/UI/UnitStatusManager.cs:64)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[IntroCamera] Fade-in complete.
UnityEngine.Debug:Log (object)
CombatCameraManager/<FadeInAndSetPosition>d__73:MoveNext () (at Assets/Scripts/Camera/CombatCameraManager.cs:466)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[IntroCamera] Pan complete.
UnityEngine.Debug:Log (object)
CombatCameraManager/<FadeInAndSetPosition>d__73:MoveNext () (at Assets/Scripts/Camera/CombatCameraManager.cs:484)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[IntroCamera] Zooming out to final view.
UnityEngine.Debug:Log (object)
CombatCameraManager/<ZoomOutToFinalView>d__74:MoveNext () (at Assets/Scripts/Camera/CombatCameraManager.cs:489)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:377)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatCamera] Auto-fit: Size=19.25, Center=(15.62, 8.00, -2.89), Units=2
UnityEngine.Debug:Log (object)
CombatCameraManager:AutoFitUnitsInView () (at Assets/Scripts/Camera/CombatCameraManager.cs:290)
CombatCameraManager/<ZoomOutToFinalView>d__74:MoveNext () (at Assets/Scripts/Camera/CombatCameraManager.cs:494)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:377)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[IntroCamera] Final zoom-out complete.
UnityEngine.Debug:Log (object)
CombatCameraManager/<ZoomOutToFinalView>d__74:MoveNext () (at Assets/Scripts/Camera/CombatCameraManager.cs:513)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Intro] Intro sequence finished. Starting combat.
UnityEngine.Debug:Log (object)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:408)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[IntroCamera] Intro sequence ENDED. Camera control is now unlocked.
UnityEngine.Debug:Log (object)
CombatCameraManager:EndIntroSequence () (at Assets/Scripts/Camera/CombatCameraManager.cs:525)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:409)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Phase] Intro → EnemyPlan
UnityEngine.Debug:Log (object)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:22)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)


=== ROUND 1 ===
UnityEngine.Debug:Log (object)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:477)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

--- Turn Order ---
UnityEngine.Debug:Log (object)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:482)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

1. Slime (Speed: 10)
UnityEngine.Debug:Log (object)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:485)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

2. Eugeo (Speed: 10)
UnityEngine.Debug:Log (object)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:485)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Phase] EnemyPlan → Execute
UnityEngine.Debug:Log (object)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:22)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:497)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)


--- EXECUTE ---
UnityEngine.Debug:Log (object)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:712)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:336)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:SetupRound () (at Assets/Scripts/Combat/CombatManager.cs:497)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:333)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager/<DoIntro>d__111:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:410)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

--- Lượt của: Slime ---
UnityEngine.Debug:Log (object)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:734)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[AI] Slime chuẩn bị [Smash] → [Eugeo]
UnityEngine.Debug:Log (object)
EnemyAI:PlanTurn (CombatUnit,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/EnemyAI.cs:21)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:740)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Resolve] Slime dùng Smash
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:627)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ActionResolver] Slime uses 'Smash' on 1 target(s).
UnityEngine.Debug:Log (object)
ActionResolver:Resolve (CombatUnit,SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/ClashResolver.cs:62)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:628)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Resolve] DamageEffect 'DamageEffect' deferred to animation.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:646)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Resolve] Set pending outcomes cho Slime: 1 outcomes, 1 hits.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:683)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[AnimEvent] Hit frame: 0
UnityEngine.Debug:Log (object)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:18)

  Eugeo nhận 8 dmg (True: False) → HP 92/100
UnityEngine.Debug:Log (object)
CombatUnit:TakeDamage (CombatUnit,int,bool) (at Assets/Scripts/Combat/CombatUnit.cs:120)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:249)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[Hit 0] Eugeo nhận 8 damage. HP: 92
UnityEngine.Debug:Log (object)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:255)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[AnimEvent] Skill animation end
UnityEngine.Debug:Log (object)
HitEventReceiver:OnSkillAnimationEnd () (at Assets/Scripts/Combat/HitEventReceiver.cs:33)

[ActionAnimation] Non-damage effects were already applied in ResolveAction. Skipping.
UnityEngine.Debug:Log (object)
ClashAnimationSequence/<PlayAction>d__8:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:74)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatCamera] Reset: size=19.25, pos=(15.62, 15.50, -15.88)
UnityEngine.Debug:Log (object)
CombatCameraManager:ResetCamera () (at Assets/Scripts/Camera/CombatCameraManager.cs:242)
ClashAnimationSequence/<CleanupPhase>d__19:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:360)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ClashAnimationSequence/<PlayAction>d__8:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:76)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[EVENT] Chuẩn bị kích hoạt OnActionConfirmed cho Slime với kỹ năng Smash.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:698)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

--- Lượt của: Eugeo ---
UnityEngine.Debug:Log (object)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:734)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[AP] Hồi 1 AP. Hiện có: 4
UnityEngine.Debug:Log (object)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:748)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Execute] Unit Eugeo is a player. Waiting for input...
UnityEngine.Debug:Log (object)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:751)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[PlanUI] OnPlayerTurn event received for Eugeo.
UnityEngine.Debug:Log (object)
CombatPlanningUI:OnPlayerTurn (CombatUnit) (at Assets/Scripts/Combat/CombatPlanningUI.cs:117)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:753)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[PlanUI] Found UnitView for Eugeo. Opening skill wheel.
UnityEngine.Debug:Log (object)
CombatPlanningUI:OnPlayerTurn (CombatUnit) (at Assets/Scripts/Combat/CombatPlanningUI.cs:129)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:753)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[AP] Đã dùng 0 AP. Còn lại 4.
UnityEngine.Debug:Log (object)
CombatManager:SubmitPlayerTurnAction (SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/CombatManager.cs:532)
CombatPlanningUI:OnTargetSelected (UnitView) (at Assets/Scripts/Combat/CombatPlanningUI.cs:397)
CombatPlanningUI:HandleWorldClick (UnityEngine.Vector3) (at Assets/Scripts/Combat/CombatPlanningUI.cs:206)
CombatPlanningUI:Update () (at Assets/Scripts/Combat/CombatPlanningUI.cs:104)

[Player Input] Eugeo đã chọn dùng Slash lên Slime.
UnityEngine.Debug:Log (object)
CombatManager:SubmitPlayerTurnAction (SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/CombatManager.cs:535)
CombatPlanningUI:OnTargetSelected (UnitView) (at Assets/Scripts/Combat/CombatPlanningUI.cs:397)
CombatPlanningUI:HandleWorldClick (UnityEngine.Vector3) (at Assets/Scripts/Combat/CombatPlanningUI.cs:206)
CombatPlanningUI:Update () (at Assets/Scripts/Combat/CombatPlanningUI.cs:104)

[CombatManager] Eugeo dùng kỹ năng kết thúc lượt. Chờ ExecuteRound tiếp tục.
UnityEngine.Debug:Log (object)
CombatManager:SubmitPlayerTurnAction (SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/CombatManager.cs:546)
CombatPlanningUI:OnTargetSelected (UnitView) (at Assets/Scripts/Combat/CombatPlanningUI.cs:397)
CombatPlanningUI:HandleWorldClick (UnityEngine.Vector3) (at Assets/Scripts/Combat/CombatPlanningUI.cs:206)
CombatPlanningUI:Update () (at Assets/Scripts/Combat/CombatPlanningUI.cs:104)

[Resolve] Eugeo dùng Slash
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:627)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ActionResolver] Eugeo uses 'Slash' on 1 target(s).
UnityEngine.Debug:Log (object)
ActionResolver:Resolve (CombatUnit,SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/ClashResolver.cs:62)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:628)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Resolve] DamageEffect 'DamageEffect' deferred to animation.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:646)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Resolve] Set pending outcomes cho Eugeo: 1 outcomes, 4 hits.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:683)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:761)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[AnimEvent] Hit frame: 0
UnityEngine.Debug:Log (object)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:18)

  Slime nhận 20 dmg (True: False) → HP 60/80
UnityEngine.Debug:Log (object)
CombatUnit:TakeDamage (CombatUnit,int,bool) (at Assets/Scripts/Combat/CombatUnit.cs:120)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:249)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[Hit 0] Slime nhận 20 damage. HP: 60
UnityEngine.Debug:Log (object)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:255)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[AnimEvent] VFX frame: 0
UnityEngine.Debug:Log (object)
HitEventReceiver:OnSpawnVFX (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:25)

[AnimEvent] Hit frame: 1
UnityEngine.Debug:Log (object)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:18)

  Slime nhận 20 dmg (True: False) → HP 40/80
UnityEngine.Debug:Log (object)
CombatUnit:TakeDamage (CombatUnit,int,bool) (at Assets/Scripts/Combat/CombatUnit.cs:120)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:249)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[Hit 1] Slime nhận 20 damage. HP: 40
UnityEngine.Debug:Log (object)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:255)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[AnimEvent] VFX frame: 1
UnityEngine.Debug:Log (object)
HitEventReceiver:OnSpawnVFX (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:25)

[AnimEvent] Hit frame: 2
UnityEngine.Debug:Log (object)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:18)

  Slime nhận 20 dmg (True: False) → HP 20/80
UnityEngine.Debug:Log (object)
CombatUnit:TakeDamage (CombatUnit,int,bool) (at Assets/Scripts/Combat/CombatUnit.cs:120)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:249)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[Hit 2] Slime nhận 20 damage. HP: 20
UnityEngine.Debug:Log (object)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:255)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[AnimEvent] VFX frame: 2
UnityEngine.Debug:Log (object)
HitEventReceiver:OnSpawnVFX (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:25)

[AnimEvent] Hit frame: 0
UnityEngine.Debug:Log (object)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:18)

  Slime nhận 22 dmg (True: False) → HP 0/80
UnityEngine.Debug:Log (object)
CombatUnit:TakeDamage (CombatUnit,int,bool) (at Assets/Scripts/Combat/CombatUnit.cs:120)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:249)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[Hit 3] Slime nhận 22 damage. HP: 0
UnityEngine.Debug:Log (object)
UnitView:ProcessHitAtFrame (int) (at Assets/Scripts/Combat/UnitView.cs:255)
HitEventReceiver:OnHit (int) (at Assets/Scripts/Combat/HitEventReceiver.cs:19)

[AnimEvent] Skill animation end
UnityEngine.Debug:Log (object)
HitEventReceiver:OnSkillAnimationEnd () (at Assets/Scripts/Combat/HitEventReceiver.cs:33)

[ActionAnimation] Non-damage effects were already applied in ResolveAction. Skipping.
UnityEngine.Debug:Log (object)
ClashAnimationSequence/<PlayAction>d__8:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:74)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatCamera] Reset: size=19.25, pos=(15.62, 15.50, -15.88)
UnityEngine.Debug:Log (object)
CombatCameraManager:ResetCamera () (at Assets/Scripts/Camera/CombatCameraManager.cs:242)
ClashAnimationSequence/<CleanupPhase>d__19:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:360)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ClashAnimationSequence/<PlayAction>d__8:MoveNext () (at Assets/Scripts/Combat/ClashAnimationSequence.cs:76)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[EVENT] Chuẩn bị kích hoạt OnActionConfirmed cho Eugeo với kỹ năng Slash.
UnityEngine.Debug:Log (object)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:698)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Eugeo's Passive] Nhận được sự kiện OnActionConfirmed! Đang xử lý kỹ năng: Slash.
UnityEngine.Debug:Log (object)
AleusPassive:OnOwnerActionConfirmed (CombatUnit,SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Data/Passives/AleusPassive.cs:34)
CombatUnit:RaiseActionConfirmed (SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/CombatUnit.cs:58)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:699)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Eugeo's Passive] Kỹ năng 'Slash' không phải là kỹ năng hỗ trợ đồng minh. Bỏ qua.
UnityEngine.Debug:Log (object)
AleusPassive:OnOwnerActionConfirmed (CombatUnit,SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Data/Passives/AleusPassive.cs:39)
CombatUnit:RaiseActionConfirmed (SkillData,System.Collections.Generic.List`1<CombatUnit>) (at Assets/Scripts/Combat/CombatUnit.cs:58)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:699)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Phase] Execute → Victory
UnityEngine.Debug:Log (object)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:22)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:701)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

=== VICTORY ===
UnityEngine.Debug:Log (object)
CombatManager:DoVictory () (at Assets/Scripts/Combat/CombatManager.cs:822)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:338)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:701)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatCamera] Reset: size=19.25, pos=(15.62, 15.50, -15.88)
UnityEngine.Debug:Log (object)
CombatCameraManager:ResetCamera () (at Assets/Scripts/Camera/CombatCameraManager.cs:242)
CombatCameraManager:HandleCombatEnd () (at Assets/Scripts/Camera/CombatCameraManager.cs:417)
CombatManager:DoVictory () (at Assets/Scripts/Combat/CombatManager.cs:845)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:338)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ResolveAction>d__123:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:701)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Phase] Victory → Victory
UnityEngine.Debug:Log (object)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:22)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:777)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

=== VICTORY ===
UnityEngine.Debug:Log (object)
CombatManager:DoVictory () (at Assets/Scripts/Combat/CombatManager.cs:822)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:338)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:777)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[CombatCamera] Reset: size=19.25, pos=(15.62, 15.50, -15.88)
UnityEngine.Debug:Log (object)
CombatCameraManager:ResetCamera () (at Assets/Scripts/Camera/CombatCameraManager.cs:242)
CombatCameraManager:HandleCombatEnd () (at Assets/Scripts/Camera/CombatCameraManager.cs:417)
CombatManager:DoVictory () (at Assets/Scripts/Combat/CombatManager.cs:845)
CombatManager:HandlePhaseChanged (CombatPhase,CombatPhase) (at Assets/Scripts/Combat/CombatManager.cs:338)
CombatStateMachine:TransitionTo (CombatPhase) (at Assets/Scripts/Combat/CombatStateMachine.cs:25)
CombatManager:CheckForCombatEnd () (at Assets/Scripts/Combat/CombatManager.cs:796)
CombatManager/<ExecuteRound>d__124:MoveNext () (at Assets/Scripts/Combat/CombatManager.cs:777)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Quest] Step completed: Đánh bại Slime
UnityEngine.Debug:Log (object)
QuestManager:CompleteCurrentStep () (at Assets/Scripts/Quest/QuestManager.cs:137)
QuestManager:OnEnemyGroupDefeated (EnemyGroupData) (at Assets/Scripts/Quest/QuestManager.cs:129)
CombatSceneStarter/<HandleCombatEnd>d__6:MoveNext () (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:55)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

NullReferenceException: Object reference not set to an instance of an object
SceneLoaderManager+<UnloadAdditive>d__14.MoveNext () (at Assets/Scripts/Combat/SceneLoaderManager.cs:51)
UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at <dc764b59c049482b93bebc78a7e33a04>:0)
UnityEngine.MonoBehaviour:StartCoroutine(IEnumerator)
SceneLoaderManager:UnloadCombatScene() (at Assets/Scripts/Combat/SceneLoaderManager.cs:45)
<HandleCombatEnd>d__6:MoveNext() (at Assets/Scripts/Combat/Formation/CombatSceneStarter.cs:61)
UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)

[SceneLoaderManager] MapRoot activated.
UnityEngine.Debug:Log (object)
SceneLoaderManager/<UnloadAdditive>d__14:MoveNext () (at Assets/Scripts/Combat/SceneLoaderManager.cs:60)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Quest] Step completed: Trò chuyện với Vergil
UnityEngine.Debug:Log (object)
QuestManager:CompleteCurrentStep () (at Assets/Scripts/Quest/QuestManager.cs:137)
QuestManager:OnDialogueEnded (string) (at Assets/Scripts/Quest/QuestManager.cs:119)
DialogueTrigger:OnDialogueComplete () (at Assets/Scripts/Dialogue/DialogueTrigger.cs:364)
DialogueBubbleUI:ShowSequential (DialogueLineData[],UnityEngine.Transform,System.Action,int) (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:60)
DialogueBubbleUI/<>c__DisplayClass20_0:<ShowSequential>b__0 () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:63)
DialogueBubbleUI:Hide () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:73)
DialogueBubbleUI:Update () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:40)

[Quest] Quest completed: Khởi đầu mới
UnityEngine.Debug:Log (object)
QuestManager:CompleteCurrentStep () (at Assets/Scripts/Quest/QuestManager.cs:144)
QuestManager:OnDialogueEnded (string) (at Assets/Scripts/Quest/QuestManager.cs:119)
DialogueTrigger:OnDialogueComplete () (at Assets/Scripts/Dialogue/DialogueTrigger.cs:364)
DialogueBubbleUI:ShowSequential (DialogueLineData[],UnityEngine.Transform,System.Action,int) (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:60)
DialogueBubbleUI/<>c__DisplayClass20_0:<ShowSequential>b__0 () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:63)
DialogueBubbleUI:Hide () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:73)
DialogueBubbleUI:Update () (at Assets/Scripts/Dialogue/DialogueBubbleUI.cs:40)

[FormationManager] Đã mở khóa nhân vật mới: 'Vergil'
UnityEngine.Debug:Log (object)
FormationManager:UnlockCharacter (CharacterData) (at Assets/Scripts/Combat/Formation/FormationManager.cs:332)
QuestManager:ApplyRewards (QuestReward[]) (at Assets/Scripts/Quest/QuestManager.cs:210)
QuestManager/<>c__DisplayClass31_0:<GiveRewards>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:187)
QuestRewardUI:OnConfirmClicked () (at Assets/Scripts/Quest/QuestRewardUI.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@bb329a87fcdc/Runtime/UGUI/EventSystem/EventSystem.cs:514)

[Quest Reward] Mở khóa nhân vật: Vergil
UnityEngine.Debug:Log (object)
QuestManager:ApplyRewards (QuestReward[]) (at Assets/Scripts/Quest/QuestManager.cs:211)
QuestManager/<>c__DisplayClass31_0:<GiveRewards>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:187)
QuestRewardUI:OnConfirmClicked () (at Assets/Scripts/Quest/QuestRewardUI.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@bb329a87fcdc/Runtime/UGUI/EventSystem/EventSystem.cs:514)

[Quest] Starting next quest: Mạnh mẽ
UnityEngine.Debug:Log (object)
QuestManager:StartNextQuest () (at Assets/Scripts/Quest/QuestManager.cs:176)
QuestManager/<>c__DisplayClass29_0:<CompleteQuestAndAdvance>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:163)
QuestManager/<>c__DisplayClass31_0:<GiveRewards>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:188)
QuestRewardUI:OnConfirmClicked () (at Assets/Scripts/Quest/QuestRewardUI.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@bb329a87fcdc/Runtime/UGUI/EventSystem/EventSystem.cs:514)

[Quest] Started fresh quest: Mạnh mẽ
UnityEngine.Debug:Log (object)
QuestManager:StartQuest (QuestData) (at Assets/Scripts/Quest/QuestManager.cs:109)
QuestManager:StartNextQuest () (at Assets/Scripts/Quest/QuestManager.cs:177)
QuestManager/<>c__DisplayClass29_0:<CompleteQuestAndAdvance>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:163)
QuestManager/<>c__DisplayClass31_0:<GiveRewards>b__0 () (at Assets/Scripts/Quest/QuestManager.cs:188)
QuestRewardUI:OnConfirmClicked () (at Assets/Scripts/Quest/QuestRewardUI.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@bb329a87fcdc/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SerializationException: Type 'InventoryUI' in Assembly 'Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is not marked as serializable.
System.Runtime.Serialization.FormatterServices.InternalGetSerializableMembers (System.RuntimeType type) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.FormatterServices+<>c__DisplayClass9_0.<GetSerializableMembers>b__0 (System.Runtime.Serialization.MemberHolder _) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Collections.Concurrent.ConcurrentDictionary`2[TKey,TValue].GetOrAdd (TKey key, System.Func`2[T,TResult] valueFactory) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.FormatterServices.GetSerializableMembers (System.Type type, System.Runtime.Serialization.StreamingContext context) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.WriteObjectInfo.InitMemberInfo () (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.WriteObjectInfo.InitSerialize (System.Object obj, System.Runtime.Serialization.ISurrogateSelector surrogateSelector, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.Formatters.Binary.SerObjectInfoInit serObjectInfoInit, System.Runtime.Serialization.IFormatterConverter converter, System.Runtime.Serialization.Formatters.Binary.ObjectWriter objectWriter, System.Runtime.Serialization.SerializationBinder binder) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.WriteObjectInfo.Serialize (System.Object obj, System.Runtime.Serialization.ISurrogateSelector surrogateSelector, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.Formatters.Binary.SerObjectInfoInit serObjectInfoInit, System.Runtime.Serialization.IFormatterConverter converter, System.Runtime.Serialization.Formatters.Binary.ObjectWriter objectWriter, System.Runtime.Serialization.SerializationBinder binder) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.ObjectWriter.Write (System.Runtime.Serialization.Formatters.Binary.WriteObjectInfo objectInfo, System.Runtime.Serialization.Formatters.Binary.NameInfo memberNameInfo, System.Runtime.Serialization.Formatters.Binary.NameInfo typeNameInfo) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.ObjectWriter.Serialize (System.Object graph, System.Runtime.Remoting.Messaging.Header[] inHeaders, System.Runtime.Serialization.Formatters.Binary.__BinaryWriter serWriter, System.Boolean fCheck) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Serialize (System.IO.Stream serializationStream, System.Object graph, System.Runtime.Remoting.Messaging.Header[] headers, System.Boolean fCheck) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Serialize (System.IO.Stream serializationStream, System.Object graph, System.Runtime.Remoting.Messaging.Header[] headers) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
System.Runtime.Serialization.Formatters.Binary.BinaryFormatter.Serialize (System.IO.Stream serializationStream, System.Object graph) (at <41d29b352f6a475ab1bf7c6628b82790>:0)
InventoryManager.SaveToFile () (at Assets/Scripts/Inventory/InventoryManager.cs:52)D:\unity\gametn\COMPLETE_GAME_SYSTEM_DOCUMENTATION.md
đọc file md trong đường dẫn
InventoryManager.SaveInventory () (at Assets/Scripts/Inventory/InventoryManager.cs:45)
InventoryManager.OnApplicationQuit () (at Assets/Scripts/Inventory/InventoryManager.cs:75)

