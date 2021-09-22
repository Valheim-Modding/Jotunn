using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Simple dragging <see cref="MonoBehaviour"/>
    /// </summary>
    public class DragWindowCntrl : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        /// <summary>
        ///     Add this MonoBehaviour to a GameObject
        /// </summary>
        /// <param name="go"></param>
        [Obsolete("Use gameObject.AddComponent<DragWindowCntrl>() instead")]
        public static void ApplyDragWindowCntrl(GameObject go)
        {
            go.AddComponent<DragWindowCntrl>();
        }

        private RectTransform window;

        //delta drag
        private Vector2 delta;

        private void Awake()
        {
            window = (RectTransform)transform;
        }

        /// <summary>
        ///     BeginDrag event trigger
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            delta = Input.mousePosition - window.position;
        }

        /// <summary>
        ///     Drag event trigger
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            Vector2 pos = eventData.position - delta;
            Rect rect = window.rect;
            Vector2 lossyScale = window.lossyScale;

            float minX = rect.width / 2f * lossyScale.x;
            float maxX = Screen.width - minX;
            float minY = rect.height / 2f * lossyScale.y;
            float maxY = Screen.height - minY;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
    }
}
