using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VoidRunner.Systems.Save;
using VoidRunner.Utils;

namespace VoidRunner.UI
{
    /// <summary>
    /// Task D (2026-08-11): panel CHỌN TÀU ở MainMenu — preview 3D xoay (RenderTexture + camera
    /// layer ShipPreview), 2 nút mũi tên chuyển SF Fighter ⇄ Sparrow, tên tàu hiển thị giữa 2 mũi tên.
    /// v3.3: bỏ nút SELECT — đổi tàu là lưu luôn SaveSystem.SelectedShip.
    /// Tạo bằng code idempotent — không cần kéo thả scene. PlayerController đọc SelectedShip khi vào game.
    ///
    /// Cấu trúc (tạo lúc Start, ẩn sẵn):
    ///   Canvas/ ShipButton (nút mở) · ShipSelectPanel (ẩn) · (ngoài canvas) ShipPreviewCamera + ShipPreviewRoot
    /// Camera chỉ render layer "ShipPreview" (6) — model preview duy nhất hiển thị.
    /// </summary>
    public class ShipSelectManager : MonoBehaviour
    {
        [Header("Tàu chọn được (tool Setup Ship Select tự gán)")]
        [SerializeField] private GameObject[] shipPrefabs;

        [Header("Preview")]
        [SerializeField] private float previewRotateSpeed = 35f;
        [SerializeField] private Color previewBg = new Color(0.1f, 0.07f, 0.18f, 1f);

        private Canvas _canvas;
        private Button _shipButton;
        private GameObject _panel;
        private TextMeshProUGUI _nameText;
        private RawImage _previewImage;
        private Camera _previewCam;
        private RenderTexture _rt;
        private Transform _previewRoot; // chứa model đang xem (thay con khi đổi)
        private GameObject _currentModel;
        private GameObject _dimmer; // che menu phía sau khi mở panel (giống HowToPlay/Credits)
        private int _selected;

        private static readonly string[] ShipNames = { "SF FIGHTER", "SPARROW" };

        private void Start()
        {
            _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            _selected = Mathf.Clamp(SaveSystem.SelectedShip, 0, Mathf.Max(0, ShipNames.Length - 1));

            EnsureButton();
            EnsurePanel();
            EnsurePreviewCamera();

            if (_shipButton != null) _shipButton.onClick.AddListener(TogglePanel);
        }

        private void OnDestroy()
        {
            if (_shipButton != null) _shipButton.onClick.RemoveListener(TogglePanel);
            if (_rt != null) _rt.Release();
        }

        private void EnsureButton()
        {
            Transform existing = _canvas.transform.Find("ShipButton");
            if (existing != null) { _shipButton = existing.GetComponent<Button>(); return; }

            var go = new GameObject("ShipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-160f, -245f); // CÙNG HÀNG với CREDITS (bên phải 160) — SHIP bên trái (Fix Spacing 2026-08-12 v3: theo layout mới Best -160)
            rt.sizeDelta = new Vector2(300f, 56f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.65f, 0.9f, 1f); // cyan — nút chính phụ, nổi bật hơn tím

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 4f);
            lrt.offsetMax = new Vector2(-10f, -4f);

            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = "SHIP";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);

            _shipButton = btn;
        }

