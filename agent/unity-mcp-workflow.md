# Unity MCP Workflow — 5 Tool Chính

## Kết Nối

```
Tôi (Codebuff) ──HTTP──→ localhost:8080 ──→ Unity MCP Server (v10.2.0)
```

- **Server URL:** `http://127.0.0.1:8080/mcp`
- **Transport:** HTTP Local
- **Protocol:** JSON-RPC 2.0 over SSE (Server-Sent Events)
- **Session:** Cần initialize trước khi gọi tool

---

## 5 Tool Chính

### 1. render_ui — Mắt Tôi

```json
{
  "tool": "manage_ui",
  "action": "render_ui",
  "path": "Assets/UI/MainMenu.uxml"
}
```

| Thông tin | Chi tiết |
|---|---|
| **Làm gì** | Chụp screenshot UI panel → PNG |
| **Khi nào dùng** | Muốn thấy kết quả layout thực tế |
| **Output** | PNG file (có thể phân tích visual) |
| **Lưu ý** | Play mode: cần 2 lần gọi (lần 1 queue, lần 2 lấy result) |

---

### 2. execute_code — Tay Tôi

```json
{
  "tool": "execute_code",
  "action": "execute",
  "code": "var go = GameObject.Find(\"PlayButton\"); var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(0, 140); return \"Done\";"
}
```

