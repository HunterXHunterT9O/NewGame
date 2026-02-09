using UnityEngine;
using UnityEditor;
using System;
using System.IO;

#if NUGET_SKIASHARP_INSTALLED
using SkiaSharp;
#endif

namespace EdgeOfUniverse.Editor.UIGenerator
{
    /// <summary>
    /// Generates Mission Control UI textures with SkiaSharp.
    /// XCOM-style industrial sci-fi panels, buttons, bars, and indicators.
    /// Uses 9-slice friendly designs for resizable panels.
    /// </summary>
    public class MissionControlUIGenerator : EditorWindow
    {
        // Palette — matches SciFiUIGenerator
        private static readonly Color32 MetalDark = new Color32(35, 39, 42, 255);
        private static readonly Color32 MetalMid = new Color32(55, 62, 68, 255);
        private static readonly Color32 MetalLight = new Color32(85, 95, 105, 255);
        private static readonly Color32 MetalHighlight = new Color32(120, 130, 140, 255);

        private static readonly Color32 DangerRed = new Color32(180, 45, 45, 255);
        private static readonly Color32 WarningAmber = new Color32(200, 140, 40, 255);
        private static readonly Color32 InfoBlue = new Color32(60, 140, 180, 255);
        private static readonly Color32 SafeGreen = new Color32(55, 140, 75, 255);

        private static readonly Color32 AccentCyan = new Color32(80, 180, 200, 255);
        private static readonly Color32 GlowCyan = new Color32(100, 220, 240, 128);

        private static readonly Color32 PanelBg = new Color32(18, 22, 28, 235);
        private static readonly Color32 PanelBgLight = new Color32(28, 34, 42, 230);
        private static readonly Color32 HeaderBg = new Color32(35, 45, 58, 255);

        private string outputPath = "Assets/UI/Generated/MissionControl";

        [MenuItem("Tools/Edge of Universe/Generate Mission Control UI")]
        public static void ShowWindow()
        {
            var window = GetWindow<MissionControlUIGenerator>("Mission Control UI Gen");
            window.minSize = new Vector2(350, 300);
        }

        /// <summary>
        /// Generate all Mission Control textures non-interactively.
        /// Called from MissionControlSceneSetup.
        /// </summary>
        public static void GenerateAll()
        {
            var gen = CreateInstance<MissionControlUIGenerator>();
            gen.GenerateAllTextures();
            DestroyImmediate(gen);
        }

        /// <summary>
        /// Returns true if all expected textures exist.
        /// </summary>
        public static bool TexturesExist()
        {
            string basePath = "Assets/UI/Generated/MissionControl";
            string[] required = {
                "MC_Panel_9Slice.png", "MC_Panel_Header_9Slice.png",
                "MC_Bar_TopBar.png", "MC_Bar_BottomBar.png",
                "MC_Button_Deploy.png", "MC_Button_Deploy_Hover.png", "MC_Button_Deploy_Disabled.png",
                "MC_Button_Confirm.png", "MC_Button_Cancel.png",
                "MC_Panel_Confirm.png",
                "MC_BarFill.png", "MC_BarFrame.png",
                "MC_DangerBarFill.png", "MC_TravelBarFill.png", "MC_TravelBarFrame.png",
                "MC_Diamond.png", "MC_Diamond_Lit.png",
                "MC_Separator.png", "MC_Scanline.png",
                "MC_Panel_Results_9Slice.png", "MC_MiningBarFill.png", "MC_MiningBarFrame.png",
                "MC_Button_Continue.png"
            };

            foreach (var file in required)
            {
                if (!File.Exists(Path.Combine(Application.dataPath, "..", basePath, file)))
                    return false;
            }
            return true;
        }

