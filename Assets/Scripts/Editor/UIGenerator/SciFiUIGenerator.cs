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
    /// Generates sci-fi UI elements with a worn, industrial aesthetic using SkiaSharp.
    /// Matches the game's military/industrial feel with weathered metal textures.
    /// </summary>
    public class SciFiUIGenerator : EditorWindow
    {
        // Color palette matching the game's aesthetic
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

        private string outputPath = "Assets/UI/Generated";
        private int buttonWidth = 200;
        private int buttonHeight = 50;
        private int panelWidth = 400;
        private int panelHeight = 300;

        [MenuItem("Tools/Edge of Universe/UI Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SciFiUIGenerator>("Sci-Fi UI Generator");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Edge of Universe - UI Generator", EditorStyles.boldLabel);
            GUILayout.Label("Worn Industrial Sci-Fi Aesthetic", EditorStyles.miniLabel);
            GUILayout.Space(10);

#if !NUGET_SKIASHARP_INSTALLED
            EditorGUILayout.HelpBox(
                "SkiaSharp is not installed.\n\n" +
                "1. Open NuGet menu: NuGet → Manage NuGet Packages\n" +
                "2. Search for 'SkiaSharp'\n" +
                "3. Install SkiaSharp (latest version)\n" +
                "4. Add 'NUGET_SKIASHARP_INSTALLED' to Scripting Define Symbols\n" +
                "   (Edit → Project Settings → Player → Scripting Define Symbols)",
                MessageType.Warning);

            GUILayout.Space(10);
            if (GUILayout.Button("Open Player Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            GUILayout.Space(20);
            GUILayout.Label("Preview (using Unity fallback):", EditorStyles.boldLabel);
#else
            EditorGUILayout.HelpBox("SkiaSharp is installed and ready!", MessageType.Info);
#endif

            GUILayout.Space(10);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);

            GUILayout.Space(10);
            GUILayout.Label("Button Settings", EditorStyles.boldLabel);
            buttonWidth = EditorGUILayout.IntSlider("Width", buttonWidth, 100, 500);
            buttonHeight = EditorGUILayout.IntSlider("Height", buttonHeight, 30, 100);

            GUILayout.Space(10);
            GUILayout.Label("Panel Settings", EditorStyles.boldLabel);
            panelWidth = EditorGUILayout.IntSlider("Width", panelWidth, 200, 800);
            panelHeight = EditorGUILayout.IntSlider("Height", panelHeight, 150, 600);

            GUILayout.Space(20);
            GUILayout.Label("Generate UI Elements", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate All UI Elements", GUILayout.Height(40)))
            {
                GenerateAllUI();
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Buttons"))
                GenerateButtons();
            if (GUILayout.Button("Panels"))
                GeneratePanels();
            if (GUILayout.Button("Threat Meter"))
                GenerateThreatMeter();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Progress Bars"))
                GenerateProgressBars();
            if (GUILayout.Button("Icons"))
                GenerateIcons();
            if (GUILayout.Button("Compass"))
                GenerateCompass();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Squad Frames"))
                GenerateSquadFrames();
            if (GUILayout.Button("Warning Indicators"))
                GenerateWarningIndicators();
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateAllUI()
        {
            EnsureOutputDirectory();
            GenerateButtons();
            GeneratePanels();
            GenerateThreatMeter();
            GenerateProgressBars();
            GenerateIcons();
            GenerateCompass();
            GenerateSquadFrames();
            GenerateWarningIndicators();

            AssetDatabase.Refresh();
            Debug.Log($"[UI Generator] All UI elements generated in {outputPath}");
        }

        private void EnsureOutputDirectory()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Create subdirectories
            string[] subdirs = { "Buttons", "Panels", "ThreatMeter", "ProgressBars", "Icons", "Compass", "SquadFrames", "Warnings" };
            foreach (var subdir in subdirs)
            {
                string subPath = Path.Combine(fullPath, subdir);
                if (!Directory.Exists(subPath))
                {
                    Directory.CreateDirectory(subPath);
                }
            }
        }

        #region Button Generation

        private void GenerateButtons()
        {
            EnsureOutputDirectory();

#if NUGET_SKIASHARP_INSTALLED
            GenerateButtonWithSkia("Button_Normal", MetalMid, MetalLight, false, false);
            GenerateButtonWithSkia("Button_Hover", MetalLight, AccentCyan, false, true);
            GenerateButtonWithSkia("Button_Pressed", MetalDark, MetalMid, true, false);
            GenerateButtonWithSkia("Button_Disabled", MetalDark, MetalDark, false, false);

            GenerateButtonWithSkia("Button_Danger_Normal", DangerRed, new Color32(220, 60, 60, 255), false, false);
            GenerateButtonWithSkia("Button_Danger_Hover", new Color32(200, 55, 55, 255), DangerRed, false, true);

            GenerateButtonWithSkia("Button_Confirm_Normal", SafeGreen, new Color32(70, 160, 90, 255), false, false);
            GenerateButtonWithSkia("Button_Confirm_Hover", new Color32(65, 155, 85, 255), SafeGreen, false, true);
#else
            GenerateButtonFallback("Button_Normal", MetalMid, MetalLight);
            GenerateButtonFallback("Button_Hover", MetalLight, AccentCyan);
            GenerateButtonFallback("Button_Pressed", MetalDark, MetalMid);
            GenerateButtonFallback("Button_Disabled", MetalDark, MetalDark);
            GenerateButtonFallback("Button_Danger_Normal", DangerRed, new Color32(220, 60, 60, 255));
            GenerateButtonFallback("Button_Danger_Hover", new Color32(200, 55, 55, 255), DangerRed);
            GenerateButtonFallback("Button_Confirm_Normal", SafeGreen, new Color32(70, 160, 90, 255));
            GenerateButtonFallback("Button_Confirm_Hover", new Color32(65, 155, 85, 255), SafeGreen);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Buttons generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateButtonWithSkia(string name, Color32 baseColor, Color32 accentColor, bool pressed, bool glowing)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(buttonWidth, buttonHeight)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                var rect = new SKRect(2, 2, buttonWidth - 2, buttonHeight - 2);
                float cornerRadius = 4;

                // Background with gradient
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0),
                        new SKPoint(0, buttonHeight),
                        new SKColor[] {
                            ToSKColor(pressed ? baseColor : accentColor),
                            ToSKColor(pressed ? accentColor : baseColor)
                        },
                        null,
                        SKShaderTileMode.Clamp);

                    canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, bgPaint);
                }

                // Worn scratches effect
                AddWornEffect(canvas, buttonWidth, buttonHeight, 0.15f);

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(MetalHighlight);
                    canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);
                }

                // Inner highlight (top edge)
                using (var highlightPaint = new SKPaint())
                {
                    highlightPaint.IsAntialias = true;
                    highlightPaint.Style = SKPaintStyle.Stroke;
                    highlightPaint.StrokeWidth = 1;
                    highlightPaint.Color = new SKColor(255, 255, 255, 40);

                    var innerRect = new SKRect(4, 4, buttonWidth - 4, buttonHeight / 3);
                    canvas.DrawRoundRect(innerRect, cornerRadius - 2, cornerRadius - 2, highlightPaint);
                }

                // Glow effect
                if (glowing)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.IsAntialias = true;
                        glowPaint.Style = SKPaintStyle.Stroke;
                        glowPaint.StrokeWidth = 3;
                        glowPaint.Color = ToSKColor(GlowCyan);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 4);
                        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, glowPaint);
                    }
                }

                // Corner accents (industrial look)
                DrawCornerAccents(canvas, buttonWidth, buttonHeight, accentColor);

                SaveSurface(surface, $"Buttons/{name}.png");
            }
        }

        private void DrawCornerAccents(SKCanvas canvas, int width, int height, Color32 color)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = ToSKColor(color);
                paint.StrokeWidth = 2;
                paint.Style = SKPaintStyle.Stroke;

                int cornerLen = 8;

                // Top-left
                canvas.DrawLine(4, 8, 4, 4 + cornerLen, paint);
                canvas.DrawLine(4, 8, 4 + cornerLen, 8, paint);

                // Top-right
                canvas.DrawLine(width - 4, 8, width - 4, 4 + cornerLen, paint);
                canvas.DrawLine(width - 4, 8, width - 4 - cornerLen, 8, paint);

                // Bottom-left
                canvas.DrawLine(4, height - 8, 4, height - 4 - cornerLen, paint);
                canvas.DrawLine(4, height - 8, 4 + cornerLen, height - 8, paint);

                // Bottom-right
                canvas.DrawLine(width - 4, height - 8, width - 4, height - 4 - cornerLen, paint);
                canvas.DrawLine(width - 4, height - 8, width - 4 - cornerLen, height - 8, paint);
            }
        }

        private void AddWornEffect(SKCanvas canvas, int width, int height, float intensity)
        {
            System.Random rand = new System.Random(42); // Consistent seed for reproducible results

            using (var scratchPaint = new SKPaint())
            {
                scratchPaint.IsAntialias = true;
                scratchPaint.StrokeWidth = 1;
                scratchPaint.Style = SKPaintStyle.Stroke;

                int scratchCount = (int)(width * height * 0.001f * intensity);

                for (int i = 0; i < scratchCount; i++)
                {
                    float x1 = rand.Next(width);
                    float y1 = rand.Next(height);
                    float len = rand.Next(5, 20);
                    float angle = (float)(rand.NextDouble() * Math.PI);

                    float x2 = x1 + len * (float)Math.Cos(angle);
                    float y2 = y1 + len * (float)Math.Sin(angle);

                    byte alpha = (byte)(rand.Next(10, 40));
                    scratchPaint.Color = rand.NextDouble() > 0.5
                        ? new SKColor(255, 255, 255, alpha)
                        : new SKColor(0, 0, 0, alpha);

                    canvas.DrawLine(x1, y1, x2, y2, scratchPaint);
                }
            }
        }
