using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Monobehaviour to handle make subentries in mod settings selectable, and pass controller button presses to appropriate
    ///     GameObject.
    /// </summary>
    public class SelectableConfig : Selectable, ISubmitHandler
    {
        /// <summary>
        ///     GameObject to receive OnSubmit call
        /// </summary>
        public GameObject InteractionTarget { 
            get
            {
                return _interactionTarget;
            } 
            set
            {
                var handler = value.GetComponent<ISubmitHandler>();
                if(handler != null)
                {
                    _interactionTarget = value;
                    interactionTargetSubmitHandler = handler;
                    var selectable = value.GetComponent<Selectable>();
                    transition = selectable.transition;
                    spriteState = selectable.spriteState;
                    navigation = selectable.navigation;
                    interactable = selectable.interactable;
                    image = selectable.image;
                    colors = selectable.colors;
                    animationTriggers = selectable.animationTriggers;
                    targetGraphic = selectable.targetGraphic;
                    selectable.OnDeselect(new BaseEventData(EventSystem.current));


                } else
                {
                    Debug.LogError("GameObject '" + gameObject.name + "' does not have a component that implements ISubmitHandler");
                }
            }
        }

        private GameObject _interactionTarget;
        private ISubmitHandler interactionTargetSubmitHandler;

        /// <summary>
        ///     Handler for OnSubmit. Passes OnSubmit call and eventData to InteractionTarget
        /// </summary>
        /// <param name="eventData">OnSubmit Event Data</param>
        public void OnSubmit(BaseEventData eventData)
        {
            if (InteractionTarget != null)
            {
                interactionTargetSubmitHandler.OnSubmit(eventData);
            }
        }

        //public override void OnSelect(BaseEventData eventData)
        //{
        //    base.OnSelect(eventData);
        //    InteractionTarget.GetComponent<Selectable>().OnSelect(eventData);
        //}

        //public override void OnDeselect(BaseEventData eventData)
        //{
        //    base.OnDeselect(eventData);
        //    InteractionTarget.GetComponent<Selectable>().OnDeselect(eventData);
        //}
    }
}
