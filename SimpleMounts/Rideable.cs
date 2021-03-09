using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Managers;

namespace SimpleMounts
{
    public class Rideable : MonoBehaviour, Interactable
    {
        public static bool IsRiding = false;
        public bool requiresSaddle = true;
        public float requiredRidingLevel = 0f;
        public float speed = 8f;
        public float runSpeed = 12f;
        public float jumpForce = 15f;
        private bool isBeingRidden = false;
        private float oldRiderSpeed, oldRiderRunSpeed;
        private float oldAnimalSpeed, oldAnimalRunSpeed, oldAnimalJumpForce;
        private Humanoid rider;
        private Character animal;

        private void Start()
        {
            animal = GetComponent<Character>();
        }

        private void Update()
        {
            if (IsRiding && isBeingRidden)
            {
                // Update animal move/look direction
                animal.SetMoveDir(rider.GetMoveDir());
                animal.SetLookDir(rider.GetLookDir());
                rider.transform.rotation = animal.GetLookYaw();

                // Stop riding
                if (ZInput.GetButton("Unmount"))
                {
                    StopRide();
                }

                // Jump
                if (ZInput.GetButton("Jump"))
                {
                    animal.Jump();
                }
            }
        }

        public void StartRide()
        {
            if (isBeingRidden || !rider || IsRiding)
            {
                return;
            }

            // Set status vars
            isBeingRidden = true;
            IsRiding = true;

            // Disable AnimalAI and stop their movement
            GetComponent<AnimalAI>().enabled = false;
            GetComponent<Character>().SetMoveDir(Vector3.zero);
            animal.m_onDeath += StopRide;

            // Position player
            rider.SetMoveDir(Vector3.zero);
            ReflectionUtils.InvokePrivate(rider, "SetCrouch", new object[] { true });
            float height = GetComponent<Collider>().bounds.size.y;
            rider.gameObject.transform.position = gameObject.transform.position + new Vector3(0, height / 2f, 0);
            rider.gameObject.transform.rotation = gameObject.transform.rotation;
            FixedJoint joint = rider.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = GetComponent<Rigidbody>();

            // Set animal run speed
            oldAnimalSpeed = animal.m_speed;
            oldAnimalRunSpeed = animal.m_runSpeed;
            oldAnimalJumpForce = animal.m_jumpForce;
            animal.m_speed = speed;
            animal.m_runSpeed = runSpeed;
            animal.m_jumpForce = jumpForce;

            // Set rider run speed
            oldRiderSpeed = rider.m_speed;
            oldRiderRunSpeed = rider.m_runSpeed;
            rider.m_speed = 0;
            rider.m_runSpeed = 0;

            Debug.Log("StartRiding");
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Riding animal");
        }

        public void StopRide()
        {
            if (!isBeingRidden || !IsRiding || !rider)
            {
                return;
            }

            // Set status vars
            isBeingRidden = false;
            IsRiding = false;

            // Disable AnimalAI and stop their movement
            GetComponent<AnimalAI>().enabled = true;

            // Position player
            ReflectionUtils.InvokePrivate(rider, "SetCrouch", new object[] { false });
            Destroy(rider.GetComponent<FixedJoint>());

            // Restore animal run speed
            if (animal)
            {
                animal.m_speed = oldAnimalSpeed;
                animal.m_runSpeed = oldAnimalRunSpeed;
                animal.m_jumpForce = oldAnimalJumpForce;
            }

            // Set rider run speed
            rider.m_speed = oldRiderSpeed;
            rider.m_runSpeed = oldRiderRunSpeed;

            rider = null;
            Debug.Log("StopRiding");
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Stopped riding animal");
        }

        public bool CanRide(Humanoid user)
        {
            // Check if required skill level met
            if (requiredRidingLevel > 0)
            {
                Skills.SkillDef skill = SkillManager.Instance.GetSkill("riding");
                
                if (user.GetSkillFactor(skill.m_skill) < requiredRidingLevel)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Need to have min riding skill level " + requiredRidingLevel);
                    return false;
                }
            }

            // Check if saddle present
            if (requiresSaddle)
            {
                foreach (ItemDrop.ItemData item in user.GetInventory().GetAllItems())
                {
                    if (item.m_shared.m_name == "Saddle")
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You need a saddle to ride this animal");
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        public bool Interact(Humanoid user, bool hold)
        {
            if (IsRiding && isBeingRidden)
            {
                StopRide();
                return true;
            }

            if (CanRide(user))
            {
                rider = user;
                StartRide();
            }

            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (IsRiding && isBeingRidden)
            {
                StopRide();
                return true;
            }

            if (item.m_shared.m_name != "Saddle")
            {
                return false;
            }

            if (CanRide(user))
            {
                rider = user;
                StartRide();
            }

            return true;
        }
    }
}
