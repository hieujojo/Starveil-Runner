# 🎮 Game UI Audit — Starveil Runner

**Ngày:** 2026-09-01  
**Phương pháp:** Nielsen's 10 Heuristics + Game UX Principles (adapted from tundraray/overture + Claude-Code-Game-Studios)  
**Đánh giá:** Mỗi lỗi có **Severity** (Critical/Major/Minor) + **Effort fix** (Easy/Medium/Hard)

---

## 📊 Tổng quan

| Screen | Files | Trạng thái | Severity |
|---|---|---|---|
| Main Menu | `MainMenuManager.cs` | ✅ Đã fix spacing | — |
| Ship Select | `ShipSelectManager.cs` | ✅ OK | — |
| HUD (Score/Combo) | `UIManager.cs` | ✅ Có animation feedback | — |
| Pause | `PauseManager.cs` | ✅ OK | — |
| Game Over | `UIManager.cs` | ✅ Layout đã cân bằng | — |
| Leaderboard | `LeaderboardView.cs` | ✅ Đã fix position | — |
| Credits | `CreditsPanelBuilder.cs` | ✅ OK | — |
| How To Play | `MainMenuManager.cs` | ✅ Có ScrollRect | — |
| Volume Slider | `VolumeSliderBuilder.cs` | ✅ OK | — |

---

## 🔴 Critical Issues (Phải fix ngay)

### C1: HUD Score — **ĐÃ FIXED** - Có DOTween punch scale + popup "+N" khi thu coin

| | Chi tiết |
|---|---|
| **Screen** | HUD (gameplay) |
| **Vấn đề** | Score text chỉ cập nhật số — không có animation, không có hiệu ứng khi score tăng |
| **So sánh** | Subway Surfers: score "+100" popup bay lên · Temple Run: score pulse animation |
| **Nielsen** | #1 Visibility of system status |
| **Fix** | Thêm DOTween punch scale khi score thay đổi hoặc "+100" popup |

### C2: HUD Combo — **ĐÃ FIXED** - Có fade in/out + scale animation khi combo thay đổi

| | Chi tiết |
|---|---|
| **Screen** | HUD (gameplay) |
| **Vấn đề** | `comboText.gameObject.SetActive(multiplier > 1)` — xuất hiện/biến mất tức thì, không transition |
| **So sánh** | Subway Surfers: combo x2 → x3 → x4 có glow + shake effect |
| **Nielsen** | #1 Visibility of system status |
| **Fix** | Fade in/out + scale animation khi combo thay đổi |

---

## 🟡 Major Issues (Nên fix)

### M1: Main Menu — **ĐÃ FIXED** - Khoảng cách 80px đều giữa các nút

| | Chi tiết |
|---|---|
| **Screen** | Main Menu |
| **Vấn đề** | Các nút (PLAY, HOW TO PLAY, SHIP, CREDITS) có Y positions: 60, 24, -245, -245 — gap giữa PLAY và HOW TO PLAY = 36px, nhưng gap giữa HOW TO PLAY và SHIP = 269px |
| **Impact** | Trông mất cân đối — nửa trên.dense, nửa dưới.rỗng |
| **Nielsen** | #8 Aesthetic and minimalist design |
| **Fix** | Cân bằng spacing: PLAY (80), HOW TO PLAY (0), SHIP (-80), CREDITS (-160) hoặc tương tự |

### M2: Main Menu — **ĐÃ FIXED** - Có text 'PRESS SPACE TO START' dưới nút PLAY

| | Chi tiết |
|---|---|
| **Screen** | Main Menu |
| **Vấn đề** | Không có text hướng dẫn "Press SPACE to play" hoặc "Tap to start" |
| **Impact** | Player mới không biết bắt đầu từ đâu (đặc biệt trên mobile/touch) |
| **Nielsen** | #6 Recognition rather than recall |
| **Fix** | Thêm text nhỏ "PRESS SPACE TO START" nhấp nháy dưới nút PLAY |

### M3: Game Over — **ĐÃ FIXED** - Leaderboard panel dịch lên y=20f, tránh chồng nút Retry/Menu

| | Chi tiết |
|---|---|
| **Screen** | Game Over |
| **Vấn đề** | Leaderboard panel (520×380) anchor giữa Game Over panel — có thể đè lên Retry/Menu buttons |
| **Impact** | Player không thấy nút Retry nếu leaderboard panel quá to |
| **Nielsen** | #5 Error prevention |
| **Fix** | Leaderboard nên có scroll hoặc resize để không đè buttons, hoặc dời buttons lên trên |

