# 🛠️ WoodQuiz Fix Plan - ✅ COMPLETED

## ✅ Completed Fixes

### Fix 1 ✅ WoodQuizPuzzle.cs — Vector2Int struct copy bug
- **File:** `Assets/_Project/Scripts/Systems/Quest/WoodQuizPuzzle.cs`
- **Hàm:** `TryMoveBlock()`
- **Vấn đề:** `var cell = block.cells[i]` tạo copy của Vector2Int (struct). `cell.x += dx` chỉ modify copy, không modify list gốc → block không bao giờ di chuyển.
- **Fix:** Thay bằng `Vector2Int cell = block.cells[i];` + `block.cells[i] = new Vector2Int(newX, newY);` — gán lại vào list.

### Fix 2 ✅ PuzzlePrefabCreator.cs — Thêm blockContainer + blockPrefab
- **File:** `Assets/Editor/PuzzlePrefabCreator.cs`
- **Hàm:** `BuildWoodQuizPrefab()`
- **Thay đổi:**
  - Thêm `BlockContainer` (RectTransform, cùng vị trí/size với grid)
  - Gán `p.blockContainer = blockRect`
  - Thêm `blockPrefab` template (Image + CanvasGroup + WoodQuizBlockDrag) bên trong container
  - Gán `p.blockPrefab = blockTemplate`
  - Fix grid spacing từ `(4,4)` → `Vector2.zero`
  - Fix grid size từ `300,380` → `280,350` (khớp 4x5 * 70px)

### Fix 3 ✅ WoodQuizAutoSetup.cs — Thêm blockContainer + blockPrefab
- **File:** `Assets/Editor/WoodQuizAutoSetup.cs`
- **Hàm:** `EnsurePrefab()`
- **Thay đổi:** Giống Fix 2 — thêm blockContainer, blockPrefab, fix spacing/size.

### Fix 4 ✅ Grid spacing consistency
- Prefab creator và runtime code đều dùng `spacing = Vector2.zero` (thống nhất)

### Fix 5 ✅ WoodQuizAutoSetup consistency
- `WoodQuizAutoSetup.EnsurePrefab()` đã đồng bộ với `PuzzlePrefabCreator.BuildWoodQuizPrefab()`

### Fix 6 ✅ Block đỏ (M) đổi từ NGANG → DỌC
- **Vấn đề:** Các PuzzleData asset (`wood_quiz_01.asset`, `wood.asset`) lưu layout với khối master M **ngang** (`#MM#` / `#MM.`), không khớp với code default trong `WoodQuizPuzzle.ApplyDefaultLayout()` và `WoodQuizAutoSetup` (M **dọc** `#.M#` trên 2 row).
- **Files đã sửa:**
  - `Assets/_Project/Data/Puzzle/wood_quiz_01.asset`: `#MM#` → `#.M#` (M dọc 1x2)
  - `Assets/_Project/Data/Puzzle/wood.asset`: `#MM.` (2x2) → `#.M#` (M dọc 1x2)
  - `Assets/_Project/Scripts/Systems/Quest/WoodQuizPuzzle.cs`: cập nhật comment/doc mô tả đúng M dọc 1x2
  - `Assets/_Project/Scripts/Systems/Quest/PuzzleData.cs`: cập nhật tooltip `WoodQuizConfig.boardLayout` (M = block dọc 1x2)
- **Layout mới (4x5) — M dọc 1x2:**
  ```
  ####
  #.M#
  #.M#
  #AB#
  #.G#
  ```
  - M = master đỏ DỌC 1x2 (chiếm 2 ô theo chiều dọc)
  - A/B = block 1x1 | G = goal
  - Cách giải: A xuống → B trái → M xuống

