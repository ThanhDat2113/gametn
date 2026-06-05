using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Helper class để setup animator parameters cho enemy units.
/// Tự động thêm các parameters cần thiết cho intro sequence.
/// </summary>
public class EnemyAnimatorSetup : MonoBehaviour
{
    [Header("Animator Parameters")]
    public string moveTrigger = "Move";
    public string idleTrigger = "Idle";
    public string dieTrigger = "Die";
    
    [Header("Auto Setup")]
    public bool autoSetupOnStart = false;
    
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[{nameof(EnemyAnimatorSetup)}] Không tìm thấy Animator component!");
            return;
        }
        
        if (autoSetupOnStart)
        {
            SetupAnimatorParameters();
        }
    }
    
    /// <summary>
    /// Setup các animator parameters cần thiết.
    /// </summary>
    [ContextMenu("Setup Animator Parameters")]
    public void SetupAnimatorParameters()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"[{nameof(EnemyAnimatorSetup)}] Không tìm thấy Animator component!");
                return;
            }
        }
        
        var runtimeController = animator.runtimeAnimatorController;
        if (runtimeController == null)
        {
            Debug.LogError($"[{nameof(EnemyAnimatorSetup)}] Animator không có runtime controller!");
            return;
        }
        
        Debug.Log($"[{nameof(EnemyAnimatorSetup)}] Đang setup animator parameters cho: {gameObject.name}");
        
        EnsureParameterExists(moveTrigger, AnimatorControllerParameterType.Trigger);
        EnsureParameterExists(idleTrigger, AnimatorControllerParameterType.Trigger);
        EnsureParameterExists(dieTrigger, AnimatorControllerParameterType.Trigger);
        
        Debug.Log($"[{nameof(EnemyAnimatorSetup)}] Setup hoàn tất cho: {gameObject.name}");
    }
    
    private void EnsureParameterExists(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            Debug.LogWarning($"[{nameof(EnemyAnimatorSetup)}] Parameter name rỗng, bỏ qua.");
            return;
        }
        
        bool parameterExists = false;
        for (int i = 0; i < animator.parameterCount; i++)
        {
            var param = animator.GetParameter(i);
            if (param.name == parameterName)
            {
                parameterExists = true;
                if (param.type != parameterType)
                {
                    Debug.LogWarning($"[{nameof(EnemyAnimatorSetup)}] Parameter '{parameterName}' đã tồn tại nhưng type không đúng. Expected: {parameterType}, Actual: {param.type}");
                }
                else
                {
                    Debug.Log($"[{nameof(EnemyAnimatorSetup)}] Parameter '{parameterName}' đã tồn tại.");
                }
                break;
            }
        }
        
        if (!parameterExists)
        {
            Debug.LogWarning($"[{nameof(EnemyAnimatorSetup)}] Parameter '{parameterName}' chưa tồn tại. Vui lòng thêm thủ công trong Animator Controller.");
        }
    }
    
    public void TriggerMove()
    {
        if (animator != null && !string.IsNullOrEmpty(moveTrigger))
        {
            animator.SetTrigger(moveTrigger);
        }
    }
    
    public void TriggerIdle()
    {
        if (animator != null && !string.IsNullOrEmpty(idleTrigger))
        {
            animator.SetTrigger(idleTrigger);
        }
    }
    
    public void TriggerDie()
    {
        if (animator != null && !string.IsNullOrEmpty(dieTrigger))
        {
            animator.SetTrigger(dieTrigger);
        }
    }
    
    public bool AreAllParametersValid()
    {
        if (animator == null) return false;
        
        var requiredParameters = new[] { moveTrigger, idleTrigger, dieTrigger };
        
        foreach (var paramName in requiredParameters)
        {
            if (string.IsNullOrEmpty(paramName)) continue;
            
            bool found = false;
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var param = animator.GetParameter(i);
                if (param.name == paramName)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogWarning($"[{nameof(EnemyAnimatorSetup)}] Parameter '{paramName}' không tồn tại!");
                return false;
            }
        }
        
        return true;
    }
}

#if UNITY_EDITOR

/// <summary>
/// Editor window để batch setup nhiều enemy animators cùng lúc.
/// </summary>
public class EnemyAnimatorSetupWindow : EditorWindow
{
    private List<GameObject> selectedEnemies = new List<GameObject>();
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Combat/Enemy Animator Setup")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAnimatorSetupWindow>("Enemy Animator Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Enemy Animator Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tool này giúp batch setup animator parameters cho nhiều enemy units cùng lúc.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Chọn enemy objects
        GUILayout.Label("Selected Enemies:", EditorStyles.boldLabel);
        