#endif

        private void GenerateButtonFallback(string name, Color32 baseColor, Color32 accentColor)
        {
            Texture2D tex = new Texture2D(buttonWidth, buttonHeight, TextureFormat.RGBA32, false);

            // Fill with gradient
            for (int y = 0; y < buttonHeight; y++)
            {
                float t = y / (float)buttonHeight;
                Color32 color = Color32.Lerp(accentColor, baseColor, t);

                for (int x = 0; x < buttonWidth; x++)
                {
                    // Simple border
                    if (x < 2 || x >= buttonWidth - 2 || y < 2 || y >= buttonHeight - 2)
                    {
                        tex.SetPixel(x, y, MetalHighlight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            // Add some noise for worn effect
            System.Random rand = new System.Random(42);
            for (int i = 0; i < buttonWidth * buttonHeight / 50; i++)
            {
                int x = rand.Next(4, buttonWidth - 4);
                int y = rand.Next(4, buttonHeight - 4);
                Color existing = tex.GetPixel(x, y);
                float noise = (float)(rand.NextDouble() * 0.1 - 0.05);
                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(existing.r + noise),
                    Mathf.Clamp01(existing.g + noise),
                    Mathf.Clamp01(existing.b + noise),
                    existing.a
                ));
            }

            tex.Apply();
            SaveTexture(tex, $"Buttons/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Panel Generation

        private void GeneratePanels()
        {
            EnsureOutputDirectory();

#if NUGET_SKIASHARP_INSTALLED
            GeneratePanelWithSkia("Panel_Main", panelWidth, panelHeight, MetalDark, MetalMid, true);
            GeneratePanelWithSkia("Panel_Secondary", panelWidth, panelHeight, MetalMid, MetalLight, false);
            GeneratePanelWithSkia("Panel_Alert", panelWidth, panelHeight / 2, DangerRed, MetalDark, true);
            GeneratePanelWithSkia("Panel_Info", panelWidth, panelHeight / 2, InfoBlue, MetalDark, true);
            GeneratePanelWithSkia("Panel_Tooltip", 250, 100, MetalDark, MetalMid, false);
#else
            GeneratePanelFallback("Panel_Main", panelWidth, panelHeight, MetalDark, MetalMid);
            GeneratePanelFallback("Panel_Secondary", panelWidth, panelHeight, MetalMid, MetalLight);
            GeneratePanelFallback("Panel_Alert", panelWidth, panelHeight / 2, DangerRed, MetalDark);
            GeneratePanelFallback("Panel_Info", panelWidth, panelHeight / 2, InfoBlue, MetalDark);
            GeneratePanelFallback("Panel_Tooltip", 250, 100, MetalDark, MetalMid);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Panels generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GeneratePanelWithSkia(string name, int width, int height, Color32 borderColor, Color32 bgColor, bool hasHeader)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float cornerRadius = 6;
                var mainRect = new SKRect(0, 0, width, height);

                // Main background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = ToSKColor(bgColor);
                    canvas.DrawRoundRect(mainRect, cornerRadius, cornerRadius, bgPaint);
                }

                // Header bar if applicable
                if (hasHeader)
                {
                    int headerHeight = 30;
                    using (var headerPaint = new SKPaint())
                    {
                        headerPaint.IsAntialias = true;
                        headerPaint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(0, 0),
                            new SKPoint(0, headerHeight),
                            new SKColor[] { ToSKColor(borderColor), ToSKColor(MetalDark) },
                            null,
                            SKShaderTileMode.Clamp);

                        var headerPath = new SKPath();
                        headerPath.AddRoundRect(new SKRect(0, 0, width, headerHeight), cornerRadius, cornerRadius);
                        headerPath.AddRect(new SKRect(0, headerHeight - cornerRadius, width, headerHeight));
                        canvas.DrawPath(headerPath, headerPaint);
                    }

                    // Header divider line
                    using (var linePaint = new SKPaint())
                    {
                        linePaint.Color = ToSKColor(MetalHighlight);
                        linePaint.StrokeWidth = 1;
                        canvas.DrawLine(0, headerHeight, width, headerHeight, linePaint);
                    }
                }

                // Worn effect
                AddWornEffect(canvas, width, height, 0.08f);

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(borderColor);
                    canvas.DrawRoundRect(new SKRect(1, 1, width - 1, height - 1), cornerRadius, cornerRadius, borderPaint);
                }

                // Corner brackets (industrial detail)
                DrawPanelCornerBrackets(canvas, width, height, borderColor);

                // Rivet details
                DrawRivets(canvas, width, height);

                SaveSurface(surface, $"Panels/{name}.png");
            }
        }

        private void DrawPanelCornerBrackets(SKCanvas canvas, int width, int height, Color32 color)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = ToSKColor(color);
                paint.StrokeWidth = 3;
                paint.Style = SKPaintStyle.Stroke;

                int bracketLen = 20;
                int offset = 6;

                // Top-left
                canvas.DrawLine(offset, offset + bracketLen, offset, offset, paint);
                canvas.DrawLine(offset, offset, offset + bracketLen, offset, paint);

                // Top-right
                canvas.DrawLine(width - offset, offset + bracketLen, width - offset, offset, paint);
                canvas.DrawLine(width - offset, offset, width - offset - bracketLen, offset, paint);

                // Bottom-left
                canvas.DrawLine(offset, height - offset - bracketLen, offset, height - offset, paint);
                canvas.DrawLine(offset, height - offset, offset + bracketLen, height - offset, paint);

                // Bottom-right
                canvas.DrawLine(width - offset, height - offset - bracketLen, width - offset, height - offset, paint);
                canvas.DrawLine(width - offset, height - offset, width - offset - bracketLen, height - offset, paint);
            }
        }

        private void DrawRivets(SKCanvas canvas, int width, int height)
        {
            using (var rivetPaint = new SKPaint())
            {
                rivetPaint.IsAntialias = true;

                // Positions for rivets
                SKPoint[] positions = {
                    new SKPoint(15, 15), new SKPoint(width - 15, 15),
                    new SKPoint(15, height - 15), new SKPoint(width - 15, height - 15)
                };

                foreach (var pos in positions)
                {
                    // Rivet shadow
                    rivetPaint.Color = new SKColor(0, 0, 0, 80);
                    canvas.DrawCircle(pos.X + 1, pos.Y + 1, 4, rivetPaint);

                    // Rivet body
                    rivetPaint.Color = ToSKColor(MetalLight);
                    canvas.DrawCircle(pos.X, pos.Y, 4, rivetPaint);

                    // Rivet highlight
                    rivetPaint.Color = new SKColor(255, 255, 255, 60);
                    canvas.DrawCircle(pos.X - 1, pos.Y - 1, 2, rivetPaint);
                }
            }
        }