        private void OnGUI()
        {
            GUILayout.Label("Mission Control UI Generator", EditorStyles.boldLabel);
            GUILayout.Label("XCOM-style industrial sci-fi textures", EditorStyles.miniLabel);
            GUILayout.Space(10);

#if !NUGET_SKIASHARP_INSTALLED
            EditorGUILayout.HelpBox(
                "SkiaSharp not installed. Will use Unity Texture2D fallback.\n" +
                "For best results, install SkiaSharp via NuGet.",
                MessageType.Warning);
            GUILayout.Space(10);
#else
            EditorGUILayout.HelpBox("SkiaSharp ready — full quality generation.", MessageType.Info);
#endif

            outputPath = EditorGUILayout.TextField("Output Path", outputPath);

            GUILayout.Space(15);

            bool exists = TexturesExist();
            if (exists)
            {
                EditorGUILayout.HelpBox("All Mission Control textures found.", MessageType.Info);
            }

            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.8f);
            if (GUILayout.Button(exists ? "Regenerate All Textures" : "Generate All Textures", GUILayout.Height(40)))
            {
                GenerateAllTextures();
            }
            GUI.backgroundColor = Color.white;
        }

        private void GenerateAllTextures()
        {
            EnsureDirectory();

            GeneratePanels();
            GenerateBars();
            GenerateButtons();
            GenerateIndicators();
            GenerateScanline();
            GenerateMiningTextures();

            AssetDatabase.Refresh();
            Debug.Log($"[MC UI Gen] All Mission Control textures generated in {outputPath}");
        }

        private void EnsureDirectory()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", outputPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        #region Panels (9-slice friendly)

        private void GeneratePanels()
        {
#if NUGET_SKIASHARP_INSTALLED
            // Plain panel — 9-slice with corner brackets and border
            Generate9SlicePanelSkia("MC_Panel_9Slice", 128, 128, PanelBg, AccentCyan, false);
            // Panel with header accent bar
            Generate9SlicePanelSkia("MC_Panel_Header_9Slice", 128, 128, PanelBg, AccentCyan, true);
            // Top bar
            GenerateBarPanelSkia("MC_Bar_TopBar", 512, 55, PanelBg, AccentCyan, true);
            // Bottom bar
            GenerateBarPanelSkia("MC_Bar_BottomBar", 512, 50, PanelBg, AccentCyan, false);
            // Confirm dialog
            GenerateConfirmPanelSkia("MC_Panel_Confirm", 400, 200);
#else
            Generate9SlicePanelFallback("MC_Panel_9Slice", 128, 128, PanelBg, AccentCyan, false);
            Generate9SlicePanelFallback("MC_Panel_Header_9Slice", 128, 128, PanelBg, AccentCyan, true);
            GenerateBarPanelFallback("MC_Bar_TopBar", 512, 55, PanelBg, AccentCyan);
            GenerateBarPanelFallback("MC_Bar_BottomBar", 512, 50, PanelBg, AccentCyan);
            GenerateConfirmPanelFallback("MC_Panel_Confirm", 400, 200);
#endif
            Debug.Log("[MC UI Gen] Panels generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void Generate9SlicePanelSkia(string name, int w, int h, Color32 bg, Color32 accent, bool hasHeader)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                var rect = new SKRect(0, 0, w, h);
                float cr = 4;

                // Background gradient (subtle vertical)
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(0, h),
                        new SKColor[] { ToSK(PanelBgLight), ToSK(bg) },
                        null, SKShaderTileMode.Clamp);
                    canvas.DrawRoundRect(rect, cr, cr, bgPaint);
                }

                // Header bar
                if (hasHeader)
                {
                    int hh = 32;
                    using (var headerPaint = new SKPaint())
                    {
                        headerPaint.IsAntialias = true;
                        headerPaint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(0, 0), new SKPoint(0, hh),
                            new SKColor[] { ToSK(HeaderBg), ToSK(PanelBg) },
                            null, SKShaderTileMode.Clamp);

                        var hp = new SKPath();
                        hp.AddRoundRect(new SKRect(1, 1, w - 1, hh), cr, cr);
                        hp.AddRect(new SKRect(1, hh - cr, w - 1, hh));
                        canvas.DrawPath(hp, headerPaint);
                    }

                    // Cyan accent line under header
                    using (var linePaint = new SKPaint())
                    {
                        linePaint.Color = ToSK(accent);
                        linePaint.StrokeWidth = 2;
                        canvas.DrawLine(8, hh, w - 8, hh, linePaint);
                    }