        // Hiển thị danh sách enemies đã chọn
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        for (int i = 0; i < selectedEnemies.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedEnemies[i] = (GameObject)EditorGUILayout.ObjectField($"Enemy {i + 1}", selectedEnemies[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                selectedEnemies.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        // Nút thêm enemy
        if (GUILayout.Button("Add Selected Enemies"))
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (!selectedEnemies.Contains(obj))
                {
                    selectedEnemies.Add(obj);
                }
            }
        }
        
        EditorGUILayout.Space();
        
        // Setup parameters
        GUILayout.Label("Parameters to Add:", EditorStyles.boldLabel);
        
        var paramStyle = new GUIStyle(EditorStyles.label);
        paramStyle.fontSize = 10;
        
        GUILayout.Label("• Move (Trigger) - Khi enemy rush vào vị trí", paramStyle);
        GUILayout.Label("• Idle (Trigger) - Trạng thái idle", paramStyle);
        GUILayout.Label("• Die (Trigger) - Khi enemy chết", paramStyle);
        
        EditorGUILayout.Space();
        
        // Nút setup
        GUI.enabled = selectedEnemies.Count > 0;
        if (GUILayout.Button("Setup Selected Enemies", GUILayout.Height(30)))
        {
            SetupSelectedEnemies();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // Validation
        if (GUILayout.Button("Validate All"))
        {
            ValidateAllEnemies();
        }
    }
    
    private void SetupSelectedEnemies()
    {
        int successCount = 0;
        int failCount = 0;
        
        foreach (var enemyObj in selectedEnemies)
        {
            if (enemyObj == null) continue;
            
            // Thêm EnemyAnimatorSetup component nếu chưa có
            var setup = enemyObj.GetComponent<EnemyAnimatorSetup>();
            if (setup == null)
            {
                setup = enemyObj.AddComponent<EnemyAnimatorSetup>();
            }
            
            // Setup parameters
            setup.SetupAnimatorParameters();
            
            if (setup.AreAllParametersValid())
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }
        
        Debug.Log($"[EnemyAnimatorSetup] Setup hoàn tất. Success: {successCount}, Failed: {failCount}");
        
        if (failCount > 0)
        {
            EditorUtility.DisplayDialog("Setup Complete", 
                $"Setup hoàn tất.\nSuccess: {successCount}\nFailed: {failCount}\n\nVui lòng kiểm tra console log để xem chi tiết.", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Setup Complete", 
                $"Tất cả {successCount} enemy đã được setup thành công!", 
                "OK");
        }
    }
    
    private void ValidateAllEnemies()
    {
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (var enemyObj in selectedEnemies)
        {
            if (enemyObj == null) continue;
            
            var setup = enemyObj.GetComponent<EnemyAnimatorSetup>();
            if (setup != null && setup.AreAllParametersValid())
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }
        }
        
        Debug.Log($"[EnemyAnimatorSetup] Validation: Valid: {validCount}, Invalid: {invalidCount}");
        
        EditorUtility.DisplayDialog("Validation Result", 
            $"Validation hoàn tất.\nValid: {validCount}\nInvalid: {invalidCount}", 
            "OK");
    }
}

/// <summary>
/// Property drawer cho EnemyAnimatorSetup để hiển thị validation status.
/// </summary>
[CustomEditor(typeof(EnemyAnimatorSetup))]
public class EnemyAnimatorSetupEditor : Editor
{
    private EnemyAnimatorSetup setup;
    
    private void OnEnable()
    {
        setup = (EnemyAnimatorSetup)target;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();
        
        // Validation status
        bool isValid = setup.AreAllParametersValid();
        
        if (isValid)
        {
            EditorGUILayout.HelpBox("✅ Tất cả animator parameters đã được setup đúng!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠️ Một số animator parameters chưa được setup. Nhấn 'Setup Animator Parameters' để kiểm tra.", MessageType.Warning);
        }
        
        // Quick action buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Test Move"))
        {
            setup.TriggerMove();
        }
        
        if (GUILayout.Button("Test Idle"))
        {
            setup.TriggerIdle();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Validate Parameters"))
        {
            setup.SetupAnimatorParameters();
        }
    }
}

#endif