| Thông tin | Chi tiết |
|---|---|
| **Làm gì** | Chạy C# trực tiếp trong Unity Editor |
| **Khi nào dùng** | Set vị trí, size, color, đọc giá trị realtime |
| **Compiler** | CodeDom (C# 6) hoặc Roslyn (C# 12+) |
| **Lưu ý** | Code chạy trong `UnityEngine` + `UnityEditor` namespace |

**Ví dụ thực tế:**
```csharp
// Đọc vị trí tất cả element trong Canvas
var canvas = GameObject.Find("Canvas");
string result = "";
foreach(Transform child in canvas.transform) {
    var rt = child.GetComponent<RectTransform>();
    result += child.name + " pos=(" + rt.anchoredPosition.x + "," + rt.anchoredPosition.y + ")";
}
return result;
```

---

### 3. manage_ui — Tay Tôi (Properties)

```json
{
  "tool": "manage_ui",
  "action": "modify_visual_element",
  "path": "Assets/UI/MainMenu.uxml",
  "element_name": "PlayButton",
  "text": "START",
  "style": {"font-size": "32px", "color": "#00BFFF"}
}
```

| Thông tin | Chi tiết |
|---|---|
| **Làm gì** | Sửa UXML elements (text, class, style) |
| **Khi nào dùng** | Sửa text, font size, color, visibility |
| **Actions** | `create`, `read`, `update`, `delete`, `modify_visual_element`, `render_ui` |
| **Lưu ý** | Chỉ cho UI Toolkit (UXML/USS), không cho legacy UI (RectTransform) |

---

### 4. read_console — Tai Tôi

```json
{
  "tool": "read_console",
  "action": "get",
  "types": ["error", "warning"],
  "count": "10"
}
```

| Thông tin | Chi tiết |
|---|---|
| **Làm gì** | Đọc log từ Unity Console |
| **Khi nào dùng** | Kiểm tra lỗi sau mỗi lần fix |
| **Types** | `error`, `warning`, `log`, `all` |
| **Output** | Danh sách log entries (message, stacktrace, type) |

---

### 5. run_tests — Bộ Não Tôi

```json
{
  "tool": "run_tests",
  "mode": "PlayMode",
  "include_details": true
}
```

| Thông tin | Chi tiết |
|---|---|
| **Làm gì** | Chạy EditMode/PlayMode tests |
| **Khi nào dùng** | Verify fix không break gì trước khi commit |
| **Modes** | `EditMode` (25 tests), `PlayMode` (21 tests) |
| **Output** | Summary (total, passed, failed, skipped) + chi tiết từng test |

**Workflow test:**
```json
// Bước 1: Chạy test
{"tool": "run_tests", "mode": "PlayMode"}

// Bước 2: Lấy kết quả (sau 30 giây)
{"tool": "get_test_job", "job_id": "..."}
```

---

## Workflow Completo

```
┌─────────────────────────────────────────────────────────┐
│                    WORKFLOW HOÀN CHỈNH                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  1. render_ui ──→ 2. phân tích ──→ 3. fix code          │
│       ↑                              │                  │
│       └──────────────────────────────┘                  │
│                    (lặp cho đến khi OK)                  │
│                                                         │
│  + read_console (kiểm tra lỗi)                         │
│  + run_tests (verify không break gì)                    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Bước 1: render_ui — Chụp Ảnh

```
render_ui → Unity chụp screenshot → trả PNG
  → Tôi phân tích:
    - Vị trí element
    - Khoảng cách giữa elements
    - Màu sắc, font size
    - Alignment, overlap
```

### Bước 2: Phân Tích — Tôi Đọc Ảnh

```
render_ui result: [PNG data]
  → Tôi phân tích:
    - "PLAY button quá gần HOW TO PLAY"
    - "Volume slider text bị đè"
    - "SHIP và CREDITS chạm nhau"
```

### Bước 3: Fix Code — Tôi Sửa

```
execute_code → set vị trí mới
hoặc
manage_ui → modify element properties
```

### Bước 4: Verify — render_ui Lại

```
render_ui →creenshot mới → So sánh với trước
  → "Đã fix xong" hoặc "Vẫn sai, lặp lại"
```

### Bước 5: Đọc Console — Kiểm Tra Lỗi

```
read_console types=error
  → Nếu có lỗi → fix trước khi tiếp tục
```

### Bước 6: Run Tests — Verify Không Break

```
run_tests mode=EditMode
  → Nếu fail → fix test hoặc code
  → Nếu pass → tiếp tục
```

### Bước 7: Commit + Push

```
git add → git commit → git push
```

---

## Thời Gian So Sánh

| Workflow | Thời gian | Số vòng lặp |
|---|---|---|
| **Cũ** (không MCP) | 30-60 phút | 10-15 vòng |
| **Mới** (với render_ui) | 5-10 phút | 3-5 vòng |
| **Tối ưu** (render_ui + execute_code) | 2-5 phút | 2-3 vòng |

---

## Tool Palette Nhanh

| Tool | Vai trò | Khi nào dùng |
|---|---|---|
| `render_ui` | Mắt tôi — thấy UI | Mọi lần muốn thấy kết quả |
| `execute_code` | Tay tôi — sửa UI | Fix position/size/color |
| `manage_ui` | Tay tôi — sửa properties | Fix text/class/style |
| `read_console` | Tai tôi — nghe lỗi | Sau mỗi lần fix |
| `run_tests` | Bộ não tôi — verify logic | Trước khi commit |

---

## Lưu Ý Quan Trọng

| Lưu ý | Chi tiết |
|---|---|
| **render_ui Play mode** | Cần 2 lần gọi: lần 1 queue, lần 2 lấy PNG |
| **execute_code compiler** | CodeDom (C# 6) mặc định, Roslyn (C# 12+) nếu cài |
| **run_tests timeout** | PlayMode cần 120s, EditMode cần 30s |
| **Session ID** | Cần initialize trước khi gọi tool |
| **HTTP protocol** | JSON-RPC 2.0 over SSE, cần header `Accept: application/json, text/event-stream` |

---

## Ví Dụ Thuc Tế: Fix MainMenu Layout

```
Bước 1: render_ui → chụp MainMenu
  → "PLAY quá gần HOW TO PLAY"

Bước 2: execute_code → set PlayButton y=140
  → HowToPlayButton y=40

Bước 3: render_ui → chụp lại
  → "Gap đã OK, nhưng VolumeSlider xấu"

Bước 4: execute_code → set VolumeSlider size=380×60
  → label fontSize=18

Bước 5: render_ui → chụp lại
  → "Volume đã OK, nhưng SHIP/CREDITS chạm nhau"

Bước 6: execute_code → set Ship x=-180, Credits x=180

Bước 7: render_ui →creenshot cuối
  → "Tất cả OK!"

Bước 8: read_console → 0 errors

Bước 9: run_tests → 46/46 passed

Bước 10: commit + push
```

**Tổng thời gian: 5-10 phút** (thay vì 30-60 phút trước đây)
