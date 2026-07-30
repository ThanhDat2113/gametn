using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Water_Settings : MonoBehaviour
{
    Material waterVolume;
    Material waterMaterial;

    void Update()
    {
        if(waterVolume == null)
        {
            waterVolume = (Material)Resources.Load("Water_Volume");
        }

        if (waterMaterial == null)
        {
            // Kiểm tra MeshRenderer có tồn tại không
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                waterMaterial = renderer.sharedMaterial;
            }
        }

        // ⭐ QUAN TRỌNG: Kiểm tra CẢ HAI trước khi dùng
        if (waterVolume != null && waterMaterial != null)
        {
            waterVolume.SetVector("pos", new Vector4(0, (waterVolume.GetVector("bounds").y / -2) + transform.position.y + (waterMaterial.GetFloat("_Displacement_Amount") / 3), 0, 0));
        }
        else
        {
            // Debug để biết lỗi là gì
            if (waterVolume == null)
                Debug.LogError("Không tìm thấy material Water_Volume trong Resources!");
            if (waterMaterial == null)
                Debug.LogError("Không tìm thấy MeshRenderer hoặc sharedMaterial!");
        }
    }
}