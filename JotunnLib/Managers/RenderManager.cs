using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for rendering <see cref="Sprite">Sprites</see> of <see cref="GameObject">GameObjects</see>
    /// </summary>
    public class RenderManager : IManager
    {
        /// <summary>
        ///     Rotation of the prefab that will result in an isometric view
        /// </summary>
        public static readonly Quaternion IsometricRotation = Quaternion.Euler(23, 51, 25.8f);

        private static RenderManager _instance;

        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static RenderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RenderManager();
                }

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
            if (GUIManager.IsHeadless())
            {
                return;
            }

            Main.Instance.StartCoroutine(RenderQueue());
        }

        /// <summary>
        ///     Create a <see cref="Sprite"/> of the <paramref name="target"/>
        /// </summary>
        /// <param name="target">GameObject to render</param>
        /// <param name="callback">Callback for the generated <see cref="Sprite"/></param>
        /// <returns>If there is no active visual Mesh attached to the target, this method invokes the callback with null immediately and returns false.</returns>
        public bool EnqueueRender(GameObject target, Action<Sprite> callback)
        {
            return EnqueueRender(new RenderRequest(target), callback);
        }

        /// <summary>
        ///     Enqueue a render of the <see cref="RenderRequest"/>
        /// </summary>
        /// <param name="renderRequest"></param>
        /// <param name="callback">Callback for the generated <see cref="Sprite"/></param>
        /// <returns>If there is no active visual Mesh attached to the target, this method invokes the callback with null immediately and returns false.</returns>
        public bool EnqueueRender(RenderRequest renderRequest, Action<Sprite> callback)
        {
            if(!renderRequest.Target)
            {
                throw new ArgumentException("Target is required");
            }
            if(callback == null)
            {
                throw new ArgumentException("Callback is required");
            }
            if (!renderRequest.Target.GetComponentsInChildren<Component>(false).Any(IsVisualComponent))
            {
                callback.Invoke(null);
                return false;
            }
            renderRequest.Callback = callback;
            RenderRequestQueue.Enqueue(renderRequest);
            return true;
        }

        /// <summary>
        ///     Queues a new prefab to be rendered. The resulting <see cref="Sprite"/> will be ready at the next frame.
        ///     If there is no active visual Mesh attached to the target, this method invokes the callback with null immediately.
        /// </summary>
        /// <param name="target">Object to be rendered. A copy of the provided GameObject will be created for rendering</param>
        /// <param name="callback">Action that gets called when the rendering is complete</param>
        /// <param name="width">Width of the resulting <see cref="Sprite"/></param>
        /// <param name="height">Height of the resulting <see cref="Sprite"/></param>
        /// <returns>Only true if the target was queued for rendering</returns>
        [Obsolete("Use RenderRequest instead")]
#pragma warning disable S3427 // Method overloads with default parameter values should not overlap 
        public bool EnqueueRender(GameObject target, Action<Sprite> callback, int width = 128, int height = 128)
#pragma warning restore S3427 // Method overloads with default parameter values should not overlap 
        {
            return EnqueueRender(new RenderRequest(target)
            {
                Width = width,
                Height = height
            }, callback);
        }

        private void Render(RenderObject renderObject)
        {
            int width = renderObject.Request.Width;
            int height = renderObject.Request.Height;

            RenderTexture oldRenderTexture = RenderTexture.active;
            Renderer.targetTexture = RenderTexture.GetTemporary(width, height, 32);
            Renderer.fieldOfView = renderObject.Request.FieldOfView;
            RenderTexture.active = Renderer.targetTexture;

            renderObject.Spawn.SetActive(true);

            // calculate the Z position of the prefab as it needs to be far away from the camera
            float maxMeshSize = Mathf.Max(renderObject.Size.x, renderObject.Size.y) + 0.1f;
            float distance = maxMeshSize / Mathf.Tan(Renderer.fieldOfView * Mathf.Deg2Rad) * renderObject.Request.DistanceMultiplier;

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
                    spawnQueue.Enqueue(SpawnSafe(request));
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
        /// <param name="request"></param>
        /// <returns></returns>
        private static RenderObject SpawnSafe(RenderRequest request)
        {
            GameObject prefab = request.Target;
            // remember activeSelf to not mess with prefab data
            bool wasActiveSelf = prefab.activeSelf;
            prefab.SetActive(false);

            // spawn the prefab inactive
            GameObject spawn = Object.Instantiate(prefab, new Vector3(0, 0, 0), request.Rotation);
            spawn.name = prefab.name; 
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

            return new RenderObject(spawn, size)
            {
                Request = request
            };
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


        /// <summary>
        ///     Queues a new prefab to be rendered. The resulting <see cref="Sprite"/> will be ready at the next frame. 
        /// </summary>
        /// <returns>Only true if the target was queued for rendering</returns>
        public class RenderRequest
        {
            /// <summary>
            ///     Target GameObject to create a <see cref="Sprite"/> from
            /// </summary>
            public readonly GameObject Target;

            /// <summary>
            ///     Width of the generated <see cref="Sprite"/>
            /// </summary>
            public int Width { get; set; } = 128;
            /// <summary>
            ///     Height of the generated <see cref="Sprite"/>
            /// </summary>
            public int Height { get; set; } = 128;
            /// <summary>
            ///     Rotation of the prefab to capture
            /// </summary>
            public Quaternion Rotation { get; set; } = Quaternion.identity;
            /// <summary>
            ///     Field of view of the camera used to create the <see cref="Sprite"/>. Default is small to simulate orthographic view. An orthographic camera is not possible because of shaders
            /// </summary>
            public float FieldOfView { get; set; } = 0.5f;
            /// <summary>
            ///     Distance multiplier, should not be required with the default <see cref="FieldOfView"/>
            /// </summary>
            public float DistanceMultiplier { get; set; } = 1f;

            /// <summary>
            ///     Callback for the generated <see cref="Sprite"/>
            /// </summary>
            public Action<Sprite> Callback { get; internal set; }

            /// <summary>
            ///     Create a new RenderRequest
            /// </summary>
            /// <param name="target">Object to be rendered. A copy of the provided GameObject will be created for rendering</param> 
            public RenderRequest(GameObject target)
            {
                Target = target;
            }
        }
    }
}
