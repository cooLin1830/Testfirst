using UnityEngine;
using UnityEngine.Events;

namespace DimensionShift
{
    public static class DimensionPrototypeFactory
    {
        public static GameObject BuildPrototypeScene()
        {
            GameObject root = new GameObject("Dimension Shift Demo Root");

            DimensionModeManager manager = new GameObject("Dimension Mode Manager").AddComponent<DimensionModeManager>();
            manager.transform.SetParent(root.transform);

            CreateLighting(root.transform);
            Camera camera = CreateCamera(root.transform);
            GameObject player = CreatePlayer(root.transform);
            camera.GetComponent<DimensionCameraRig>().Target = player.transform;

            CreateLevel(root.transform);
            new GameObject("Dimension HUD").AddComponent<DimensionHud>().transform.SetParent(root.transform);

            manager.SetMode(DimensionMode.TwoD);
            return root;
        }

        private static void CreateLighting(Transform root)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.08f;

            RenderSettings.ambientLight = new Color(0.74f, 0.78f, 0.84f);
        }

        private static Camera CreateCamera(Transform root)
        {
            GameObject cameraObject = new GameObject("Dimension Camera");
            cameraObject.transform.SetParent(root);
            cameraObject.transform.position = new Vector3(0f, 4f, -16f);
            cameraObject.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.96f, 0.97f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<DimensionCameraRig>();
            return camera;
        }

