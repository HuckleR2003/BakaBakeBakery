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

        private sealed class WorldReferences
        {
            public GameObject LockedOvenBay;
            public GameObject SecondOven;
            public GameObject CabinUpgrade;
            public GameObject GoldenMinuteLight;
            public GameObject CountryBread;
            public GameObject KaiserRolls;
            public GameObject Croissant;
            public GameObject CinnamonSwirl;
            public GameObject Finezja;
            public GameObject CinnamonMonocle;
            public GameObject[] IngredientDisplays;
            public GameObject[] OvenRawDisplays;
            public GameObject[] OvenBakedDisplays;
            public BakeryCounterDisplay[] CounterDisplays;
            public Transform FridgeDoor;
            public Transform OvenDoor;
            public OvenGlowPulse OvenGlow;
            public Transform[] SteamPuffs;
            public Transform HangingBell;
        }

        private sealed class CharacterReferences
        {
            public BakeryWorkerView Worker;
            public Collider BakerHitTarget;
            public BakeryCustomerActor[] Customers;
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
                Crust = GetOrCreateMaterial("M_BreadCrust", Hex("E08A3F"), 0f, 0.32f),
                Cocoa = GetOrCreateMaterial("M_Cocoa", Hex("382824"), 0f, 0.22f),
                Sage = GetOrCreateMaterial("M_Sage", Hex("4E7865"), 0.02f, 0.28f),
                Cherry = GetOrCreateMaterial("M_SourCherry", Hex("B4454F"), 0f, 0.3f),
                Glow = GetOrCreateMaterial("M_OvenGlow", Hex("FFB45D"), 0f, 0.4f, Hex("FF8A36") * 2.1f),
                EveningBlue = GetOrCreateMaterial("M_EveningBlue", Hex("385776"), 0f, 0.18f),
                Wood = GetOrCreateMaterial("M_WarmWood", Hex("714328"), 0f, 0.26f),
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
                    2),
                CreateRecipe(
                    "Recipe_ChocolateMuffin",
                    RecipeId.ChocolateMuffin,
                    "Chocolate Muffin",
                    "Mila's first discovery: a soft cocoa crumb under a cracked chocolate crown.",
                    2f,
                    7f,
                    1f,
                    3,
                    8,
                    0,
                    1),
                CreateRecipe(
                    "Recipe_JamTurnover",
                    RecipeId.JamTurnover,
                    "Village Jam Turnover",
                    "Folded puff pastry with a bright strawberry seam.",
                    1f,
                    7f,
                    1f,
                    2,
                    10,
                    0,
                    1),
                CreateRecipe(
                    "Recipe_ChocolatePillow",
                    RecipeId.ChocolatePillow,
                    "Chocolate Pillow",
                    "A flaky square hiding a warm chocolate centre.",
                    2f,
                    8f,
                    1f,
                    2,
                    12,
                    0,
                    1)
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
            BuildFlatCamera(root.transform, "Studio Intro Camera", Hex("030305"));
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
            PlayerSettings.bundleVersion = "0.5.0";
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
            RenderSettings.ambientLight = Hex("32445A") * 0.72f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Hex("1D2C43");
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 36f;

            var root = new GameObject("MainBakery");
            root.AddComponent<BuildSmokeProbe>();
            root.AddComponent<BakeryAmbientDistrict>();
            BuildPlatform(root.transform, materials);
            BuildBackdrop(root.transform, materials);
            var world = BuildFoodTruck(root.transform, materials);
            BuildStreetDetails(root.transform, materials);
            var characters = BuildCharacters(root.transform, materials);
            BuildLighting(root.transform, materials);
            var camera = BuildCamera(root.transform);
            BuildGameplayController(root, camera, world, characters);
            return camera;
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

            CreateFacade(backdrop, new Vector3(-7.5f, 2.2f, 4.4f), new Vector3(4f, 4.4f, 0.8f), materials.Cherry, materials.Glow, materials.White);
            CreateFacade(backdrop, new Vector3(-2.9f, 2.6f, 4.65f), new Vector3(4.2f, 5.2f, 0.8f), materials.Flour, materials.Glow, materials.White);
            CreateFacade(backdrop, new Vector3(6.8f, 2.35f, 4.5f), new Vector3(4.5f, 4.7f, 0.8f), materials.EveningBlue, materials.Glow, materials.White);
        }

        private static void CreateFacade(Transform parent, Vector3 position, Vector3 scale, Material wall, Material window, Material smoke)
        {
            var facade = new GameObject("Quiet Facade").transform;
            facade.SetParent(parent, false);
            facade.localPosition = position;
            CreatePrimitive(PrimitiveType.Cube, "Wall", facade, Vector3.zero, scale, wall);
            CreatePrimitive(PrimitiveType.Cube, "Window Light A", facade, new Vector3(-0.9f, 0.4f, -0.43f), new Vector3(0.7f, 1.15f, 0.08f), window);
            CreatePrimitive(PrimitiveType.Cube, "Window Light B", facade, new Vector3(0.9f, 0.4f, -0.43f), new Vector3(0.7f, 1.15f, 0.08f), window);
            for (var side = -1; side <= 1; side += 2)
            {
                var x = side * 0.9f;
                CreatePrimitive(PrimitiveType.Cube, "Window Crossbar", facade, new Vector3(x, 0.4f, -0.49f), new Vector3(0.06f, 1.16f, 0.04f), wall);
                CreatePrimitive(PrimitiveType.Cube, "Window Sill", facade, new Vector3(x, -0.2f, -0.5f), new Vector3(0.88f, 0.12f, 0.2f), wall);
                CreatePrimitive(PrimitiveType.Cube, "Flower Box", facade, new Vector3(x, -0.35f, -0.57f), new Vector3(0.72f, 0.22f, 0.28f), wall);
            }
            CreatePrimitive(PrimitiveType.Cube, "Cornice", facade, new Vector3(0f, scale.y * 0.5f, -0.05f), new Vector3(scale.x + 0.25f, 0.18f, scale.z + 0.12f), wall);
            CreatePrimitive(PrimitiveType.Cube, "Roof Left", facade, new Vector3(-scale.x * 0.22f, scale.y * 0.5f + 0.48f, 0f), new Vector3(scale.x * 0.58f, 0.16f, scale.z + 0.5f), wall, Quaternion.Euler(0f, 0f, 22f));
            CreatePrimitive(PrimitiveType.Cube, "Roof Right", facade, new Vector3(scale.x * 0.22f, scale.y * 0.5f + 0.48f, 0f), new Vector3(scale.x * 0.58f, 0.16f, scale.z + 0.5f), wall, Quaternion.Euler(0f, 0f, -22f));
            var chimney = new GameObject("Backdrop Chimney").transform;
            chimney.SetParent(facade, false);
            chimney.localPosition = new Vector3(scale.x * 0.28f, scale.y * 0.5f + 0.75f, 0.1f);
            CreatePrimitive(PrimitiveType.Cube, "Brick Stack", chimney, Vector3.zero, new Vector3(0.42f, 1.1f, 0.5f), wall);
            for (var index = 0; index < 3; index++)
            {
                CreatePrimitive(PrimitiveType.Sphere, $"Backdrop Smoke {index}", chimney, new Vector3(0f, 0.5f + index * 0.35f, 0f), Vector3.one * (0.2f + index * 0.08f), smoke);
            }
        }

        private static WorldReferences BuildFoodTruck(Transform parent, Materials materials)
        {
            var references = new WorldReferences();
            var truck = new GameObject("Food Truck - Bakery Level 1").transform;
            truck.SetParent(parent, false);
            truck.localPosition = new Vector3(0f, 0.2f, 0.5f);

            CreatePrimitive(PrimitiveType.Cube, "Floor", truck, new Vector3(0f, 0.42f, 0f), new Vector3(9.8f, 0.34f, 4.1f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Back Wall", truck, new Vector3(0f, 2.22f, 1.87f), new Vector3(9.8f, 3.75f, 0.24f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Left Wall", truck, new Vector3(-4.76f, 2.22f, 0f), new Vector3(0.28f, 3.75f, 3.9f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cube, "Right Wall", truck, new Vector3(4.76f, 2.22f, 0f), new Vector3(0.28f, 3.75f, 3.9f), materials.Flour);
            CreatePrimitive(PrimitiveType.Cube, "Front Sill", truck, new Vector3(0f, 0.92f, -1.88f), new Vector3(9.8f, 1.12f, 0.24f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Roof", truck, new Vector3(0f, 4.2f, 0f), new Vector3(10.15f, 0.28f, 4.3f), materials.Flour);
            var shutter = new GameObject("Service Shutter").transform;
            shutter.SetParent(truck, false);
            shutter.localPosition = new Vector3(0f, 4.05f, -2.03f);
            for (var slat = 0; slat < 7; slat++)
            {
                CreatePrimitive(PrimitiveType.Cube, $"Wooden Shutter Slat {slat}", shutter, new Vector3(0f, -slat * 0.04f, 0f), new Vector3(8.85f, 0.27f, 0.11f), slat % 2 == 0 ? materials.Wood : materials.Cocoa);
            }

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

            var truckSign = new GameObject("Food Truck Lettering");
            truckSign.transform.SetParent(truck, false);
            truckSign.transform.localPosition = new Vector3(0f, 4.02f, -2.46f);
            truckSign.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var truckText = truckSign.AddComponent<TextMesh>();
            truckText.text = "BAKA-BAKE";
            truckText.anchor = TextAnchor.MiddleCenter;
            truckText.alignment = TextAlignment.Center;
            truckText.fontSize = 96;
            truckText.characterSize = 0.055f;
            truckText.color = Hex("FFE3A5");
            truckText.fontStyle = FontStyle.Bold;

            CreateWheel(truck, new Vector3(-3.55f, 0.45f, -1.94f), materials);
            CreateWheel(truck, new Vector3(3.55f, 0.45f, -1.94f), materials);

            BuildFridge(truck, materials, references);
            BuildOven(truck, materials, references);
            BuildPreparationArea(truck, materials, references);
            BuildServiceCounter(truck, materials, references);
            references.HangingBell = BuildHangingBell(truck, materials);
            references.CabinUpgrade = BuildCabinUpgrade(truck, materials);
            references.GoldenMinuteLight = BuildGoldenMinuteLight(truck);
            BuildDeliveryVehicles(truck, materials);
            return references;
        }

        private static void BuildDeliveryVehicles(Transform truck, Materials materials)
        {
            var bicycle = new GameObject("Morning Bicycle").transform;
            bicycle.SetParent(truck, false);
            bicycle.localPosition = new Vector3(-5.25f, 0.52f, 1.35f);
            bicycle.localRotation = Quaternion.Euler(0f, 18f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, "Bicycle Wheel Rear", bicycle, new Vector3(-0.72f, 0.42f, 0f), new Vector3(0.72f, 0.08f, 0.72f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cylinder, "Bicycle Wheel Front", bicycle, new Vector3(0.72f, 0.42f, 0f), new Vector3(0.72f, 0.08f, 0.72f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            CreateLimb(bicycle, "Bicycle Lower Frame", new Vector3(-0.62f, 0.48f, -0.08f), new Vector3(0f, 0.93f, -0.08f), 0.07f, materials.Cherry);
            CreateLimb(bicycle, "Bicycle Upper Frame", new Vector3(0f, 0.93f, -0.08f), new Vector3(0.56f, 0.5f, -0.08f), 0.07f, materials.Cherry);
            CreateLimb(bicycle, "Bicycle Base Frame", new Vector3(-0.62f, 0.48f, -0.08f), new Vector3(0.56f, 0.5f, -0.08f), 0.07f, materials.Cherry);
            CreateLimb(bicycle, "Handlebar", new Vector3(0.56f, 0.5f, -0.08f), new Vector3(0.73f, 1.15f, -0.08f), 0.055f, materials.Metal);
            CreatePrimitive(PrimitiveType.Cube, "Market Basket", bicycle, new Vector3(-0.75f, 0.95f, -0.03f), new Vector3(0.62f, 0.5f, 0.5f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Delivery Badge", bicycle, new Vector3(-0.75f, 1.26f, -0.31f), new Vector3(0.42f, 0.2f, 0.05f), materials.Glow);

            var car = new GameObject("Old Delivery Car").transform;
            car.SetParent(truck, false);
            car.localPosition = new Vector3(-5.7f, 0.45f, 1.35f);
            CreatePrimitive(PrimitiveType.Cube, "Car Body", car, new Vector3(0f, 0.62f, 0f), new Vector3(2.8f, 0.85f, 1.35f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Cube, "Car Cabin", car, new Vector3(-0.28f, 1.28f, 0.02f), new Vector3(1.55f, 0.75f, 1.16f), materials.EveningBlue);
            CreatePrimitive(PrimitiveType.Cube, "Delivery Crate", car, new Vector3(1.2f, 1.16f, 0f), new Vector3(0.85f, 0.8f, 1.08f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cylinder, "Car Wheel Rear", car, new Vector3(-0.92f, 0.32f, -0.72f), new Vector3(0.52f, 0.14f, 0.52f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cylinder, "Car Wheel Front", car, new Vector3(0.92f, 0.32f, -0.72f), new Vector3(0.52f, 0.14f, 0.52f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            car.gameObject.SetActive(false);
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

        private static void BuildFridge(Transform truck, Materials materials, WorldReferences references)
        {
            var fridge = new GameObject("Station - Refrigerator").transform;
            fridge.SetParent(truck, false);
            fridge.localPosition = new Vector3(-3.65f, 0.62f, 0.92f);
            CreatePrimitive(PrimitiveType.Cube, "Fridge Body", fridge, new Vector3(0f, 1.28f, 0f), new Vector3(1.38f, 2.55f, 1.18f), materials.Flour);
            var doorPivot = new GameObject("Fridge Door Hinge").transform;
            doorPivot.SetParent(fridge, false);
            doorPivot.localPosition = new Vector3(-0.61f, 1.31f, -0.61f);
            CreatePrimitive(PrimitiveType.Cube, "Fridge Door", doorPivot, new Vector3(0.61f, 0f, 0f), new Vector3(1.22f, 2.26f, 0.09f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cylinder, "Fridge Handle", doorPivot, new Vector3(1.04f, 0.03f, -0.1f), new Vector3(0.08f, 0.48f, 0.08f), materials.Metal);
            CreatePrimitive(PrimitiveType.Cube, "Flour Label", doorPivot, new Vector3(0.36f, 0.41f, -0.09f), new Vector3(0.35f, 0.45f, 0.04f), materials.Paper);
            references.FridgeDoor = doorPivot;
        }

        private static void BuildOven(Transform truck, Materials materials, WorldReferences references)
        {
            var oven = new GameObject("Station - Oven 1").transform;
            oven.SetParent(truck, false);
            oven.localPosition = new Vector3(-1.35f, 0.6f, 0.86f);
            CreatePrimitive(PrimitiveType.Cube, "Oven Body", oven, new Vector3(0f, 1.1f, 0f), new Vector3(1.65f, 2.18f, 1.25f), materials.Metal);
            var doorPivot = new GameObject("Oven Door Hinge").transform;
            doorPivot.SetParent(oven, false);
            doorPivot.localPosition = new Vector3(0f, 0.45f, -0.68f);
            CreatePrimitive(PrimitiveType.Cube, "Oven Door", doorPivot, new Vector3(0f, 0.58f, 0f), new Vector3(1.3f, 1.15f, 0.08f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Oven Window", doorPivot, new Vector3(0f, 0.59f, -0.06f), new Vector3(1.05f, 0.82f, 0.04f), materials.Glow);
            references.OvenDoor = doorPivot;
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
            references.OvenGlow = ovenLightObject.AddComponent<OvenGlowPulse>();

            var recipeCount = Enum.GetValues(typeof(RecipeId)).Length;
            references.OvenRawDisplays = new GameObject[recipeCount];
            references.OvenBakedDisplays = new GameObject[recipeCount];
            for (var index = 0; index < recipeCount; index++)
            {
                var recipeId = (RecipeId)index;
                references.OvenRawDisplays[index] = CreateRecipeVisual(
                    oven,
                    $"Oven Raw - {recipeId}",
                    recipeId,
                    materials,
                    true,
                    new Vector3(0f, 1.02f, -0.94f),
                    0.66f).gameObject;
                references.OvenBakedDisplays[index] = CreateRecipeVisual(
                    oven,
                    $"Oven Baked - {recipeId}",
                    recipeId,
                    materials,
                    false,
                    new Vector3(0f, 1.02f, -0.96f),
                    0.66f).gameObject;
                references.OvenRawDisplays[index].SetActive(false);
                references.OvenBakedDisplays[index].SetActive(false);
            }

            var chimney = new GameObject("Oven Chimney").transform;
            chimney.SetParent(truck, false);
            chimney.localPosition = new Vector3(-1.35f, 4.18f, 0.86f);
            CreatePrimitive(PrimitiveType.Cylinder, "Chimney Pipe", chimney, Vector3.zero, new Vector3(0.32f, 0.55f, 0.32f), materials.Metal);
            references.SteamPuffs = new Transform[3];
            for (var index = 0; index < references.SteamPuffs.Length; index++)
            {
                var puff = CreatePrimitive(
                    PrimitiveType.Sphere,
                    $"Steam Puff {index + 1}",
                    chimney,
                    new Vector3((index - 1) * 0.18f, 0.5f + index * 0.16f, 0f),
                    Vector3.one * (0.28f + index * 0.08f),
                    materials.White);
                puff.SetActive(false);
                references.SteamPuffs[index] = puff.transform;
            }

            var coveredBay = new GameObject("Future Oven Bay").transform;
            coveredBay.SetParent(truck, false);
            coveredBay.localPosition = new Vector3(0.35f, 0.6f, 0.86f);
            CreatePrimitive(PrimitiveType.Cube, "Covered Bay", coveredBay, new Vector3(0f, 0.82f, 0f), new Vector3(1.2f, 1.65f, 1.05f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Cube, "Bay Strap", coveredBay, new Vector3(0f, 0.82f, -0.55f), new Vector3(0.18f, 1.72f, 0.08f), materials.Flour);
            references.LockedOvenBay = coveredBay.gameObject;

            var secondOven = new GameObject("Station - Oven 2").transform;
            secondOven.SetParent(truck, false);
            secondOven.localPosition = new Vector3(0.35f, 0.6f, 0.86f);
            CreatePrimitive(PrimitiveType.Cube, "Oven Body", secondOven, new Vector3(0f, 0.84f, 0f), new Vector3(1.2f, 1.68f, 1.05f), materials.Metal);
            CreatePrimitive(PrimitiveType.Cube, "Oven Door", secondOven, new Vector3(0f, 0.78f, -0.55f), new Vector3(0.92f, 0.82f, 0.07f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Oven Glow", secondOven, new Vector3(0f, 0.78f, -0.6f), new Vector3(0.72f, 0.56f, 0.035f), materials.Glow);
            CreatePrimitive(PrimitiveType.Cylinder, "Oven Dial", secondOven, new Vector3(0f, 1.38f, -0.58f), new Vector3(0.16f, 0.05f, 0.16f), materials.Cherry, Quaternion.Euler(90f, 0f, 0f));
            secondOven.gameObject.SetActive(false);
            references.SecondOven = secondOven.gameObject;
        }

        private static void BuildPreparationArea(
            Transform truck,
            Materials materials,
            WorldReferences references)
        {
            var prep = new GameObject("Station - Preparation").transform;
            prep.SetParent(truck, false);
            prep.localPosition = new Vector3(2.25f, 0.6f, 0.82f);
            CreatePrimitive(PrimitiveType.Cube, "Prep Cabinet", prep, new Vector3(0f, 0.58f, 0f), new Vector3(2.35f, 1.16f, 1.15f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Prep Top", prep, new Vector3(0f, 1.22f, -0.02f), new Vector3(2.5f, 0.14f, 1.3f), materials.Wood);
            references.IngredientDisplays = new GameObject[Enum.GetValues(typeof(RecipeId)).Length];
            for (var index = 0; index < references.IngredientDisplays.Length; index++)
            {
                references.IngredientDisplays[index] = CreateIngredientDisplay(
                    prep,
                    (RecipeId)index,
                    materials).gameObject;
                references.IngredientDisplays[index].SetActive(false);
            }

            for (var index = 0; index < 7; index++)
            {
                var mote = CreatePrimitive(PrimitiveType.Sphere, $"Flour Mote {index}", prep, Vector3.zero, Vector3.one * 0.04f, materials.Flour);
                mote.SetActive(false);
            }

            CreatePrimitive(PrimitiveType.Cube, "Rear Shelf", truck, new Vector3(2.45f, 3.0f, 1.66f), new Vector3(3.3f, 0.15f, 0.45f), materials.Wood);
            for (var index = 0; index < 4; index++)
            {
                CreatePrimitive(PrimitiveType.Cylinder, "Shelf Jar", truck, new Vector3(1.35f + index * 0.72f, 3.3f, 1.58f), new Vector3(0.26f, 0.3f, 0.26f), index % 2 == 0 ? materials.Flour : materials.Cherry);
            }
        }

        private static void BuildServiceCounter(
            Transform truck,
            Materials materials,
            WorldReferences references)
        {
            var counter = new GameObject("Station - Service Counter").transform;
            counter.SetParent(truck, false);
            counter.localPosition = new Vector3(1.48f, 0.45f, -1.35f);
            CreatePrimitive(PrimitiveType.Cube, "Counter Front", counter, new Vector3(0f, 0.68f, 0f), new Vector3(5.7f, 1.35f, 0.68f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Counter Top", counter, new Vector3(0f, 1.41f, -0.04f), new Vector3(5.95f, 0.16f, 0.92f), materials.Wood);

            references.CounterDisplays = new BakeryCounterDisplay[Enum.GetValues(typeof(RecipeId)).Length];
            for (var index = 0; index < references.CounterDisplays.Length; index++)
            {
                references.CounterDisplays[index] = CreateCounterProductDisplay(
                    counter,
                    (RecipeId)index,
                    materials);
            }

            references.CountryBread = references.CounterDisplays[0].gameObject;
            references.KaiserRolls = references.CounterDisplays[1].gameObject;
            references.Croissant = references.CounterDisplays[2].gameObject;
            references.CinnamonSwirl = references.CounterDisplays[3].gameObject;
            references.Finezja = references.CounterDisplays[4].gameObject;
            references.CinnamonMonocle = references.CounterDisplays[5].gameObject;
        }

        private static BakeryCounterDisplay CreateCounterProductDisplay(
            Transform counter,
            RecipeId recipeId,
            Materials materials)
        {
            var displayRoot = new GameObject($"Product - {recipeId}").transform;
            displayRoot.SetParent(counter, false);
            displayRoot.localPosition = new Vector3(0f, 1.62f, -0.08f);
            var servings = new Transform[8];
            for (var index = 0; index < servings.Length; index++)
            {
                var row = index / 4;
                var column = index % 4;
                var position = new Vector3(-1.95f + column * 1.3f, row * 0.27f, row == 0 ? -0.12f : 0.2f);
                var serving = CreateRecipeVisual(
                    displayRoot,
                    $"Serving {index + 1:00}",
                    recipeId,
                    materials,
                    false,
                    position,
                    0.68f);
                serving.gameObject.SetActive(false);
                servings[index] = serving;
            }

            var display = displayRoot.gameObject.AddComponent<BakeryCounterDisplay>();
            var serializedDisplay = new SerializedObject(display);
            serializedDisplay.FindProperty("recipeId").enumValueIndex = (int)recipeId;
            SetReferenceArray(serializedDisplay, "servings", servings);
            serializedDisplay.ApplyModifiedPropertiesWithoutUndo();
            return display;
        }

        private static Transform CreateIngredientDisplay(
            Transform prep,
            RecipeId recipeId,
            Materials materials)
        {
            var ingredients = new GameObject($"Raw Ingredients - {recipeId}").transform;
            ingredients.SetParent(prep, false);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Flour Sack",
                ingredients,
                new Vector3(-0.78f, 1.52f, 0.08f),
                new Vector3(0.45f, 0.68f, 0.36f),
                materials.Flour,
                Quaternion.Euler(0f, 0f, -5f));
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Mixing Bowl",
                ingredients,
                new Vector3(-0.18f, 1.4f, -0.08f),
                new Vector3(0.46f, 0.15f, 0.46f),
                materials.Metal);
            CreateRecipeVisual(
                ingredients,
                "Prepared Raw Batch",
                recipeId,
                materials,
                true,
                new Vector3(0.55f, 1.45f, -0.18f),
                0.82f);

            if (recipeId == RecipeId.ButterCroissant)
            {
                CreatePrimitive(PrimitiveType.Cube, "Butter Slab", ingredients, new Vector3(0.88f, 1.62f, 0.2f), new Vector3(0.42f, 0.12f, 0.32f), materials.Paper);
            }
            else if (recipeId == RecipeId.CinnamonSwirl || recipeId == RecipeId.CinnamonMonocle)
            {
                CreatePrimitive(PrimitiveType.Cylinder, "Cinnamon Jar", ingredients, new Vector3(0.9f, 1.65f, 0.2f), new Vector3(0.22f, 0.34f, 0.22f), materials.Cocoa);
            }
            else if (recipeId == RecipeId.Finezja)
            {
                CreatePrimitive(PrimitiveType.Cylinder, "Vanilla Cream", ingredients, new Vector3(0.82f, 1.64f, 0.18f), new Vector3(0.18f, 0.32f, 0.18f), materials.White);
                CreatePrimitive(PrimitiveType.Cylinder, "Strawberry Cream", ingredients, new Vector3(1.12f, 1.64f, 0.18f), new Vector3(0.18f, 0.32f, 0.18f), materials.Cherry);
            }
            else
            {
                CreatePrimitive(PrimitiveType.Sphere, "Egg A", ingredients, new Vector3(0.88f, 1.47f, 0.2f), new Vector3(0.2f, 0.27f, 0.2f), materials.White);
                CreatePrimitive(PrimitiveType.Sphere, "Egg B", ingredients, new Vector3(1.12f, 1.47f, 0.17f), new Vector3(0.2f, 0.27f, 0.2f), materials.White);
            }

            return ingredients;
        }

        private static Transform CreateRecipeVisual(
            Transform parent,
            string name,
            RecipeId recipeId,
            Materials materials,
            bool raw,
            Vector3 position,
            float scale)
        {
            var visual = new GameObject(name).transform;
            visual.SetParent(parent, false);
            visual.localPosition = position;
            visual.localScale = Vector3.one * scale;
            var baseMaterial = raw ? materials.Flour : materials.Crust;

            switch (recipeId)
            {
                case RecipeId.CountryBread:
                    CreatePrimitive(PrimitiveType.Sphere, raw ? "Unbaked Loaf" : "Country Loaf", visual, Vector3.zero, new Vector3(0.82f, 0.42f, 0.62f), baseMaterial);
                    if (!raw)
                    {
                        for (var score = -1; score <= 1; score++)
                        {
                            CreatePrimitive(PrimitiveType.Cube, "Flour Score", visual, new Vector3(score * 0.23f, 0.34f, -0.08f), new Vector3(0.08f, 0.025f, 0.62f), materials.Flour, Quaternion.Euler(0f, -18f, 0f));
                        }
                    }
                    break;
                case RecipeId.KaiserRoll:
                    for (var index = 0; index < 3; index++)
                    {
                        var x = (index - 1) * 0.33f;
                        CreatePrimitive(PrimitiveType.Sphere, $"Roll {index + 1}", visual, new Vector3(x, index == 1 ? 0.1f : 0f, 0f), new Vector3(0.4f, 0.25f, 0.36f), baseMaterial);
                    }

                    break;
                case RecipeId.ButterCroissant:
                    for (var index = 0; index < 5; index++)
                    {
                        var normalized = index / 4f;
                        CreatePrimitive(
                            PrimitiveType.Sphere,
                            $"Fold {index + 1}",
                            visual,
                            new Vector3(Mathf.Lerp(-0.52f, 0.52f, normalized), 0f, -Mathf.Sin(normalized * Mathf.PI) * 0.2f),
                            Vector3.one * Mathf.Lerp(0.28f, 0.42f, Mathf.Sin(normalized * Mathf.PI)),
                            baseMaterial);
                    }

                    break;
                case RecipeId.CinnamonSwirl:
                    CreatePrimitive(PrimitiveType.Cylinder, "Swirl Base", visual, Vector3.zero, new Vector3(0.66f, 0.14f, 0.66f), baseMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, "Cinnamon Centre", visual, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.03f, 0.22f), raw ? materials.Paper : materials.Cocoa);
                    break;
                case RecipeId.Finezja:
                    CreatePrimitive(PrimitiveType.Sphere, "Finezja Base", visual, Vector3.zero, new Vector3(0.76f, 0.25f, 0.48f), baseMaterial);
                    if (!raw)
                    {
                        for (var index = 0; index < 5; index++)
                        {
                            CreatePrimitive(
                                PrimitiveType.Sphere,
                                index % 2 == 0 ? "Vanilla Cream" : "Strawberry Cream",
                                visual,
                                new Vector3(-0.38f + index * 0.19f, 0.22f, -0.03f),
                                new Vector3(0.22f, 0.16f, 0.26f),
                                index % 2 == 0 ? materials.White : materials.Cherry);
                        }
                    }

                    break;
                case RecipeId.CinnamonMonocle:
                    CreatePrimitive(PrimitiveType.Cylinder, "Monocle Disc", visual, Vector3.zero, new Vector3(0.66f, 0.13f, 0.66f), baseMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, "Cinnamon Eye", visual, new Vector3(0f, 0.17f, 0f), new Vector3(0.24f, 0.025f, 0.24f), raw ? materials.Paper : materials.Cocoa);
                    break;
                case RecipeId.ChocolateMuffin:
                    for (var index = 0; index < 3; index++)
                    {
                        var x = (index - 1) * 0.42f;
                        CreatePrimitive(PrimitiveType.Cylinder, "Paper Cup", visual, new Vector3(x, -0.02f, 0f), new Vector3(0.3f, 0.22f, 0.3f), materials.Paper);
                        CreatePrimitive(PrimitiveType.Sphere, raw ? "Muffin Batter" : "Chocolate Crown", visual, new Vector3(x, 0.22f, 0f), new Vector3(0.36f, 0.25f, 0.36f), raw ? materials.Flour : materials.Cocoa);
                        if (!raw)
                        {
                            CreatePrimitive(PrimitiveType.Sphere, "Chocolate Chip", visual, new Vector3(x - 0.08f, 0.39f, -0.08f), Vector3.one * 0.08f, materials.Glow);
                            CreatePrimitive(PrimitiveType.Sphere, "Chocolate Chip", visual, new Vector3(x + 0.1f, 0.35f, 0.04f), Vector3.one * 0.07f, materials.Cocoa);
                        }
                    }
                    break;
                case RecipeId.JamTurnover:
                    for (var index = 0; index < 2; index++)
                    {
                        var x = (index - 0.5f) * 0.7f;
                        CreatePrimitive(PrimitiveType.Cube, "Folded Pastry", visual, new Vector3(x, 0f, 0f), new Vector3(0.58f, 0.22f, 0.5f), baseMaterial, Quaternion.Euler(0f, 45f, 0f));
                        CreatePrimitive(PrimitiveType.Sphere, "Jam Seam", visual, new Vector3(x, 0.14f, -0.18f), new Vector3(0.28f, 0.07f, 0.08f), raw ? materials.Paper : materials.Cherry);
                    }
                    break;
                case RecipeId.ChocolatePillow:
                    for (var index = 0; index < 2; index++)
                    {
                        var x = (index - 0.5f) * 0.72f;
                        CreatePrimitive(PrimitiveType.Cube, "Flaky Pillow", visual, new Vector3(x, 0f, 0f), new Vector3(0.62f, 0.3f, 0.54f), baseMaterial, Quaternion.Euler(0f, index == 0 ? -4f : 5f, 0f));
                        CreatePrimitive(PrimitiveType.Cube, "Chocolate Window", visual, new Vector3(x, 0.17f, -0.16f), new Vector3(0.25f, 0.04f, 0.18f), raw ? materials.Paper : materials.Cocoa);
                    }
                    break;
            }

            return visual;
        }

        private static Transform BuildHangingBell(Transform truck, Materials materials)
        {
            var bell = new GameObject("Hanging Service Bell").transform;
            bell.SetParent(truck, false);
            bell.localPosition = new Vector3(4.08f, 3.42f, -2.45f);
            CreatePrimitive(PrimitiveType.Cylinder, "Bell Shade", bell, Vector3.zero, new Vector3(0.24f, 0.28f, 0.24f), materials.Crust);
            CreatePrimitive(PrimitiveType.Sphere, "Bell Clapper", bell, new Vector3(0f, -0.3f, 0f), Vector3.one * 0.13f, materials.Cocoa);
            return bell;
        }

        private static GameObject BuildCabinUpgrade(Transform truck, Materials materials)
        {
            var cabin = new GameObject("Bakery Level 2 - Wooden Home").transform;
            cabin.SetParent(truck, false);

            CreatePrimitive(PrimitiveType.Cube, "Left Timber", cabin, new Vector3(-4.45f, 2.35f, -1.98f), new Vector3(0.28f, 3.9f, 0.28f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Right Timber", cabin, new Vector3(4.45f, 2.35f, -1.98f), new Vector3(0.28f, 3.9f, 0.28f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Front Beam", cabin, new Vector3(0f, 4.02f, -2.02f), new Vector3(9.25f, 0.28f, 0.32f), materials.Wood);
            CreatePrimitive(PrimitiveType.Cube, "Window Shelf", cabin, new Vector3(0f, 1.54f, -2.02f), new Vector3(6.9f, 0.2f, 0.48f), materials.Wood);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Glowing Bakery Sign",
                cabin,
                new Vector3(0f, 4.48f, -1.98f),
                new Vector3(5.5f, 0.74f, 0.15f),
                materials.Cocoa);
            var signTextObject = new GameObject("Baka-Bake-Bakery Lettering");
            signTextObject.transform.SetParent(cabin, false);
            signTextObject.transform.localPosition = new Vector3(0f, 4.48f, -2.08f);
            signTextObject.transform.localScale = Vector3.one * 0.12f;
            var signText = signTextObject.AddComponent<TextMesh>();
            signText.text = "BAKA-BAKE-BAKERY";
            signText.anchor = TextAnchor.MiddleCenter;
            signText.alignment = TextAlignment.Center;
            signText.fontSize = 52;
            signText.characterSize = 0.72f;
            signText.color = Hex("FFD08A");
            signText.fontStyle = FontStyle.Bold;

            for (var index = 0; index < 9; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    $"Sign Bulb {index:00}",
                    cabin,
                    new Vector3(-2.35f + index * 0.59f, 4.14f, -2.12f),
                    new Vector3(0.1f, 0.1f, 0.08f),
                    materials.Glow);
            }

            CreatePrimitive(PrimitiveType.Cylinder, "Herb Pot", cabin, new Vector3(-3.65f, 1.73f, -2.16f), new Vector3(0.35f, 0.28f, 0.35f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Sphere, "Herb Leaves", cabin, new Vector3(-3.65f, 2.06f, -2.16f), new Vector3(0.48f, 0.42f, 0.38f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cylinder, "Herb Pot", cabin, new Vector3(3.65f, 1.73f, -2.16f), new Vector3(0.35f, 0.28f, 0.35f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Sphere, "Herb Leaves", cabin, new Vector3(3.65f, 2.06f, -2.16f), new Vector3(0.48f, 0.42f, 0.38f), materials.Sage);

            cabin.gameObject.SetActive(false);
            return cabin.gameObject;
        }

        private static GameObject BuildGoldenMinuteLight(Transform truck)
        {
            var lightObject = new GameObject("Neighbourhood Warmth Light");
            lightObject.transform.SetParent(truck, false);
            lightObject.transform.localPosition = new Vector3(0f, 3.4f, -2.7f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Hex("FFD08A");
            light.intensity = 5.8f;
            light.range = 10f;
            light.shadows = LightShadows.None;
            lightObject.SetActive(false);
            return lightObject;
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

        private static CharacterReferences BuildCharacters(Transform parent, Materials materials)
        {
            var characters = new GameObject("Characters").transform;
            characters.SetParent(parent, false);

            var idleStation = CreateStation(characters, "Baker Station - Idle", new Vector3(-0.1f, 0.72f, -0.62f));
            var fridgeStation = CreateStation(characters, "Baker Station - Fridge", new Vector3(-3.25f, 0.72f, 0.02f));
            var prepStation = CreateStation(characters, "Baker Station - Preparation", new Vector3(2.15f, 0.72f, 0.08f));
            var ovenStation = CreateStation(characters, "Baker Station - Oven", new Vector3(-2.22f, 0.72f, -0.02f));
            var counterStation = CreateStation(characters, "Baker Station - Counter", new Vector3(1.15f, 0.72f, -0.62f));
            var references = CreateBaker(
                characters,
                idleStation,
                fridgeStation,
                prepStation,
                ovenStation,
                counterStation,
                materials);
            var serviceStation = CreateStation(characters, "Customer Station - Service", new Vector3(4.65f, 0.5f, -2.9f));
            var queueStation = CreateStation(characters, "Customer Station - Queue", new Vector3(3.35f, 0.5f, -2.72f));
            var grandmotherEntrance = CreateStation(characters, "Mrs Rose - Entrance", new Vector3(7.2f, 0.5f, -2.72f));
            var grandmotherExit = CreateStation(characters, "Mrs Rose - Exit", new Vector3(7.45f, 0.5f, -3.15f));
            var neighbourEntrance = CreateStation(characters, "Neighbour - Entrance", new Vector3(-7.1f, 0.5f, -2.62f));
            var neighbourExit = CreateStation(characters, "Neighbour - Exit", new Vector3(-7.4f, 0.5f, -3.05f));
            references.Customers = new[]
            {
                CreateGrandmother(
                    characters,
                    grandmotherEntrance,
                    serviceStation,
                    queueStation,
                    grandmotherExit,
                    materials),
                CreateNeighbour(
                    characters,
                    neighbourEntrance,
                    serviceStation,
                    queueStation,
                    neighbourExit,
                    materials)
            };
            CreateFriend(characters, materials);
            return references;
        }

        private static void CreateFriend(Transform parent, Materials materials)
        {
            var mila = new GameObject("Friend - Mila").transform;
            mila.SetParent(parent, false);
            mila.localPosition = new Vector3(-5.65f, 0.5f, -2.35f);
            mila.localRotation = Quaternion.Euler(0f, 18f, 0f);

            CreatePrimitive(PrimitiveType.Capsule, "Mila Coat", mila, new Vector3(0f, 0.88f, 0f), new Vector3(0.76f, 0.82f, 0.68f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Mila Dress Panel", mila, new Vector3(0f, 0.82f, -0.36f), new Vector3(0.62f, 0.88f, 0.06f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Cube, "Satchel Strap", mila, new Vector3(0f, 1.16f, -0.42f), new Vector3(0.08f, 1.15f, 0.04f), materials.Cocoa, Quaternion.Euler(0f, 0f, -24f));
            CreatePrimitive(PrimitiveType.Cube, "Recipe Satchel", mila, new Vector3(0.48f, 0.72f, -0.22f), new Vector3(0.48f, 0.52f, 0.2f), materials.Wood);
            CreateLimb(mila, "Mila Leg Left", new Vector3(-0.2f, 0.5f, 0f), new Vector3(-0.2f, 0.05f, -0.03f), 0.19f, materials.Cocoa);
            CreateLimb(mila, "Mila Leg Right", new Vector3(0.2f, 0.5f, 0f), new Vector3(0.2f, 0.05f, 0.03f), 0.19f, materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Shoe Left", mila, new Vector3(-0.2f, 0.03f, -0.14f), new Vector3(0.27f, 0.13f, 0.4f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Shoe Right", mila, new Vector3(0.2f, 0.03f, -0.14f), new Vector3(0.27f, 0.13f, 0.4f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Head", mila, new Vector3(0f, 2.02f, 0f), new Vector3(0.74f, 0.76f, 0.69f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Hair Cap", mila, new Vector3(0f, 2.28f, 0.14f), new Vector3(0.82f, 0.5f, 0.74f), materials.Hair);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Bun", mila, new Vector3(-0.42f, 2.34f, 0.14f), Vector3.one * 0.38f, materials.Hair);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Fringe Left", mila, new Vector3(-0.22f, 2.23f, -0.25f), new Vector3(0.24f, 0.3f, 0.14f), materials.Hair);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Fringe Right", mila, new Vector3(0.2f, 2.25f, -0.25f), new Vector3(0.22f, 0.27f, 0.14f), materials.Hair);
            AddFaceDetails(mila, new Vector3(0f, 2.02f, 0f), materials);
            var leftArm = CreateLimb(mila, "Mila Arm Left", new Vector3(-0.35f, 1.4f, 0f), new Vector3(-0.64f, 1.04f, -0.38f), 0.21f, materials.Sage);
            var rightArm = CreateLimb(mila, "Mila Arm Right", new Vector3(0.35f, 1.4f, 0f), new Vector3(0.64f, 1.18f, -0.4f), 0.21f, materials.Sage);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Hand Left", mila, leftArm.localPosition + new Vector3(-0.2f, -0.18f, -0.18f), Vector3.one * 0.18f, materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Mila Hand Right", mila, rightArm.localPosition + new Vector3(0.2f, -0.12f, -0.18f), Vector3.one * 0.18f, materials.Skin);
        }

        private static void AddFaceDetails(Transform visual, Vector3 headPosition, Materials materials)
        {
            CreatePrimitive(PrimitiveType.Sphere, "Ear Left", visual, headPosition + new Vector3(-0.38f, 0f, 0f), new Vector3(0.13f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Right", visual, headPosition + new Vector3(0.38f, 0f, 0f), new Vector3(0.13f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left Detail", visual, headPosition + new Vector3(-0.17f, 0.05f, -0.35f), new Vector3(0.085f, 0.1f, 0.06f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right Detail", visual, headPosition + new Vector3(0.17f, 0.05f, -0.35f), new Vector3(0.085f, 0.1f, 0.06f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Brow Left", visual, headPosition + new Vector3(-0.17f, 0.19f, -0.34f), new Vector3(0.18f, 0.025f, 0.025f), materials.Hair, Quaternion.Euler(0f, 0f, -7f));
            CreatePrimitive(PrimitiveType.Cube, "Brow Right", visual, headPosition + new Vector3(0.17f, 0.19f, -0.34f), new Vector3(0.18f, 0.025f, 0.025f), materials.Hair, Quaternion.Euler(0f, 0f, 7f));
            CreatePrimitive(PrimitiveType.Sphere, "Nose Detail", visual, headPosition + new Vector3(0f, -0.07f, -0.4f), new Vector3(0.1f, 0.13f, 0.1f), materials.Skin);
            CreatePrimitive(PrimitiveType.Cube, "Smile Detail", visual, headPosition + new Vector3(0f, -0.2f, -0.37f), new Vector3(0.2f, 0.035f, 0.035f), materials.Cocoa, Quaternion.Euler(0f, 0f, -2f));
            CreatePrimitive(PrimitiveType.Sphere, "Cheek Left Detail", visual, headPosition + new Vector3(-0.27f, -0.12f, -0.33f), new Vector3(0.1f, 0.055f, 0.035f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Sphere, "Cheek Right Detail", visual, headPosition + new Vector3(0.27f, -0.12f, -0.33f), new Vector3(0.1f, 0.055f, 0.035f), materials.Cherry);
        }

        private static Transform CreateStation(Transform parent, string name, Vector3 position)
        {
            var station = new GameObject(name).transform;
            station.SetParent(parent, false);
            station.localPosition = position;
            return station;
        }

        private static CharacterReferences CreateBaker(
            Transform parent,
            Transform idleStation,
            Transform fridgeStation,
            Transform prepStation,
            Transform ovenStation,
            Transform counterStation,
            Materials materials)
        {
            var baker = new GameObject("Baker - Manual Worker").transform;
            baker.SetParent(parent, false);
            baker.localPosition = idleStation.localPosition;
            baker.localRotation = Quaternion.Euler(0f, -18f, 0f);

            var visual = new GameObject("Baker Visual").transform;
            visual.SetParent(baker, false);
            CreatePrimitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.88f, 0f), new Vector3(0.72f, 0.76f, 0.62f), materials.Cloth);
            CreatePrimitive(PrimitiveType.Cube, "Apron", visual, new Vector3(0f, 0.9f, -0.36f), new Vector3(0.68f, 0.94f, 0.07f), materials.Sage);
            CreatePrimitive(PrimitiveType.Cube, "Apron Pocket", visual, new Vector3(0f, 0.78f, -0.41f), new Vector3(0.32f, 0.2f, 0.035f), materials.Flour);
            var leftLeg = CreateLimb(visual, "Leg Left", new Vector3(-0.22f, 0.48f, 0f), new Vector3(-0.22f, 0.05f, -0.02f), 0.2f, materials.Cocoa);
            var rightLeg = CreateLimb(visual, "Leg Right", new Vector3(0.22f, 0.48f, 0f), new Vector3(0.22f, 0.05f, 0.02f), 0.2f, materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Left", visual, new Vector3(-0.22f, 0.03f, -0.13f), new Vector3(0.28f, 0.14f, 0.4f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Right", visual, new Vector3(0.22f, 0.03f, -0.13f), new Vector3(0.28f, 0.14f, 0.4f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 2.02f, 0f), new Vector3(0.72f, 0.74f, 0.68f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair", visual, new Vector3(0f, 2.28f, 0.14f), new Vector3(0.78f, 0.46f, 0.7f), materials.Hair);
            CreatePrimitive(PrimitiveType.Cylinder, "Hat Band", visual, new Vector3(0f, 2.5f, 0f), new Vector3(0.74f, 0.13f, 0.74f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Chef Hat", visual, new Vector3(0f, 2.7f, 0f), new Vector3(0.88f, 0.42f, 0.78f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left", visual, new Vector3(-0.17f, 2.06f, -0.34f), new Vector3(0.085f, 0.1f, 0.065f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right", visual, new Vector3(0.17f, 2.06f, -0.34f), new Vector3(0.085f, 0.1f, 0.065f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Nose", visual, new Vector3(0f, 1.94f, -0.39f), new Vector3(0.11f, 0.13f, 0.11f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Left", visual, new Vector3(-0.38f, 2.02f, 0f), new Vector3(0.13f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Right", visual, new Vector3(0.38f, 2.02f, 0f), new Vector3(0.13f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Cube, "Brow Left", visual, new Vector3(-0.17f, 2.2f, -0.34f), new Vector3(0.18f, 0.025f, 0.025f), materials.Hair, Quaternion.Euler(0f, 0f, -6f));
            CreatePrimitive(PrimitiveType.Cube, "Brow Right", visual, new Vector3(0.17f, 2.2f, -0.34f), new Vector3(0.18f, 0.025f, 0.025f), materials.Hair, Quaternion.Euler(0f, 0f, 6f));
            CreatePrimitive(PrimitiveType.Sphere, "Cheek Left", visual, new Vector3(-0.27f, 1.9f, -0.32f), new Vector3(0.12f, 0.08f, 0.04f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Sphere, "Cheek Right", visual, new Vector3(0.27f, 1.9f, -0.32f), new Vector3(0.12f, 0.08f, 0.04f), materials.Cherry);
            CreatePrimitive(PrimitiveType.Cube, "Smile", visual, new Vector3(0f, 1.82f, -0.37f), new Vector3(0.2f, 0.035f, 0.035f), materials.Cocoa);
            var leftArm = CreateLimb(visual, "Arm Left", new Vector3(-0.34f, 1.38f, -0.02f), new Vector3(-0.72f, 1.0f, -0.42f), 0.22f, materials.Cloth);
            var rightArm = CreateLimb(visual, "Arm Right", new Vector3(0.34f, 1.38f, -0.02f), new Vector3(0.78f, 1.13f, -0.44f), 0.22f, materials.Cloth);
            CreatePrimitive(PrimitiveType.Sphere, "Hand Left", visual, new Vector3(-0.72f, 1.0f, -0.42f), new Vector3(0.2f, 0.2f, 0.2f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hand Right", visual, new Vector3(0.78f, 1.13f, -0.44f), new Vector3(0.2f, 0.2f, 0.2f), materials.Skin);
            for (var finger = -1; finger <= 1; finger++)
            {
                CreatePrimitive(PrimitiveType.Capsule, "Finger Left", visual, new Vector3(-0.72f + finger * 0.06f, 0.89f, -0.46f), new Vector3(0.035f, 0.1f, 0.035f), materials.Skin);
                CreatePrimitive(PrimitiveType.Capsule, "Finger Right", visual, new Vector3(0.78f + finger * 0.06f, 1.02f, -0.48f), new Vector3(0.035f, 0.1f, 0.035f), materials.Skin);
            }

            var recipeCount = Enum.GetValues(typeof(RecipeId)).Length;
            var rawCarryDisplays = new GameObject[recipeCount];
            var bakedCarryDisplays = new GameObject[recipeCount];
            for (var index = 0; index < recipeCount; index++)
            {
                var recipeId = (RecipeId)index;
                rawCarryDisplays[index] = CreateRecipeVisual(
                    visual,
                    $"Carried Raw - {recipeId}",
                    recipeId,
                    materials,
                    true,
                    new Vector3(0.5f, 1.28f, -0.58f),
                    0.46f).gameObject;
                bakedCarryDisplays[index] = CreateRecipeVisual(
                    visual,
                    $"Carried Baked - {recipeId}",
                    recipeId,
                    materials,
                    false,
                    new Vector3(0.5f, 1.28f, -0.58f),
                    0.46f).gameObject;
                rawCarryDisplays[index].SetActive(false);
                bakedCarryDisplays[index].SetActive(false);
            }

            var hitTarget = baker.gameObject.AddComponent<CapsuleCollider>();
            hitTarget.center = new Vector3(0f, 1.35f, 0f);
            hitTarget.height = 3.1f;
            hitTarget.radius = 0.7f;
            var worker = baker.gameObject.AddComponent<BakeryWorkerView>();
            var serializedWorker = new SerializedObject(worker);
            serializedWorker.FindProperty("visualRoot").objectReferenceValue = visual;
            serializedWorker.FindProperty("idleStation").objectReferenceValue = idleStation;
            serializedWorker.FindProperty("fridgeStation").objectReferenceValue = fridgeStation;
            serializedWorker.FindProperty("prepStation").objectReferenceValue = prepStation;
            serializedWorker.FindProperty("ovenStation").objectReferenceValue = ovenStation;
            serializedWorker.FindProperty("counterStation").objectReferenceValue = counterStation;
            serializedWorker.FindProperty("leftLeg").objectReferenceValue = leftLeg;
            serializedWorker.FindProperty("rightLeg").objectReferenceValue = rightLeg;
            serializedWorker.FindProperty("leftArm").objectReferenceValue = leftArm;
            serializedWorker.FindProperty("rightArm").objectReferenceValue = rightArm;
            SetReferenceArray(serializedWorker, "rawCarryDisplays", rawCarryDisplays);
            SetReferenceArray(serializedWorker, "bakedCarryDisplays", bakedCarryDisplays);
            serializedWorker.ApplyModifiedPropertiesWithoutUndo();

            return new CharacterReferences
            {
                Worker = worker,
                BakerHitTarget = hitTarget
            };
        }

        private static BakeryCustomerActor CreateGrandmother(
            Transform parent,
            Transform entranceStation,
            Transform serviceStation,
            Transform queueStation,
            Transform exitStation,
            Materials materials)
        {
            var customer = new GameObject("Customer - Grandmother").transform;
            customer.SetParent(parent, false);
            customer.localPosition = entranceStation.localPosition;
            customer.localRotation = Quaternion.Euler(0f, -58f, 0f);

            var visual = new GameObject("Mrs Rose Visual").transform;
            visual.SetParent(customer, false);
            CreatePrimitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.82f, 0f), new Vector3(0.8f, 0.78f, 0.7f), materials.Cherry);
            var leftLeg = CreateLimb(visual, "Leg Left", new Vector3(-0.2f, 0.5f, 0f), new Vector3(-0.2f, 0.06f, 0f), 0.19f, materials.Cocoa);
            var rightLeg = CreateLimb(visual, "Leg Right", new Vector3(0.2f, 0.5f, 0f), new Vector3(0.2f, 0.06f, 0f), 0.19f, materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.95f, 0f), new Vector3(0.76f, 0.73f, 0.7f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair Cap", visual, new Vector3(0f, 2.18f, 0.12f), new Vector3(0.8f, 0.48f, 0.72f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Hair Bun", visual, new Vector3(0.42f, 2.2f, 0.16f), new Vector3(0.42f, 0.42f, 0.42f), materials.White);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left", visual, new Vector3(-0.17f, 2.0f, -0.34f), new Vector3(0.09f, 0.1f, 0.07f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right", visual, new Vector3(0.17f, 2.0f, -0.34f), new Vector3(0.09f, 0.1f, 0.07f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Nose", visual, new Vector3(0f, 1.9f, -0.39f), new Vector3(0.12f, 0.14f, 0.11f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Left", visual, new Vector3(-0.4f, 1.96f, 0f), new Vector3(0.14f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Right", visual, new Vector3(0.4f, 1.96f, 0f), new Vector3(0.14f, 0.19f, 0.12f), materials.Skin);
            CreatePrimitive(PrimitiveType.Cube, "Kind Smile", visual, new Vector3(0f, 1.79f, -0.37f), new Vector3(0.2f, 0.035f, 0.035f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Glasses Bridge", visual, new Vector3(0f, 2.03f, -0.41f), new Vector3(0.18f, 0.035f, 0.035f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cylinder, "Glasses Left", visual, new Vector3(-0.2f, 2.03f, -0.4f), new Vector3(0.17f, 0.025f, 0.17f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cylinder, "Glasses Right", visual, new Vector3(0.2f, 2.03f, -0.4f), new Vector3(0.17f, 0.025f, 0.17f), materials.Cocoa, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cube, "Collar", visual, new Vector3(0f, 1.46f, -0.35f), new Vector3(0.62f, 0.18f, 0.08f), materials.Flour);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Left", visual, new Vector3(-0.2f, 0.03f, -0.13f), new Vector3(0.27f, 0.13f, 0.38f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Right", visual, new Vector3(0.2f, 0.03f, -0.13f), new Vector3(0.27f, 0.13f, 0.38f), materials.Cocoa);
            var leftArm = CreateLimb(visual, "Arm Left", new Vector3(-0.38f, 1.35f, -0.02f), new Vector3(-0.27f, 0.95f, -0.42f), 0.22f, materials.Cherry);
            var rightArm = CreateLimb(visual, "Arm Right", new Vector3(0.38f, 1.35f, -0.02f), new Vector3(0.25f, 0.95f, -0.42f), 0.22f, materials.Cherry);
            CreatePrimitive(PrimitiveType.Cylinder, "Walking Cane", visual, new Vector3(0.58f, 0.7f, -0.28f), new Vector3(0.08f, 0.72f, 0.08f), materials.Wood);
            var parcel = CreatePurchaseParcel(visual, materials);
            return ConfigureCustomerActor(
                customer,
                visual,
                entranceStation,
                serviceStation,
                queueStation,
                exitStation,
                leftLeg,
                rightLeg,
                leftArm,
                rightArm,
                parcel);
        }

        private static BakeryCustomerActor CreateNeighbour(
            Transform parent,
            Transform entranceStation,
            Transform serviceStation,
            Transform queueStation,
            Transform exitStation,
            Materials materials)
        {
            var neighbour = new GameObject("Customer - Neighbour").transform;
            neighbour.SetParent(parent, false);
            neighbour.localPosition = entranceStation.localPosition;
            neighbour.localRotation = Quaternion.Euler(0f, 52f, 0f);

            var visual = new GameObject("Neighbour Visual").transform;
            visual.SetParent(neighbour, false);
            CreatePrimitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.82f, 0f), new Vector3(0.76f, 0.76f, 0.66f), materials.EveningBlue);
            var leftLeg = CreateLimb(visual, "Leg Left", new Vector3(-0.2f, 0.5f, 0f), new Vector3(-0.2f, 0.06f, 0f), 0.19f, materials.Cocoa);
            var rightLeg = CreateLimb(visual, "Leg Right", new Vector3(0.2f, 0.5f, 0f), new Vector3(0.2f, 0.06f, 0f), 0.19f, materials.Cocoa);
            CreatePrimitive(PrimitiveType.Cube, "Jacket Panel", visual, new Vector3(0f, 0.88f, -0.38f), new Vector3(0.6f, 0.78f, 0.06f), materials.Sage);
            CreatePrimitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.95f, 0f), new Vector3(0.72f, 0.72f, 0.68f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair", visual, new Vector3(0f, 2.18f, 0.13f), new Vector3(0.75f, 0.42f, 0.7f), materials.Hair);
            CreatePrimitive(PrimitiveType.Cylinder, "Cap", visual, new Vector3(0f, 2.38f, -0.02f), new Vector3(0.72f, 0.12f, 0.72f), materials.Crust);
            CreatePrimitive(PrimitiveType.Cube, "Cap Peak", visual, new Vector3(0f, 2.34f, -0.42f), new Vector3(0.52f, 0.08f, 0.32f), materials.Crust);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Left", visual, new Vector3(-0.17f, 2.0f, -0.34f), new Vector3(0.085f, 0.1f, 0.065f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Eye Right", visual, new Vector3(0.17f, 2.0f, -0.34f), new Vector3(0.085f, 0.1f, 0.065f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Nose", visual, new Vector3(0f, 1.88f, -0.39f), new Vector3(0.11f, 0.13f, 0.11f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Left", visual, new Vector3(-0.38f, 1.95f, 0f), new Vector3(0.13f, 0.18f, 0.11f), materials.Skin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear Right", visual, new Vector3(0.38f, 1.95f, 0f), new Vector3(0.13f, 0.18f, 0.11f), materials.Skin);
            CreatePrimitive(PrimitiveType.Cube, "Smile", visual, new Vector3(0f, 1.77f, -0.37f), new Vector3(0.19f, 0.032f, 0.032f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Jacket Button A", visual, new Vector3(0f, 1.08f, -0.43f), Vector3.one * 0.07f, materials.Crust);
            CreatePrimitive(PrimitiveType.Sphere, "Jacket Button B", visual, new Vector3(0f, 0.86f, -0.43f), Vector3.one * 0.07f, materials.Crust);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Left", visual, new Vector3(-0.2f, 0.03f, -0.13f), new Vector3(0.27f, 0.13f, 0.38f), materials.Cocoa);
            CreatePrimitive(PrimitiveType.Sphere, "Shoe Right", visual, new Vector3(0.2f, 0.03f, -0.13f), new Vector3(0.27f, 0.13f, 0.38f), materials.Cocoa);
            var leftArm = CreateLimb(visual, "Arm Left", new Vector3(-0.36f, 1.34f, 0f), new Vector3(-0.62f, 0.94f, -0.34f), 0.21f, materials.EveningBlue);
            var rightArm = CreateLimb(visual, "Arm Right", new Vector3(0.36f, 1.34f, 0f), new Vector3(0.55f, 1.02f, -0.38f), 0.21f, materials.EveningBlue);
            CreatePrimitive(PrimitiveType.Cube, "Bread Tote", visual, new Vector3(-0.62f, 0.72f, -0.27f), new Vector3(0.52f, 0.62f, 0.2f), materials.Cloth);
            var parcel = CreatePurchaseParcel(visual, materials);
            return ConfigureCustomerActor(
                neighbour,
                visual,
                entranceStation,
                serviceStation,
                queueStation,
                exitStation,
                leftLeg,
                rightLeg,
                leftArm,
                rightArm,
                parcel);
        }

        private static GameObject CreatePurchaseParcel(Transform visual, Materials materials)
        {
            var parcel = new GameObject("Fresh Purchase Parcel").transform;
            parcel.SetParent(visual, false);
            parcel.localPosition = new Vector3(0.5f, 0.92f, -0.52f);
            CreatePrimitive(PrimitiveType.Cube, "Paper Bag", parcel, Vector3.zero, new Vector3(0.46f, 0.58f, 0.24f), materials.Paper);
            CreatePrimitive(PrimitiveType.Sphere, "Warm Bake", parcel, new Vector3(0f, 0.34f, 0f), new Vector3(0.36f, 0.18f, 0.28f), materials.Crust);
            parcel.gameObject.SetActive(false);
            return parcel.gameObject;
        }

        private static BakeryCustomerActor ConfigureCustomerActor(
            Transform actorRoot,
            Transform visualRoot,
            Transform entranceStation,
            Transform serviceStation,
            Transform queueStation,
            Transform exitStation,
            Transform leftLeg,
            Transform rightLeg,
            Transform leftArm,
            Transform rightArm,
            GameObject parcel)
        {
            var actor = actorRoot.gameObject.AddComponent<BakeryCustomerActor>();
            var serializedActor = new SerializedObject(actor);
            SetReference(serializedActor, "visualRoot", visualRoot);
            SetReference(serializedActor, "entranceStation", entranceStation);
            SetReference(serializedActor, "serviceStation", serviceStation);
            SetReference(serializedActor, "queueStation", queueStation);
            SetReference(serializedActor, "exitStation", exitStation);
            SetReference(serializedActor, "leftLeg", leftLeg);
            SetReference(serializedActor, "rightLeg", rightLeg);
            SetReference(serializedActor, "leftArm", leftArm);
            SetReference(serializedActor, "rightArm", rightArm);
            SetReference(serializedActor, "purchaseParcel", parcel);
            serializedActor.ApplyModifiedPropertiesWithoutUndo();
            return actor;
        }

        private static Transform CreateLimb(
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
            return limb.transform;
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
            sun.color = Hex("C9DCFF");
            sun.intensity = 0.96f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;

            var fillObject = new GameObject("Evening Fill");
            fillObject.transform.SetParent(lighting, false);
            fillObject.transform.localRotation = Quaternion.Euler(35f, 145f, 0f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Hex("6E94C3");
            fill.intensity = 0.44f;
            fill.shadows = LightShadows.None;

            var cameraFillObject = new GameObject("Camera Soft Fill");
            cameraFillObject.transform.SetParent(lighting, false);
            cameraFillObject.transform.localRotation = Quaternion.LookRotation(new Vector3(-7.5f, -4.5f, 18f).normalized);
            var cameraFill = cameraFillObject.AddComponent<Light>();
            cameraFill.type = LightType.Directional;
            cameraFill.color = Hex("FFF2DE");
            cameraFill.intensity = 1.08f;
            cameraFill.shadows = LightShadows.None;

            var interiorObject = new GameObject("Truck Interior Fill");
            interiorObject.transform.SetParent(lighting, false);
            interiorObject.transform.localPosition = new Vector3(0f, 3.15f, -1.25f);
            var interior = interiorObject.AddComponent<Light>();
            interior.type = LightType.Point;
            interior.color = Hex("FFE0B6");
            interior.intensity = 3.6f;
            interior.range = 10f;
            interior.shadows = LightShadows.Soft;
            interior.shadowStrength = 0.58f;

            var counterObject = new GameObject("Counter Honey Light");
            counterObject.transform.SetParent(lighting, false);
            counterObject.transform.localPosition = new Vector3(2.2f, 3.5f, -2.2f);
            counterObject.transform.localRotation = Quaternion.Euler(58f, 180f, 0f);
            var counter = counterObject.AddComponent<Light>();
            counter.type = LightType.Spot;
            counter.color = Hex("FFC879");
            counter.intensity = 1.55f;
            counter.range = 5.2f;
            counter.spotAngle = 54f;
            counter.shadows = LightShadows.Soft;
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
            camera.backgroundColor = Hex("16243A");
            camera.allowHDR = true;
            camera.allowMSAA = true;

            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void BuildGameplayController(
            GameObject root,
            Camera camera,
            WorldReferences world,
            CharacterReferences characters)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>($"{DataRoot}/BakeryCatalog.asset");
            if (catalog == null)
            {
                throw new InvalidOperationException("Bakery catalog was not available while building gameplay.");
            }

            var worldView = root.AddComponent<BakeryWorldView>();
            var serializedWorldView = new SerializedObject(worldView);
            SetReferenceArray(serializedWorldView, "ingredientDisplays", world.IngredientDisplays);
            SetReferenceArray(serializedWorldView, "ovenRawDisplays", world.OvenRawDisplays);
            SetReferenceArray(serializedWorldView, "ovenBakedDisplays", world.OvenBakedDisplays);
            SetReferenceArray(serializedWorldView, "counterDisplays", world.CounterDisplays);
            SetReference(serializedWorldView, "fridgeDoor", world.FridgeDoor);
            SetReference(serializedWorldView, "ovenDoor", world.OvenDoor);
            SetReference(serializedWorldView, "ovenGlow", world.OvenGlow);
            SetReferenceArray(serializedWorldView, "steamPuffs", world.SteamPuffs);
            SetReference(serializedWorldView, "hangingBell", world.HangingBell);
            SetReferenceArray(serializedWorldView, "customers", characters.Customers);
            serializedWorldView.ApplyModifiedPropertiesWithoutUndo();

            var controller = root.AddComponent<BakeryGameController>();
            var serializedController = new SerializedObject(controller);
            SetReference(serializedController, "catalog", catalog);
            SetReference(serializedController, "worker", characters.Worker);
            SetReference(serializedController, "worldView", worldView);
            SetReference(serializedController, "interactionCamera", camera);
            SetReference(serializedController, "bakerHitTarget", characters.BakerHitTarget);
            SetReference(serializedController, "lockedOvenBay", world.LockedOvenBay);
            SetReference(serializedController, "secondOvenVisual", world.SecondOven);
            SetReference(serializedController, "cabinUpgradeVisual", world.CabinUpgrade);
            SetReference(serializedController, "goldenMinuteLight", world.GoldenMinuteLight);
            SetReference(serializedController, "countryBreadDisplay", world.CountryBread);
            SetReference(serializedController, "kaiserRollDisplay", world.KaiserRolls);
            SetReference(serializedController, "croissantDisplay", world.Croissant);
            SetReference(serializedController, "cinnamonSwirlDisplay", world.CinnamonSwirl);
            SetReference(serializedController, "finezjaDisplay", world.Finezja);
            SetReference(serializedController, "cinnamonMonocleDisplay", world.CinnamonMonocle);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            }

            if (value == null)
            {
                throw new InvalidOperationException($"Gameplay reference '{propertyName}' was not built.");
            }

            property.objectReferenceValue = value;
        }

        private static void SetReferenceArray<T>(
            SerializedObject serializedObject,
            string propertyName,
            IReadOnlyList<T> values)
            where T : Object
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized array '{propertyName}'.");
            }

            if (values == null)
            {
                throw new InvalidOperationException($"Gameplay array '{propertyName}' was not built.");
            }

            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] == null)
                {
                    throw new InvalidOperationException($"Gameplay array '{propertyName}' contains a missing reference at {index}.");
                }

                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
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

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
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