                    // Header glow
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.Color = new SKColor(accent.r, accent.g, accent.b, 25);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4);
                        canvas.DrawLine(8, hh, w - 8, hh, glowPaint);
                    }
                }

                // Worn scratches
                AddWornEffect(canvas, w, h, 0.06f);

                // Noise dots for texture
                AddNoiseDots(canvas, w, h, 0.03f);

                // Inner scanlines (very subtle)
                using (var scanPaint = new SKPaint())
                {
                    scanPaint.Color = new SKColor(0, 0, 0, 12);
                    for (int y = 0; y < h; y += 3)
                        canvas.DrawLine(2, y, w - 2, y, scanPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSK(MetalMid);
                    canvas.DrawRoundRect(new SKRect(1, 1, w - 1, h - 1), cr, cr, borderPaint);
                }

                // Accent border (outer glow line)
                using (var accentPaint = new SKPaint())
                {
                    accentPaint.IsAntialias = true;
                    accentPaint.Style = SKPaintStyle.Stroke;
                    accentPaint.StrokeWidth = 1;
                    accentPaint.Color = new SKColor(accent.r, accent.g, accent.b, 60);
                    canvas.DrawRoundRect(new SKRect(0, 0, w, h), cr + 1, cr + 1, accentPaint);
                }

                // Corner brackets
                DrawCornerBrackets(canvas, w, h, accent, 16, 5);

                // Rivets
                DrawRivets(canvas, w, h, 12);

                SaveSurface(surface, $"{name}.png");
            }
        }

        private void GenerateBarPanelSkia(string name, int w, int h, Color32 bg, Color32 accent, bool topAccent)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(0, h),
                        new SKColor[] { ToSK(PanelBgLight), ToSK(bg) },
                        null, SKShaderTileMode.Clamp);
                    canvas.DrawRect(0, 0, w, h, bgPaint);
                }

                // Worn effect
                AddWornEffect(canvas, w, h, 0.04f);
                AddNoiseDots(canvas, w, h, 0.02f);

                // Scanlines
                using (var scanPaint = new SKPaint())
                {
                    scanPaint.Color = new SKColor(0, 0, 0, 10);
                    for (int y = 0; y < h; y += 3)
                        canvas.DrawLine(0, y, w, y, scanPaint);
                }

                // Accent line (top or bottom edge)
                using (var accentPaint = new SKPaint())
                {
                    accentPaint.Color = ToSK(accent);
                    accentPaint.StrokeWidth = 2;
                    if (topAccent)
                        canvas.DrawLine(0, h - 1, w, h - 1, accentPaint);
                    else
                        canvas.DrawLine(0, 1, w, 1, accentPaint);
                }

                // Glow on accent line
                using (var glowPaint = new SKPaint())
                {
                    glowPaint.Color = new SKColor(accent.r, accent.g, accent.b, 30);
                    glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3);
                    if (topAccent)
                        canvas.DrawLine(0, h - 1, w, h - 1, glowPaint);
                    else
                        canvas.DrawLine(0, 1, w, 1, glowPaint);
                }

                // Section divider marks
                using (var divPaint = new SKPaint())
                {
                    divPaint.Color = new SKColor(accent.r, accent.g, accent.b, 40);
                    divPaint.StrokeWidth = 1;
                    // Small vertical ticks
                    for (int x = 64; x < w; x += 128)
                    {
                        int tickH = 6;
                        int yStart = topAccent ? h - tickH - 2 : 3;
                        canvas.DrawLine(x, yStart, x, yStart + tickH, divPaint);
                    }
                }

                SaveSurface(surface, $"{name}.png");
            }
        }

        private void GenerateConfirmPanelSkia(string name, int w, int h)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float cr = 6;
                var rect = new SKRect(0, 0, w, h);

                // Dark background with red-tinted header
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = ToSK(PanelBg);
                    canvas.DrawRoundRect(rect, cr, cr, bgPaint);
                }

                // Warning header bar
                int hh = 36;
                using (var headerPaint = new SKPaint())
                {
                    headerPaint.IsAntialias = true;
                    headerPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(0, hh),
                        new SKColor[] {
                            new SKColor(DangerRed.r, DangerRed.g, DangerRed.b, 180),
                            ToSK(PanelBg)
                        },
                        null, SKShaderTileMode.Clamp);

                    var hp = new SKPath();
                    hp.AddRoundRect(new SKRect(1, 1, w - 1, hh), cr, cr);
                    hp.AddRect(new SKRect(1, hh - cr, w - 1, hh));
                    canvas.DrawPath(hp, headerPaint);
                }

                // Red accent line
                using (var linePaint = new SKPaint())
                {
                    linePaint.Color = new SKColor(DangerRed.r, DangerRed.g, DangerRed.b, 200);
                    linePaint.StrokeWidth = 2;
                    canvas.DrawLine(12, hh, w - 12, hh, linePaint);
                }

                // Worn
                AddWornEffect(canvas, w, h, 0.05f);

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = new SKColor(DangerRed.r, DangerRed.g, DangerRed.b, 180);
                    canvas.DrawRoundRect(new SKRect(1, 1, w - 1, h - 1), cr, cr, borderPaint);
                }

                // Outer glow
                using (var glowPaint = new SKPaint())
                {
                    glowPaint.IsAntialias = true;
                    glowPaint.Style = SKPaintStyle.Stroke;
                    glowPaint.StrokeWidth = 3;
                    glowPaint.Color = new SKColor(DangerRed.r, DangerRed.g, DangerRed.b, 50);
                    glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 4);
                    canvas.DrawRoundRect(new SKRect(1, 1, w - 1, h - 1), cr, cr, glowPaint);
                }

                DrawCornerBrackets(canvas, w, h, DangerRed, 18, 5);

                SaveSurface(surface, $"{name}.png");
            }
        }