#endif

        private void GeneratePanelFallback(string name, int width, int height, Color32 borderColor, Color32 bgColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;

                    // Border
                    if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                    {
                        pixels[idx] = borderColor;
                    }
                    else
                    {
                        pixels[idx] = bgColor;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            SaveTexture(tex, $"Panels/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Threat Meter Generation

        private void GenerateThreatMeter()
        {
            EnsureOutputDirectory();

            // Generate threat meter states: CLEAR, DETECTED, ALERTED, SWARM, CRITICAL
            string[] states = { "Clear", "Detected", "Alerted", "Swarm", "Critical" };
            Color32[] stateColors = { SafeGreen, WarningAmber, new Color32(230, 120, 30, 255), DangerRed, new Color32(255, 30, 30, 255) };

            int meterWidth = 300;
            int meterHeight = 40;

            for (int i = 0; i < states.Length; i++)
            {
#if NUGET_SKIASHARP_INSTALLED
                GenerateThreatMeterStateSkia(states[i], meterWidth, meterHeight, stateColors[i], (float)(i + 1) / states.Length);
#else
                GenerateThreatMeterStateFallback(states[i], meterWidth, meterHeight, stateColors[i], (float)(i + 1) / states.Length);
#endif
            }

            // Generate the meter background/frame
#if NUGET_SKIASHARP_INSTALLED
            GenerateThreatMeterFrameSkia(meterWidth, meterHeight);
#else
            GenerateThreatMeterFrameFallback(meterWidth, meterHeight);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Threat Meter generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateThreatMeterStateSkia(string state, int width, int height, Color32 color, float fillAmount)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                int fillWidth = (int)(width * fillAmount);

                // Fill gradient
                using (var fillPaint = new SKPaint())
                {
                    fillPaint.IsAntialias = true;
                    fillPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0),
                        new SKPoint(0, height),
                        new SKColor[] {
                            ToSKColor(color),
                            new SKColor((byte)(color.r * 0.6f), (byte)(color.g * 0.6f), (byte)(color.b * 0.6f), 255)
                        },
                        null,
                        SKShaderTileMode.Clamp);

                    canvas.DrawRect(new SKRect(4, 4, fillWidth - 4, height - 4), fillPaint);
                }

                // Glow at the edge
                if (fillAmount > 0.1f)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.IsAntialias = true;
                        glowPaint.Color = new SKColor(color.r, color.g, color.b, 150);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6);
                        canvas.DrawRect(new SKRect(fillWidth - 10, 0, fillWidth, height), glowPaint);
                    }
                }

                // Scanlines for digital feel
                using (var scanPaint = new SKPaint())
                {
                    scanPaint.Color = new SKColor(0, 0, 0, 30);
                    for (int y = 0; y < height; y += 2)
                    {
                        canvas.DrawLine(0, y, fillWidth, y, scanPaint);
                    }
                }

                SaveSurface(surface, $"ThreatMeter/Threat_{state}.png");
            }
        }

        private void GenerateThreatMeterFrameSkia(int width, int height)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Dark background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = new SKColor(20, 22, 25, 230);
                    canvas.DrawRoundRect(new SKRect(0, 0, width, height), 4, 4, bgPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(MetalLight);
                    canvas.DrawRoundRect(new SKRect(1, 1, width - 1, height - 1), 4, 4, borderPaint);
                }

                // Segment markers
                using (var markerPaint = new SKPaint())
                {
                    markerPaint.Color = ToSKColor(MetalHighlight);
                    markerPaint.StrokeWidth = 1;

                    for (int i = 1; i < 5; i++)
                    {
                        float x = width * i / 5f;
                        canvas.DrawLine(x, 2, x, height - 2, markerPaint);
                    }
                }

                SaveSurface(surface, "ThreatMeter/Threat_Frame.png");
            }
        }
