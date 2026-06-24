using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script để tái cấu trúc project.
/// Chạy: Tools → Restructure Project
/// </summary>
public class RestructureProject : EditorWindow
{
    [MenuItem("Tools/Restructure Project")]
    public static void ShowWindow()
    {
        GetWindow<RestructureProject>("Restructure Project");
    }

    private bool dryRun = true;
    private Vector2 scrollPos;
    private string log = "";

    private void OnGUI()
    {
        GUILayout.Label("Restructure Project", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Script này sẽ di chuyển files để tái cấu trúc project.\n" +
            "GUIDs được giữ nguyên — references không bị hỏng.",
            MessageType.Info);

        dryRun = EditorGUILayout.Toggle("Dry Run (chỉ log, không move)", dryRun);

        if (GUILayout.Button("Run Restructure"))
        {
            log = "";
            RunRestructure();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunRestructure()
    {
        Log("=== BẮT ĐẦU TÁI CẤU TRÚC ===\n");

        // 1. Scripts reorganization
        MoveFile("Assets/Scripts/Managers/GameInitializer.cs", "Assets/_Project/Scripts/Core/GameInitializer.cs");
        MoveFile("Assets/Scripts/Managers/PlayerProgression.cs", "Assets/_Project/Scripts/Core/PlayerProgression.cs");
        MoveFile("Assets/Scripts/Audio/AudioManager.cs", "Assets/_Project/Scripts/Core/AudioManager.cs");
        MoveFile("Assets/Scripts/Combat/SceneLoaderManager.cs", "Assets/_Project/Scripts/Core/SceneLoaderManager.cs");
        MoveFile("Assets/Scripts/Managers/EventManager.cs", "Assets/_Project/Scripts/Core/EventManager.cs");
        MoveFile("Assets/Scripts/Managers/GameEvent.cs", "Assets/_Project/Scripts/Core/GameEvent.cs");

        // Combat
        MoveFile("Assets/Scripts/Combat/CombatManager.cs", "Assets/_Project/Scripts/Combat/CombatManager.cs");
        MoveFile("Assets/Scripts/Combat/CombatUnit.cs", "Assets/_Project/Scripts/Combat/CombatUnit.cs");
        MoveFile("Assets/Scripts/Combat/CombatStateMachine.cs", "Assets/_Project/Scripts/Combat/CombatStateMachine.cs");
        MoveFile("Assets/Scripts/Combat/CombatAudioManager.cs", "Assets/_Project/Scripts/Combat/CombatAudioManager.cs");
        MoveFile("Assets/Scripts/Combat/CombatResultUI.cs", "Assets/_Project/Scripts/Combat/CombatResultUI.cs");
        MoveFile("Assets/Scripts/Combat/CombatPlanningUI.cs", "Assets/_Project/Scripts/UI/Combat/CombatPlanningUI.cs");
        MoveFile("Assets/Scripts/Combat/ClashAnimationSequence.cs", "Assets/_Project/Scripts/Combat/ClashAnimationSequence.cs");
        MoveFile("Assets/Scripts/Combat/ClashResolver.cs", "Assets/_Project/Scripts/Combat/ClashResolver.cs");
        MoveFile("Assets/Scripts/Combat/EnemyAI.cs", "Assets/_Project/Scripts/Combat/AI/EnemyAI.cs");
        MoveFile("Assets/Scripts/Combat/UnitView.cs", "Assets/_Project/Scripts/Combat/UnitView.cs");
        MoveFile("Assets/Scripts/Combat/HitEventReceiver.cs", "Assets/_Project/Scripts/Combat/HitEventReceiver.cs");
        MoveFile("Assets/Scripts/Combat/HitData.cs", "Assets/_Project/Scripts/Combat/HitData.cs");
        MoveFile("Assets/Scripts/Combat/ActionSlotUI.cs", "Assets/_Project/Scripts/UI/Combat/ActionSlotUI.cs");
        MoveFile("Assets/Scripts/Combat/TargetingArrowController.cs", "Assets/_Project/Scripts/UI/Combat/TargetingArrowController.cs");
        MoveFile("Assets/Scripts/Combat/DamageText.cs", "Assets/_Project/Scripts/UI/Combat/DamageText.cs");
        MoveFile("Assets/Scripts/Combat/DamageTextManager.cs", "Assets/_Project/Scripts/UI/Combat/DamageTextManager.cs");
        MoveFile("Assets/Scripts/Combat/CombatTestStarter.cs", "Assets/_Project/Scripts/Combat/CombatTestStarter.cs");
        MoveFile("Assets/Scripts/Combat/EnemyGroupData.cs", "Assets/_Project/Scripts/Data/EnemyGroupData.cs");
        MoveFile("Assets/Scripts/Combat/FormationData.cs", "Assets/_Project/Scripts/Data/FormationData.cs");
        MoveFile("Assets/Scripts/Combat/AnimationConstants.cs", "Assets/_Project/Scripts/Combat/AnimationConstants.cs");

        // Formation
        MoveFile("Assets/Scripts/Combat/Formation/FormationManager.cs", "Assets/_Project/Scripts/Map/Formation/FormationManager.cs");
        MoveFile("Assets/Scripts/Combat/Formation/SlotUI.cs", "Assets/_Project/Scripts/UI/Combat/SlotUI.cs");
        MoveFile("Assets/Scripts/Combat/Formation/CharacterDragItem.cs", "Assets/_Project/Scripts/UI/Combat/CharacterDragItem.cs");
        MoveFile("Assets/Scripts/Combat/Formation/CombatSceneStarter.cs", "Assets/_Project/Scripts/Combat/CombatSceneStarter.cs");
        MoveFile("Assets/Scripts/Combat/Formation/FormationDataStorage.cs", "Assets/_Project/Scripts/Data/FormationDataStorage.cs");

        // Data
        MoveFile("Assets/Scripts/Data/CharacterData.cs", "Assets/_Project/Scripts/Characters/CharacterData.cs");
        MoveFile("Assets/Scripts/Data/SkillData.cs", "Assets/_Project/Scripts/Skills/SkillData.cs");
        MoveFile("Assets/Scripts/Data/SkillEffect.cs", "Assets/_Project/Scripts/Skills/SkillEffect.cs");
        MoveFile("Assets/Scripts/Data/ExperienceConfig.cs", "Assets/_Project/Scripts/Data/ExperienceConfig.cs");
        MoveFile("Assets/Scripts/Data/ItemData.cs", "Assets/_Project/Scripts/Systems/Inventory/ItemData.cs");
        MoveFile("Assets/Scripts/Data/EquipmentData.cs", "Assets/_Project/Scripts/Systems/Equipment/EquipmentData.cs");

        // Effects
        MoveFile("Assets/Scripts/Data/Effects/DamageEffect.cs", "Assets/_Project/Scripts/Combat/Effects/DamageEffect.cs");
        MoveFile("Assets/Scripts/Data/Effects/HealEffect.cs", "Assets/_Project/Scripts/Combat/Effects/HealEffect.cs");
        MoveFile("Assets/Scripts/Data/Effects/BuffStatEffect.cs", "Assets/_Project/Scripts/Combat/Effects/BuffStatEffect.cs");
        MoveFile("Assets/Scripts/Data/Effects/ApplyStatusEffect.cs", "Assets/_Project/Scripts/Combat/Effects/ApplyStatusEffect.cs");
        MoveFile("Assets/Scripts/Data/Effects/ShieldEffect.cs", "Assets/_Project/Scripts/Combat/Effects/ShieldEffect.cs");
        MoveFile("Assets/Scripts/Data/Effects/LifeStealEffect.cs", "Assets/_Project/Scripts/Combat/Effects/LifeStealEffect.cs");

        // Passives
        MoveFile("Assets/Scripts/Data/Passives/PassiveAbility.cs", "Assets/_Project/Scripts/Characters/Passives/PassiveAbility.cs");
        MoveFile("Assets/Scripts/Data/Passives/LucioPassive.cs", "Assets/_Project/Scripts/Characters/Passives/LucioPassive.cs");
        MoveFile("Assets/Scripts/Data/Passives/CharlottePassive.cs", "Assets/_Project/Scripts/Characters/Passives/CharlottePassive.cs");
        MoveFile("Assets/Scripts/Data/Passives/AeosPassive.cs", "Assets/_Project/Scripts/Characters/Passives/AeosPassive.cs");
        MoveFile("Assets/Scripts/Data/Passives/AleusPassive.cs", "Assets/_Project/Scripts/Characters/Passives/AleusPassive.cs");
        MoveFile("Assets/Scripts/Data/Passives/CelinePassive.cs", "Assets/_Project/Scripts/Characters/Passives/CelinePassive.cs");
        MoveFile("Assets/Scripts/Data/Passives/HanaPassive.cs", "Assets/_Project/Scripts/Characters/Passives/HanaPassive.cs");

        // Systems
        MoveFile("Assets/Scripts/Quest/QuestManager.cs", "Assets/_Project/Scripts/Systems/Quest/QuestManager.cs");
        MoveFile("Assets/Scripts/Quest/QuestData.cs", "Assets/_Project/Scripts/Systems/Quest/QuestData.cs");
        MoveFile("Assets/Scripts/Quest/QuestReward.cs", "Assets/_Project/Scripts/Systems/Quest/QuestReward.cs");
        MoveFile("Assets/Scripts/Quest/QuestStep.cs", "Assets/_Project/Scripts/Systems/Quest/QuestStep.cs");
        MoveFile("Assets/Scripts/Quest/QuestUI.cs", "Assets/_Project/Scripts/UI/Quest/QuestUI.cs");
        MoveFile("Assets/Scripts/Quest/QuestRewardUI.cs", "Assets/_Project/Scripts/UI/Quest/QuestRewardUI.cs");

        MoveFile("Assets/Scripts/Dialogue/DialogueTrigger.cs", "Assets/_Project/Scripts/Systems/Dialogue/DialogueTrigger.cs");
        MoveFile("Assets/Scripts/Dialogue/DialogueBubbleUI.cs", "Assets/_Project/Scripts/UI/Dialogue/DialogueBubbleUI.cs");
        MoveFile("Assets/Scripts/Dialogue/DialogueLineData.cs", "Assets/_Project/Scripts/Systems/Dialogue/DialogueLineData.cs");
        MoveFile("Assets/Scripts/Dialogue/DialogueCharacter.cs", "Assets/_Project/Scripts/Systems/Dialogue/DialogueCharacter.cs");
        MoveFile("Assets/Scripts/Dialogue/FadeController.cs", "Assets/_Project/Scripts/Core/FadeController.cs");
        MoveFile("Assets/Scripts/Dialogue/MainMenu.cs", "Assets/_Project/Scripts/UI/MainMenu.cs");

        MoveFile("Assets/Scripts/Inventory/Inventory.cs", "Assets/_Project/Scripts/Systems/Inventory/Inventory.cs");
        MoveFile("Assets/Scripts/Inventory/InventoryManager.cs", "Assets/_Project/Scripts/Systems/Inventory/InventoryManager.cs");
        MoveFile("Assets/Scripts/Inventory/InventoryUI.cs", "Assets/_Project/Scripts/UI/Inventory/InventoryUI.cs");
        MoveFile("Assets/Scripts/Inventory/InventorySlotUI.cs", "Assets/_Project/Scripts/UI/Inventory/InventorySlotUI.cs");

        // Camera
        MoveFile("Assets/Scripts/Camera/CombatCameraManager.cs", "Assets/_Project/Scripts/Combat/CombatCameraManager.cs");
        MoveFile("Assets/Scripts/Camera/CombatCameraAnimationIntegration.cs", "Assets/_Project/Scripts/Combat/CombatCameraAnimationIntegration.cs");

        // Map
        MoveFile("Assets/Scripts/MapGamePlay/Mapuicontroller.cs", "Assets/_Project/Scripts/Map/MapUIController.cs");
        MoveFile("Assets/Scripts/MapGamePlay/BillboardSprite.cs", "Assets/_Project/Scripts/Map/BillboardSprite.cs");
        MoveFile("Assets/Scripts/Combat/MapEnemy.cs", "Assets/_Project/Scripts/Map/MapEnemy.cs");

        // Enums
        MoveFile("Assets/Scripts/Enums/CombatEnums.cs", "Assets/_Project/Scripts/Enums/CombatEnums.cs");

        // UI
        MoveFile("Assets/Scripts/UI/MapMenuManager.cs", "Assets/_Project/Scripts/UI/Map/MapMenuManager.cs");
        MoveFile("Assets/Scripts/UI/CharacterSlotUI.cs", "Assets/_Project/Scripts/UI/Map/CharacterSlotUI.cs");
        MoveFile("Assets/Scripts/UI/UnitStatusManager.cs", "Assets/_Project/Scripts/UI/Combat/UnitStatusManager.cs");
        MoveFile("Assets/Scripts/UI/UnitStatusSlot.cs", "Assets/_Project/Scripts/UI/Combat/UnitStatusSlot.cs");
        MoveFile("Assets/Scripts/UI/FloatingText.cs", "Assets/_Project/Scripts/UI/Shared/FloatingText.cs");
        MoveFile("Assets/Scripts/UI/FloatingTextController.cs", "Assets/_Project/Scripts/UI/Shared/FloatingTextController.cs");
        MoveFile("Assets/Scripts/UI/CharacterPanelManager.cs", "Assets/_Project/Scripts/UI/Map/CharacterPanelManager.cs");

        // Equipment
        MoveFile("Assets/Scripts/Equipment/EquipmentPanel.cs", "Assets/_Project/Scripts/UI/Equipment/EquipmentPanel.cs");
        MoveFile("Assets/Scripts/Equipment/CharacterEquipment.cs", "Assets/_Project/Scripts/Systems/Equipment/CharacterEquipment.cs");
        MoveFile("Assets/Scripts/Equipment/EquipmentManager.cs", "Assets/_Project/Scripts/Systems/Equipment/EquipmentManager.cs");

        // Loading
        MoveFile("Assets/Scripts/Loading/LoadingSceneController.cs", "Assets/_Project/Scripts/UI/Loading/LoadingSceneController.cs");
        MoveFile("Assets/Scripts/Loading/SceneLoader.cs", "Assets/_Project/Scripts/Core/SceneLoader.cs");

        // AI BehaviorTree
        MoveFile("Assets/Scripts/AI/BehaviorTree/Node.cs", "Assets/_Project/Scripts/Combat/AI/BehaviorTree/Node.cs");
        MoveFile("Assets/Scripts/AI/BehaviorTree/Selector.cs", "Assets/_Project/Scripts/Combat/AI/BehaviorTree/Selector.cs");
        MoveFile("Assets/Scripts/AI/BehaviorTree/Sequence.cs", "Assets/_Project/Scripts/Combat/AI/BehaviorTree/Sequence.cs");
        MoveFile("Assets/Scripts/AI/BehaviorTree/AttackClosestEnemyNode.cs", "Assets/_Project/Scripts/Combat/AI/BehaviorTree/AttackClosestEnemyNode.cs");

        // Status Effects
        MoveFile("Assets/Scripts/Combat/StatusEffects/ChallengeStack.cs", "Assets/_Project/Scripts/Combat/StatusEffects/ChallengeStack.cs");
        MoveFile("Assets/Scripts/Data/Effects/StatusEffectType.cs", "Assets/_Project/Scripts/Combat/StatusEffects/StatusEffectType.cs");

        // Commands
        MoveFile("Assets/Scripts/Combat/Commands/ICombatCommand.cs", "Assets/_Project/Scripts/Combat/Commands/ICombatCommand.cs");
        MoveFile("Assets/Scripts/Combat/Commands/DamageCommand.cs", "Assets/_Project/Scripts/Combat/Commands/DamageCommand.cs");
        MoveFile("Assets/Scripts/Combat/Commands/MultiHitDamageCommand.cs", "Assets/_Project/Scripts/Combat/Commands/MultiHitDamageCommand.cs");

        // Extra combat files
        MoveFile("Assets/Scripts/Combat/ClashVisualController.cs", "Assets/_Project/Scripts/Combat/ClashVisualController.cs");
        MoveFile("Assets/Scripts/Combat/EnemyAnimatorSetup.cs", "Assets/_Project/Scripts/Combat/EnemyAnimatorSetup.cs");
        MoveFile("Assets/Scripts/Combat/CombatSessionData.cs", "Assets/_Project/Scripts/Data/CombatSessionData.cs");
        MoveFile("Assets/Scripts/Combat/CombatExperienceManager.cs", "Assets/_Project/Scripts/Combat/CombatExperienceManager.cs");
        MoveFile("Assets/Scripts/Combat/FormationProgressHelper.cs", "Assets/_Project/Scripts/Map/Formation/FormationProgressHelper.cs");

        // PlayerProgressData
        MoveFile("Assets/Scripts/Data/PlayerProgressData.cs", "Assets/_Project/Scripts/Core/PlayerProgressData.cs");

        // New combat result files
        MoveFile("Assets/Scripts/Combat/VictoryPanel.cs", "Assets/_Project/Scripts/UI/Combat/VictoryPanel.cs");
        MoveFile("Assets/Scripts/Combat/VictoryPanelController.cs", "Assets/_Project/Scripts/UI/Combat/VictoryPanelController.cs");
        MoveFile("Assets/Scripts/Combat/VictoryEntryUI.cs", "Assets/_Project/Scripts/UI/Combat/VictoryEntryUI.cs");
        MoveFile("Assets/Scripts/Combat/DefeatPanel.cs", "Assets/_Project/Scripts/UI/Combat/DefeatPanel.cs");

        // Equipment additional
        MoveFile("Assets/Scripts/Equipment/EquipmentDragItem.cs", "Assets/_Project/Scripts/UI/Equipment/EquipmentDragItem.cs");
        MoveFile("Assets/Scripts/Equipment/EquipmentListUI.cs", "Assets/_Project/Scripts/UI/Equipment/EquipmentListUI.cs");

        Log("\n=== HOÀN THÀNH ===");
        if (dryRun)
        {
            Log("(Dry Run — không có file nào thực sự được move)");
            Log("Tắt Dry Run và chạy lại để thực hiện.");
        }
        else
        {
            Log("Đã move files. Refresh Asset Database...");
            AssetDatabase.Refresh();
        }
    }

    private void MoveFile(string source, string dest)
    {
        if (!File.Exists(source))
        {
            Log($"⚠️ SKIP: {source} không tồn tại");
            return;
        }

        // Tạo folder đích
        string destDir = Path.GetDirectoryName(dest);
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        if (dryRun)
        {
            Log($"📋 WOULD MOVE: {source} → {dest}");
            return;
        }

        string error = AssetDatabase.MoveAsset(source, dest);
        if (string.IsNullOrEmpty(error))
            Log($"✅ MOVED: {source} → {dest}");
        else
            Log($"❌ ERROR moving {source}: {error}");
    }

    private void Log(string msg)
    {
        log += msg + "\n";
        Debug.Log(msg);
    }
}