#endif

        private void Generate9SlicePanelFallback(string name, int w, int h, Color32 bg, Color32 accent, bool hasHeader)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2)
                        pixels[idx] = MetalMid;
                    else if (hasHeader && y >= h - 32)
                        pixels[idx] = HeaderBg;
                    else
                        pixels[idx] = bg;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateBarPanelFallback(string name, int w, int h, Color32 bg, Color32 accent)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (y == 0 || y == h - 1)
                        pixels[idx] = accent;
                    else
                        pixels[idx] = bg;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateConfirmPanelFallback(string name, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2)
                        pixels[idx] = DangerRed;
                    else
                        pixels[idx] = PanelBg;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Buttons

        private void GenerateButtons()
        {
            int bw = 290, bh = 50;
            int cbw = 130, cbh = 45;

#if NUGET_SKIASHARP_INSTALLED
            GenerateButtonSkia("MC_Button_Deploy", bw, bh, AccentCyan, MetalDark, false, false);
            GenerateButtonSkia("MC_Button_Deploy_Hover", bw, bh, AccentCyan, MetalMid, false, true);
            GenerateButtonSkia("MC_Button_Deploy_Disabled", bw, bh, MetalDark, MetalDark, false, false);
            GenerateButtonSkia("MC_Button_Confirm", cbw, cbh, SafeGreen, MetalDark, false, false);
            GenerateButtonSkia("MC_Button_Cancel", cbw, cbh, DangerRed, MetalDark, false, false);
#else
            GenerateButtonFallback("MC_Button_Deploy", bw, bh, AccentCyan);
            GenerateButtonFallback("MC_Button_Deploy_Hover", bw, bh, new Color32(100, 200, 220, 255));
            GenerateButtonFallback("MC_Button_Deploy_Disabled", bw, bh, MetalDark);
            GenerateButtonFallback("MC_Button_Confirm", cbw, cbh, SafeGreen);
            GenerateButtonFallback("MC_Button_Cancel", cbw, cbh, DangerRed);
#endif
            Debug.Log("[MC UI Gen] Buttons generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateButtonSkia(string name, int w, int h, Color32 accent, Color32 bg, bool pressed, bool glowing)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                var rect = new SKRect(2, 2, w - 2, h - 2);
                float cr = 3;

                // Background — dark with subtle gradient
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(0, h),
                        new SKColor[] {
                            new SKColor((byte)(bg.r + 15), (byte)(bg.g + 15), (byte)(bg.b + 15), 240),
                            new SKColor(bg.r, bg.g, bg.b, 240)
                        },
                        null, SKShaderTileMode.Clamp);
                    canvas.DrawRoundRect(rect, cr, cr, bgPaint);
                }

                // Accent border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSK(accent);
                    canvas.DrawRoundRect(rect, cr, cr, borderPaint);
                }

                // Inner highlight (top edge)
                using (var hlPaint = new SKPaint())
                {
                    hlPaint.IsAntialias = true;
                    hlPaint.Style = SKPaintStyle.Stroke;
                    hlPaint.StrokeWidth = 1;
                    hlPaint.Color = new SKColor(255, 255, 255, 20);
                    canvas.DrawLine(6, 4, w - 6, 4, hlPaint);
                }

                // Worn scratches
                AddWornEffect(canvas, w, h, 0.08f);

                // Glow effect
                if (glowing)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.IsAntialias = true;
                        glowPaint.Style = SKPaintStyle.Stroke;
                        glowPaint.StrokeWidth = 4;
                        glowPaint.Color = new SKColor(accent.r, accent.g, accent.b, 80);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 5);
                        canvas.DrawRoundRect(rect, cr, cr, glowPaint);
                    }
                }

                // Corner accents
                DrawCornerBrackets(canvas, w, h, accent, 10, 4);

                SaveSurface(surface, $"{name}.png");
            }
        }
