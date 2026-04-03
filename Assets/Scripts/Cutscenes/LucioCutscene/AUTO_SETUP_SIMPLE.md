# Auto-Setup Helper - Simple Guide

Hướng dẫn **siêu chi tiết** từng bước để dùng auto-setup.

---

## 📸 Visual Steps

### Step 1: Create Empty GameObject
```
Right-click trong Hierarchy
     ↓
Create Empty
     ↓
(Tạo một object trống)
```

Đặt tên: `LucioCutsceneSetupManager` (hoặc tên khác cũng được)

### Step 2: Add Script
```
Click vào GameObject vừa tạo
     ↓
Vào Inspector, tìm nút "Add Component"
     ↓
Type: "LucioCutsceneSetupHelper"
     ↓
Click để thêm
```

Bây giờ Inspector sẽ hiển thị:
```
LucioCutsceneSetupHelper (Script)

Auto-Setup Options
  ☑ Auto Create Characters
  ☑ Auto Create Camera Controller
  ☑ Auto Create Beam System
  ☑ Auto Create QTEU
```

### Step 3: Click Setup Button

**Cách 1 - Dùng Context Menu (Dễ nhất):**
```
Right-click trên component name "LucioCutsceneSetupHelper"
     ↓
Chọn "Setup Lucio Cutscene"
     ↓
Chờ 2-3 giây để script chạy
     ↓
Xong! ✅
```

**Cách 2 - Dùng nút trong Inspector:**
```
Tìm nút "Setup Lucio Cutscene" trong section
     ↓
Click nó
     ↓
Chờ 2-3 giây
     ↓
Xong! ✅
```

### Step 4: Kiểm tra Hierarchy

Setup sẽ tạo ra:
```
Hierarchy
├─ Lucio (Blue cube - nhân vật)
├─ Cedric (Red cube - nhân vật)  
├─ BeamAttackSystem (trống - chưa có prefab)
├─ LucioCutsceneManager (trống - cái chính)
├─ Main Camera (được update)
└─ Canvas (nếu chưa có thì tạo mới)
   └─ QTEPanel
      ├─ ProgressBar (thanh xanh)
      └─ Text (chữ hướng dẫn)
```

### Step 5: Assign Beam Prefabs (QUAN TRỌNG!)

**Bây giờ phải làm việc này thủ công:**

1. Click vào **BeamAttackSystem** trong Hierarchy
2. Vào Inspector, scroll down tìm **Beam Prefabs**
3. Thấy 2 field:
   ```
   Lucio Beam Prefab: [  ]  <- Drag prefab vào đây
   Cedric Beam Prefab: [ ]  <- Drag prefab vào đây
   ```
4. Lấy 2 beam prefab của bạn từ Project folder
5. Drag vào 2 field trên

**Ví dụ:**
```
Project/Assets/Prefabs/Beams/
├─ LucioBeam.prefab    <- Drag vào Lucio Beam Prefab
└─ CedricBeam.prefab   <- Drag vào Cedric Beam Prefab
```

### Step 6: Play!

1. Press **Play** (Spacebar hoặc Play button)
2. Cutscene sẽ chạy **tự động**
3. Xem camera zoom, flick, shake
4. Khi QTE bắt đầu:
   - Nhìn thấy text: "Spam Space! Beam Power: 0%"
   - **SPAM SPACE** liên tục
   - Beam sẽ grow từ 0% → 100%
   - Nếu > 50% = Lucio wins!

---

## ✅ Checklist

- [ ] Tạo empty GameObject
- [ ] Add LucioCutsceneSetupHelper script
- [ ] Click "Setup Lucio Cutscene" button
- [ ] Chờ setup hoàn thành
- [ ] Tìm BeamAttackSystem
- [ ] Assign Lucio beam prefab
- [ ] Assign Cedric beam prefab
- [ ] Press Play
- [ ] SPAM SPACE during QTE!

---

## 🤔 FAQ

**Q: Nó làm gì khi tôi click Setup?**
A: Script sẽ:
- Tạo 2 nhân vật Lucio & Cedric
- Add camera script vào Main Camera
- Tạo BeamAttackSystem
- Tạo QTE UI (Canvas + components)
- Tạo LucioCutsceneManager

**Q: Tại sao vẫn phải assign prefab thủ công?**
A: Vì script không biết prefab của bạn ở đâu. Setup chỉ tạo structure, prefab phải bạn assign.

**Q: Có gì sai lạc không?**
A: Mở Console xem có error không:
- View → Console (hoặc Ctrl+Shift+C)
- Không hiện error = OK
- Có error = Click cái script đó để debug

**Q: Làm sao xóa setup?**
A: Xóa GameObject `LucioCutsceneSetupManager` đi (nó chỉ là helper tool)

**Q: Cái này auto-setup xong thì làm gì?**
A:
1. Assign beam prefab ✓
2. Play scene ✓
3. SPAM SPACE! ✓

---

## 🐛 Troubleshooting

| Sự cố | Cách sửa |
|-------|---------|
| Không gì xảy ra | Check script đã add chưa, click Setup button lại |
| Error trong Console | Mở Console xem lỗi, ghi note, thử lại |
| Không thấy nhân vật | Setup có chạy không? Check Hierarchy |
| Không có UI | Kiểm tra Canvas có được tạo không? |
| Beam không hiển thị | Prefab đã assign? Checked? |
| Input không hoạt động | Kiểm tra Main Camera tag đúng không |

---

## 📞 Quick Reference

```
Setup Button: Right-click LucioCutsceneSetupHelper → "Setup Lucio Cutscene"

Assign Prefab: 
  BeamAttackSystem (Hierarchy)
  → Inspector
  → Beam Prefabs
  → Drag 2 prefab vào

Play: 
  Press Play
  Wait for QTE
  SPAM SPACE!
```

---

**Vậy thế là xong! Không phức tạp đâu! 🎉**

Nếu còn không hiểu, hãy mô tả chỗ nào bạn bị stuck, tôi sẽ giúp chi tiết hơn!
