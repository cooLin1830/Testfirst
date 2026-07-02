using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DimensionShift.PetsLike
{
    public static class PetsPrototypeFactory
    {
        private const string Player2DVisualPrefabPath = "Assets/Art/2d/player/Player2DVisual.prefab";
        private const string Player25DCharacterPath = "Assets/Art/3D/character.fbx";

        public static GameObject Build(PetsPrototypeLevelKind levelKind)
        {
            return Build(levelKind, null);
        }

        public static GameObject Build(PetsPrototypeLevelKind levelKind, PetsEditableLevelAsset editableLevel)
        {
            if (levelKind == PetsPrototypeLevelKind.Tutorial)
            {
                return BuildTutorialLevel();
            }

            if (levelKind == PetsPrototypeLevelKind.EditableAsset)
            {
                return BuildEditableLevel(editableLevel);
            }

            return BuildMechanicTestRoom();
        }

        public static GameObject BuildMechanicTestRoom()
        {
            return BuildLevel("PETS-like Mechanic Test Room", CreateTestRoomDefinition(), false);
        }

        public static GameObject BuildTutorialLevel()
        {
            return BuildLevel("PETS-like Tutorial Level", CreateTutorialDefinition(), true);
        }

        public static GameObject BuildEditableLevel(PetsEditableLevelAsset editableLevel)
        {
            PetsLevelDefinition definition = editableLevel != null
                ? editableLevel.ToLevelDefinition()
                : CreateTestRoomDefinition();
            return BuildLevel("PETS-like Editable Level", definition, false);
        }

        private static GameObject BuildLevel(string rootName, PetsLevelDefinition definition, bool addTutorialMarkers)
        {
            GameObject root = new GameObject(rootName);

            PetsModeManager modeManager = new GameObject("PETS Perspective Mode Manager").AddComponent<PetsModeManager>();
            modeManager.transform.SetParent(root.transform);

            CreateLighting(root.transform);

            Material whiteMaterial = CreateMaterial("Paper White", new Color(0.98f, 0.98f, 0.96f));
            Material blackMaterial = CreateMaterial("Ink Black", Color.black);
            Material switchMaterial = CreateMaterial("Switch Tile", new Color(0.12f, 0.52f, 1f));
            Material exitMaterial = CreateMaterial("Exit Tile", new Color(0.1f, 0.82f, 0.35f));
            Material brickMaterial = CreateMaterial("Breakable Brick", new Color(0.72f, 0.22f, 0.16f));
            Material boxMaterial = CreateMaterial("Push Box", new Color(0.68f, 0.43f, 0.18f));
            Material bouncePadMaterial = CreateMaterial("Bounce Pad", new Color(1f, 0.82f, 0.16f));

            PetsLevelRuntime level = new GameObject("PETS Level Runtime").AddComponent<PetsLevelRuntime>();
            level.transform.SetParent(root.transform);

            level.Build(definition, whiteMaterial, blackMaterial, switchMaterial, exitMaterial, brickMaterial, boxMaterial, bouncePadMaterial);

            if (addTutorialMarkers)
            {
                CreateTutorialMarkers(root.transform, level);
            }

            GameObject player = CreatePlayer(root.transform);
            PetsLikePlayerController controller = player.GetComponent<PetsLikePlayerController>();
            controller.Configure(level, definition.Spawn);

            Camera camera = CreateCamera(root.transform);
            camera.GetComponent<PetsCameraRig>().Target = player.transform;

            PetsHud hud = new GameObject("PETS HUD").AddComponent<PetsHud>();
            hud.transform.SetParent(root.transform);
            hud.Player = controller;

            modeManager.TrySetInitialMode(PetsPerspectiveMode.TwoD);
            return root;
        }

        private static PetsLevelDefinition CreateTestRoomDefinition()
        {
            PetsLevelDefinition definition = new PetsLevelDefinition(15, 8, 1f)
            {
                Spawn = new PetsGridCoord(2, 1)
            };

            RectInt mainRoom = new RectInt(1, 1, 12, 5);
            definition.FillRect(mainRoom, PetsCellKind.WhiteInterior);
            definition.DrawRectOutline(mainRoom, PetsCellKind.WhiteLine);

            definition.SetCell(4, 1, PetsCellKind.SwitchToTwoPointFiveD);
            definition.SetCell(7, 1, PetsCellKind.BlackRegion);
            definition.SetCell(9, 1, PetsCellKind.SwitchTo2D);
            definition.SetCell(11, 1, PetsCellKind.Exit);
            definition.SetCell(3, 1, PetsCellKind.BouncePad);
            definition.SetProp(5, 3, PetsPropKind.BreakableBrick);
            definition.SetProp(10, 3, PetsPropKind.PushBox);
            definition.SetProp(6, 4, PetsPropKind.HeadBreakBox);
            definition.SetProp(6, 1, PetsPropKind.Star);
            definition.SetProp(9, 3, PetsPropKind.Star);

            definition.SetCell(6, 2, PetsCellKind.BlackRegion);
            definition.SetCell(7, 2, PetsCellKind.BlackRegion);
            definition.SetCell(8, 2, PetsCellKind.BlackRegion);

            return definition;
        }

        private static PetsLevelDefinition CreateTutorialDefinition()
        {
            PetsLevelDefinition definition = new PetsLevelDefinition(40, 16, 1.15f)
            {
                Spawn = new PetsGridCoord(2, 2)
            };

            // 2D starts from the user's sketch: closed white rooms/passages, not loose platform lines.
            FillRect(definition, 0, 1, 3, 3, PetsCellKind.WhiteInterior);
            FillRect(definition, 3, 2, 7, 3, PetsCellKind.WhiteInterior);
            FillRect(definition, 6, 3, 14, 4, PetsCellKind.WhiteInterior);
            FillRect(definition, 8, 1, 9, 2, PetsCellKind.WhiteInterior);
            FillRect(definition, 15, 3, 17, 4, PetsCellKind.WhiteInterior);
            FillRect(definition, 18, 3, 19, 4, PetsCellKind.WhiteInterior);
            FillRect(definition, 21, 3, 28, 4, PetsCellKind.WhiteInterior);
            FillRect(definition, 22, 4, 24, 8, PetsCellKind.WhiteInterior);
            FillRect(definition, 23, 8, 29, 10, PetsCellKind.WhiteInterior);
            FillRect(definition, 29, 7, 34, 9, PetsCellKind.WhiteInterior);
            FillRect(definition, 34, 8, 39, 10, PetsCellKind.WhiteInterior);

            definition.SetCell(13, 3, PetsCellKind.BlackRegion);
            definition.SetCell(15, 3, PetsCellKind.SwitchToTwoPointFiveD);
            definition.SetCell(25, 3, PetsCellKind.BlackRegion);
            definition.SetCell(28, 8, PetsCellKind.SwitchTo2D);
            definition.SetCell(34, 7, PetsCellKind.BlackRegion);
            definition.SetCell(35, 8, PetsCellKind.Exit);
            definition.SetProp(12, 3, PetsPropKind.Star);
            definition.SetProp(26, 8, PetsPropKind.Star);

            return definition;
        }

        private static void CreateTutorialMarkers(Transform root, PetsLevelRuntime level)
        {
            Transform markerRoot = new GameObject("Tutorial Markers").transform;
            markerRoot.SetParent(root);

            CreateTutorialMarker(markerRoot, level, new PetsGridCoord(1, 1), "start", new Color(0.18f, 0.28f, 1f), 0.12f);
            CreateTutorialMarker(markerRoot, level, new PetsGridCoord(8, 1), "*", new Color(1f, 0.58f, 0.03f), 0.32f);
            CreateTutorialMarker(markerRoot, level, new PetsGridCoord(27, 3), "*", new Color(1f, 0.58f, 0.03f), 0.32f);
            CreateTutorialMarker(markerRoot, level, new PetsGridCoord(37, 9), "->", new Color(0.2f, 0.28f, 1f), 0.28f);
        }

        private static void CreateTutorialMarker(Transform parent, PetsLevelRuntime level, PetsGridCoord coord, string text, Color color, float size)
        {
            GameObject marker = new GameObject($"Tutorial Marker {text} {coord.x},{coord.y}");
            marker.transform.SetParent(parent);
            marker.AddComponent<PetsTutorialMarker>().Configure(level, coord, text, color, size);
        }

        private static void FillRect(PetsLevelDefinition definition, int xMin, int yMin, int xMaxInclusive, int yMaxInclusive, PetsCellKind kind)
        {
            for (int x = xMin; x <= xMaxInclusive; x++)
            {
                for (int y = yMin; y <= yMaxInclusive; y++)
                {
                    definition.SetCell(x, y, kind);
                }
            }
        }

        private static void FillLine(PetsLevelDefinition definition, int xMin, int xMaxInclusive, int y, PetsCellKind kind)
        {
            for (int x = xMin; x <= xMaxInclusive; x++)
            {
                definition.SetCell(x, y, kind);
            }
        }

        private static void FillLine(PetsLevelDefinition definition, int xMin, int xMaxInclusive, int y, PetsCellKind kind, PetsPerspectiveMode mode)
        {
            for (int x = xMin; x <= xMaxInclusive; x++)
            {
                definition.SetCell(x, y, kind, mode);
            }
        }

        private static void CreateLighting(Transform root)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.92f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.35f;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;

            RenderSettings.ambientLight = new Color(0.76f, 0.76f, 0.72f);
        }

        private static Camera CreateCamera(Transform root)
        {
            GameObject cameraObject = new GameObject("PETS Camera");
            cameraObject.transform.SetParent(root);
            cameraObject.transform.position = new Vector3(2f, 4f, -13f);
            cameraObject.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.98f, 0.98f, 0.96f);

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<PetsCameraRig>();
            return camera;
        }

        private static GameObject CreatePlayer(Transform root)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.SetParent(root);
            player.transform.localScale = new Vector3(0.46f, 0.46f, 0.46f);

            Renderer renderer = player.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("Player Ink", new Color(0.08f, 0.08f, 0.08f));
            renderer.enabled = false;

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.drag = 0f;
            body.angularDrag = 0.05f;

            CreatePlayerVisuals(player.transform);
            player.AddComponent<PetsLikePlayerController>();
            return player;
        }

        private static void CreatePlayerVisuals(Transform player)
        {
            Material playerMaterial = CreateMaterial("Player Visual Ink", new Color(0.08f, 0.08f, 0.08f));

            GameObject twoDVisual = new GameObject("2D Flat Player Visual");
            twoDVisual.transform.SetParent(player);
            twoDVisual.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            twoDVisual.transform.localRotation = Quaternion.identity;
            twoDVisual.transform.localScale = Vector3.one;

            GameObject flatBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flatBody.name = "Flat Body";
            flatBody.transform.SetParent(twoDVisual.transform);
            flatBody.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            flatBody.transform.localScale = new Vector3(0.42f, 0.58f, 0.035f);
            flatBody.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            RemoveGeneratedCollider(flatBody.GetComponent<Collider>());

            GameObject flatHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flatHead.name = "Flat Head";
            flatHead.transform.SetParent(twoDVisual.transform);
            flatHead.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            flatHead.transform.localScale = new Vector3(0.42f, 0.42f, 0.035f);
            flatHead.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            RemoveGeneratedCollider(flatHead.GetComponent<Collider>());

            GameObject twoPointFiveDVisual = new GameObject("2.5D Solid Player Visual");
            twoPointFiveDVisual.transform.SetParent(player);
            twoPointFiveDVisual.transform.localPosition = Vector3.zero;
            twoPointFiveDVisual.transform.localRotation = Quaternion.identity;
            twoPointFiveDVisual.transform.localScale = Vector3.one;

            GameObject solidBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            solidBody.name = "Solid Body";
            solidBody.transform.SetParent(twoPointFiveDVisual.transform);
            solidBody.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            solidBody.transform.localScale = new Vector3(0.46f, 0.68f, 0.46f);
            solidBody.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            RemoveGeneratedCollider(solidBody.GetComponent<Collider>());

            Renderer solidBodyRenderer = solidBody.GetComponent<Renderer>();
            GameObject art25DVisual = CreatePlayer25DVisual(twoPointFiveDVisual.transform);
            if (art25DVisual != null)
            {
                solidBodyRenderer.enabled = false;
            }

            GameObject artTwoDVisual = CreatePlayerArtVisual(player);
            if (artTwoDVisual != null)
            {
                twoDVisual.SetActive(false);
            }

            Renderer[] renderers =
            {
                flatBody.GetComponent<Renderer>(),
                flatHead.GetComponent<Renderer>(),
                solidBodyRenderer
            };

            PetsPlayerVisualRig rig = player.gameObject.AddComponent<PetsPlayerVisualRig>();
            rig.Configure(artTwoDVisual != null ? artTwoDVisual : twoDVisual, twoPointFiveDVisual, renderers, new Color(0.08f, 0.08f, 0.08f), Color.white);
        }

        private static GameObject CreatePlayerArtVisual(Transform player)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Player2DVisualPrefabPath);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, player);
            instance.name = "Player2DVisual";
            instance.transform.localPosition = new Vector3(0f, 0f, -0.58f);
            instance.transform.localRotation = Quaternion.identity;

            if (instance.GetComponent<PetsPlayer2DAnimator>() == null)
            {
                instance.AddComponent<PetsPlayer2DAnimator>();
            }

            return instance;
#else
            return null;
#endif
        }

        private static GameObject CreatePlayer25DVisual(Transform parent)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Player25DCharacterPath);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = "Character FBX Visual";
            instance.transform.localPosition = new Vector3(0f, -0.54f, 0f);
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one;

            RemoveGeneratedColliders(instance);
            return instance;
#else
            return null;
#endif
        }

        private static void RemoveGeneratedColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                RemoveGeneratedCollider(colliders[i]);
            }
        }

        private static void RemoveGeneratedCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            if (shader.name == "Standard")
            {
                material.SetFloat("_Glossiness", 0f);
            }
            return material;
        }
    }
}
