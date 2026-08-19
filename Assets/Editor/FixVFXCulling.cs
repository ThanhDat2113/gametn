using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Editor script tự động sửa culling flags cho tất cả VFX Graph (.vfx) files.
/// 
/// Vấn đề: VFX Graph mặc định có m_CullingFlags = 3 (CullByDistance | CullByViewportSize)
/// và bounds nhỏ (1x1x1) → khi camera zoom ra xa, VFX bị culling và không hiển thị.
/// 
/// Giải pháp: Đặt m_CullingFlags = 0 (tắt culling) để VFX luôn render.
/// </summary>
public static class FixVFXCulling
{
    [MenuItem("Tools/Fix VFX Culling (All .vfx files)")]
    public static void FixAllVFXCulling()
    {
        string[] vfxFiles = Directory.GetFiles(Application.dataPath, "*.vfx", SearchOption.AllDirectories);
        int fixedCount = 0;

        foreach (string file in vfxFiles)
        {
            if (FixVFXFile(file))
                fixedCount++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[FixVFXCulling] Đã sửa {fixedCount}/{vfxFiles.Length} file VFX. Tắt culling để VFX luôn render khi zoom xa.");
    }

    private static bool FixVFXFile(string filePath)
    {
        string content = File.ReadAllText(filePath);
        string original = content;

        // Tắt hoàn toàn culling: m_CullingFlags: 3 → 0
        // Chỉ tắt culling, KHÔNG thay đổi boundsMode/needsComputeBounds để tránh gây lỗi VFX Graph
        content = Regex.Replace(content, @"m_CullingFlags:\s*\d+", "m_CullingFlags: 0");

        if (content != original)
        {
            File.WriteAllText(filePath, content);
            Debug.Log($"[FixVFXCulling] Đã sửa: {Path.GetFileName(filePath)}");
            return true;
        }
        return false;
    }
}