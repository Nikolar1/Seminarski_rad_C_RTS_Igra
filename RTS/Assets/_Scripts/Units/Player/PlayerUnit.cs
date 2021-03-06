using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NR.RTS.Units.Player
{
    [RequireComponent(typeof(VectorDestinationSetter))]
    public class PlayerUnit : SharedUnit
    {
        public AudioSource unitedEffectsSource;
        private bool isMining = false;
        private bool spawned = false;
        private void Update()
        {
            base.SharedUpdate();
            currentAttackCooldown -= Time.deltaTime;
            if (isRepairing)
            {
                Repair();
            }
            else if (isMining)
            {
                Work();
            }
            else if (hasTarget)
            {
                Attack();
            }
            else
            {
                CheckForEnemyTargets(true);
            }
        }

        protected void LateUpdate()
        {
            if (Combat.HandleHealth(healthBarAmount, currentHealth, baseStats.health))
            {
                Die();
            }
        }

        public void MoveUnit(Vector2 destination)
        {
            hasTarget = false;
            vDS.SetDestination(destination);
            if (spawned)
            {
                unitedEffectsSource.clip = baseStats.orderSounds[UnityEngine.Random.Range(0, baseStats.orderSounds.Length - 1)];
                unitedEffectsSource.Play();
            }
            else
            {
                spawned = true;
            }
        }

        public void MoveUnit(Transform target, bool isPlayerOwnedTarget = true, bool isResource = false)
        {

            if (isPlayerOwnedTarget)
            {
                //if the target is player owned unit will follow and stop just as it collides
                if (baseStats.canRepair)
                {
                    isRepairing = true;
                    this.target = target;
                }

                vDS.SetDestination(target, 1.2f);
                return;
            }
            else if (isResource && baseStats.canWork)
            {
                isMining = true;
                this.target = target;
                vDS.SetDestination(target);
                return;
            }
            this.target = target;
            hasTarget = true;
            targetUnit = target.GetComponent<Enemy.EnemyUnit>();

            //1.2 is the distance needed for the unit to stop just as it collides with the enemy
            vDS.SetDestination(target, 1.2f + baseStats.range);
            unitedEffectsSource.clip = baseStats.orderSounds[UnityEngine.Random.Range(0, baseStats.orderSounds.Length - 1)];
            unitedEffectsSource.Play();
        }

        public void Work()
        {
            if (target == null)
            {
                isMining = false;
                vDS.RemoveDestination();
                return;
            }
            bool aceptableDistance = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 1.2f))
            {
                if (collider.gameObject.transform == target)
                {
                    aceptableDistance = true;
                }
            }
            if (currentAttackCooldown <= 0 && aceptableDistance)
            {
                float temp = target.GetComponent<Resources.Resource>().Mine();
                currentAttackCooldown = attackCooldown;
                RTS.Player.PlayerResourceManager.instance.AddGold(temp);
                clipQueue.Add(baseStats.workingSounds[UnityEngine.Random.Range(0, baseStats.workingSounds.Length - 1)]);
                if (temp < 25)
                {
                    target = null;
                    isMining = false;
                    vDS.RemoveDestination();
                    return;
                }
            }
        }

        new private void Die()
        {
            InputManager.InputHandler.instance.selectedUnits.Remove(gameObject.transform);
            transform.GetComponent<Interactable.IUnit>().OnInteractExit();
            RTS.Player.VoiceAssistant.instance.PlayUnitLost();
            base.Die();
        }

        
    }
}