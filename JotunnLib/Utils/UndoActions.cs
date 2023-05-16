using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Pre-defined actions to use with the <see cref="UndoManager"/>.
    /// </summary>
    public static class UndoActions
    {
        private static class UndoHelper
        {
            public static void CopyData(ZDO from, ZDO to)
            {
                var refresh = to.m_prefab != from.m_prefab;
                to.m_floats = from.m_floats;
                to.m_vec3 = from.m_vec3;
                to.m_quats = from.m_quats;
                to.m_ints = from.m_ints;
                to.m_longs = from.m_longs;
                to.m_strings = from.m_strings;
                to.m_byteArrays = from.m_byteArrays;
                to.m_prefab = from.m_prefab;
                to.m_position = from.m_position;
                to.m_rotation = from.m_rotation;
                var zs = ZNetScene.instance;
                if (zs.m_instances.TryGetValue(to, out var view))
                {
                    var tf = view.transform;
                    tf.position = from.m_position;
                    tf.rotation = from.m_rotation;
                    tf.localScale = from.GetVec3("scale", Vector3.one);
                    if (refresh)
                    {
                        var newObj = ZNetScene.instance.CreateObject(to);
                        if (newObj)
                        {
                            UnityEngine.Object.Destroy(view.gameObject);
                            ZNetScene.instance.m_instances[to] = newObj.GetComponent<ZNetView>();
                        }
                    }
                }
                to.IncreaseDataRevision();
            }

            public static ZDO Place(ZDO zdo)
            {
                var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                if (!prefab) throw new InvalidOperationException("Invalid prefab");
                var obj = UnityEngine.Object.Instantiate<GameObject>(prefab, zdo.GetPosition(), zdo.GetRotation());
                var netView = obj.GetComponent<ZNetView>();
                if (!netView) throw new InvalidOperationException("No view");
                var added = netView.GetZDO();
                netView.SetLocalScale(zdo.GetVec3("scale", obj.transform.localScale));
                CopyData(zdo.Clone(), added);
                return added;
            }

            public static ZDO[] Place(ZDO[] data) => data.Select(Place).Where(obj => obj != null).ToArray();

            public static string Name(ZDO zdo) => global::Utils.GetPrefabName(ZNetScene.instance.GetPrefab(zdo.GetPrefab()));

            public static string Print(ZDO[] data)
            {
                if (data.Length == 1) return Name(data.First());
                var names = data.GroupBy(Name);
                var enumerable = names as IGrouping<string, ZDO>[] ?? names.ToArray();
                if (enumerable.Length == 1) return $"{enumerable.First().Key} {enumerable.First().Count()}x";
                return $" objects {data.Length}x";
            }

            public static ZDO[] Remove(ZDO[] toRemove)
            {
                var data = Clone(toRemove);
                foreach (var zdo in toRemove) RemoveZDO(zdo);
                return data;
            }

            public static ZDO[] Clone(IEnumerable<ZDO> data) => data.Select(zdo => zdo.Clone()).ToArray();

            public static void RemoveZDO(ZDO zdo)
            {
                if (!IsValid(zdo)) return;
                if (!zdo.IsOwner())
                    zdo.SetOwner(ZDOMan.GetSessionID());
                if (ZNetScene.instance.m_instances.TryGetValue(zdo, out var view))
                    ZNetScene.instance.Destroy(view.gameObject);
                else
                    ZDOMan.instance.DestroyZDO(zdo);
            }

            ///<summary>Helper to check object validity.</summary>
            public static bool IsValid(ZNetView view) => view && IsValid(view.GetZDO());

            ///<summary>Helper to check object validity.</summary>
            public static bool IsValid(ZDO zdo) => zdo != null && zdo.IsValid();

            public static void ApplyData(Dictionary<Vector3, TerrainUndoData> data, Vector3 pos, float radius)
            {
                foreach (var kvp in data)
                {
                    var compiler = TerrainComp.FindTerrainCompiler(kvp.Key);
                    if (!compiler) continue;
                    foreach (var value in kvp.Value.Heights)
                    {
                        compiler.m_smoothDelta[value.Index] = value.Smooth;
                        compiler.m_levelDelta[value.Index] = value.Level;
                        compiler.m_modifiedHeight[value.Index] = value.HeightModified;
                    }
                    foreach (var value in kvp.Value.Paints)
                    {
                        compiler.m_modifiedPaint[value.Index] = value.PaintModified;
                        compiler.m_paintMask[value.Index] = value.Paint;
                    }
                    Save(compiler);
                }
                ClutterSystem.instance?.ResetGrass(pos, radius);
            }

            public static void Save(TerrainComp compiler)
            {
                compiler.GetComponent<ZNetView>()?.ClaimOwnership();
                compiler.m_operations++;
                // These are only used to remove grass which isn't really needed.
                compiler.m_lastOpPoint = Vector3.zero;
                compiler.m_lastOpRadius = 0f;
                compiler.Save();
                compiler.m_hmap.Poke(false);
            }
        }
        
        /// <summary>
        ///     "Create" action for the <see cref="UndoManager"/>. Can undo and redo ZDO creation.
        /// </summary>
        public class UndoCreate : UndoManager.IUndoAction
        {
            /// <summary>
            ///     Current ZDO data of this action.
            /// </summary>
            public ZDO[] Data;

            /// <summary>
            ///     Create new undo data for ZDO creation operations. Clones all ZDO data to prevent NREs.
            /// </summary>
            /// <param name="data">Enumerable of ZDOs which were created.</param>
            public UndoCreate(IEnumerable<ZDO> data)
            {
                Data = UndoHelper.Clone(data);
            }
            
            /// <summary>
            ///     Description of the executed action.
            /// </summary>
            public string Description() => $"Created {UndoHelper.Print(Data)}";

            /// <summary>
            ///     Remove stored ZDOs again.
            /// </summary>
            public virtual void Undo()
            {
                Data = UndoHelper.Remove(Data);
            }
            
            /// <summary>
            ///     Success message.
            /// </summary>
            public string UndoMessage() => $"Undo: Removed {UndoHelper.Print(Data)}";
            
            /// <summary>
            ///     Recreate stored ZDOs again.
            /// </summary>
            public virtual void Redo()
            {
                Data = UndoHelper.Place(Data);
            }
            
            /// <summary>
            ///     Success message.
            /// </summary>
            public string RedoMessage() => $"Redo: Restored {UndoHelper.Print(Data)}";
        }
        
        /// <summary>
        ///     "Remove" action for the <see cref="UndoManager"/>. Can undo and redo ZDO removal.
        /// </summary>
        public class UndoRemove : UndoManager.IUndoAction
        {
            /// <summary>
            ///     Current ZDO data of this action.
            /// </summary>
            public ZDO[] Data;
            
            /// <summary>
            ///     Create new undo data for ZDO removal operations. Clones all ZDO data to prevent NREs.
            /// </summary>
            /// <param name="data">Enumerable of ZDOs which were removed.</param>
            public UndoRemove(IEnumerable<ZDO> data)
            {
                Data = UndoHelper.Clone(data);
            }
            
            /// <summary>
            ///     Description of the executed action.
            /// </summary>
            public string Description() => $"Removed {UndoHelper.Print(Data)}";

            /// <summary>
            ///     Recreate stored ZDOs again.
            /// </summary>
            public virtual void Undo()
            {
                Data = UndoHelper.Place(Data);
            }
            
            /// <summary>
            ///     Success message.
            /// </summary>
            public string UndoMessage() => $"Undo: Restored {UndoHelper.Print(Data)}";
            
            /// <summary>
            ///     Remove stored ZDOs again.
            /// </summary>
            public virtual void Redo()
            {
                Data = UndoHelper.Remove(Data);
            }
            
            /// <summary>
            ///     Success message.
            /// </summary>
            public string RedoMessage() => $"Redo: Removed {UndoHelper.Print(Data)}";
        }

        /// <summary>
        ///     Heightmap data wrapper
        /// </summary>
        public class HeightUndoData
        {
            /// <summary>
            ///     "Smooth" member of the heightmap
            /// </summary>
            public float Smooth = 0f;
            /// <summary>
            ///     "Level" member of the heightmap
            /// </summary>
            public float Level = 0f;
            /// <summary>
            ///     "Index" member of the heightmap
            /// </summary>
            public int Index = -1;
            /// <summary>
            ///     "HeightModified" member of the heightmap
            /// </summary>
            public bool HeightModified = false;
        }

        /// <summary>
        ///     Paint data wrapper
        /// </summary>
        public class PaintUndoData
        {
            /// <summary>
            ///     "PaintModified" member of the heightmap paint
            /// </summary>
            public bool PaintModified = false;
            /// <summary>
            ///     "Paint" member of the heightmap paint
            /// </summary>
            public Color Paint = Color.black;
            /// <summary>
            ///     "Index" member of the heightmap paint
            /// </summary>
            public int Index = -1;
        }

        /// <summary>
        ///     Heightmap and Paint data collection
        /// </summary>
        public class TerrainUndoData
        {
            /// <summary>
            ///     Collection of <see cref="HeightUndoData"/>
            /// </summary>
            public HeightUndoData[] Heights = new HeightUndoData[0];
            /// <summary>
            ///     Collection of <see cref="PaintUndoData"/>
            /// </summary>
            public PaintUndoData[] Paints = new PaintUndoData[0];
        }
        
        /// <summary>
        ///     "Terrain" action for the <see cref="UndoManager"/>. Can undo and redo terrain modifications.
        /// </summary>
        public class UndoTerrain : UndoManager.IUndoAction
        {
            private readonly Dictionary<Vector3, TerrainUndoData> Before;
            private readonly Dictionary<Vector3, TerrainUndoData> After;
            private readonly Vector3 Position;
            private readonly float Radius;

            /// <summary>
            ///     Create new undo data for terrain modifications.
            /// </summary>
            /// <param name="before">Terrain state before modification</param>
            /// <param name="after">Terrain state after modification</param>
            /// <param name="position">Position of the terrain modification center</param>
            /// <param name="radius">Radius of the terrain modification</param>
            public UndoTerrain(Dictionary<Vector3, TerrainUndoData> before, Dictionary<Vector3, TerrainUndoData> after, Vector3 position, float radius)
            {
                Before = before;
                After = after;
                Position = position;
                Radius = radius;
            }
            
            /// <summary>
            ///     Description of the executed action.
            /// </summary>
            public string Description() => "Changed terrain";

            /// <summary>
            ///     Sets terrain data to the stored values of the "before" state.
            /// </summary>
            public virtual void Undo()
            {
                UndoHelper.ApplyData(Before, Position, Radius);
            }

            /// <summary>
            ///     Success message.
            /// </summary>
            public string UndoMessage() => "Undoing terrain changes";
            
            /// <summary>
            ///     Sets terrain data to the stored values of the "after" state.
            /// </summary>
            public virtual void Redo()
            {
                UndoHelper.ApplyData(After, Position, Radius);
            }
            
            /// <summary>
            ///     Success message.
            /// </summary>
            public string RedoMessage() => "Redoing terrain changes";
        }
    }
}
