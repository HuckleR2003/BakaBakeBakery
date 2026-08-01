using System;
using System.Collections.Generic;
using System.IO;
using BakaBakeBakery.CameraSystem;
using BakaBakeBakery.Core;
using BakaBakeBakery.Data;
using BakaBakeBakery.Gameplay;
using BakaBakeBakery.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BakaBakeBakery.Editor
{
    public static class VisualFoundationBuilder
    {
        private const string Root = "Assets/_BakaBakeBakery";
        private const string MaterialRoot = Root + "/Art/Materials";
        private const string DataRoot = Root + "/Data";
        private const string StudioIntroScenePath = Root + "/Scenes/StudioIntro.unity";
        private const string MainMenuScenePath = Root + "/Scenes/MainMenu.unity";
        private const string MainBakeryScenePath = Root + "/Scenes/MainBakery.unity";
        private const string UxmlPath = Root + "/UI/MainBakery.uxml";
        private const string UssPath = Root + "/UI/MainBakery.uss";
        private const string StudioIntroUxmlPath = Root + "/UI/StudioIntro.uxml";
        private const string StudioIntroUssPath = Root + "/UI/StudioIntro.uss";
        private const string MainMenuUxmlPath = Root + "/UI/MainMenu.uxml";
        private const string MainMenuUssPath = Root + "/UI/MainMenu.uss";
        private const string PanelSettingsPath = Root + "/UI/BakeryPanelSettings.asset";
        private const string ScreenshotRelativePath = "Docs/Concepts/04-unity-visual-foundation.png";

        private sealed class Materials
        {
            public Material Flour;
            public Material Paper;
            public Material Crust;
            public Material Cocoa;
            public Material Sage;
            public Material Cherry;
            public Material Glow;
            public Material EveningBlue;
            public Material Wood;
            public Material Metal;
            public Material Stone;
            public Material Hair;
            public Material Skin;
            public Material Cloth;
            public Material White;
        }

        [MenuItem("Baka Bake Bakery/Rebuild Visual Foundation")]
        public static void BuildAll()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EnsureProjectFolders();

                var materials = CreateMaterials();
                CreateRecipeData();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = BuildScene(materials);
                BuildHud();
                EditorSceneManager.SaveScene(scene, MainBakeryScenePath);
                CaptureScene(camera);

                BuildStudioIntroScene();
                BuildMainMenuScene();
                ConfigurePlayerSettings();

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(StudioIntroScenePath, true),
                    new EditorBuildSettingsScene(MainMenuScenePath, true),
                    new EditorBuildSettingsScene(MainBakeryScenePath, true)
                };

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Baka Bake Bakery] Visual Foundation rebuilt successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Art");
            EnsureFolder(MaterialRoot);
            EnsureFolder(DataRoot);
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/UI");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Materials CreateMaterials()
        {
            return new Materials
            {
                Flour = GetOrCreateMaterial("M_FlourCream", Hex("F4E5C6"), 0f, 0.24f),
                Paper = GetOrCreateMaterial("M_Paper", Hex("FAEFD8"), 0f, 0.12f),
                Crust = GetOrCreateMaterial("M_BreadCrust", Hex("C8753D"), 0f, 0.32f),
                Cocoa = GetOrCreateMaterial("M_Cocoa", Hex("382824"), 0f, 0.22f),
                Sage = GetOrCreateMaterial("M_Sage", Hex("71816B"), 0.02f, 0.28f),
                Cherry = GetOrCreateMaterial("M_SourCherry", Hex("A84D46"), 0f, 0.3f),
                Glow = GetOrCreateMaterial("M_OvenGlow", Hex("FFB45D"), 0f, 0.4f, Hex("FF8A36") * 2.1f),
                EveningBlue = GetOrCreateMaterial("M_EveningBlue", Hex("526777"), 0f, 0.18f),
                Wood = GetOrCreateMaterial("M_WarmWood", Hex("82553A"), 0f, 0.26f),
                Metal = GetOrCreateMaterial("M_DarkMetal", Hex("4A4744"), 0.42f, 0.34f),
                Stone = GetOrCreateMaterial("M_StreetStone", Hex("7A7470"), 0f, 0.16f),
                Hair = GetOrCreateMaterial("M_Hair", Hex("5A3528"), 0f, 0.24f),
                Skin = GetOrCreateMaterial("M_Skin", Hex("E5B18B"), 0f, 0.3f),
                Cloth = GetOrCreateMaterial("M_Cloth", Hex("D9C8A8"), 0f, 0.08f),
                White = GetOrCreateMaterial("M_SoftWhite", Hex("F7F0E5"), 0f, 0.18f)
            };
        }

        private static Material GetOrCreateMaterial(
            string name,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emission = null)
        {
            var path = $"{MaterialRoot}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else
            {
                material.color = baseColor;
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateRecipeData()
        {
            var recipes = new[]
            {
                CreateRecipe(
                    "Recipe_CountryBread",
                    RecipeId.CountryBread,
                    "Country Bread",
                    "A warm country loaf with a deep cross score.",
                    0f,
                    4f,
                    0f,
                    1,
                    6,
                    0,
                    1),
                CreateRecipe(
                    "Recipe_KaiserRoll",
                    RecipeId.KaiserRoll,
                    "Basic Kaiser Roll",
                    "A small golden roll baked in a sociable batch.",
                    0f,
                    6f,
                    0f,
                    3,
                    3,
                    30,
                    1),
                CreateRecipe(
                    "Recipe_ButterCroissant",
                    RecipeId.ButterCroissant,
                    "Butter Croissant",
                    "A slow, layered bake with a crisp crescent silhouette.",
                    0f,
                    8f,
                    0f,
                    2,
                    8,
                    45,
                    1),
                CreateRecipe(
                    "Recipe_CinnamonSwirl",
                    RecipeId.CinnamonSwirl,
                    "Cinnamon Swirl",
                    "A warm spiral finished with a pale ribbon of glaze.",
                    0f,
                    7f,
                    2f,
                    3,
                    7,
                    75,
                    2),
                CreateRecipe(
                    "Recipe_Finezja",
                    RecipeId.Finezja,
                    "Finezja",
                    "A soft pastry crowned with ribbons of vanilla and strawberry cream.",
                    2f,
                    9f,
                    3f,
                    2,
                    11,
                    100,
                    2),
                CreateRecipe(
                    "Recipe_CinnamonMonocle",
                    RecipeId.CinnamonMonocle,
                    "Cinnamon Monocle",
                    "A tightly coiled Danish-style pastry with a crisp cinnamon spiral.",
                    1f,
                    8f,
                    1f,
                    3,
                    9,
                    125,
                    2)
            };

            var catalogPath = $"{DataRoot}/BakeryCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BakeryCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            var recipeList = serializedCatalog.FindProperty("recipes");
            recipeList.arraySize = recipes.Length;
            for (var index = 0; index < recipes.Length; index++)
            {
                recipeList.GetArrayElementAtIndex(index).objectReferenceValue = recipes[index];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static RecipeDefinition CreateRecipe(
            string assetName,
            RecipeId id,
            string displayName,
            string description,
            float preparationSeconds,
            float bakeSeconds,
            float finishingSeconds,
            int batchYield,
            int salePrice,
            int unlockAtSales,
            int requiredBakeryLevel)
        {
            var path = $"{DataRoot}/{assetName}.asset";
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            var serializedRecipe = new SerializedObject(recipe);
            serializedRecipe.FindProperty("id").enumValueIndex = (int)id;
            serializedRecipe.FindProperty("displayName").stringValue = displayName;
            serializedRecipe.FindProperty("customerDescription").stringValue = description;
            serializedRecipe.FindProperty("preparationSeconds").floatValue = preparationSeconds;
            serializedRecipe.FindProperty("bakeSeconds").floatValue = bakeSeconds;
            serializedRecipe.FindProperty("finishingSeconds").floatValue = finishingSeconds;
            serializedRecipe.FindProperty("batchYield").intValue = batchYield;
            serializedRecipe.FindProperty("salePrice").intValue = salePrice;
            serializedRecipe.FindProperty("unlockAtSales").intValue = unlockAtSales;
            serializedRecipe.FindProperty("requiredBakeryLevel").intValue = requiredBakeryLevel;
            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static void BuildStudioIntroScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("231B1B");

            var root = new GameObject("StudioIntro");
            root.AddComponent<BuildSmokeProbe>();
            BuildFlatCamera(root.transform, "Studio Intro Camera", Hex("191416"));
            BuildUiDocument<StudioIntroController>(
                "HCK Labs Studio Intro",
                StudioIntroUxmlPath,
                StudioIntroUssPath,
                100);
            EditorSceneManager.SaveScene(scene, StudioIntroScenePath);
        }

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("435665");

            var root = new GameObject("MainMenu");
            root.AddComponent<BuildSmokeProbe>();
            BuildFlatCamera(root.transform, "Main Menu Camera", Hex("435665"));
            BuildUiDocument<MainMenuController>(
                "Main Menu UI",
                MainMenuUxmlPath,
                MainMenuUssPath,
                50);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "HCK Labs";
            PlayerSettings.productName = "Baka Bake Bakery";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone,
                "com.hcklabs.bakabakebakery");
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.WebGL,
                "com.hcklabs.bakabakebakery");
        }

        private static Camera BuildFlatCamera(Transform parent, string name, Color background)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Camera BuildScene(Materials materials)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("A7B1B8") * 0.86f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Hex("5F7282");
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 36f;

            var root = new GameObject("MainBakery");
            root.AddComponent<BuildSmokeProbe>();
            BuildPlatform(root.transform, materials);
            BuildBackdrop(root.transform, materials);
            BuildFoodTruck(root.transform, materials);
            BuildStreetDetails(root.transform, materials);
            BuildCharacters(root.transform, materials);
            BuildLighting(root.transform, materials);
            return BuildCamera(root.transform);
        }

        private static void BuildPlatform(Transform parent, Materials materials)
        {
            var platform = new GameObject("Diorama Platform").transform;
            platform.SetParent(parent, false);

            CreatePrimitive(PrimitiveType.Cube, "Stone Base", platform, new Vector3(0f, -0.32f, 0f), new Vector3(12f, 0.5f, 5.5f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cylinder, "Stone Base Left", platform, new Vector3(-6f, -0.32f, 0f), new Vector3(5.5f, 0.25f, 5.5f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cylinder, "Stone Base Right", platform, new Vector3(6f, -0.32f, 0f), new Vector3(5.5f, 0.25f, 5.5f), materials.Cocoa);

            CreatePrimitive(PrimitiveType.Cube, "Street Surface", platform, new Vector3(0f, -0.04f, 0f), new Vector3(12f, 0.14f, 5.2f), materials.Stone);
            CreatePrimitive(PrimitiveType.Cylinder, "Street Surface Left", platform, new Vector3(-6f, -0.04f, 0f), new Vector3(5.2f, 0.07f, 5.2f), materials.Stone);
            CreatePrimitive(PrimitiveType.Cylinder, "Street Surface Right", platform, new Vector3(6f, -0.04f, 0f), new Vector3(5.2f, 0.07f, 5.2f), materials.Stone);

            for (var x = -7.4f; x <= 7.4f; x += 1.15f)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Paving Accent",
                    platform,
                    new Vector3(x, 0.05f, -2.24f),
                    new Vector3(0.92f, 0.035f, 0.46f),
                    materials.EveningBlue,
                    Quaternion.Euler(0f, (x % 2f) * 4f, 0f));
            }
        }

        private static void BuildBackdrop(Transform parent, Materials materials)
        {
            var backdrop = new GameObject("Backdrop").transform;
            backdrop.SetParent(parent, false);

            CreateFacade(backdrop, new Vector3(-7.5f, 2.2f, 4.4f), new Vector3(4f, 4.4f, 0.8f), materials.Cherry, materials.Glow);
            CreateFacade(backdrop, new Vector3(-2.9f, 2.6f, 4.65f), new Vector3(4.2f, 5.2f, 0.8f), materials.Flour, materials.Glow);
            CreateFacade(backdrop, new Vector3(6.8f, 2.35f, 4.5f), new Vector3(4.5f, 4.7f, 0.8f), materials.EveningBlue, materials.Glow);
        }

        private static void CreateFacade(Transform parent, Vector3 position, Vector3 scale, Material wall, Material window)
        {
            var facade = new GameObject("Quiet Facade").transform;
            facade.SetParent(parent, false);
            facade.localPosition = position;
            CreatePrimitive(PrimitiveType.Cube, "Wall", facade, Vector3.zero, scale, wall);
            CreatePrimitive(PrimitiveType.Cube, "Window A", facade, new Vector3(-0.9f, 0.4f, -0.43f), new Vector3(0.7f, 1.15f, 0.08f), window);
            CreatePrimitive(PrimitiveType.Cube, "Window B", facade, new Vector3(0.9f, 0.4f, -0.43f), new Vector3(0.7f, 1.15f, 0.08f), window);
            CreatePrimitive(PrimitiveType.Cube, "Cornice", facade, new Vector3(0f, scale.y * 0.5f, -0.05f), new Vector3(scale.x + 0.25f, 0.18f, scale.z + 0.12f), wall);
        }

        private static void BuildFoodTruck(Transform parent, Materials materials)
        {
            var truck = new GameObject("Food Truck - Bakery Level 1").transform;
            truck.SetParent(parent, false);
            truck.localPosition = new Vector3(0f, 0.2f, 0.5f);

            CreatePrimitive(PrimitiveType.Cube, "Floor", truck, new Vector3(0f, 0.42f, 0f), new Vector3(9.8f, 0.34f, 4.1f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Back Wall", truck, new Vector3(0f, 2.22f, 1.87f), new Vector3(9.8f, 3.75f, 0.24f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Left Wall", truck, new Vector3(-4.76f, 2.22f, 0f), new Vector3(0.28f, 3.75f, 3.9f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cube, "Right Wall", truck, new Vector3(4.76f, 2.22f, 0f), new Vector3(0.28f, 3.75f, 3.9f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cube, "Front Sill", truck, new Vector3(0f, 0.92f, -1.88f), new Vector3(9.8f, 1.12f, 0.24f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Roof", truck, new Vector3(0f, 4.2f, 0f), new Vector3(10.15f, 0.28f, 4.3f), materials.Flour);

            for (var index = 0; index < 11; index++)
            {
                var x = -4.45f + index * 0.89f;
                var material = index % 2 == 0 ? materials.Flour : materials.Cherry;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    $"Awning Stripe {index:00}",
                    truck,
                    new Vector3(x, 3.73f, -2.36f),
                    new Vector3(0.88f, 0.12f, 1.25f),
                    material,
                    Quaternion.Euler(10f, 0f, 0f));
            }

            CreateWheel(truck, new Vector3(-3.55f, 0.45f, -1.94f), materials);
            CreateWheel(truck, new Vector3(3.55f, 0.45f, -1.94f), materials);

            BuildFridge(truck, materials);
            BuildOven(truck, materials);
            BuildPreparationArea(truck, materials);
            BuildServiceCounter(truck, materials);
        }

        private static void CreateWheel(Transform parent, Vector3 position, Materials materials)
        {
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Wheel",
                parent,
                position,
                new Vector3(1.05f, 0.24f, 1.05f),
                materials.Cocoa,
                Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Wheel Hub",
                parent,
                position + new Vector3(0f, 0f, -0.15f),
                new Vector3(0.48f, 0.27f, 0.48f),
                materials.Metal,
                Quaternion.Euler(90f, 0f, 0f));
        }

        private static void BuildFridge(Transform truck, Materials materials)
        {
            var fridge = new GameObject("Station - Refrigerator").transform;
            fridge.SetParent(truck, false);
            fridge.localPosition = new Vector3(-3.65f, 0.62f, 0.92f);
            CreatePrimitive(PrimitiveType.Cube, "Fridge Body", fridge, new Vector3(0f, 1.28f, 0f), new Vector3(1.38f, 2.55f, 1.18f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cube, "Fridge Door", fridge, new Vector3(0f, 1.31f, -0.61f), new Vector3(1.22f, 2.26f, 0.09f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cylinder, "Fridge Handle", fridge, new Vector3(0.43f, 1.34f, -0.71f), new Vector3(0.08f, 0.48f, 0.08f), materials.Metal);
            CreatePrimitive(PrimitiveType.Cube, "Flour Label", fridge, new Vector3(-0.25f, 1.72f, -0.7f), new Vector3(0.35f, 0.45f, 0.04f), materials.Paper);
        }

        private static void BuildOven(Transform truck, Materials materials)
        {
            var oven = new GameObject("Station - Oven 1").transform;
            oven.SetParent(truck, false);
            oven.localPosition = new Vector3(-1.35f, 0.6f, 0.86f);
            CreatePrimitive(PrimitiveType.Cube, "Oven Body", oven, new Vector3(0f, 1.1f, 0f), new Vector3(1.65f, 2.18f, 1.25f), materials.Metal);
            CreatePrimitive(PrimitiveType.Cube, "Oven Door", oven, new Vector3(0f, 1.03f, -0.66f), new Vector3(1.3f, 1.15f, 0.08f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Oven Glow", oven, new Vector3(0f, 1.04f, -0.72f), new Vector3(1.05f, 0.82f, 0.04f), materials.Glow);
            CreatePrimitive(PrimitiveType.Cylinder, "Oven Dial A", oven, new Vector3(-0.42f, 1.82f, -0.68f), new Vector3(0.18f, 0.06f, 0.18f), materials.Cherry, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cylinder, "Oven Dial B", oven, new Vector3(0.42f, 1.82f, -0.68f), new Vector3(0.18f, 0.06f, 0.18f), materials.Flour, Quaternion.Euler(90f, 0f, 0f));

            var ovenLightObject = new GameObject("Oven Practical Light");
            ovenLightObject.transform.SetParent(oven, false);
            ovenLightObject.transform.localPosition = new Vector3(0f, 1.08f, -0.92f);
            var ovenLight = ovenLightObject.AddComponent<Light>();
            ovenLight.type = LightType.Point;
            ovenLight.color = Hex("FF9A4D");
            ovenLight.intensity = 3.4f;
            ovenLight.range = 4.6f;
            ovenLight.shadows = LightShadows.Soft;
            ovenLightObject.AddComponent<OvenGlowPulse>();

            var coveredBay = new GameObject("Future Oven Bay").transform;
            coveredBay.SetParent(truck, false);
            coveredBay.localPosition = new Vector3(0.35f, 0.6f, 0.86f);
            CreatePrimitive(PrimitiveType.Cube, "Covered Bay", coveredBay, new Vector3(0f, 0.82f, 0f), new Vector3(1.2f, 1.65f, 1.05f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Cube, "Bay Strap", coveredBay, new Vector3(0f, 0.82f, -0.55f), new Vector3(0.18f, 1.72f, 0.08f), materials.Flour);
        }

        private static void BuildPreparationArea(Transform truck, Materials materials)
        {
            var prep = new GameObject("Station - Preparation").transform;
            prep.SetParent(truck, false);
            prep.localPosition = new Vector3(2.25f, 0.6f, 0.82f);
            CreatePrimitive(PrimitiveType.Cube, "Prep Cabinet", prep, new Vector3(0f, 0.58f, 0f), new Vector3(2.35f, 1.16f, 1.15f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Prep Top", prep, new Vector3(0f, 1.22f, -0.02f), new Vector3(2.5f, 0.14f, 1.3f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cylinder, "Mixing Bowl", prep, new Vector3(-0.45f, 1.42f, 0f), new Vector3(0.52f, 0.16f, 0.52f), materials.Metal);
            CreatePrimitive(PrimitiveType.Sphere, "Dough", prep, new Vector3(0.46f, 1.39f, -0.05f), new Vector3(0.62f, 0.28f, 0.52f), materials.Flour);

            CreatePrimitive(PrimitiveType.Cube, "Rear Shelf", truck, new Vector3(2.45f, 3.0f, 1.66f), new Vector3(3.3f, 0.15f, 0.45f), materials.Wood);
            for (var index = 0; index < 4; index++)
            {
                CreatePrimitive(PrimitiveType.Cylinder, "Shelf Jar", truck, new Vector3(1.35f + index * 0.72f, 3.3f, 1.58f), new Vector3(0.26f, 0.3f, 0.26f), index % 2 == 0 ? materials.Flour : materials.Cherry);
            }
        }

        private static void BuildServiceCounter(Transform truck, Materials materials)
        {
            var counter = new GameObject("Station - Service Counter").transform;
            counter.SetParent(truck, false);
            counter.localPosition = new Vector3(1.48f, 0.45f, -1.35f);
            CreatePrimitive(PrimitiveType.Cube, "Counter Front", counter, new Vector3(0f, 0.68f, 0f), new Vector3(5.7f, 1.35f, 0.68f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Counter Top", counter, new Vector3(0f, 1.41f, -0.04f), new Vector3(5.95f, 0.16f, 0.92f), materials.Wood);

            CreateBread(counter, new Vector3(-2.35f, 1.63f, -0.06f), materials);
            CreateKaiserRolls(counter, new Vector3(-1.45f, 1.58f, -0.06f), materials);
            CreateCroissant(counter, new Vector3(-0.5f, 1.58f, -0.06f), materials);
            CreateCinnamonSwirl(counter, new Vector3(0.48f, 1.57f, -0.06f), materials);
            CreateFinezja(counter, new Vector3(1.45f, 1.58f, -0.06f), materials);
            CreateCinnamonMonocle(counter, new Vector3(2.35f, 1.58f, -0.06f), materials);
        }

        private static void CreateBread(Transform parent, Vector3 position, Materials materials)
        {
            var bread = new GameObject("Product - Country Bread").transform;
            bread.SetParent(parent, false);
            bread.localPosition = position;
            CreatePrimitive(PrimitiveType.Sphere, "Loaf", bread, Vector3.zero, new Vector3(0.82f, 0.42f, 0.62f), materials.Crust);
            for (var index = -1; index <= 1; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Flour Score",
                    bread,
                    new Vector3(index * 0.18f, 0.22f, -0.11f),
                    new Vector3(0.07f, 0.025f, 0.5f),
                    materials.Flour,
                    Quaternion.Euler(0f, 0f, -18f));
            }
        }

        private static void CreateKaiserRolls(Transform parent, Vector3 position, Materials materials)
        {
            var rolls = new GameObject("Product - Kaiser Rolls").transform;
            rolls.SetParent(parent, false);
            rolls.localPosition = position;
            var offsets = new[]
            {
                new Vector3(-0.26f, 0f, 0f),
                new Vector3(0.26f, 0f, 0f),
                new Vector3(0f, 0.14f, -0.18f)
            };
            foreach (var offset in offsets)
            {
                CreatePrimitive(PrimitiveType.Sphere, "Roll", rolls, offset, new Vector3(0.46f, 0.28f, 0.4f), materials.Crust);
                CreatePrimitive(PrimitiveType.Sphere, "Roll Flour", rolls, offset + new Vector3(0f, 0.13f, -0.03f), new Vector3(0.31f, 0.06f, 0.25f), materials.Flour);
            }
        }

        private static void CreateCroissant(Transform parent, Vector3 position, Materials materials)
        {
            var croissant = new GameObject("Product - Butter Croissant").transform;
            croissant.SetParent(parent, false);
            croissant.localPosition = position;
            for (var index = 0; index < 7; index++)
            {
                var normalized = index / 6f;
                var x = Mathf.Lerp(-0.55f, 0.55f, normalized);
                var z = -Mathf.Sin(normalized * Mathf.PI) * 0.24f;
                var scale = Mathf.Lerp(0.28f, 0.48f, Mathf.Sin(normalized * Mathf.PI));
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Laminated Segment",
                    croissant,
                    new Vector3(x, 0f, z),
                    new Vector3(scale, scale * 0.66f, scale * 0.78f),
                    materials.Crust);
            }
        }

        private static void CreateCinnamonSwirl(Transform parent, Vector3 position, Materials materials)
        {
            var swirl = new GameObject("Product - Cinnamon Swirl").transform;
            swirl.SetParent(parent, false);
            swirl.localPosition = position;
            CreatePrimitive(PrimitiveType.Cylinder, "Bun", swirl, Vector3.zero, new Vector3(0.72f, 0.14f, 0.72f), materials.Crust);
            CreatePrimitive(PrimitiveType.Cylinder, "Glaze", swirl, new Vector3(0f, 0.18f, 0f), new Vector3(0.56f, 0.035f, 0.56f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cylinder, "Cinnamon Centre", swirl, new Vector3(0f, 0.22f, 0f), new Vector3(0.19f, 0.028f, 0.19f), materials.Cocoa);
        }

        private static void CreateFinezja(Transform parent, Vector3 position, Materials materials)
        {
            var finezja = new GameObject("Product - Finezja").transform;
            finezja.SetParent(parent, false);
            finezja.localPosition = position;
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Soft Pastry Base",
                finezja,
                Vector3.zero,
                new Vector3(0.78f, 0.25f, 0.48f),
                materials.Crust);

            for (var index = 0; index < 5; index++)
            {
                var x = -0.42f + index * 0.21f;
                var cream = index % 2 == 0 ? materials.White : materials.Cherry;
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    index % 2 == 0 ? "Vanilla Cream" : "Strawberry Cream",
                    finezja,
                    new Vector3(x, 0.22f, -0.03f),
                    new Vector3(0.25f, 0.18f, 0.3f),
                    cream);
            }
        }

        private static void CreateCinnamonMonocle(Transform parent, Vector3 position, Materials materials)
        {
            var monocle = new GameObject("Product - Cinnamon Monocle").transform;
            monocle.SetParent(parent, false);
            monocle.localPosition = position;
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Laminated Pastry Disc",
                monocle,
                Vector3.zero,
                new Vector3(0.7f, 0.13f, 0.7f),
                materials.Crust);

            for (var index = 0; index < 12; index++)
            {
                var normalized = index / 11f;
                var angle = normalized * Mathf.PI * 3.4f;
                var radius = Mathf.Lerp(0.06f, 0.48f, normalized);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Cinnamon Spiral",
                    monocle,
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0.16f,
                        Mathf.Sin(angle) * radius),
                    new Vector3(0.16f, 0.045f, 0.13f),
                    materials.Cocoa);
            }

            var sugarDust = new[]
            {
                new Vector3(-0.31f, 0.2f, -0.17f),
                new Vector3(0.18f, 0.2f, 0.3f),
                new Vector3(0.37f, 0.2f, -0.13f)
            };
            foreach (var dustPosition in sugarDust)
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Sugar Dust",
                    monocle,
                    dustPosition,
                    new Vector3(0.07f, 0.018f, 0.07f),
                    materials.Flour);
            }
        }

        private static void BuildStreetDetails(Transform parent, Materials materials)
        {
            var details = new GameObject("Street Details").transform;
            details.SetParent(parent, false);

            var tree = new GameObject("Small Tree").transform;
            tree.SetParent(details, false);
            tree.localPosition = new Vector3(-7.45f, 0f, 1.55f);
            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 1.25f, 0.42f), materials.Wood);
            CreatePrimitive(PrimitiveType.Sphere, "Crown A", tree, new Vector3(-0.35f, 2.8f, 0f), new Vector3(1.55f, 1.35f, 1.35f), materials.Sage);
            CreatePrimitive(PrimitiveType.Sphere, "Crown B", tree, new Vector3(0.48f, 2.85f, 0.15f), new Vector3(1.35f, 1.25f, 1.25f), materials.Sage);
            CreatePrimitive(PrimitiveType.Sphere, "Crown C", tree, new Vector3(0f, 3.4f, 0f), new Vector3(1.45f, 1.15f, 1.25f), materials.EveningBlue);

            var lamp = new GameObject("Street Lamp").transform;
            lamp.SetParent(details, false);
            lamp.localPosition = new Vector3(7.35f, 0f, -0.65f);
            CreatePrimitive(PrimitiveType.Cylinder, "Lamp Post", lamp, new Vector3(0f, 1.5f, 0f), new Vector3(0.17f, 1.5f, 0.17f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Lamp Housing", lamp, new Vector3(0f, 3.15f, 0f), new Vector3(0.52f, 0.65f, 0.52f), materials.Metal);
            CreatePrimitive(PrimitiveType.Sphere, "Lamp Glow", lamp, new Vector3(0f, 3.15f, -0.03f), new Vector3(0.34f, 0.45f, 0.34f), materials.Glow);

            var lightObject = new GameObject("Lamp Practical Light");
            lightObject.transform.SetParent(lamp, false);
            lightObject.transform.localPosition = new Vector3(0f, 3.15f, -0.3f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Hex("FFC879");
            light.intensity = 2.2f;
            light.range = 5.5f;
            light.shadows = LightShadows.Soft;
        }

        private static void BuildCharacters(Transform parent, Materials materials)
        {
            var characters = new GameObject("Characters").transform;
            characters.SetParent(parent, false);
            CreateBaker(characters, new Vector3(-0.1f, 0.72f, -0.62f), materials);
            CreateGrandmother(characters, new Vector3(4.65f, 0.5f, -2.9f), materials);
        }

        private static void CreateBaker(Transform parent, Vector3 position, Materials materials)
        {
            var baker = new GameObject("Baker - Manual Worker").transform;
            baker.SetParent(parent, false);
            baker.localPosition = position;
            baker.localRotation = Quaternion.Euler(0f, -18f, 0f);

            CreatePrimitive(PrimitiveType.Capsule, "Body", baker, new Vector3(0f, 0.78f, 0f), new Vector3(0.72f, 0.72f, 0.62f), materials.Cloth);
            CreatePrimitive(PrimitiveType.Cube, "Apron", baker, new Vector3(0f, 0.84f, -0.36f), new Vector3(0.68f, 0.9f, 0.08f), materials.Sage);
            CreatePrimitive(PrimitiveType.Sphere, "Head", baker, new Vector3(0f, 1.92f, 0f), new Vector3(0.74f, 0.74f, 0.7f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair", baker, new Vector3(0f, 2.18f, 0.14f), new Vector3(0.78f, 0.46f, 0.7f), materials.Hair);
            CreatePrimitive(PrimitiveType.Cylinder, "Hat Band", baker, new Vector3(0f, 2.42f, 0f), new Vector3(0.74f, 0.13f, 0.74f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Chef Hat", baker, new Vector3(0f, 2.62f, 0f), new Vector3(0.88f, 0.42f, 0.78f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left", baker, new Vector3(-0.17f, 1.98f, -0.34f), new Vector3(0.09f, 0.11f, 0.07f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right", baker, new Vector3(0.17f, 1.98f, -0.34f), new Vector3(0.09f, 0.11f, 0.07f), materials.Cocoa);
            CreateLimb(baker, "Arm Left", new Vector3(-0.34f, 1.28f, -0.02f), new Vector3(-0.72f, 0.9f, -0.42f), 0.22f, materials.Cloth);
            CreateLimb(baker, "Arm Right", new Vector3(0.34f, 1.28f, -0.02f), new Vector3(0.78f, 1.05f, -0.44f), 0.22f, materials.Cloth);
        }

        private static void CreateGrandmother(Transform parent, Vector3 position, Materials materials)
        {
            var customer = new GameObject("Customer - Grandmother").transform;
            customer.SetParent(parent, false);
            customer.localPosition = position;
            customer.localRotation = Quaternion.Euler(0f, -58f, 0f);

            CreatePrimitive(PrimitiveType.Capsule, "Body", customer, new Vector3(0f, 0.82f, 0f), new Vector3(0.8f, 0.78f, 0.7f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Sphere, "Head", customer, new Vector3(0f, 1.95f, 0f), new Vector3(0.76f, 0.73f, 0.7f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair Cap", customer, new Vector3(0f, 2.18f, 0.12f), new Vector3(0.8f, 0.48f, 0.72f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Hair Bun", customer, new Vector3(0.42f, 2.2f, 0.16f), new Vector3(0.42f, 0.42f, 0.42f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left", customer, new Vector3(-0.17f, 2.0f, -0.34f), new Vector3(0.09f, 0.1f, 0.07f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right", customer, new Vector3(0.17f, 2.0f, -0.34f), new Vector3(0.09f, 0.1f, 0.07f), materials.Cocoa);
            CreateLimb(customer, "Arm Left", new Vector3(-0.38f, 1.35f, -0.02f), new Vector3(-0.27f, 0.95f, -0.42f), 0.22f, materials.Cherry);
            CreateLimb(customer, "Arm Right", new Vector3(0.38f, 1.35f, -0.02f), new Vector3(0.25f, 0.95f, -0.42f), 0.22f, materials.Cherry);
        }

        private static void CreateLimb(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            Material material)
        {
            var direction = end - start;
            var limb = CreatePrimitive(
                PrimitiveType.Capsule,
                name,
                parent,
                (start + end) * 0.5f,
                new Vector3(width, direction.magnitude * 0.5f, width),
                material,
                Quaternion.FromToRotation(Vector3.up, direction.normalized));
            limb.transform.localPosition = (start + end) * 0.5f;
        }

        private static void BuildLighting(Transform parent, Materials materials)
        {
            var lighting = new GameObject("Lighting").transform;
            lighting.SetParent(parent, false);

            var sunObject = new GameObject("Late Afternoon Key");
            sunObject.transform.SetParent(lighting, false);
            sunObject.transform.localRotation = Quaternion.Euler(48f, -34f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("FFD6A3");
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;

            var fillObject = new GameObject("Evening Fill");
            fillObject.transform.SetParent(lighting, false);
            fillObject.transform.localRotation = Quaternion.Euler(35f, 145f, 0f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Hex("7E9EBA");
            fill.intensity = 0.56f;
            fill.shadows = LightShadows.None;

            var cameraFillObject = new GameObject("Camera Soft Fill");
            cameraFillObject.transform.SetParent(lighting, false);
            cameraFillObject.transform.localRotation = Quaternion.LookRotation(new Vector3(-7.5f, -4.5f, 18f).normalized);
            var cameraFill = cameraFillObject.AddComponent<Light>();
            cameraFill.type = LightType.Directional;
            cameraFill.color = Hex("FFE8C8");
            cameraFill.intensity = 0.92f;
            cameraFill.shadows = LightShadows.None;

            var interiorObject = new GameObject("Truck Interior Fill");
            interiorObject.transform.SetParent(lighting, false);
            interiorObject.transform.localPosition = new Vector3(0f, 3.15f, -1.25f);
            var interior = interiorObject.AddComponent<Light>();
            interior.type = LightType.Point;
            interior.color = Hex("FFD6A3");
            interior.intensity = 4.8f;
            interior.range = 10f;
            interior.shadows = LightShadows.None;
        }

        private static Camera BuildCamera(Transform parent)
        {
            var rig = new GameObject("Camera Rig").transform;
            rig.SetParent(parent, false);
            rig.localPosition = new Vector3(0f, 1.45f, 0f);
            rig.gameObject.AddComponent<CameraEdgeSway>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(rig, false);
            cameraObject.transform.localPosition = new Vector3(7.6f, 6.8f, -18.5f);
            cameraObject.transform.LookAt(rig.position + new Vector3(0f, 0.25f, 0f));
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("526777");
            camera.allowHDR = true;
            camera.allowMSAA = true;

            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void BuildHud()
        {
            BuildUiDocument<BakeryHudController>("Bakery HUD", UxmlPath, UssPath, 20);
        }

        private static void BuildUiDocument<TController>(
            string objectName,
            string uxmlPath,
            string ussPath,
            int sortingOrder)
            where TController : MonoBehaviour
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (visualTree == null || styleSheet == null)
            {
                throw new InvalidOperationException(
                    $"UI assets could not be imported: '{uxmlPath}' and '{ussPath}'.");
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(panelSettings);

            var uiObject = new GameObject(objectName);
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = sortingOrder;

            var controller = uiObject.AddComponent<TController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("styleSheet").objectReferenceValue = styleSheet;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion? localRotation = null)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation ?? Quaternion.identity;
            primitive.transform.localScale = localScale;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return primitive;
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
            {
                return color;
            }

            throw new ArgumentException($"Invalid colour value: {hex}", nameof(hex));
        }

        private static void CaptureScene(Camera camera)
        {
            const int width = 1920;
            const int height = 1080;
            var renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 4
            };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousHdr = camera.allowHDR;

            try
            {
                camera.allowHDR = false;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var screenshotPath = Path.Combine(projectRoot, ScreenshotRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? projectRoot);
                File.WriteAllBytes(screenshotPath, texture.EncodeToPNG());
                Debug.Log($"[Baka Bake Bakery] Captured {screenshotPath}");
            }
            finally
            {
                camera.allowHDR = previousHdr;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