#endif

        private void GenerateThreatMeterStateFallback(string state, int width, int height, Color32 color, float fillAmount)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int fillWidth = (int)(width * fillAmount);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < fillWidth)
                    {
                        float gradient = 1f - (y / (float)height * 0.4f);
                        tex.SetPixel(x, y, new Color32(
                            (byte)(color.r * gradient),
                            (byte)(color.g * gradient),
                            (byte)(color.b * gradient),
                            255));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, $"ThreatMeter/Threat_{state}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateThreatMeterFrameFallback(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                    {
                        tex.SetPixel(x, y, MetalLight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(20, 22, 25, 230));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, "ThreatMeter/Threat_Frame.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Progress Bar Generation

        private void GenerateProgressBars()
        {
            EnsureOutputDirectory();

            int barWidth = 200;
            int barHeight = 20;

#if NUGET_SKIASHARP_INSTALLED
            GenerateProgressBarSkia("ProgressBar_Health", barWidth, barHeight, SafeGreen);
            GenerateProgressBarSkia("ProgressBar_Ammo", barWidth, barHeight, WarningAmber);
            GenerateProgressBarSkia("ProgressBar_Resource", barWidth, barHeight, InfoBlue);
            GenerateProgressBarSkia("ProgressBar_Oxygen", barWidth, barHeight, AccentCyan);
            GenerateProgressBarFrameSkia("ProgressBar_Frame", barWidth, barHeight);
#else
            GenerateProgressBarFallback("ProgressBar_Health", barWidth, barHeight, SafeGreen);
            GenerateProgressBarFallback("ProgressBar_Ammo", barWidth, barHeight, WarningAmber);
            GenerateProgressBarFallback("ProgressBar_Resource", barWidth, barHeight, InfoBlue);
            GenerateProgressBarFallback("ProgressBar_Oxygen", barWidth, barHeight, AccentCyan);
            GenerateProgressBarFrameFallback("ProgressBar_Frame", barWidth, barHeight);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Progress Bars generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateProgressBarSkia(string name, int width, int height, Color32 color)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                using (var fillPaint = new SKPaint())
                {
                    fillPaint.IsAntialias = true;
                    fillPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0),
                        new SKPoint(0, height),
                        new SKColor[] {
                            new SKColor(ClampByte(color.r * 1.2f), ClampByte(color.g * 1.2f), ClampByte(color.b * 1.2f), 255),
                            ToSKColor(color)
                        },
                        null,
                        SKShaderTileMode.Clamp);

                    canvas.DrawRect(new SKRect(0, 0, width, height), fillPaint);
                }

                // Scanlines
                using (var scanPaint = new SKPaint())
                {
                    scanPaint.Color = new SKColor(0, 0, 0, 25);
                    for (int y = 0; y < height; y += 2)
                    {
                        canvas.DrawLine(0, y, width, y, scanPaint);
                    }
                }

                SaveSurface(surface, $"ProgressBars/{name}.png");
            }
        }

        private void GenerateProgressBarFrameSkia(string name, int width, int height)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.Color = new SKColor(15, 18, 20, 200);
                    canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(MetalMid);
                    canvas.DrawRect(new SKRect(1, 1, width - 1, height - 1), borderPaint);
                }

                SaveSurface(surface, $"ProgressBars/{name}.png");
            }
        }
