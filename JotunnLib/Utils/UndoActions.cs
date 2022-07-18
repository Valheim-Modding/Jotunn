using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Pre defined actions to use with the <see cref="UndoManager"/>.
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
                    view.transform.position = from.m_position;
                    view.transform.rotation = from.m_rotation;
                    view.transform.localScale = from.GetVec3("scale", Vector3.one);
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
                to.IncreseDataRevision();
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
                if (data.Count() == 1) return Name(data.First());
                var names = data.GroupBy(Name);
                if (names.Count() == 1) return $"{names.First().Key} {names.First().Count()}x";
                return $" objects {data.Count()}x";
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
                    zdo.SetOwner(ZDOMan.instance.GetMyID());
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
        
        public class UndoCreate : UndoManager.IUndoAction
        {

            private ZDO[] Data;

            public UndoCreate(IEnumerable<ZDO> data)
            {
                Data = UndoHelper.Clone(data);
            }

            public void Undo()
            {
                Data = UndoHelper.Remove(Data);
            }

            public string UndoMessage() => $"Undo: Removed {UndoHelper.Print(Data)}";

            public void Redo()
            {
                Data = UndoHelper.Place(Data);
            }

            public string RedoMessage() => $"Redo: Restored {UndoHelper.Print(Data)}";
        }

        public class UndoRemove : UndoManager.IUndoAction
        {

            private ZDO[] Data;
            public UndoRemove(IEnumerable<ZDO> data)
            {
                Data = UndoHelper.Clone(data);
            }
            public void Undo()
            {
                Data = UndoHelper.Place(Data);
            }

            public void Redo()
            {
                Data = UndoHelper.Remove(Data);
            }

            public string UndoMessage() => $"Undo: Restored {UndoHelper.Print(Data)}";

            public string RedoMessage() => $"Redo: Removed {UndoHelper.Print(Data)}";
        }

        public class HeightUndoData
        {
            public float Smooth = 0f;
            public float Level = 0f;
            public int Index = -1;
            public bool HeightModified = false;
        }

        public class PaintUndoData
        {
            public bool PaintModified = false;
            public Color Paint = Color.black;
            public int Index = -1;
        }

        public class TerrainUndoData
        {
            public HeightUndoData[] Heights = new HeightUndoData[0];
            public PaintUndoData[] Paints = new PaintUndoData[0];
        }

        public class UndoTerrain : UndoManager.IUndoAction
        {

            private Dictionary<Vector3, TerrainUndoData> Before = new Dictionary<Vector3, TerrainUndoData>();
            private Dictionary<Vector3, TerrainUndoData> After = new Dictionary<Vector3, TerrainUndoData>();
            public Vector3 Position;
            public float Radius;

            public UndoTerrain(Dictionary<Vector3, TerrainUndoData> before, Dictionary<Vector3, TerrainUndoData> after, Vector3 position, float radius)
            {
                Before = before;
                After = after;
                Position = position;
                Radius = radius;
            }

            public void Undo()
            {
                UndoHelper.ApplyData(Before, Position, Radius);
            }

            public string UndoMessage() => "Undoing terrain changes";

            public void Redo()
            {
                UndoHelper.ApplyData(After, Position, Radius);
            }

            public string RedoMessage() => "Redoing terrain changes";
        }
    }
}
