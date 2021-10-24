using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for rendering sprites of GameObjects
    /// </summary>
    public class RenderManager : IManager
    {
        private static RenderManager _instance;

        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static RenderManager Instance
        {
            get
            {
                if (_instance == null) _instance = new RenderManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Unused Layer in Unity
        /// </summary>
        private const int Layer = 3;

        private readonly Queue<RenderRequest> renderQueue = new Queue<RenderRequest>();

        private static readonly Vector3 SpawnPoint = new Vector3(10000f, 10000f, 10000f);
        private Camera renderer;
        private Light light;

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            Main.Instance.StartCoroutine(RenderQueue());
        }

        /// <summary>
        ///     Queues a new prefab to be rendered. The resulting <see cref="Sprite"/> will be ready at the next frame.
        ///     If there is no active visual Mesh attached to the target, this function does nothing.
        /// </summary>
        /// <param name="target">Object to be rendered. A copy of the provided GameObject will be created for rendering</param>
        /// <param name="callback">Event that gets triggered when the rendering is complete</param>
        /// <param name="width">Width of the resulting <see cref="Sprite"/></param>
        /// <param name="height">Height of the resulting <see cref="Sprite"/></param>
        /// <returns>Only true if the target was queued for rendering</returns>
        public bool QueueRender(GameObject target, Action<Sprite> callback, int width = 128, int height = 128)
        {
            if (!target.GetComponentsInChildren<Component>(false).Any(IsVisualComponent))
            {
                return false;
            }

            renderQueue.Enqueue(new RenderRequest(target, callback, width, height));
            return true;
        }

        private void Render(RenderObject renderObject)
        {
            int width = renderObject.request.width;
            int height = renderObject.request.height;

            RenderTexture oldRenderTexture = RenderTexture.active;
            renderer.targetTexture = RenderTexture.GetTemporary(width, height, 32);
            RenderTexture.active = renderer.targetTexture;

            renderObject.spawn.SetActive(true);

            // calculate the Z position of the prefab as it needs to be far away from the camera
            // the FOV is small to simulate orthographic view. An orthographic camera is not possible because of shaders
            float maxMeshSize = Mathf.Max(renderObject.size.x, renderObject.size.y) + 0.1f;
            float distance = maxMeshSize / Mathf.Tan(renderer.fieldOfView * Mathf.Deg2Rad);
            renderer.transform.position = SpawnPoint + new Vector3(0, 0, distance);

            renderer.Render();

            renderObject.spawn.SetActive(false);
            Object.Destroy(renderObject.spawn);

            Texture2D previewImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            previewImage.Apply();

            RenderTexture.ReleaseTemporary(renderer.targetTexture);
            RenderTexture.active = oldRenderTexture;

            Sprite sprite = Sprite.Create(previewImage, new Rect(0, 0, width, height), Vector2.one / 2f);
            renderObject.request.callback?.Invoke(sprite);
        }

        private IEnumerator RenderQueue()
        {
            while (true)
            {
                Queue<RenderObject> spawnQueue = new Queue<RenderObject>();

                while (renderQueue.Count > 0)
                {
                    RenderRequest request = renderQueue.Dequeue();
                    RenderObject spawn = SpawnSafe(request.target);
                    spawn.request = request;
                    spawnQueue.Enqueue(spawn);
                }

                // wait one frame to allow Unity destroy components properly
                yield return null;

                if (spawnQueue.Count > 0)
                {
                    SetupRendering();

                    while (spawnQueue.Count > 0)
                    {
                        Render(spawnQueue.Dequeue());
                    }

                    ClearRendering();
                }
            }
        }

        private void SetupRendering()
        {
            renderer = new GameObject("Render Camera", typeof(Camera)).GetComponent<Camera>();
            renderer.backgroundColor = new Color(0, 0, 0, 0);
            renderer.clearFlags = CameraClearFlags.SolidColor;
            renderer.transform.position = SpawnPoint;
            renderer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            // small FOV to simulate orthographic view. An orthographic camera is not possible because of shaders
            renderer.fieldOfView = 0.5f;
            renderer.farClipPlane = 100000;
            renderer.cullingMask = 1 << Layer;

            light = new GameObject("Render Light", typeof(Light)).GetComponent<Light>();
            light.transform.position = SpawnPoint;
            light.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
            light.type = LightType.Directional;
            light.cullingMask = 1 << Layer;
        }

        private void ClearRendering()
        {
            Object.Destroy(renderer.gameObject);
            Object.Destroy(light.gameObject);
        }

        private static bool IsVisualComponent(Component component)
        {
            return component is Renderer || component is MeshFilter;
        }

        /// <summary>
        ///     Spawn a prefab without any Components except visuals. Also prevents calling Awake methods of the prefab.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private static RenderObject SpawnSafe(GameObject prefab)
        {
            // remember activeSelf to not mess with prefab data
            bool wasActiveSelf = prefab.activeSelf;
            prefab.SetActive(false);

            // spawn the prefab inactive
            GameObject spawn = Object.Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
            spawn.name = prefab.name;
            spawn.transform.rotation = Quaternion.Euler(0, -30f, 0);
            SetLayerRecursive(spawn.transform, Layer);

            prefab.SetActive(wasActiveSelf);

            // calculate visual center
            Vector3 min = new Vector3(1000f, 1000f, 1000f);
            Vector3 max = new Vector3(-1000f, -1000f, -1000f);

            foreach (Renderer meshRenderer in spawn.GetComponentsInChildren<Renderer>())
            {
                min = Vector3.Min(min, meshRenderer.bounds.min);
                max = Vector3.Max(max, meshRenderer.bounds.max);
            }

            // center the prefab
            spawn.transform.position = SpawnPoint - (min + max) / 2f;
            Vector3 size = new Vector3(
                Mathf.Abs(min.x) + Mathf.Abs(max.x),
                Mathf.Abs(min.y) + Mathf.Abs(max.y),
                Mathf.Abs(min.z) + Mathf.Abs(max.z));

            // needs to be destroyed first as Character depend on it
            foreach (CharacterDrop characterDrop in spawn.GetComponentsInChildren<CharacterDrop>())
            {
                Object.Destroy(characterDrop);
            }

            // needs to be destroyed first as Rigidbody depend on it
            foreach (Joint joint in spawn.GetComponentsInChildren<Joint>())
            {
                Object.Destroy(joint);
            }

            // destroy all other components except visuals
            foreach (Component component in spawn.GetComponentsInChildren<Component>(true))
            {
                if (component is Transform || IsVisualComponent(component))
                {
                    continue;
                }

                Object.Destroy(component);
            }

            // just in case it doesn't gets deleted properly later
            TimedDestruction timedDestruction = spawn.AddComponent<TimedDestruction>();
            timedDestruction.Trigger(1f);

            return new RenderObject(spawn, size);
        }

        private static void SetLayerRecursive(Transform transform, int layer)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursive(transform.GetChild(i), layer);
            }

            transform.gameObject.layer = layer;
        }

        private class RenderObject
        {
            public GameObject spawn;
            public Vector3 size;
            public RenderRequest request;

            public RenderObject(GameObject spawn, Vector3 size)
            {
                this.spawn = spawn;
                this.size = size;
            }
        }

        private class RenderRequest
        {
            public readonly GameObject target;
            public readonly Action<Sprite> callback;
            public readonly int width;
            public readonly int height;

            public RenderRequest(GameObject target, Action<Sprite> callback, int width, int height)
            {
                this.target = target;
                this.callback = callback;
                this.width = width;
                this.height = height;
            }
        }
    }
}