        private void EnsurePanel()
        {
            Transform existing = _canvas.transform.Find("ShipSelectPanel");
            if (existing != null) { _panel = existing.gameObject; CachePanelRefs(); return; }

            var panel = new GameObject("ShipSelectPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_canvas.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(680f, 720f); // FIX 2026-08-12: 520×560 → 680×720 (user: "cho select ship to thêm, đừng quá tiết kiệm UI")
            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(0.06f, 0.04f, 0.12f, 1f); // tím đen đục

            // 2026-08-12 (user: "ý tôi là UI ở trong nút ship — chủ yếu chi tiết ngoài lề như cạnh viền"):
            // viền cyan neon quanh panel — giống Credits panel (AddNeonBorder dùng chung trong file)
            AddNeonBorder(panel.transform, prt.sizeDelta, 3f, new Color(0.35f, 0.85f, 1f, 0.35f));

            // Tiêu đề
            var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            title.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -16f);
            trt.sizeDelta = new Vector2(600f, 56f);
            var ttmp = title.GetComponent<TextMeshProUGUI>();
            ttmp.text = "SELECT SHIP";
            ttmp.fontSize = 44;
            ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = new Color(1f, 0.85f, 0.3f, 1f);
            ttmp.alignment = TextAlignmentOptions.Center;
            ttmp.raycastTarget = false;
            AssignFallbackFont(ttmp);

            // Gạch chân vàng mờ dưới tiêu đề (chi tiết ngoài lề — 2026-08-12)
            var underline = new GameObject("Underline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            underline.transform.SetParent(panel.transform, false);
            var urt = (RectTransform)underline.transform;
            urt.anchorMin = new Vector2(0.5f, 1f);
            urt.anchorMax = new Vector2(0.5f, 1f);
            urt.pivot = new Vector2(0.5f, 1f);
            urt.anchoredPosition = new Vector2(0f, -78f); // dưới title (title đáy ~-72)
            urt.sizeDelta = new Vector2(320f, 3f);
            var uimg = underline.GetComponent<Image>();
            uimg.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            uimg.raycastTarget = false;

            // Khung preview (RawImage)
            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            previewGo.transform.SetParent(panel.transform, false);
            var prt2 = (RectTransform)previewGo.transform;
            prt2.anchorMin = new Vector2(0.5f, 1f);
            prt2.anchorMax = new Vector2(0.5f, 1f);
            prt2.pivot = new Vector2(0.5f, 1f);
            prt2.anchoredPosition = new Vector2(0f, -92f);
            prt2.sizeDelta = new Vector2(480f, 400f);
            _previewImage = previewGo.GetComponent<RawImage>();

            // Khung viền "viewfinder" quanh preview (chi tiết ngoài lề — 2026-08-12)
            AddNeonBorder(previewGo.transform, prt2.sizeDelta, 2f, new Color(0.35f, 0.85f, 1f, 0.55f));

            // Tên tàu đang chọn
            var nameGo = new GameObject("ShipName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(panel.transform, false);
            var nrt = (RectTransform)nameGo.transform;
            nrt.anchorMin = new Vector2(0.5f, 1f);
            nrt.anchorMax = new Vector2(0.5f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            // 2026-08-12 v3.3 (user: "bỏ luôn chữ SELECT giữa 2 nút mũi tên, hiển thị tên tàu là được"):
            // tên tàu nằm CÙNG HÀNG 2 mũi tên (y=-575) — thay vị trí nút SELECT cũ (giữa 2 mũi tên,
            // cao 62 = cao mũi tên, rộng 320 vừa khoảng trống giữa 2 mũi tên: -160..+160)
            nrt.anchoredPosition = new Vector2(0f, -575f);
            nrt.sizeDelta = new Vector2(320f, 62f);
            _nameText = nameGo.GetComponent<TextMeshProUGUI>();
            _nameText.fontSize = 36;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = Color.white;
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.raycastTarget = false;
            _nameText.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(_nameText);

            // Nút mũi tên trái / phải (tên tàu nằm giữa 2 mũi tên — v3.3, không còn nút SELECT)
            CreateArrowButton(panel.transform, "PrevButton", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-225f, -575f), "<");
            CreateArrowButton(panel.transform, "NextButton", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(225f, -575f), ">");

            // Nút đóng — dấu X nhỏ góc trên phải (fix 2026-08-12: nút CLOSE to che chữ)
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var krrt = (RectTransform)closeGo.transform;
            krrt.anchorMin = new Vector2(1f, 1f);
            krrt.anchorMax = new Vector2(1f, 1f);
            krrt.pivot = new Vector2(1f, 1f);
            krrt.anchoredPosition = new Vector2(-12f, -12f);
            krrt.sizeDelta = new Vector2(44f, 44f); // vuông nhỏ — dấu X
            var kimg = closeGo.GetComponent<Image>();
            kimg.color = new Color(0.48f, 0.29f, 1f, 1f);
            var kbtn = closeGo.GetComponent<Button>();
            kbtn.transition = Selectable.Transition.ColorTint;
            kbtn.onClick.AddListener(TogglePanel);

            var klabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            klabel.transform.SetParent(closeGo.transform, false);
            var klrt = (RectTransform)klabel.transform;
            klrt.anchorMin = Vector2.zero;
            klrt.anchorMax = Vector2.one;
            klrt.offsetMin = Vector2.zero;
            klrt.offsetMax = Vector2.zero;
            var ktmp = klabel.GetComponent<TextMeshProUGUI>();
            ktmp.text = "X"; // chữ X đậm = dấu X (font chỉ pack ASCII — ✕ U+2715 sẽ ra ô vuông □, R5.2)
            ktmp.fontSize = 30;
            ktmp.fontStyle = FontStyles.Bold;
            ktmp.color = Color.white;
            ktmp.alignment = TextAlignmentOptions.Center;
            ktmp.raycastTarget = false;
            ktmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(ktmp);

            _panel = panel;
            panel.SetActive(false);
            CachePanelRefs();
        }

        /// <summary>
        /// Cache refs + subscribe nút mũi tên. ⚠️ FIX 2026-08-12 (bug "bấm chuột mũi tên không
        /// đổi tàu"): trước đây CreateArrowButton cũng AddListener → DOUBLE-SUBSCRIBE → 1 click gọi
        /// SelectPrev 2 lần → 2 tàu đổi qua rồi về cũ = nhìn như không đổi (phím hoạt động vì 1 lần/frame).
        /// Giờ: RemoveAllListeners trước khi Add → idempotent, không bao giờ subscribe 2 lần.
        /// </summary>
        private void CachePanelRefs()
        {
            if (_panel == null) return;
            _previewImage = _panel.transform.Find("Preview")?.GetComponent<RawImage>();
            _nameText = _panel.transform.Find("ShipName")?.GetComponent<TextMeshProUGUI>();
            var prevBtn = _panel.transform.Find("PrevButton")?.GetComponent<Button>();
            var nextBtn = _panel.transform.Find("NextButton")?.GetComponent<Button>();
            if (prevBtn != null)
            {
                prevBtn.onClick.RemoveAllListeners();
                prevBtn.onClick.AddListener(SelectPrev);
            }
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(SelectNext);
            }
        }

        private void CreateArrowButton(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, string arrow)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(130f, 62f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.2f, 0.6f, 1f);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            // Viền cyan cho nút mũi tên (chi tiết ngoài lề — 2026-08-12)
            AddNeonBorder(go.transform, rt.sizeDelta, 2f, new Color(0.35f, 0.85f, 1f, 0.9f));
            // ⚠️ KHÔNG AddListener ở đây — CachePanelRefs() đảm nhận (tránh double-subscribe,
            // bug 2026-08-12 "bấm chuột mũi tên không đổi tàu" — fix ở CachePanelRefs).

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = arrow;
            tmp.fontSize = 46;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            AssignFallbackFont(tmp);
        }

