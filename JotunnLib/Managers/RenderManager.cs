using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Jotunn.Utils;
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
        private Sprite EmptySprite { get; } = Sprite.Create(Texture2D.whiteTexture, Rect.zero, Vector2.one);

        /// <summary>
        ///     Appended string to file names to force a cache recreation, if changed
        /// </summary>
        private static string cacheRevision = "c0";

        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static RenderManager Instance => _instance ??= new RenderManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private RenderManager() { }

        static RenderManager()
        {
            ((IManager)Instance).Init();
        }

        /// <summary>
        ///     Arbitrary unused Layer in the game
        /// </summary>
        private const int Layer = 31;

        private Camera Renderer;
        private Light Light;

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("RenderManager");

            if (GUIUtils.IsHeadless)
            {
                return;
            }

            Main.Instance.StartCoroutine(ClearRenderRoutine());
        }

        /// <summary>
        ///     Create a <see cref="Sprite"/> of the <paramref name="target"/>
        /// </summary>
        /// <param name="target">GameObject to render</param>
        /// <param name="callback">Callback for the generated <see cref="Sprite"/></param>
        /// <returns>If no active visual component is attached to the target or any child, this method invokes the callback with null immediately and returns false.</returns>
        [Obsolete("Use Render instead")]
        public bool EnqueueRender(GameObject target, Action<Sprite> callback)
        {
            return EnqueueRender(new RenderRequest(target), callback);
        }

        /// <summary>
        ///     Enqueue a render of the <see cref="RenderRequest"/>
        /// </summary>
        /// <param name="renderRequest"></param>
        /// <param name="callback">Callback for the generated <see cref="Sprite"/></param>
        /// <returns>If no active visual component is attached to the target or any child, this method invokes the callback with null immediately and returns false.</returns>
        [Obsolete("Use Render instead")]
        public bool EnqueueRender(RenderRequest renderRequest, Action<Sprite> callback)
        {
            if (!renderRequest.Target)
            {
                throw new ArgumentException("Target is required");
            }

            if (callback == null)
            {
                throw new ArgumentException("Callback is required");
            }

            if (!renderRequest.Target.GetComponentsInChildren<Component>(false).Any(IsVisualComponent))
            {
                callback.Invoke(null);
                return false;
            }

            renderRequest.Callback = callback;
            callback?.Invoke(Render(renderRequest));
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
        [Obsolete("Use Render instead")]
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

        /// <summary>
        ///     Create a <see cref="Sprite"/> of the <paramref name="target"/>
        /// </summary>
        /// <param name="target">Can be a prefab or any existing GameObject in the world</param>
        /// <returns>If no active visual component is attached to the target or any child, this method returns null.</returns>
        public Sprite Render(GameObject target)
        {
            return Render(new RenderRequest(target));
        }

        /// <summary>
        ///     Create a <see cref="Sprite"/> of the <paramref name="target"/>
        /// </summary>
        /// <param name="target">Can be a prefab or any existing GameObject in the world</param>
        /// <param name="rotation">Rotation while rendering of the GameObject. See <code>RenderManager.IsometricRotation</code> for example/></param>
        /// <returns>If no active visual component is attached to the target or any child, this method returns null.</returns>
        public Sprite Render(GameObject target, Quaternion rotation)
        {
            return Render(new RenderRequest(target) { Rotation = rotation });
        }

        /// <summary>
        ///     Create a <see cref="Sprite"/> from a <see cref="RenderRequest"/>/>
        /// </summary>
        /// <param name="renderRequest"></param>
        /// <returns>If no active visual component is attached to the target or any child, this method returns null.</returns>
        public Sprite Render(RenderRequest renderRequest)
        {
            if (!renderRequest.Target)
            {
                throw new ArgumentException("Target is required");
            }

            if (!renderRequest.Target.GetComponentsInChildren<Component>(false).Any(IsVisualComponent))
            {
                return null;
            }

            if (GUIUtils.IsHeadless)
            {
                return EmptySprite;
            }

            if (renderRequest.UseCache && ContainsIconCache(renderRequest.Target, renderRequest.TargetPlugin, out Sprite icon))
            {
                return icon;
            }

            if (!Renderer)
            {
                SetupRendering();
            }

            RenderObject spawned = SpawnRenderClone(renderRequest);
            // no safe spawn possible
            if (spawned == null) return null;
            Sprite rendered = RenderSprite(spawned);

            if (renderRequest.UseCache)
            {
                CacheIcon(renderRequest.Target, renderRequest.TargetPlugin, rendered);
            }

            return rendered;
        }

        private bool ContainsIconCache(GameObject target, BepInPlugin plugin, out Sprite sprite)
        {
            string version = GetVersion(plugin);
            string path = GetCachePath(target.name, version);
            bool exists = File.Exists(path);

            if (!exists)
            {
                sprite = null;
                return false;
            }

            byte[] bytesPNG = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(bytesPNG);

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f);
            return true;
        }

        private void CacheIcon(GameObject target, BepInPlugin plugin, Sprite rendered)
        {
            string version = GetVersion(plugin);
            Directory.CreateDirectory(Utils.Paths.IconCachePath);
            File.WriteAllBytes(GetCachePath(target.name, version), rendered.texture.EncodeToPNG());
        }

        private string GetCachePath(string name, string version)
        {
            return Path.Combine(Utils.Paths.IconCachePath, $"{name}-{version}-{cacheRevision}.png");
        }

        private string GetVersion(BepInPlugin plugin)
        {
            if (plugin != null)
            {
                return plugin.GUID + "-" + plugin.Version;
            }

            return GameVersions.ValheimVersion.ToString();
        }

        private Sprite RenderSprite(RenderObject renderObject)
        {
            int width = renderObject.Request.Width;
            int height = renderObject.Request.Height;

            using (new CreateTemporaryRenderTexture(Renderer, width, height))
            {
                Renderer.fieldOfView = renderObject.Request.FieldOfView;

                renderObject.Spawn.SetActive(true);

                // calculate the Z position of the prefab as it needs to be far away from the camera
                float maxMeshSize = Mathf.Max(renderObject.Size.x, renderObject.Size.y) + 0.1f;
                float distance = (maxMeshSize / Mathf.Tan(Renderer.fieldOfView * Mathf.Deg2Rad)) * renderObject.Request.DistanceMultiplier;

                Renderer.transform.position = new Vector3(0, 0, distance);

                Renderer.Render();

                renderObject.Spawn.SetActive(false);
                Object.Destroy(renderObject.Spawn);

                Texture2D previewImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
                previewImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                previewImage.Apply();

                return Sprite.Create(previewImage, new Rect(0, 0, width, height), Vector2.one / 2f);
            }
        }

        private IEnumerator ClearRenderRoutine()
        {
            while (true)
            {
                if (Renderer)
                {
                    ClearRendering();
                }

                yield return null;
            }
        }

        private void SetupRendering()
        {
            Renderer = new GameObject("Render Camera", typeof(Camera)).GetComponent<Camera>();
            Renderer.backgroundColor = new Color(0, 0, 0, 0);
            Renderer.clearFlags = CameraClearFlags.SolidColor;
            Renderer.transform.position = Vector3.zero;
            Renderer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Renderer.fieldOfView = 0.5f;
            Renderer.farClipPlane = 100000;
            Renderer.cullingMask = 1 << Layer;

            Light = new GameObject("Render Light", typeof(Light)).GetComponent<Light>();
            Light.transform.position = Vector3.zero;
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

        private static bool IsRenderComponent(Component component)
        {
            return component is MeshRenderer || component is SkinnedMeshRenderer || component is MeshFilter || component is Transform;
        }

        private static bool IsParticleComponent(Component component)
        {
            return component is ParticleSystemRenderer || component is ParticleSystem;
        }

        /// <summary>
        ///     Spawn a prefab without any Components except visuals. Also prevents calling Awake methods of the prefab.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static RenderObject SpawnRenderClone(RenderRequest request)
        {
            // create temporary parent and clone target as child
            GameObject parent = new GameObject();
            parent.SetActive(false);
            GameObject spawn = Object.Instantiate(request.Target, parent.transform);

            // build dag of coomponent dependencies
            DAG dag = new DAG(parent.transform);

            List<Component> components = dag.TopologicalSort();
            // can not be spawned safe
            if (dag.hasCycle) return null;

            // delete all components except visuals and transforms
            foreach (var component in components)
            {
                if (!(IsRenderComponent(component) || (request.ParticleSimulationTime >= 0 && IsParticleComponent(component))))
                    Object.DestroyImmediate(component);
            }

            SetLayerRecursive(spawn);
            spawn.transform.SetParent(null);
            Object.DestroyImmediate(parent);
            spawn.transform.position = Vector3.zero;
            spawn.transform.rotation = request.Rotation;
            spawn.name = request.Target.name;

            // calculate visual center
            Vector3 min = new Vector3(1000f, 1000f, 1000f);
            Vector3 max = new Vector3(-1000f, -1000f, -1000f);

            foreach (Renderer renderer in spawn.GetComponentsInChildren<Renderer>())
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    min = Vector3.Min(min, renderer.bounds.min);
                    max = Vector3.Max(max, renderer.bounds.max);
                }
            }

            // center the prefab
            spawn.transform.position = -(min + max) / 2f;

            Vector3 size = new Vector3(
                Mathf.Abs(min.x) + Mathf.Abs(max.x),
                Mathf.Abs(min.y) + Mathf.Abs(max.y),
                Mathf.Abs(min.z) + Mathf.Abs(max.z));

            // simulate particle effects (in case of simulation time less than 0, components have already been destroyed)
            foreach (ParticleSystem particleSystem in spawn.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Simulate(request.ParticleSimulationTime);
            }

            // just in case it doesn't gets deleted properly later
            TimedDestruction timedDestruction = spawn.AddComponent<TimedDestruction>();
            timedDestruction.Trigger(1f);

            return new RenderObject(spawn, size)
            {
                Request = request
            };
        }

        private static void SetLayerRecursive(GameObject root)
        {
            root.layer = Layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursive(child.gameObject);
            }
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
        ///     Wrapper to create and set a short-lived RenderTexture to a Camera, which is cleaned up afterwards
        /// </summary>
        private class CreateTemporaryRenderTexture : IDisposable
        {
            private readonly Camera camera;
            private readonly RenderTexture previousRenderTexture;
            private readonly RenderTexture temporaryRenderTexture;

            public CreateTemporaryRenderTexture(Camera camera, int textureWidth, int textureHeight)
            {
                this.camera = camera;
                previousRenderTexture = RenderTexture.active;
                temporaryRenderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight);

                camera.targetTexture = temporaryRenderTexture;
                RenderTexture.active = temporaryRenderTexture;
            }

            public void Dispose()
            {
                RenderTexture.active = previousRenderTexture;
                camera.targetTexture = null;

                // release the temporary render texture after it is no longer referenced by anyone
                RenderTexture.ReleaseTemporary(temporaryRenderTexture);
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
            ///     Pixel width of the generated <see cref="Sprite"/>
            /// </summary>
            public int Width { get; set; } = 128;

            /// <summary>
            ///     Pixel height of the generated <see cref="Sprite"/>
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
            [Obsolete]
            public Action<Sprite> Callback { get; internal set; }

            /// <summary>
            ///     Optional, Used for <see cref="UseCache"/> to determine a unique name-version combination
            /// </summary>
            public BepInPlugin TargetPlugin { get; set; } = null;

            /// <summary>
            ///     Save the render on the disc and reuse when called again. This reduces the time to re-render drastically.
            ///     When the game version changes, a new render will be made. When a <see cref="TargetPlugin"/> is set, the version and name
            ///     will be used to determine if a new render should be made
            /// </summary>
            public bool UseCache { get; set; } = false;

            /// <summary>
            ///     Create a new RenderRequest
            /// </summary>
            /// <param name="target">Object to be rendered. A copy of the provided GameObject will be created for rendering</param> 
            public RenderRequest(GameObject target)
            {
                Target = target;
            }

            /// <summary>
            ///     Simulates the particle effects for this amount of seconds. <0 for no particles.
            /// </summary>
            public float ParticleSimulationTime { get; set; } = 5f;
        }
    }

    /// <summary>
    ///     Directed Acyclic Graph (DAG) class to resolve component dependencies
    /// </summary>
    public class DAG
    {
        /// <summary>
        ///     Are there cycles in the graph
        /// </summary>
        public bool hasCycle { get; private set; }
        private readonly Transform initial;
        private readonly Dictionary<Component, List<Component>> nodes = new Dictionary<Component, List<Component>>();
        private readonly HashSet<Component> visitedNodes = new HashSet<Component>();

        /// <summary>
        ///     Constructor taking the component to resolve the dependencies for
        /// </summary>
        public DAG(Transform initial)
        {
            if (initial == null) return;
            hasCycle = false;
            this.initial = initial;

            Parse(initial);
        }

        private void Parse(Component element)
        {
            if (element == null) return;

            visitedNodes.Add(element);

            if (!nodes.ContainsKey(element)) nodes.Add(element, new List<Component>());

            foreach (Component component in element.gameObject.GetComponentsInChildren<Component>(true))
            {
                // element already processed
                if (visitedNodes.Contains(component)) continue;

                nodes[element].Add(component);
                Parse(component);
            }
        }

        /// <summary>
        ///     Retrieves the components from DAG according to their dependencies in order to destroy the components in correct order 
        /// </summary>
        /// <returns>List of <see cref="Component"/></returns>
        public List<Component> TopologicalSort()
        {
            List<Component> result = new List<Component>();
            HashSet<Component> visited = new HashSet<Component>();
            HashSet<Component> recursionStack = new HashSet<Component>();

            foreach (var node in nodes.Keys)
            {
                if (!visited.Contains(node))
                {
                    if (!TopologicalSortUtil(node, visited, recursionStack, result))
                    {
                        Logger.LogError($"Cycles detected in component dependencies for component {node}. Unable to determine deletion order for {initial}.");
                        return null;
                    }
                }
            }

            return result;
        }

        private bool TopologicalSortUtil(Component node, HashSet<Component> visited, HashSet<Component> recursionStack, List<Component> result)
        {
            if (recursionStack.Contains(node))
            {
                // Cycle detected
                hasCycle = true;
                return false;
            }

            if (!visited.Contains(node))
            {
                visited.Add(node);
                recursionStack.Add(node);

                foreach (var neighbor in nodes[node])
                {
                    if (!TopologicalSortUtil(neighbor, visited, recursionStack, result))
                    {
                        return false;
                    }
                }

                recursionStack.Remove(node);
                result.Add(node);
            }

            return true;
        }

    }


}
