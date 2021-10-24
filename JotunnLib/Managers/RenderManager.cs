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

        private readonly Queue<RenderRequest> RenderRequestQueue = new Queue<RenderRequest>();

        private static readonly Vector3 SpawnPoint = new Vector3(10000f, 10000f, 10000f);
        private Camera Renderer;
        private Light Light;

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

            RenderRequestQueue.Enqueue(new RenderRequest(target, callback, width, height));
            return true;
        }

        private void Render(RenderObject renderObject)
        {
            int width = renderObject.Request.Width;
            int height = renderObject.Request.Height;

            RenderTexture oldRenderTexture = RenderTexture.active;
            Renderer.targetTexture = RenderTexture.GetTemporary(width, height, 32);
            RenderTexture.active = Renderer.targetTexture;

            renderObject.Spawn.SetActive(true);

            // calculate the Z position of the prefab as it needs to be far away from the camera
            // the FOV is small to simulate orthographic view. An orthographic camera is not possible because of shaders
            float maxMeshSize = Mathf.Max(renderObject.Size.x, renderObject.Size.y) + 0.1f;
            float distance = maxMeshSize / Mathf.Tan(Renderer.fieldOfView * Mathf.Deg2Rad);
            Renderer.transform.position = SpawnPoint + new Vector3(0, 0, distance);

            Renderer.Render();

            renderObject.Spawn.SetActive(false);
            Object.Destroy(renderObject.Spawn);

            Texture2D previewImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            previewImage.Apply();

            RenderTexture.ReleaseTemporary(Renderer.targetTexture);
            RenderTexture.active = oldRenderTexture;

            Sprite sprite = Sprite.Create(previewImage, new Rect(0, 0, width, height), Vector2.one / 2f);
            renderObject.Request.Callback?.Invoke(sprite);
        }

        private IEnumerator RenderQueue()
        {
            while (true)
            {
                Queue<RenderObject> spawnQueue = new Queue<RenderObject>();

                while (RenderRequestQueue.Count > 0)
                {
                    RenderRequest request = RenderRequestQueue.Dequeue();
                    RenderObject spawn = SpawnSafe(request.Target);
                    spawn.Request = request;
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
            Renderer = new GameObject("Render Camera", typeof(Camera)).GetComponent<Camera>();
            Renderer.backgroundColor = new Color(0, 0, 0, 0);
            Renderer.clearFlags = CameraClearFlags.SolidColor;
            Renderer.transform.position = SpawnPoint;
            Renderer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            // small FOV to simulate orthographic view. An orthographic camera is not possible because of shaders
            Renderer.fieldOfView = 0.5f;
            Renderer.farClipPlane = 100000;
            Renderer.cullingMask = 1 << Layer;

            Light = new GameObject("Render Light", typeof(Light)).GetComponent<Light>();
            Light.transform.position = SpawnPoint;
            Light.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
            Light.type = LightType.Directional;
            Light.cullingMask = 1 << Layer;
        }

        private void ClearRendering()
        {
            Object.Destroy(Renderer.gameObject);
            Object.Destroy(Light.gameObject);
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
            public readonly GameObject Spawn;
            public readonly Vector3 Size;
            public RenderRequest Request;

            public RenderObject(GameObject spawn, Vector3 size)
            {
                Spawn = spawn;
                Size = size;
            }
        }

        private class RenderRequest
        {
            public readonly GameObject Target;
            public readonly Action<Sprite> Callback;
            public readonly int Width;
            public readonly int Height;

            public RenderRequest(GameObject target, Action<Sprite> callback, int width, int height)
            {
                Target = target;
                Callback = callback;
                Width = width;
                Height = height;
            }
        }
    }
}