#endif

        private void GenerateProgressBarFallback(string name, int width, int height, Color32 color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                float gradient = 1f - (y / (float)height * 0.2f);
                Color32 c = new Color32(
                    (byte)(color.r * gradient),
                    (byte)(color.g * gradient),
                    (byte)(color.b * gradient),
                    255);

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            SaveTexture(tex, $"ProgressBars/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private void GenerateProgressBarFrameFallback(string name, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                    {
                        tex.SetPixel(x, y, MetalMid);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(15, 18, 20, 200));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, $"ProgressBars/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Icon Generation

        private void GenerateIcons()
        {
            EnsureOutputDirectory();

            int iconSize = 64;

#if NUGET_SKIASHARP_INSTALLED
            GenerateIconSkia("Icon_Warning", iconSize, "warning");
            GenerateIconSkia("Icon_Skull", iconSize, "skull");
            GenerateIconSkia("Icon_Shield", iconSize, "shield");
            GenerateIconSkia("Icon_Gear", iconSize, "gear");
            GenerateIconSkia("Icon_Medical", iconSize, "medical");
            GenerateIconSkia("Icon_Ammo", iconSize, "ammo");
#else
            GenerateIconFallback("Icon_Warning", iconSize, WarningAmber);
            GenerateIconFallback("Icon_Skull", iconSize, DangerRed);
            GenerateIconFallback("Icon_Shield", iconSize, InfoBlue);
            GenerateIconFallback("Icon_Gear", iconSize, MetalLight);
            GenerateIconFallback("Icon_Medical", iconSize, SafeGreen);
            GenerateIconFallback("Icon_Ammo", iconSize, WarningAmber);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Icons generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateIconSkia(string name, int size, string iconType)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float center = size / 2f;
                float radius = size / 2f - 4;

                // Icon background circle
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = new SKColor(30, 33, 36, 200);
                    canvas.DrawCircle(center, center, radius, bgPaint);
                }

                // Draw specific icon shape
                using (var iconPaint = new SKPaint())
                {
                    iconPaint.IsAntialias = true;
                    iconPaint.Style = SKPaintStyle.Stroke;
                    iconPaint.StrokeWidth = 3;
                    iconPaint.StrokeCap = SKStrokeCap.Round;

                    switch (iconType)
                    {
                        case "warning":
                            iconPaint.Color = ToSKColor(WarningAmber);
                            DrawWarningTriangle(canvas, iconPaint, center, center, radius * 0.7f);
                            break;
                        case "skull":
                            iconPaint.Color = ToSKColor(DangerRed);
                            DrawSkullIcon(canvas, iconPaint, center, center, radius * 0.6f);
                            break;
                        case "shield":
                            iconPaint.Color = ToSKColor(InfoBlue);
                            DrawShieldIcon(canvas, iconPaint, center, center, radius * 0.7f);
                            break;
                        case "gear":
                            iconPaint.Color = ToSKColor(MetalLight);
                            DrawGearIcon(canvas, iconPaint, center, center, radius * 0.6f);
                            break;
                        case "medical":
                            iconPaint.Color = ToSKColor(SafeGreen);
                            DrawMedicalIcon(canvas, iconPaint, center, center, radius * 0.5f);
                            break;
                        case "ammo":
                            iconPaint.Color = ToSKColor(WarningAmber);
                            DrawAmmoIcon(canvas, iconPaint, center, center, radius * 0.5f);
                            break;
                    }
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(MetalMid);
                    canvas.DrawCircle(center, center, radius, borderPaint);
                }

                SaveSurface(surface, $"Icons/{name}.png");
            }
        }

        private void DrawWarningTriangle(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            var path = new SKPath();
            path.MoveTo(cx, cy - size);
            path.LineTo(cx - size, cy + size * 0.7f);
            path.LineTo(cx + size, cy + size * 0.7f);
            path.Close();
            canvas.DrawPath(path, paint);

            // Exclamation mark
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawRect(new SKRect(cx - 2, cy - size * 0.3f, cx + 2, cy + size * 0.1f), paint);
            canvas.DrawCircle(cx, cy + size * 0.35f, 3, paint);
        }

        private void DrawSkullIcon(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            // Skull outline (simplified)
            canvas.DrawOval(new SKRect(cx - size, cy - size * 0.8f, cx + size, cy + size * 0.4f), paint);

            // Eye sockets
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawCircle(cx - size * 0.4f, cy - size * 0.2f, size * 0.2f, paint);
            canvas.DrawCircle(cx + size * 0.4f, cy - size * 0.2f, size * 0.2f, paint);

            // Jaw
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(cx - size * 0.5f, cy + size * 0.5f, cx + size * 0.5f, cy + size * 0.5f, paint);
        }

        private void DrawShieldIcon(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            var path = new SKPath();
            path.MoveTo(cx, cy - size);
            path.LineTo(cx + size, cy - size * 0.5f);
            path.LineTo(cx + size * 0.8f, cy + size * 0.5f);
            path.LineTo(cx, cy + size);
            path.LineTo(cx - size * 0.8f, cy + size * 0.5f);
            path.LineTo(cx - size, cy - size * 0.5f);
            path.Close();
            canvas.DrawPath(path, paint);
        }

        private void DrawGearIcon(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            // Outer gear teeth (simplified)
            canvas.DrawCircle(cx, cy, size, paint);
            canvas.DrawCircle(cx, cy, size * 0.5f, paint);

            // Teeth
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45 * (float)Math.PI / 180;
                float x1 = cx + (float)Math.Cos(angle) * size * 0.7f;
                float y1 = cy + (float)Math.Sin(angle) * size * 0.7f;
                float x2 = cx + (float)Math.Cos(angle) * size * 1.1f;
                float y2 = cy + (float)Math.Sin(angle) * size * 1.1f;
                canvas.DrawLine(x1, y1, x2, y2, paint);
            }
        }

        private void DrawMedicalIcon(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            float thickness = size * 0.4f;

            // Vertical bar
            canvas.DrawRect(new SKRect(cx - thickness / 2, cy - size, cx + thickness / 2, cy + size), paint);
            // Horizontal bar
            canvas.DrawRect(new SKRect(cx - size, cy - thickness / 2, cx + size, cy + thickness / 2), paint);
        }

        private void DrawAmmoIcon(SKCanvas canvas, SKPaint paint, float cx, float cy, float size)
        {
            // Bullet shape
            float bulletWidth = size * 0.4f;

            // Draw 3 bullets
            for (int i = -1; i <= 1; i++)
            {
                float bx = cx + i * bulletWidth * 1.2f;

                // Bullet tip
                var path = new SKPath();
                path.MoveTo(bx, cy - size);
                path.LineTo(bx - bulletWidth / 2, cy - size * 0.3f);
                path.LineTo(bx + bulletWidth / 2, cy - size * 0.3f);
                path.Close();
                canvas.DrawPath(path, paint);

                // Bullet body
                canvas.DrawRect(new SKRect(bx - bulletWidth / 2, cy - size * 0.3f, bx + bulletWidth / 2, cy + size * 0.8f), paint);
            }
        }