        /// <summary>Camera preview chỉ render layer ShipPreview (6) — model hiện tại duy nhất trên đó.</summary>
        private void EnsurePreviewCamera()
        {
            // FindObjectsByType (KHÔNG phải FindAnyObjectByType — cái kia trả 1 object, foreach sẽ lỗi)
            var existingCams = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var c in existingCams)
            {
                if (c != null && c.name == "ShipPreviewCamera")
                {
                    _previewCam = c;
                    break;
                }
            }

            if (_previewCam == null)
            {
                var go = new GameObject("ShipPreviewCamera", typeof(Camera));
                _previewCam = go.GetComponent<Camera>();
                _previewCam.orthographic = true;
                _previewCam.orthographicSize = 2.0f; // FIX 2026-08-12: khung preview to hơn → model cũng nên to hơn
                _previewCam.nearClipPlane = 0.1f;
                _previewCam.farClipPlane = 50f;
                _previewCam.clearFlags = CameraClearFlags.SolidColor;
                _previewCam.backgroundColor = previewBg;
                _previewCam.cullingMask = 1 << 6; // layer ShipPreview
                _previewCam.depth = -10f;
            }

            if (_rt == null)
            {
                _rt = new RenderTexture(512, 512, 16); // FIX 2026-08-12: khung preview 480×400 → 256² bị mờ, nâng 512²
                _previewCam.targetTexture = _rt;
                if (_previewImage != null) _previewImage.texture = _rt;
            }

            // Root chứa model preview — đặt TRƯỚC camera (model nhìn về camera = quay về -Z camera)
            if (_previewRoot == null)
            {
                var root = new GameObject("ShipPreviewRoot");
                _previewRoot = root.transform;
                _previewRoot.position = new Vector3(0f, 0f, 0f);
            }
            // Camera nhìn vào gốc từ phía trước
            _previewCam.transform.position = new Vector3(0f, 0.7f, -4f);
            _previewCam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (_previewRoot == null) return;