        private static GameObject CreatePlayer(Transform root)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.SetParent(root);
            player.transform.position = new Vector3(-8f, 1.2f, 0f);
            player.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);
            player.GetComponent<Renderer>().material = CreateMaterial(new Color(0.1f, 0.1f, 0.1f));

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.drag = 0f;
            body.angularDrag = 0.05f;

            player.AddComponent<DimensionPlayerController>();

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            face.name = "White Face";
            face.transform.SetParent(player.transform);
            face.transform.localPosition = new Vector3(0f, 0.34f, -0.37f);
            face.transform.localScale = new Vector3(0.38f, 0.38f, 0.08f);
            face.GetComponent<Renderer>().material = CreateMaterial(Color.white);
            DestroyGeneratedObject(face.GetComponent<Collider>());

            return player;
        }

        private static void CreateLevel(Transform root)
        {
            Transform level = new GameObject("Mode Aware Level").transform;
            level.SetParent(root);

            CreatePlatform(level, "Main 2D Ground", new Vector3(0f, -0.15f, 0f), new Vector3(22f, 0.3f, 1.3f), new Color(0.94f, 0.94f, 0.91f));
            CreatePlatform(level, "3D Ground Pad", new Vector3(2f, -0.18f, 3.5f), new Vector3(14f, 0.26f, 7f), new Color(0.86f, 0.92f, 0.96f));
            CreatePlatform(level, "Upper 2D Ledge", new Vector3(-4.5f, 3.1f, 0f), new Vector3(5.5f, 0.28f, 1.2f), new Color(0.94f, 0.94f, 0.91f));
            CreatePlatform(level, "Exit Ledge", new Vector3(8.6f, 2.35f, 0f), new Vector3(5.2f, 0.28f, 1.2f), new Color(0.94f, 0.94f, 0.91f));

            CreateWall(level, "2D Slice Back Guide", new Vector3(0f, 1.5f, 0.72f), new Vector3(22f, 3.4f, 0.08f), new Color(0.82f, 0.84f, 0.86f, 0.45f), false);
            CreateWall(level, "3D Side Wall", new Vector3(5.9f, 1.2f, 1.8f), new Vector3(0.4f, 2.4f, 4.2f), new Color(0.72f, 0.74f, 0.78f), true);

            CreateBridge(level);
            DimensionDoor door = CreateDoor(level, new Vector3(7.3f, 1.2f, 0f));
            DimensionPressurePlate plate = CreatePressurePlate(level, new Vector3(1.5f, 0.08f, 3.3f));
            plate.onPressed.AddListener(new UnityAction(() => door.SetOpen(true)));
            plate.onReleased.AddListener(new UnityAction(() => door.SetOpen(false)));

            CreateBox(level, new Vector3(-1.5f, 0.65f, 3.3f));
            CreateHazard(level, new Vector3(-2.2f, 0.35f, 0f), new Vector3(1.8f, 0.7f, 1.1f));
            CreatePickup(level, new Vector3(8.6f, 3.1f, 0f));
            CreateModeLabels(level);
        }

        private static void CreateBridge(Transform parent)
        {
            GameObject bridge = CreatePlatform(parent, "Bridge - 2D Platform / 3D Thin Block", new Vector3(3.8f, 1.2f, 0f), new Vector3(2.6f, 0.28f, 1.1f), new Color(0.8f, 0.6f, 0.38f));
            ModeAwareTransform modeTransform = bridge.AddComponent<ModeAwareTransform>();
            modeTransform.Configure(
                bridge.transform.localPosition,
                new Vector3(3.8f, 0.22f, 2.2f),
                Vector3.zero,
                new Vector3(0f, 0f, 90f),
                bridge.transform.localScale,
                new Vector3(0.3f, 2.5f, 1.1f));
        }

        private static DimensionDoor CreateDoor(Transform parent, Vector3 position)
        {
            GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObject.name = "Pressure Door";
            doorObject.transform.SetParent(parent);
            doorObject.transform.position = position;
            doorObject.transform.localScale = new Vector3(0.55f, 2.4f, 1.2f);
            doorObject.GetComponent<Renderer>().material = CreateMaterial(new Color(0.95f, 0.35f, 0.28f));
            return doorObject.AddComponent<DimensionDoor>();
        }

        private static DimensionPressurePlate CreatePressurePlate(Transform parent, Vector3 position)
        {
            GameObject plateObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plateObject.name = "3D Pressure Plate";
            plateObject.transform.SetParent(parent);
            plateObject.transform.position = position;
            plateObject.transform.localScale = new Vector3(1.3f, 0.06f, 1.3f);
            plateObject.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.55f, 1f));
            plateObject.GetComponent<Collider>().isTrigger = true;
            return plateObject.AddComponent<DimensionPressurePlate>();
        }

        private static GameObject CreateBox(Transform parent, Vector3 position)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Mode Aware Pushable Box";
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            box.GetComponent<Renderer>().material = CreateMaterial(new Color(0.9f, 0.48f, 0.23f));

            Rigidbody body = box.AddComponent<Rigidbody>();
            body.mass = 2.2f;
            body.drag = 0.25f;
            body.angularDrag = 2f;

            box.AddComponent<DimensionPushableBox>();
            return box;
        }

        private static GameObject CreateHazard(Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject hazard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazard.name = "Black 2D Hazard / 3D Decoration";
            hazard.transform.SetParent(parent);
            hazard.transform.position = position;
            hazard.transform.localScale = scale;
            hazard.GetComponent<Renderer>().material = CreateMaterial(Color.black);
            hazard.GetComponent<Collider>().isTrigger = true;
            hazard.AddComponent<DimensionHazard>();
            return hazard;
        }

        private static GameObject CreatePickup(Transform parent, Vector3 position)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = "Goal Pickup";
            pickup.transform.SetParent(parent);
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.55f;
            pickup.GetComponent<Renderer>().material = CreateMaterial(new Color(1f, 0.86f, 0.2f));
            pickup.GetComponent<Collider>().isTrigger = true;
            pickup.AddComponent<DimensionPickup>();
            return pickup;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.transform.localScale = scale;
            platform.GetComponent<Renderer>().material = CreateMaterial(color);
            return platform;
        }

        private static GameObject CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Color color, bool solid)
        {
            GameObject wall = CreatePlatform(parent, name, position, scale, color);
            wall.GetComponent<Collider>().enabled = solid;
            return wall;
        }

        private static void CreateModeLabels(Transform parent)
        {
            CreateLabel(parent, "2D: side slice, Z is locked", new Vector3(-7f, 2.1f, -0.58f), Color.black);
            CreateLabel(parent, "3D: use W/S for depth", new Vector3(1.5f, 0.7f, 4.6f), new Color(0.05f, 0.2f, 0.5f));
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position, Color color)
        {
            GameObject labelObject = new GameObject(text);
            labelObject.transform.SetParent(parent);
            labelObject.transform.position = position;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.24f;
            textMesh.fontSize = 42;
            textMesh.color = color;
        }

        private static Material CreateMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }

        private static void DestroyGeneratedObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