#endif

        private void GenerateButtonFallback(string name, int w, int h, Color32 accent)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2)
                        tex.SetPixel(x, y, accent);
                    else
                        tex.SetPixel(x, y, MetalDark);
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Bars (Resource, Danger, Travel)

        private void GenerateBars()
        {
#if NUGET_SKIASHARP_INSTALLED
            // Generic resource bar fill (tintable white-ish gradient with scanlines)
            GenerateBarFillSkia("MC_BarFill", 200, 14, new Color32(200, 210, 220, 255));
            // Bar frame
            GenerateBarFrameSkia("MC_BarFrame", 200, 14);
            // Danger bar fill (red-orange gradient)
            GenerateBarFillSkia("MC_DangerBarFill", 290, 10, DangerRed);
            // Travel progress bar fill
            GenerateBarFillSkia("MC_TravelBarFill", 500, 18, AccentCyan);
            // Travel bar frame
            GenerateBarFrameSkia("MC_TravelBarFrame", 500, 18);
#else
            GenerateBarFillFallback("MC_BarFill", 200, 14, new Color32(200, 210, 220, 255));
            GenerateBarFrameFallback("MC_BarFrame", 200, 14);
            GenerateBarFillFallback("MC_DangerBarFill", 290, 10, DangerRed);
            GenerateBarFillFallback("MC_TravelBarFill", 500, 18, AccentCyan);
            GenerateBarFrameFallback("MC_TravelBarFrame", 500, 18);
#endif
            Debug.Log("[MC UI Gen] Bars generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateBarFillSkia(string name, int w, int h, Color32 color)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Gradient fill — brighter at top
                using (var fillPaint = new SKPaint())
                {
                    fillPaint.IsAntialias = true;
                    fillPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(0, h),
                        new SKColor[] {
                            new SKColor(ClampByte(color.r * 1.3f), ClampByte(color.g * 1.3f), ClampByte(color.b * 1.3f), 255),
                            ToSK(color)
                        },
                        null, SKShaderTileMode.Clamp);
                    canvas.DrawRect(0, 0, w, h, fillPaint);
                }

                // Scanlines for digital texture
                using (var scanPaint = new SKPaint())
                {
                    scanPaint.Color = new SKColor(0, 0, 0, 30);
                    for (int y = 0; y < h; y += 2)
                        canvas.DrawLine(0, y, w, y, scanPaint);
                }

                // Leading edge glow
                using (var edgePaint = new SKPaint())
                {
                    edgePaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(w - 20, 0), new SKPoint(w, 0),
                        new SKColor[] {
                            SKColors.Transparent,
                            new SKColor(255, 255, 255, 60)
                        },
                        null, SKShaderTileMode.Clamp);
                    canvas.DrawRect(w - 20, 0, 20, h, edgePaint);
                }

                SaveSurface(surface, $"{name}.png");
            }
        }

        private void GenerateBarFrameSkia(string name, int w, int h)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Dark background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.Color = new SKColor(12, 15, 18, 200);
                    canvas.DrawRect(0, 0, w, h, bgPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 1;
                    borderPaint.Color = ToSK(MetalMid);
                    canvas.DrawRect(new SKRect(0.5f, 0.5f, w - 0.5f, h - 0.5f), borderPaint);
                }

                // Segment marks (every 20%)
                using (var markPaint = new SKPaint())
                {
                    markPaint.Color = new SKColor(MetalHighlight.r, MetalHighlight.g, MetalHighlight.b, 40);
                    markPaint.StrokeWidth = 1;
                    for (int i = 1; i < 5; i++)
                    {
                        float x = w * i / 5f;
                        canvas.DrawLine(x, 2, x, h - 2, markPaint);
                    }
                }

                SaveSurface(surface, $"{name}.png");
            }
        }