#endif

        private void GenerateIconFallback(string name, int size, Color32 color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            float center = size / 2f;
            float radius = size / 2f - 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));

                    if (dist < radius)
                    {
                        if (dist > radius - 3)
                        {
                            tex.SetPixel(x, y, color);
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color32(30, 33, 36, 200));
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, $"Icons/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Compass Generation

        private void GenerateCompass()
        {
            EnsureOutputDirectory();

            int compassSize = 128;

#if NUGET_SKIASHARP_INSTALLED
            GenerateCompassSkia(compassSize);
#else
            GenerateCompassFallback(compassSize);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Compass generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateCompassSkia(int size)
        {
            // Main compass body
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float center = size / 2f;
                float radius = size / 2f - 4;

                // Outer worn metal ring
                using (var ringPaint = new SKPaint())
                {
                    ringPaint.IsAntialias = true;
                    ringPaint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint(center, center),
                        radius,
                        new SKColor[] { ToSKColor(MetalMid), ToSKColor(MetalDark) },
                        null,
                        SKShaderTileMode.Clamp);
                    canvas.DrawCircle(center, center, radius, ringPaint);
                }

                // Inner face
                using (var facePaint = new SKPaint())
                {
                    facePaint.IsAntialias = true;
                    facePaint.Color = new SKColor(25, 28, 32, 255);
                    canvas.DrawCircle(center, center, radius * 0.85f, facePaint);
                }

                // Compass rose markings
                using (var markPaint = new SKPaint())
                {
                    markPaint.IsAntialias = true;
                    markPaint.Color = ToSKColor(MetalHighlight);
                    markPaint.StrokeWidth = 2;
                    markPaint.Style = SKPaintStyle.Stroke;

                    // Cardinal directions
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * 90 * (float)Math.PI / 180;
                        float x1 = center + (float)Math.Cos(angle) * radius * 0.6f;
                        float y1 = center + (float)Math.Sin(angle) * radius * 0.6f;
                        float x2 = center + (float)Math.Cos(angle) * radius * 0.8f;
                        float y2 = center + (float)Math.Sin(angle) * radius * 0.8f;
                        canvas.DrawLine(x1, y1, x2, y2, markPaint);
                    }

                    // Ordinal directions
                    markPaint.StrokeWidth = 1;
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = (i * 90 + 45) * (float)Math.PI / 180;
                        float x1 = center + (float)Math.Cos(angle) * radius * 0.65f;
                        float y1 = center + (float)Math.Sin(angle) * radius * 0.65f;
                        float x2 = center + (float)Math.Cos(angle) * radius * 0.75f;
                        float y2 = center + (float)Math.Sin(angle) * radius * 0.75f;
                        canvas.DrawLine(x1, y1, x2, y2, markPaint);
                    }
                }

                // Center pivot
                using (var pivotPaint = new SKPaint())
                {
                    pivotPaint.IsAntialias = true;
                    pivotPaint.Color = ToSKColor(MetalLight);
                    canvas.DrawCircle(center, center, 6, pivotPaint);
                    pivotPaint.Color = ToSKColor(MetalDark);
                    canvas.DrawCircle(center, center, 3, pivotPaint);
                }

                // Worn scratches
                AddWornEffect(canvas, size, size, 0.2f);

                // Outer border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 3;
                    borderPaint.Color = ToSKColor(MetalLight);
                    canvas.DrawCircle(center, center, radius, borderPaint);
                }

                SaveSurface(surface, "Compass/Compass_Body.png");
            }

            // Compass needle (separate for rotation)
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float center = size / 2f;
                float needleLength = size * 0.35f;

                using (var needlePaint = new SKPaint())
                {
                    needlePaint.IsAntialias = true;

                    // North pointer (red/warm - points to daughter)
                    var northPath = new SKPath();
                    northPath.MoveTo(center, center - needleLength);
                    northPath.LineTo(center - 6, center);
                    northPath.LineTo(center + 6, center);
                    northPath.Close();

                    needlePaint.Color = ToSKColor(DangerRed);
                    needlePaint.Style = SKPaintStyle.Fill;
                    canvas.DrawPath(northPath, needlePaint);

                    // South pointer (silver)
                    var southPath = new SKPath();
                    southPath.MoveTo(center, center + needleLength);
                    southPath.LineTo(center - 6, center);
                    southPath.LineTo(center + 6, center);
                    southPath.Close();

                    needlePaint.Color = ToSKColor(MetalLight);
                    canvas.DrawPath(southPath, needlePaint);

                    // Center circle
                    needlePaint.Color = ToSKColor(MetalHighlight);
                    canvas.DrawCircle(center, center, 4, needlePaint);
                }

                SaveSurface(surface, "Compass/Compass_Needle.png");
            }

            // Compass glass overlay
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float center = size / 2f;
                float radius = size / 2f - 8;

                // Glass reflection effect
                using (var glassPaint = new SKPaint())
                {
                    glassPaint.IsAntialias = true;
                    glassPaint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint(center - radius * 0.3f, center - radius * 0.3f),
                        radius * 1.5f,
                        new SKColor[] {
                            new SKColor(255, 255, 255, 40),
                            new SKColor(255, 255, 255, 0)
                        },
                        null,
                        SKShaderTileMode.Clamp);
                    canvas.DrawCircle(center, center, radius * 0.8f, glassPaint);
                }

                SaveSurface(surface, "Compass/Compass_Glass.png");
            }
        }
