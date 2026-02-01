using UnityEngine;
using UnityEditor;
using EdgeOfUniverse.VFX;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Generates VFX prefabs for the RTS game.
    /// </summary>
    public class VFXPrefabGenerator : EditorWindow
    {
        private string outputPath = "Assets/VFX/Prefabs";

        [MenuItem("Tools/Edge of Universe/Generate VFX Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<VFXPrefabGenerator>("VFX Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("VFX Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Label("Investor-Grade Visual Effects", EditorStyles.miniLabel);
            GUILayout.Space(10);

            outputPath = EditorGUILayout.TextField("Output Path", outputPath);

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will generate particle effect prefabs:\n\n" +
                "CORE EFFECTS:\n" +
                "• Move Command (Holographic waypoint)\n" +
                "• Selection Burst (Tactical acknowledge)\n" +
                "• Dust Trail (Movement feedback)\n" +
                "\nCOMBAT - MUZZLE FLASHES:\n" +
                "• Rifle Muzzle Flash\n" +
                "• Shotgun Muzzle Flash\n" +
                "• Heavy Weapon Muzzle Flash\n" +
                "\nCOMBAT - IMPACTS:\n" +
                "• Metal Impact (Sparks)\n" +
                "• Rock Impact (Dust & chips)\n" +
                "• Organic Impact (Ichor)\n" +
                "• Dirt Impact (Dust cloud)\n" +
                "\nEXPLOSIONS:\n" +
                "• Explosion (Multi-layer)",
                MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate All VFX Prefabs", GUILayout.Height(40)))
            {
                GenerateAllPrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup VFX Manager in Scene"))
            {
                SetupVFXManager();
            }
        }

        private void GenerateAllPrefabs()
        {
            EnsureDirectory();

            // Core effects
            CreateMoveCommandPrefab();
            CreateSelectionBurstPrefab();
            CreateDustTrailPrefab();

            // Muzzle flash variations
            CreateRifleMuzzleFlash();
            CreateShotgunMuzzleFlash();
            CreateHeavyMuzzleFlash();

            // Surface-specific impacts
            CreateMetalImpact();
            CreateRockImpact();
            CreateOrganicImpact();
            CreateDirtImpact();

            // Explosion
            CreateExplosionPrefab();

            AssetDatabase.Refresh();
            Debug.Log("[VFX Generator] All investor-grade VFX prefabs created!");
        }

        private void EnsureDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/VFX"))
                AssetDatabase.CreateFolder("Assets", "VFX");
            if (!AssetDatabase.IsValidFolder(outputPath))
                AssetDatabase.CreateFolder("Assets/VFX", "Prefabs");
        }

        #region Move Command

        private void CreateMoveCommandPrefab()
        {
            GameObject root = new GameObject("VFX_MoveCommand");

            // Ring particle
            GameObject ring = CreateParticleChild(root, "Ring");
            var ringPS = ring.GetComponent<ParticleSystem>();
            var ringMain = ringPS.main;
            ringMain.startLifetime = 1f;
            ringMain.startSpeed = 0f;
            ringMain.startSize = 2f;
            ringMain.startColor = new Color(0.3f, 0.9f, 0.4f, 0.8f);
            ringMain.maxParticles = 1;
            ringMain.loop = false;

            var ringEmission = ringPS.emission;
            ringEmission.rateOverTime = 0;
            ringEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var ringSize = ringPS.sizeOverLifetime;
            ringSize.enabled = true;
            ringSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));

            var ringColor = ringPS.colorOverLifetime;
            ringColor.enabled = true;
            Gradient ringGrad = new Gradient();
            ringGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 0f), new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            ringColor.color = ringGrad;

            var ringRenderer = ring.GetComponent<ParticleSystemRenderer>();
            ringRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            ringRenderer.material = CreateParticleMaterial(new Color(0.3f, 0.9f, 0.4f));

            // Vertical beam
            GameObject beam = CreateParticleChild(root, "Beam");
            var beamPS = beam.GetComponent<ParticleSystem>();
            var beamMain = beamPS.main;
            beamMain.startLifetime = 0.5f;
            beamMain.startSpeed = 8f;
            beamMain.startSize = 0.3f;
            beamMain.startColor = new Color(0.4f, 1f, 0.5f, 0.6f);
            beamMain.maxParticles = 20;
            beamMain.loop = false;
            beamMain.gravityModifier = 0f;

            var beamShape = beamPS.shape;
            beamShape.shapeType = ParticleSystemShapeType.Cone;
            beamShape.angle = 5f;
            beamShape.radius = 0.1f;
            beamShape.rotation = new Vector3(-90, 0, 0);

            var beamEmission = beamPS.emission;
            beamEmission.rateOverTime = 0;
            beamEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var beamColor = beamPS.colorOverLifetime;
            beamColor.enabled = true;
            Gradient beamGrad = new Gradient();
            beamGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 1f, 0.5f), 0f), new GradientColorKey(new Color(0.2f, 0.8f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            beamColor.color = beamGrad;

            var beamRenderer = beam.GetComponent<ParticleSystemRenderer>();
            beamRenderer.material = CreateParticleMaterial(new Color(0.4f, 1f, 0.5f));

            SavePrefab(root, "VFX_MoveCommand");
        }

        #endregion

        #region Selection Burst

        private void CreateSelectionBurstPrefab()
        {
            GameObject root = new GameObject("VFX_SelectionBurst");

            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.3f, 0.8f, 1f, 0.8f);
            main.maxParticles = 30;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0f), new GradientColorKey(new Color(0.2f, 0.6f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.3f, 0.8f, 1f));

            SavePrefab(root, "VFX_SelectionBurst");
        }

        #endregion

        #region Dust Trail

        private void CreateDustTrailPrefab()
        {
            GameObject root = new GameObject("VFX_DustTrail");

            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new Color(0.6f, 0.55f, 0.45f, 0.4f);
            main.maxParticles = 50;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90, 0, 0);

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 0f), new GradientColorKey(new Color(0.5f, 0.45f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateSoftParticleMaterial(new Color(0.6f, 0.55f, 0.45f));

            SavePrefab(root, "VFX_DustTrail");
        }

        #endregion

        #region Muzzle Flash Variations

        private void CreateRifleMuzzleFlash()
        {
            GameObject root = new GameObject("VFX_MuzzleFlash_Rifle");

            // Core flash
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.05f;
            main.startSpeed = 0f;
            main.startSize = 0.6f;
            main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            main.maxParticles = 1;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.85f, 0.4f));

            // Light smoke puff
            GameObject smoke = CreateParticleChild(root, "Smoke");
            var smokePS = smoke.GetComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = 0.3f;
            smokeMain.startSpeed = 2f;
            smokeMain.startSize = 0.2f;
            smokeMain.startColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);
            smokeMain.maxParticles = 5;
            smokeMain.loop = false;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 10f;
            smokeShape.radius = 0.05f;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 0;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var smokeSize = smokePS.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));

            var smokeColor = smokePS.colorOverLifetime;
            smokeColor.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smokeColor.color = smokeGrad;

            var smokeRenderer = smoke.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = CreateSoftParticleMaterial(Color.gray);

            // Sparks
            GameObject sparks = CreateParticleChild(root, "Sparks");
            var sparksPS = sparks.GetComponent<ParticleSystem>();
            var sparksMain = sparksPS.main;
            sparksMain.startLifetime = 0.1f;
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 15f);
            sparksMain.startSize = 0.08f;
            sparksMain.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            sparksMain.maxParticles = 5;
            sparksMain.loop = false;
            sparksMain.gravityModifier = 1f;

            var sparksShape = sparksPS.shape;
            sparksShape.shapeType = ParticleSystemShapeType.Cone;
            sparksShape.angle = 12f;
            sparksShape.radius = 0.03f;

            var sparksEmission = sparksPS.emission;
            sparksEmission.rateOverTime = 0;
            sparksEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4) });

            var sparksRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.material = CreateAdditiveMaterial(new Color(1f, 0.7f, 0.2f));

            SavePrefab(root, "VFX_MuzzleFlash_Rifle");
        }

        private void CreateShotgunMuzzleFlash()
        {
            GameObject root = new GameObject("VFX_MuzzleFlash_Shotgun");

            // Wide flash
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.07f;
            main.startSpeed = 0f;
            main.startSize = 1.2f;
            main.startColor = new Color(1f, 0.75f, 0.3f, 1f);
            main.maxParticles = 1;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.75f, 0.3f));

            // Heavy smoke
            GameObject smoke = CreateParticleChild(root, "Smoke");
            var smokePS = smoke.GetComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = 0.5f;
            smokeMain.startSpeed = 3f;
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            smokeMain.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            smokeMain.maxParticles = 15;
            smokeMain.loop = false;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 45f;  // Wide spread
            smokeShape.radius = 0.1f;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 0;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            var smokeSize = smokePS.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));

            var smokeColor = smokePS.colorOverLifetime;
            smokeColor.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smokeColor.color = smokeGrad;

            var smokeRenderer = smoke.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = CreateSoftParticleMaterial(Color.gray);

            // Many sparks (wide spread)
            GameObject sparks = CreateParticleChild(root, "Sparks");
            var sparksPS = sparks.GetComponent<ParticleSystem>();
            var sparksMain = sparksPS.main;
            sparksMain.startLifetime = 0.15f;
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 18f);
            sparksMain.startSize = 0.1f;
            sparksMain.startColor = new Color(1f, 0.6f, 0.1f, 1f);
            sparksMain.maxParticles = 15;
            sparksMain.loop = false;
            sparksMain.gravityModifier = 2f;

            var sparksShape = sparksPS.shape;
            sparksShape.shapeType = ParticleSystemShapeType.Cone;
            sparksShape.angle = 35f;  // Wide
            sparksShape.radius = 0.08f;

            var sparksEmission = sparksPS.emission;
            sparksEmission.rateOverTime = 0;
            sparksEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            var sparksRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.material = CreateAdditiveMaterial(new Color(1f, 0.6f, 0.1f));

            SavePrefab(root, "VFX_MuzzleFlash_Shotgun");
        }

        private void CreateHeavyMuzzleFlash()
        {
            GameObject root = new GameObject("VFX_MuzzleFlash_Heavy");

            // Massive flash
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = 1.8f;
            main.startColor = new Color(1f, 0.9f, 0.5f, 1f);
            main.maxParticles = 1;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.8f, 1, 1.3f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.9f, 0.5f));

            // Lingering smoke
            GameObject smoke = CreateParticleChild(root, "Smoke");
            var smokePS = smoke.GetComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = 1f;
            smokeMain.startSpeed = 2f;
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            smokeMain.startColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            smokeMain.maxParticles = 20;
            smokeMain.loop = false;
            smokeMain.gravityModifier = -0.1f;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 20f;
            smokeShape.radius = 0.15f;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 0;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var smokeSize = smokePS.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2.5f));

            var smokeColor = smokePS.colorOverLifetime;
            smokeColor.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smokeColor.color = smokeGrad;

            var smokeRenderer = smoke.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = CreateSoftParticleMaterial(Color.gray);

            // Heavy sparks with trails
            GameObject sparks = CreateParticleChild(root, "Sparks");
            var sparksPS = sparks.GetComponent<ParticleSystem>();
            var sparksMain = sparksPS.main;
            sparksMain.startLifetime = 0.25f;
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 25f);
            sparksMain.startSize = 0.12f;
            sparksMain.startColor = new Color(1f, 0.8f, 0.3f, 1f);
            sparksMain.maxParticles = 20;
            sparksMain.loop = false;
            sparksMain.gravityModifier = 3f;

            var sparksShape = sparksPS.shape;
            sparksShape.shapeType = ParticleSystemShapeType.Cone;
            sparksShape.angle = 18f;
            sparksShape.radius = 0.1f;

            var sparksEmission = sparksPS.emission;
            sparksEmission.rateOverTime = 0;
            sparksEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var sparksTrails = sparksPS.trails;
            sparksTrails.enabled = true;
            sparksTrails.lifetime = 0.15f;
            sparksTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var sparksRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.material = CreateAdditiveMaterial(new Color(1f, 0.8f, 0.3f));
            sparksRenderer.trailMaterial = CreateAdditiveMaterial(new Color(1f, 0.5f, 0.1f));

            // Ground dust kick
            GameObject dust = CreateParticleChild(root, "GroundDust");
            dust.transform.localPosition = new Vector3(0, -0.5f, 0);
            var dustPS = dust.GetComponent<ParticleSystem>();
            var dustMain = dustPS.main;
            dustMain.startLifetime = 0.8f;
            dustMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            dustMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            dustMain.startColor = new Color(0.6f, 0.55f, 0.45f, 0.4f);
            dustMain.maxParticles = 10;
            dustMain.loop = false;

            var dustShape = dustPS.shape;
            dustShape.shapeType = ParticleSystemShapeType.Circle;
            dustShape.radius = 0.5f;
            dustShape.rotation = new Vector3(-90, 0, 0);

            var dustEmission = dustPS.emission;
            dustEmission.rateOverTime = 0;
            dustEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var dustSize = dustPS.sizeOverLifetime;
            dustSize.enabled = true;
            dustSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));

            var dustColor = dustPS.colorOverLifetime;
            dustColor.enabled = true;
            Gradient dustGrad = new Gradient();
            dustGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 0f), new GradientColorKey(new Color(0.5f, 0.45f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            dustColor.color = dustGrad;

            var dustRenderer = dust.GetComponent<ParticleSystemRenderer>();
            dustRenderer.material = CreateSoftParticleMaterial(new Color(0.6f, 0.55f, 0.45f));

            SavePrefab(root, "VFX_MuzzleFlash_Heavy");
        }

        // Legacy - keep for backwards compatibility
        private void CreateMuzzleFlashPrefab()
        {
            CreateRifleMuzzleFlash();
        }

        #endregion

        #region Surface-Specific Impacts

        private void CreateMetalImpact()
        {
            GameObject root = new GameObject("VFX_Impact_Metal");

            // Bright orange sparks with bounce
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize = 0.08f;
            main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            main.maxParticles = 20;
            main.loop = false;
            main.gravityModifier = 2f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            // Bounce/collision for realistic metal sparks
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.bounce = 0.6f;
            collision.lifetimeLoss = 0.2f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.1f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.7f, 0.2f));
            renderer.trailMaterial = CreateAdditiveMaterial(new Color(1f, 0.5f, 0.1f));

            SavePrefab(root, "VFX_Impact_Metal");
        }

        private void CreateRockImpact()
        {
            GameObject root = new GameObject("VFX_Impact_Rock");

            // Dust puff
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(0.6f, 0.55f, 0.5f, 0.6f);
            main.maxParticles = 15;
            main.loop = false;
            main.gravityModifier = 0.3f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 0f), new GradientColorKey(new Color(0.5f, 0.45f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateSoftParticleMaterial(new Color(0.6f, 0.55f, 0.5f));

            // Small debris chips
            GameObject chips = CreateParticleChild(root, "Chips");
            var chipsPS = chips.GetComponent<ParticleSystem>();
            var chipsMain = chipsPS.main;
            chipsMain.startLifetime = 0.5f;
            chipsMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            chipsMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            chipsMain.startColor = new Color(0.5f, 0.45f, 0.4f, 1f);
            chipsMain.maxParticles = 8;
            chipsMain.loop = false;
            chipsMain.gravityModifier = 4f;

            var chipsShape = chipsPS.shape;
            chipsShape.shapeType = ParticleSystemShapeType.Hemisphere;
            chipsShape.radius = 0.05f;

            var chipsEmission = chipsPS.emission;
            chipsEmission.rateOverTime = 0;
            chipsEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

            var chipsRenderer = chips.GetComponent<ParticleSystemRenderer>();
            chipsRenderer.material = CreateParticleMaterial(new Color(0.5f, 0.45f, 0.4f));

            // Few sparks
            GameObject sparks = CreateParticleChild(root, "Sparks");
            var sparksPS = sparks.GetComponent<ParticleSystem>();
            var sparksMain = sparksPS.main;
            sparksMain.startLifetime = 0.2f;
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            sparksMain.startSize = 0.05f;
            sparksMain.startColor = new Color(1f, 0.7f, 0.3f, 0.8f);
            sparksMain.maxParticles = 5;
            sparksMain.loop = false;
            sparksMain.gravityModifier = 2f;

            var sparksShape = sparksPS.shape;
            sparksShape.shapeType = ParticleSystemShapeType.Hemisphere;
            sparksShape.radius = 0.03f;

            var sparksEmission = sparksPS.emission;
            sparksEmission.rateOverTime = 0;
            sparksEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var sparksRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.material = CreateAdditiveMaterial(new Color(1f, 0.7f, 0.3f));

            SavePrefab(root, "VFX_Impact_Rock");
        }

        private void CreateOrganicImpact()
        {
            GameObject root = new GameObject("VFX_Impact_Organic");

            // Dark ichor spray - disturbing but not gratuitous
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(0.15f, 0.2f, 0.1f, 0.9f);  // Dark green-black
            main.maxParticles = 20;
            main.loop = false;
            main.gravityModifier = 2f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.08f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.15f, 0.2f, 0.1f), 0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.15f, 0.2f, 0.1f));

            // Wet splatter droplets
            GameObject splatter = CreateParticleChild(root, "Splatter");
            var splatterPS = splatter.GetComponent<ParticleSystem>();
            var splatterMain = splatterPS.main;
            splatterMain.startLifetime = 0.3f;
            splatterMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            splatterMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            splatterMain.startColor = new Color(0.2f, 0.25f, 0.15f, 1f);
            splatterMain.maxParticles = 12;
            splatterMain.loop = false;
            splatterMain.gravityModifier = 3f;

            var splatterShape = splatterPS.shape;
            splatterShape.shapeType = ParticleSystemShapeType.Hemisphere;
            splatterShape.radius = 0.05f;

            var splatterEmission = splatterPS.emission;
            splatterEmission.rateOverTime = 0;
            splatterEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var splatterRenderer = splatter.GetComponent<ParticleSystemRenderer>();
            splatterRenderer.material = CreateParticleMaterial(new Color(0.2f, 0.25f, 0.15f));

            SavePrefab(root, "VFX_Impact_Organic");
        }

        private void CreateDirtImpact()
        {
            GameObject root = new GameObject("VFX_Impact_Dirt");

            // Large dust cloud
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new Color(0.55f, 0.45f, 0.35f, 0.5f);
            main.maxParticles = 20;
            main.loop = false;
            main.gravityModifier = -0.1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.4f, 1, 2f));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.55f, 0.45f, 0.35f), 0f), new GradientColorKey(new Color(0.45f, 0.38f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateSoftParticleMaterial(new Color(0.55f, 0.45f, 0.35f));

            // Dirt clumps flying out
            GameObject clumps = CreateParticleChild(root, "Clumps");
            var clumpsPS = clumps.GetComponent<ParticleSystem>();
            var clumpsMain = clumpsPS.main;
            clumpsMain.startLifetime = 0.6f;
            clumpsMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            clumpsMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            clumpsMain.startColor = new Color(0.4f, 0.32f, 0.25f, 1f);
            clumpsMain.maxParticles = 15;
            clumpsMain.loop = false;
            clumpsMain.gravityModifier = 4f;

            var clumpsShape = clumpsPS.shape;
            clumpsShape.shapeType = ParticleSystemShapeType.Hemisphere;
            clumpsShape.radius = 0.08f;

            var clumpsEmission = clumpsPS.emission;
            clumpsEmission.rateOverTime = 0;
            clumpsEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            var clumpsRenderer = clumps.GetComponent<ParticleSystemRenderer>();
            clumpsRenderer.material = CreateParticleMaterial(new Color(0.4f, 0.32f, 0.25f));

            SavePrefab(root, "VFX_Impact_Dirt");
        }

        // Legacy - keep for backwards compatibility
        private void CreateBulletImpactPrefab()
        {
            CreateMetalImpact();
        }

        #endregion

        #region Explosion

        private void CreateExplosionPrefab()
        {
            GameObject root = new GameObject("VFX_Explosion");

            // Core flash
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize = 3f;
            main.startColor = new Color(1f, 0.9f, 0.5f, 1f);
            main.maxParticles = 1;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.5f));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f), new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.8f, 0.3f));

            // Fire particles
            GameObject fire = CreateParticleChild(root, "Fire");
            var firePS = fire.GetComponent<ParticleSystem>();
            var fireMain = firePS.main;
            fireMain.startLifetime = 0.8f;
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            fireMain.startColor = new Color(1f, 0.6f, 0.2f, 0.8f);
            fireMain.maxParticles = 30;
            fireMain.loop = false;
            fireMain.gravityModifier = -0.5f;

            var fireShape = firePS.shape;
            fireShape.shapeType = ParticleSystemShapeType.Sphere;
            fireShape.radius = 0.5f;

            var fireEmission = firePS.emission;
            fireEmission.rateOverTime = 0;
            fireEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var fireColor = firePS.colorOverLifetime;
            fireColor.enabled = true;
            Gradient fireGrad = new Gradient();
            fireGrad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.2f, 0.05f, 0f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            fireColor.color = fireGrad;

            var fireRenderer = fire.GetComponent<ParticleSystemRenderer>();
            fireRenderer.material = CreateAdditiveMaterial(new Color(1f, 0.6f, 0.2f));

            // Smoke
            GameObject smoke = CreateParticleChild(root, "Smoke");
            var smokePS = smoke.GetComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = 2f;
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
            smokeMain.startColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            smokeMain.maxParticles = 20;
            smokeMain.loop = false;
            smokeMain.gravityModifier = -0.2f;
            smokeMain.startDelay = 0.1f;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.8f;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 0;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var smokeSize = smokePS.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));

            var smokeColor = smokePS.colorOverLifetime;
            smokeColor.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.25f, 0.2f), 0f), new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smokeColor.color = smokeGrad;

            var smokeRenderer = smoke.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = CreateSoftParticleMaterial(new Color(0.2f, 0.2f, 0.2f));

            // Debris
            GameObject debris = CreateParticleChild(root, "Debris");
            var debrisPS = debris.GetComponent<ParticleSystem>();
            var debrisMain = debrisPS.main;
            debrisMain.startLifetime = 1.5f;
            debrisMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            debrisMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            debrisMain.startColor = new Color(0.4f, 0.35f, 0.3f, 1f);
            debrisMain.maxParticles = 30;
            debrisMain.loop = false;
            debrisMain.gravityModifier = 2f;

            var debrisShape = debrisPS.shape;
            debrisShape.shapeType = ParticleSystemShapeType.Sphere;
            debrisShape.radius = 0.3f;

            var debrisEmission = debrisPS.emission;
            debrisEmission.rateOverTime = 0;
            debrisEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var debrisRenderer = debris.GetComponent<ParticleSystemRenderer>();
            debrisRenderer.material = CreateParticleMaterial(new Color(0.4f, 0.35f, 0.3f));

            SavePrefab(root, "VFX_Explosion");
        }

        #endregion

        #region Helpers

        private GameObject CreateParticleChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.AddComponent<ParticleSystem>();
            return child;
        }

        private Material CreateParticleMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            return mat;
        }

        private Material CreateAdditiveMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1); // Additive
            return mat;
        }

        private Material CreateSoftParticleMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SoftParticlesEnabled", 1);
            mat.SetFloat("_SoftParticlesNearFadeDistance", 0.5f);
            mat.SetFloat("_SoftParticlesFarFadeDistance", 1f);
            return mat;
        }

        private void SavePrefab(GameObject obj, string name)
        {
            string path = $"{outputPath}/{name}.prefab";

            // Save materials
            var renderers = obj.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (var r in renderers)
            {
                if (r.material != null)
                {
                    string matPath = $"{outputPath}/{name}_{r.gameObject.name}_Mat.mat";
                    AssetDatabase.CreateAsset(r.material, matPath);
                }
                if (r.trailMaterial != null)
                {
                    string matPath = $"{outputPath}/{name}_{r.gameObject.name}_TrailMat.mat";
                    AssetDatabase.CreateAsset(r.trailMaterial, matPath);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);
            Debug.Log($"[VFX Generator] Created: {name}");
        }

        private void SetupVFXManager()
        {
            // Find or create VFX Manager
            VFXManager manager = FindAnyObjectByType<VFXManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("VFXManager");
                manager = managerObj.AddComponent<VFXManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create VFX Manager");
            }

            // Assign prefabs
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("moveCommandPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_MoveCommand.prefab");
            so.FindProperty("selectionBurstPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_SelectionBurst.prefab");
            so.FindProperty("dustTrailPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_DustTrail.prefab");
            so.FindProperty("muzzleFlashPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_MuzzleFlash.prefab");
            so.FindProperty("bulletImpactPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_BulletImpact.prefab");
            so.FindProperty("explosionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{outputPath}/VFX_Explosion.prefab");
            so.ApplyModifiedProperties();

            Selection.activeGameObject = manager.gameObject;
            Debug.Log("[VFX Generator] VFX Manager setup complete!");
        }

        #endregion
    }
}