#endif

        private void GenerateBarFillFallback(string name, int w, int h, Color32 color)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float gradient = 1f - (y / (float)h * 0.3f);
                Color32 c = new Color32(
                    ClampByte(color.r * gradient), ClampByte(color.g * gradient),
                    ClampByte(color.b * gradient), 255);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateBarFrameFallback(string name, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32 bg = new Color32(12, 15, 18, 200);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                        tex.SetPixel(x, y, MetalMid);
                    else
                        tex.SetPixel(x, y, bg);
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Indicators (Diamonds, Separator)

        private void GenerateIndicators()
        {
#if NUGET_SKIASHARP_INSTALLED
            GenerateDiamondSkia("MC_Diamond", 24, MetalMid, false);
            GenerateDiamondSkia("MC_Diamond_Lit", 24, AccentCyan, true);
            GenerateSeparatorSkia("MC_Separator", 300, 3);
#else
            GenerateDiamondFallback("MC_Diamond", 24, MetalMid);
            GenerateDiamondFallback("MC_Diamond_Lit", 24, AccentCyan);
            GenerateSeparatorFallback("MC_Separator", 300, 3);
#endif
            Debug.Log("[MC UI Gen] Indicators generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateDiamondSkia(string name, int size, Color32 color, bool glowing)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float c = size / 2f;
                float r = c - 3;

                // Diamond shape
                var path = new SKPath();
                path.MoveTo(c, c - r);       // top
                path.LineTo(c + r, c);       // right
                path.LineTo(c, c + r);       // bottom
                path.LineTo(c - r, c);       // left
                path.Close();

                // Fill
                using (var fillPaint = new SKPaint())
                {
                    fillPaint.IsAntialias = true;
                    fillPaint.Color = ToSK(color);
                    canvas.DrawPath(path, fillPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 1.5f;
                    borderPaint.Color = new SKColor(
                        ClampByte(color.r * 1.3f), ClampByte(color.g * 1.3f),
                        ClampByte(color.b * 1.3f), 255);
                    canvas.DrawPath(path, borderPaint);
                }

                // Glow
                if (glowing)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.IsAntialias = true;
                        glowPaint.Style = SKPaintStyle.Stroke;
                        glowPaint.StrokeWidth = 3;
                        glowPaint.Color = new SKColor(color.r, color.g, color.b, 80);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 3);
                        canvas.DrawPath(path, glowPaint);
                    }
                }

                // Inner highlight
                using (var hlPaint = new SKPaint())
                {
                    hlPaint.IsAntialias = true;
                    hlPaint.Color = new SKColor(255, 255, 255, 30);
                    canvas.DrawCircle(c - 1, c - 2, 3, hlPaint);
                }

                SaveSurface(surface, $"{name}.png");
            }
        }

        private void GenerateSeparatorSkia(string name, int w, int h)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float cy = h / 2f;

                // Gradient line — bright center, fading edges
                using (var linePaint = new SKPaint())
                {
                    linePaint.IsAntialias = true;
                    linePaint.StrokeWidth = 1;
                    linePaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(w, 0),
                        new SKColor[] {
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0),
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 120),
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 120),
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0)
                        },
                        new float[] { 0f, 0.2f, 0.8f, 1f },
                        SKShaderTileMode.Clamp);
                    canvas.DrawLine(0, cy, w, cy, linePaint);
                }

                // Glow
                using (var glowPaint = new SKPaint())
                {
                    glowPaint.StrokeWidth = 2;
                    glowPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0), new SKPoint(w, 0),
                        new SKColor[] {
                            SKColors.Transparent,
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 30),
                            new SKColor(AccentCyan.r, AccentCyan.g, AccentCyan.b, 30),
                            SKColors.Transparent
                        },
                        new float[] { 0f, 0.2f, 0.8f, 1f },
                        SKShaderTileMode.Clamp);
                    glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2);
                    canvas.DrawLine(0, cy, w, cy, glowPaint);
                }

                SaveSurface(surface, $"{name}.png");
            }
        }