#endif

        private void GenerateCompassFallback(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            float center = size / 2f;
            float radius = size / 2f - 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));

                    if (dist < radius)
                    {
                        if (dist > radius * 0.85f)
                        {
                            tex.SetPixel(x, y, MetalMid);
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color32(25, 28, 32, 255));
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, "Compass/Compass_Body.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Squad Frame Generation

        private void GenerateSquadFrames()
        {
            EnsureOutputDirectory();

            int frameSize = 80;

#if NUGET_SKIASHARP_INSTALLED
            GenerateSquadFrameSkia("SquadFrame_Normal", frameSize, MetalMid, false, false);
            GenerateSquadFrameSkia("SquadFrame_Selected", frameSize, AccentCyan, true, false);
            GenerateSquadFrameSkia("SquadFrame_Wounded", frameSize, WarningAmber, false, true);
            GenerateSquadFrameSkia("SquadFrame_Critical", frameSize, DangerRed, false, true);
            GenerateSquadFrameSkia("SquadFrame_Dead", frameSize, new Color32(60, 60, 60, 255), false, false);
#else
            GenerateSquadFrameFallback("SquadFrame_Normal", frameSize, MetalMid);
            GenerateSquadFrameFallback("SquadFrame_Selected", frameSize, AccentCyan);
            GenerateSquadFrameFallback("SquadFrame_Wounded", frameSize, WarningAmber);
            GenerateSquadFrameFallback("SquadFrame_Critical", frameSize, DangerRed);
            GenerateSquadFrameFallback("SquadFrame_Dead", frameSize, new Color32(60, 60, 60, 255));
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Squad Frames generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateSquadFrameSkia(string name, int size, Color32 borderColor, bool glowing, bool pulsing)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                var rect = new SKRect(3, 3, size - 3, size - 3);
                float cornerRadius = 6;

                // Background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = new SKColor(20, 23, 26, 180);
                    canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, bgPaint);
                }

                // Glow effect
                if (glowing)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.IsAntialias = true;
                        glowPaint.Style = SKPaintStyle.Stroke;
                        glowPaint.StrokeWidth = 4;
                        glowPaint.Color = new SKColor(borderColor.r, borderColor.g, borderColor.b, 100);
                        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 5);
                        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, glowPaint);
                    }
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 3;
                    borderPaint.Color = ToSKColor(borderColor);
                    canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);
                }

                // Corner accents
                using (var accentPaint = new SKPaint())
                {
                    accentPaint.IsAntialias = true;
                    accentPaint.Color = ToSKColor(borderColor);
                    accentPaint.StrokeWidth = 2;
                    accentPaint.Style = SKPaintStyle.Stroke;

                    int cornerLen = 12;
                    int offset = 6;

                    // Top-left
                    canvas.DrawLine(offset, offset, offset, offset + cornerLen, accentPaint);
                    canvas.DrawLine(offset, offset, offset + cornerLen, offset, accentPaint);

                    // Top-right
                    canvas.DrawLine(size - offset, offset, size - offset, offset + cornerLen, accentPaint);
                    canvas.DrawLine(size - offset, offset, size - offset - cornerLen, offset, accentPaint);

                    // Bottom-left
                    canvas.DrawLine(offset, size - offset, offset, size - offset - cornerLen, accentPaint);
                    canvas.DrawLine(offset, size - offset, offset + cornerLen, size - offset, accentPaint);

                    // Bottom-right
                    canvas.DrawLine(size - offset, size - offset, size - offset, size - offset - cornerLen, accentPaint);
                    canvas.DrawLine(size - offset, size - offset, size - offset - cornerLen, size - offset, accentPaint);
                }

                // Status indicator dot (bottom right)
                using (var dotPaint = new SKPaint())
                {
                    dotPaint.IsAntialias = true;
                    dotPaint.Color = ToSKColor(borderColor);
                    canvas.DrawCircle(size - 12, size - 12, 5, dotPaint);
                }

                SaveSurface(surface, $"SquadFrames/{name}.png");
            }
        }