### M4: Pause — **ĐÃ FIXED** - Tăng kích thước nút Pause lên 80×80

| | Chi tiết |
|---|---|
| **Screen** | HUD (gameplay) |
| **Vấn đề** | Nút pause "II" nhỏ, dễ bị missed — player không biết có thể pause |
| **Nielsen** | #6 Recognition rather than recall |
| **Fix** | Tăng size nút pause hoặc thêm tooltip "ESC to pause" khi hover |

### M5: Ship Select — **ĐÃ FIXED** - Giảm arrow buttons xuống 80×50

| | Chi tiết |
|---|---|
| **Screen** | Ship Select |
| **Vấn đề** | Arrow buttons 130×62 — to hơn cần thiết cho 2 ký tự "<" và ">" |
| **Impact** | Chiếm quá nhiều không gian, trông thô |
| **Nielsen** | #8 Aesthetic and minimalist design |
| **Fix** | Giảm size arrow buttons xuống ~80×50 |

---

## 🟢 Minor Issues (Có thể fix)

### m1: Credits — **ĐÃ FIXED** - Tăng font size lên 24 để cân bằng với tiêu đề

| | Chi tiết |
|---|---|
| **Screen** | Credits |
| **Vấn đề** | Font size 22 trên panel 760×660 — hơi nhỏ so với tiêu đề 48 |
| **Fix** | Tăng lên 24 hoặc giảm tiêu đề xuống 40 cho cân bằng |

### m2: How To Play — **ĐÃ FIXED** - Thêm ScrollRect cho panel

| | Chi tiết |
|---|---|
| **Screen** | How To Play |
| **Vấn đề** | Nếu nội dung dài hơn panel → tràn ra ngoài, không scroll được |
| **Fix** | Thêm ScrollRect nếu content > panel height |

### m3: Leaderboard — **ĐÃ FIXED** - Có CTA 'Enter name and press SUBMIT!' dưới trạng thái empty

| | Chi tiết |
|---|---|
| **Screen** | Leaderboard (Game Over) |
| **Vấn đề** | Text "No scores yet — be the first!" không có hướng dẫn下一步 |
| **Fix** | Thêm "Enter your name and press SUBMIT!" |

---

## 📏 Design Principles đang tuân thủ

| Principle | Trạng thái | Ví dụ |
|---|---|---|
| **Consistent color scheme** | ✅ | Cyan (chính), tím (phụ), vàng (điểm nhấn) |
| **Neon arcade theme** | ✅ | Viền cyan, nền tím đen, text sáng |
| **Close buttons** | ✅ | Tất cả popup đều có X close |
| **Dimmer overlay** | ✅ | HowToPlay, Credits, ShipSelect đều có dimmer |
| **Offline-safe** | ✅ | Leaderboard fail → game vẫn chạy |
| **Idempotent builders** | ✅ | Tất cả UI tạo bằng code, chạy lại không nhân đôi |

---

## 🎯 Priority Fix Order

| # | Issue | Severity | Effort | Impact |
|---|---|---|---|---|
| 1 | C1: HUD score animation | Critical | Easy | Gameplay feel +10x |
| 2 | C2: HUD combo transition | Critical | Easy | Gameplay feel +5x |
| 3 | M1: Menu spacing | Major | Easy | First impression +3x |
| 4 | M3: Game Over overlap | Major | Medium | Usability +2x |
| 5 | M4: Pause button visibility | Major | Easy | Usability +1x |
| 6 | M2: Menu interaction hint | Major | Easy | Onboarding +2x |
| 7 | M5: Arrow button size | Minor | Easy | Polish +1x |
| 8 | m1-m3: Minor polish | Minor | Easy | Polish +0.5x |

---

## 📐 Reference: Game UX Best Practices

| Nguồn | Principle | Áp dụng |
|---|---|---|
| Nielsen #1 | Visibility of system status | HUD feedback, combo animation |
| Nielsen #5 | Error prevention | Close buttons, dimmer click-to-close |
| Nielsen #6 | Recognition > recall | Pause button visible, interaction hints |
| Nielsen #8 | Aesthetic minimalist | Spacing, button sizing |
| Game UX Guide | Immediate feedback | Score popup, combo glow |
| Game UX Guide | Progressive disclosure | How To Play panel |
| Game UX Guide | Consistent visual language | Color scheme (cyan/purple/gold) |

---

*Audit by Buffy (Freebuff) — adapted from tundraray/overture game-analysis-criteria + Claude-Code-Game-Studios team-ui patterns*