#endif

        private void GenerateDiamondFallback(string name, int size, Color32 color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size / 2f;
            float r = c - 3;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diamond = rotated square: |x-c| + |y-c| < r
                    float dist = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                    if (dist < r)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateSeparatorFallback(string name, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            int cy = h / 2;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y == cy)
                    {
                        float alpha = 1f - Mathf.Abs(x - w / 2f) / (w / 2f);
                        alpha *= 0.5f;
                        tex.SetPixel(x, y, new Color32(AccentCyan.r, AccentCyan.g, AccentCyan.b, (byte)(alpha * 255)));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    }
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Scanline Overlay

        private void GenerateScanline()
        {
#if NUGET_SKIASHARP_INSTALLED
            GenerateScanlineSkia("MC_Scanline", 4, 256);
#else
            GenerateScanlineFallback("MC_Scanline", 4, 256);
#endif
            Debug.Log("[MC UI Gen] Scanline generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateScanlineSkia(string name, int w, int h)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                using (var paint = new SKPaint())
                {
                    for (int y = 0; y < h; y++)
                    {
                        // Alternating scanline pattern with subtle variation
                        byte alpha;
                        if (y % 4 < 2)
                            alpha = (byte)(6 + (y % 8 < 4 ? 2 : 0)); // subtle variation
                        else
                            alpha = 0;

                        paint.Color = new SKColor(200, 220, 230, alpha);
                        canvas.DrawLine(0, y, w, y, paint);
                    }
                }

                SaveSurface(surface, $"{name}.png");
            }
        }
#endif

        private void GenerateScanlineFallback(string name, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                byte alpha = (byte)((y % 4 < 2) ? 6 : 0);
                Color32 c = new Color32(200, 220, 230, alpha);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            SaveTexture(tex, $"{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Mining Textures

        private void GenerateMiningTextures()
        {
#if NUGET_SKIASHARP_INSTALLED
            // Results panel — larger 9-slice with cyan header
            Generate9SlicePanelSkia("MC_Panel_Results_9Slice", 256, 256, PanelBg, AccentCyan, true);
            // Mining progress bar fill (amber/yellow gradient)
            GenerateBarFillSkia("MC_MiningBarFill", 500, 18, WarningAmber);
            // Mining progress bar frame
            GenerateBarFrameSkia("MC_MiningBarFrame", 500, 18);
            // Continue button
            GenerateButtonSkia("MC_Button_Continue", 200, 50, AccentCyan, MetalDark, false, false);
#else
            Generate9SlicePanelFallback("MC_Panel_Results_9Slice", 256, 256, PanelBg, AccentCyan, true);
            GenerateBarFillFallback("MC_MiningBarFill", 500, 18, WarningAmber);
            GenerateBarFrameFallback("MC_MiningBarFrame", 500, 18);
            GenerateButtonFallback("MC_Button_Continue", 200, 50, AccentCyan);
#endif
            Debug.Log("[MC UI Gen] Mining textures generated");
        }

        #endregion

        #region Shared Drawing Helpers

#if NUGET_SKIASHARP_INSTALLED
        private void DrawCornerBrackets(SKCanvas canvas, int w, int h, Color32 color, int len, int offset)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = new SKColor(color.r, color.g, color.b, 180);
                paint.StrokeWidth = 2;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeCap = SKStrokeCap.Square;

                // Top-left
                canvas.DrawLine(offset, offset, offset, offset + len, paint);
                canvas.DrawLine(offset, offset, offset + len, offset, paint);
                // Top-right
                canvas.DrawLine(w - offset, offset, w - offset, offset + len, paint);
                canvas.DrawLine(w - offset, offset, w - offset - len, offset, paint);
                // Bottom-left
                canvas.DrawLine(offset, h - offset, offset, h - offset - len, paint);
                canvas.DrawLine(offset, h - offset, offset + len, h - offset, paint);
                // Bottom-right
                canvas.DrawLine(w - offset, h - offset, w - offset, h - offset - len, paint);
                canvas.DrawLine(w - offset, h - offset, w - offset - len, h - offset, paint);
            }
        }

        private void DrawRivets(SKCanvas canvas, int w, int h, int inset)
        {
            SKPoint[] positions = {
                new SKPoint(inset, inset), new SKPoint(w - inset, inset),
                new SKPoint(inset, h - inset), new SKPoint(w - inset, h - inset)
            };

            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                foreach (var pos in positions)
                {
                    // Shadow
                    paint.Color = new SKColor(0, 0, 0, 60);
                    canvas.DrawCircle(pos.X + 0.5f, pos.Y + 0.5f, 3, paint);
                    // Body
                    paint.Color = ToSK(MetalLight);
                    canvas.DrawCircle(pos.X, pos.Y, 3, paint);
                    // Highlight
                    paint.Color = new SKColor(255, 255, 255, 40);
                    canvas.DrawCircle(pos.X - 0.5f, pos.Y - 0.5f, 1.5f, paint);
                }
            }
        }

        private void AddWornEffect(SKCanvas canvas, int w, int h, float intensity)
        {
            var rand = new System.Random(42);
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.StrokeWidth = 1;
                paint.Style = SKPaintStyle.Stroke;

                int count = (int)(w * h * 0.001f * intensity);
                for (int i = 0; i < count; i++)
                {
                    float x1 = rand.Next(w);
                    float y1 = rand.Next(h);
                    float len = rand.Next(3, 15);
                    float angle = (float)(rand.NextDouble() * Math.PI);
                    float x2 = x1 + len * (float)Math.Cos(angle);
                    float y2 = y1 + len * (float)Math.Sin(angle);

                    byte alpha = (byte)rand.Next(8, 30);
                    paint.Color = rand.NextDouble() > 0.5
                        ? new SKColor(255, 255, 255, alpha)
                        : new SKColor(0, 0, 0, alpha);
                    canvas.DrawLine(x1, y1, x2, y2, paint);
                }
            }
        }

        private void AddNoiseDots(SKCanvas canvas, int w, int h, float density)
        {
            var rand = new System.Random(137);
            int count = (int)(w * h * density);
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = false;
                for (int i = 0; i < count; i++)
                {
                    float x = rand.Next(w);
                    float y = rand.Next(h);
                    byte alpha = (byte)rand.Next(5, 20);
                    paint.Color = rand.NextDouble() > 0.5
                        ? new SKColor(255, 255, 255, alpha)
                        : new SKColor(0, 0, 0, alpha);
                    canvas.DrawPoint(x, y, paint);
                }
            }
        }

        private SKColor ToSK(Color32 c) => new SKColor(c.r, c.g, c.b, c.a);

        private void SaveSurface(SKSurface surface, string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", outputPath, relativePath);
            using (var image = surface.Snapshot())
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(fullPath))
            {
                data.SaveTo(stream);
            }
        }
#endif

        private void SaveTexture(Texture2D texture, string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", outputPath, relativePath);
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);
        }

        private static byte ClampByte(float value) => (byte)Mathf.Clamp(value, 0, 255);

        #endregion
    }
}