#endif

        private void GenerateSquadFrameFallback(string name, int size, Color32 borderColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 3 || x >= size - 3 || y < 3 || y >= size - 3)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(20, 23, 26, 180));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, $"SquadFrames/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Warning Indicator Generation

        private void GenerateWarningIndicators()
        {
            EnsureOutputDirectory();

            int indicatorSize = 48;

#if NUGET_SKIASHARP_INSTALLED
            GenerateWarningIndicatorSkia("Warning_Noise", indicatorSize, WarningAmber, "noise");
            GenerateWarningIndicatorSkia("Warning_Detected", indicatorSize, DangerRed, "eye");
            GenerateWarningIndicatorSkia("Warning_LowAmmo", indicatorSize, WarningAmber, "ammo");
            GenerateWarningIndicatorSkia("Warning_LowHealth", indicatorSize, DangerRed, "health");
            GenerateWarningIndicatorSkia("Warning_Extraction", indicatorSize, InfoBlue, "evac");
#else
            GenerateWarningIndicatorFallback("Warning_Noise", indicatorSize, WarningAmber);
            GenerateWarningIndicatorFallback("Warning_Detected", indicatorSize, DangerRed);
            GenerateWarningIndicatorFallback("Warning_LowAmmo", indicatorSize, WarningAmber);
            GenerateWarningIndicatorFallback("Warning_LowHealth", indicatorSize, DangerRed);
            GenerateWarningIndicatorFallback("Warning_Extraction", indicatorSize, InfoBlue);
#endif

            AssetDatabase.Refresh();
            Debug.Log("[UI Generator] Warning Indicators generated");
        }

#if NUGET_SKIASHARP_INSTALLED
        private void GenerateWarningIndicatorSkia(string name, int size, Color32 color, string iconType)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                float center = size / 2f;

                // Warning triangle background
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.IsAntialias = true;
                    bgPaint.Color = new SKColor(color.r, color.g, color.b, 40);

                    var path = new SKPath();
                    path.MoveTo(center, 4);
                    path.LineTo(size - 4, size - 4);
                    path.LineTo(4, size - 4);
                    path.Close();
                    canvas.DrawPath(path, bgPaint);
                }

                // Border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.IsAntialias = true;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Color = ToSKColor(color);

                    var path = new SKPath();
                    path.MoveTo(center, 4);
                    path.LineTo(size - 4, size - 4);
                    path.LineTo(4, size - 4);
                    path.Close();
                    canvas.DrawPath(path, borderPaint);
                }

                // Icon
                using (var iconPaint = new SKPaint())
                {
                    iconPaint.IsAntialias = true;
                    iconPaint.Color = ToSKColor(color);
                    iconPaint.Style = SKPaintStyle.Fill;

                    // Exclamation mark
                    canvas.DrawRect(new SKRect(center - 2, center - 8, center + 2, center + 2), iconPaint);
                    canvas.DrawCircle(center, center + 8, 3, iconPaint);
                }

                SaveSurface(surface, $"Warnings/{name}.png");
            }
        }
#endif

        private void GenerateWarningIndicatorFallback(string name, int size, Color32 color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple triangle approximation
                    float normalizedY = (float)y / size;
                    float halfWidth = normalizedY * center;

                    if (x >= center - halfWidth && x <= center + halfWidth && y > 4)
                    {
                        tex.SetPixel(x, y, new Color32(color.r, color.g, color.b, 100));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    }
                }
            }

            tex.Apply();
            SaveTexture(tex, $"Warnings/{name}.png");
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Utility Methods

#if NUGET_SKIASHARP_INSTALLED
        private SKColor ToSKColor(Color32 color)
        {
            return new SKColor(color.r, color.g, color.b, color.a);
        }

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

        private static byte ClampByte(float value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }

        #endregion
    }
}
