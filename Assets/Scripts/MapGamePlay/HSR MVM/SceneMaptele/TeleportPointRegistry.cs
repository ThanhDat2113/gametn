using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý và registry tất cả TeleportPoint trong scene
/// Giúp dễ dàng lấy danh sách và chọn spawn point
/// </summary>
public class TeleportPointRegistry : MonoBehaviour
{
    private static List<TeleportPoint> registeredPoints = new List<TeleportPoint>();
    private static TeleportPointRegistry _instance;

    public static TeleportPointRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TeleportPointRegistry>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TeleportPointRegistry");
                    _instance = obj.AddComponent<TeleportPointRegistry>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Đăng ký một TeleportPoint
    /// </summary>
    public static void RegisterPoint(TeleportPoint point)
    {
        if (point == null) return;
        if (!registeredPoints.Contains(point))
        {
            registeredPoints.Add(point);
        }
    }

    /// <summary>
    /// Hủy đăng ký một TeleportPoint
    /// </summary>
    public static void UnregisterPoint(TeleportPoint point)
    {
        if (point == null) return;
        registeredPoints.Remove(point);
    }

    /// <summary>
    /// Lấy danh sách tất cả tên spawn point
    /// </summary>
    public static List<string> GetAllPointNames()
    {
        return registeredPoints
            .Where(p => p != null)
            .Select(p => p.PointName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }

    /// <summary>
    /// Tìm TeleportPoint theo tên
    /// </summary>
    public static TeleportPoint FindPoint(string pointName)
    {
        return registeredPoints.FirstOrDefault(p => p != null && p.PointName == pointName);
    }

    /// <summary>
    /// Lấy vị trí của một spawn point
    /// </summary>
    public static Vector3? GetPointPosition(string pointName)
    {
        var point = FindPoint(pointName);
        return point != null ? point.Position : null;
    }

    /// <summary>
    /// Lấy rotation của một spawn point
    /// </summary>
    public static Quaternion? GetPointRotation(string pointName)
    {
        var point = FindPoint(pointName);
        return point != null ? point.Rotation : null;
    }

    /// <summary>
    /// Lấy tất cả TeleportPoints
    /// </summary>
    public static List<TeleportPoint> GetAllPoints()
    {
        return new List<TeleportPoint>(registeredPoints.Where(p => p != null));
    }

    /// <summary>
    /// Validate - kiểm tra có trùng tên không
    /// </summary>
    public static bool HasDuplicateNames()
    {
        var names = GetAllPointNames();
        return names.Count != registeredPoints.Count(p => p != null);
    }

    /// <summary>
    /// Debug - print tất cả spawn points
    /// </summary>
    public static void DebugPrintAllPoints()
    {
        Debug.Log("=== TeleportPoint Registry ===");
        if (registeredPoints.Count == 0)
        {
            Debug.Log("Không có spawn point nào được register!");
            return;
        }

        foreach (var point in registeredPoints.Where(p => p != null))
        {
            Debug.Log($"✓ {point.PointName} - Vị trí: {point.Position} - {point.Description}");
        }
        Debug.Log($"Tổng: {registeredPoints.Count(p => p != null)} points");
    }
}
