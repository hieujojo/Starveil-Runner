-- Starveil Runner — Online Leaderboard (Supabase)
-- Chạy file này trong Supabase Dashboard → SQL Editor → New query → Run
-- (Dự án mới: Supabase → New project → SQL Editor)
--
-- Sau khi chạy xong:
--   1. Settings → API → copy "Project URL" + "anon public" key
--   2. Unity: Assets → Create → VoidRunner → Leaderboard Config
--   3. Dán URL + key vào asset (đặt trong thư mục Resources bất kỳ)
--   4. Build lại → Game Over → nhập tên 3 ký tự → SUBMIT

-- Bảng điểm — mỗi dòng = 1 lần Game Over
create table if not exists public.leaderboard (
  id bigint generated always as identity primary key,
  name text not null default 'AAA',      -- tên arcade 3 ký tự (đã được game chuẩn hóa)
  score integer not null default 0,      -- điểm khi Game Over
  created_at timestamptz not null default now()
);

-- Index để truy vấn top n nhanh (ORDER BY score DESC)
create index if not exists leaderboard_score_idx on public.leaderboard (score desc, created_at asc);

-- ============================================================
-- ROW LEVEL SECURITY — bắt buộc với Supabase (mặc định chặn hết)
-- Game dùng anon key nên phải mở đúng 2 quyền tối thiểu:
--   SELECT: đọc top 10 cho mọi người
--   INSERT: ghi điểm mới (chỉ ghi, không sửa/xóa)
-- ============================================================
alter table public.leaderboard enable row level security;

drop policy if exists "Leaderboard public read" on public.leaderboard;
create policy "Leaderboard public read"
  on public.leaderboard for select
  using (true);

drop policy if exists "Leaderboard public insert" on public.leaderboard;
create policy "Leaderboard public insert"
  on public.leaderboard for insert
  with check (true);