            // Dọn model cũ
            for (int i = _previewRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewRoot.GetChild(i).gameObject);
            }

            _nameText.text = _selected < ShipNames.Length ? ShipNames[_selected] : "SHIP";

            // Self-heal (R4.18): nếu chưa gán prefab trong scene (tool chưa chạy) → tự tải qua ShipCatalog
            GameObject prefab = null;
            if (shipPrefabs != null && _selected < shipPrefabs.Length) prefab = shipPrefabs[_selected];
            if (prefab == null) prefab = ShipCatalog.Load(_selected);
            if (prefab == null) return;

            GameObject model = Instantiate(prefab, _previewRoot);
            SetLayerRecursively(model, 6); // layer ShipPreview
            MaterialFixer.EnsureURPMaterials(model); // model 3rd-party dùng shader Standard → TÍM trong URP (fix 2026-08-12)

            // Chuẩn hóa scale — vừa khung camera (bounds cao ~1.2)
            Bounds b = GetRenderBounds(model);
            if (b.size.y > 0.001f)
            {
                model.transform.localScale = Vector3.one * (1.6f / b.size.y); // FIX 2026-08-12: model to hơn trong khung preview
            }
            // Model quay 180 quanh Y để nhìn về camera (camera ở -Z nhìn +Z)
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        }

        private void Update()
        {
            // Model preview xoay chậm (chỉ khi panel mở + có model)
            if (_panel != null && _panel.activeSelf && _previewRoot != null && _previewRoot.childCount > 0)
            {
                _previewRoot.Rotate(0f, previewRotateSpeed * Time.deltaTime, 0f, Space.World);
            }

            // Phím mũi tên / A / D đổi tàu khi panel mở (fix 2026-08-12: user bấm phím mũi tên không chuyển)
            if (_panel != null && _panel.activeSelf)
            {
                if (WasKeyPressed(Key.LeftArrow) || WasKeyPressed(Key.A)) SelectPrev();
                else if (WasKeyPressed(Key.RightArrow) || WasKeyPressed(Key.D)) SelectNext();
            }
        }

        private static bool WasKeyPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }

        // v3.3 (user bỏ nút SELECT): đổi tàu = LƯU NGAY SaveSystem.SelectedShip (không cần bấm xác nhận nữa)
        private void SelectPrev()
        {
            _selected = (_selected - 1 + ShipNames.Length) % ShipNames.Length;
            SaveSystem.SelectedShip = _selected;
            RefreshPreview();
        }

        private void SelectNext()
        {
            _selected = (_selected + 1) % ShipNames.Length;
            SaveSystem.SelectedShip = _selected;
            RefreshPreview();
        }

        private void TogglePanel()
        {
            if (_panel == null) return;
            bool show = !_panel.activeSelf;

            // Dimmer che menu phía sau khi mở panel (fix 2026-08-12: panel đè lộ VOID RUNNER phía sau)
            // ⚠️ FIX vòng 2 (2026-08-12, user "popup lộ menu sau lưng"): trước đây SetAsFirstSibling
            // chìm dimmer XUỐNG DƯỚI menu → menu sáng rõ sau panel (giống bug cũ MainMenuManager).
            // Đúng: dimmer SetAsLastSibling TRƯỚC, panel SetAsLastSibling SAU → dimmer che menu, panel che dimmer.
            if (show) EnsureDimmer();
            _panel.SetActive(show);
            if (_dimmer != null) _dimmer.SetActive(show);
            if (show)
            {
                _dimmer.transform.SetAsLastSibling();
                _panel.transform.SetAsLastSibling();
                RefreshPreview();
            }
        }

        /// <summary>Tạo dimmer đen 0.93 phủ canvas — click vùng tối = đóng panel (idempotent).</summary>
        private void EnsureDimmer()
        {
            if (_dimmer != null) return;
            if (_canvas == null) return;

            var go = new GameObject("ShipSelectDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.93f);
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(TogglePanel);
            go.SetActive(false);
            _dimmer = go;
        }

        /// <summary>Viền 4 cạnh neon quanh 1 RectTransform — chi tiết ngoài lề (giống Credits panel).</summary>
        private static void AddNeonBorder(Transform parent, Vector2 size, float thickness, Color color)
        {
            CreateBorderStrip(parent, "BorderTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -thickness * 0.5f), new Vector2(size.x, thickness), color);
            CreateBorderStrip(parent, "BorderBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness * 0.5f), new Vector2(size.x, thickness), color);
            CreateBorderStrip(parent, "BorderLeft", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(thickness * 0.5f, 0f), new Vector2(thickness, size.y), color);
            CreateBorderStrip(parent, "BorderRight", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-thickness * 0.5f, 0f), new Vector2(thickness, size.y), color);
        }

        private static void CreateBorderStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
            }
        }

        private static Bounds GetRenderBounds(GameObject go)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            return has ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private static void AssignFallbackFont(TextMeshProUGUI tmp)
        {
            if (tmp.font != null) return;
            var anyTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
            tmp.font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        }
    }
}
