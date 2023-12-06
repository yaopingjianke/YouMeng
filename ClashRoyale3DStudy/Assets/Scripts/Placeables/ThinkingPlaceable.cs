using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityRoyale
{
    //具有AI的游戏单位
    public class ThinkingPlaceable : Placeable
    {
        [HideInInspector] public States state = States.Dragged; //角色当前的状态
        public enum States
        {
            Dragged, //玩家正在拖拽一张游戏单位卡牌，尚未放到游戏区域内（预览区域）
            Idle, //卡牌变兵后的初始状态
            Seeking, //走向攻击目标
            Attacking, //循环播放攻击动画、不移动
            Dead, //播放死亡动画，然后从游戏区域销毁
        }

        [HideInInspector] public AttackType attackType;
        public enum AttackType
        {
            Melee, // 近程攻击
            Ranged, // 远程攻击
        }

        [HideInInspector] public ThinkingPlaceable target;  //攻击目标（只能是ThinkingPlaceable）
        [HideInInspector] public HealthBar healthBar;   //血条

        [HideInInspector] public float hitPoints;   //血值
        [HideInInspector] public float attackRange; //攻击范围
        [HideInInspector] public float attackRatio; //攻击速率
        [HideInInspector] public float lastBlowTime = -1000f;   //上次打击时间（因为攻击之间要有间隔）  
        [HideInInspector] public float damage;  //攻击伤害值
		[HideInInspector] public AudioClip attackAudioClip; //攻击音效
        
        [HideInInspector] public float timeToActNext = 0f;  //下一次造成的伤害

		//Inspector references
		[Header("Projectile for Ranged")]   
		public GameObject projectilePrefab; //投掷物预制体
        public Transform projectileSpawnPoint;  //投掷物的生成位置（弓箭手、法师的手部）  

        private Projectile projectile;  //projectilePrefab创建的投掷物实例
        protected AudioSource audioSource;  //攻击音效

		public UnityAction<ThinkingPlaceable> OnDealDamage, OnProjectileFired;  //攻击造成伤害的回调，投掷物发射的回调函数

        //设置攻击目标
        public virtual void SetTarget(ThinkingPlaceable t)
        {
            target = t;
            t.OnDie += TargetIsDead;
        }

        //开始攻击
        public virtual void StartAttack()
        {
            state = States.Attacking;
        }

        //处理打击效果
        public virtual void DealBlow()
        {
            lastBlowTime = Time.time;
        }

		// 被Animation的Event调用，处理攻击伤害
		public void DealDamage()
        {
			//only melee units play audio when the attack deals damage
			if(attackType == AttackType.Melee)
				audioSource.PlayOneShot(attackAudioClip, 1f);

			if(OnDealDamage != null)
				OnDealDamage(this);
		}

		// 被Animation的Event调用，发射投射物
		public void FireProjectile()
        {
			//ranged units play audio when the projectile is fired
			audioSource.PlayOneShot(attackAudioClip, 1f);

			if(OnProjectileFired != null)
				OnProjectileFired(this);
		}

        //寻路
        public virtual void Seek()
        {
            state = States.Seeking;
        }

        //判断目标是否已死亡
        protected void TargetIsDead(Placeable p)
        {
            //Debug.Log("My target " + p.name + " is dead", gameObject);
            state = States.Idle;
            
            target.OnDie -= TargetIsDead;

            timeToActNext = lastBlowTime + attackRatio;
        }
        
        //判断目标是否在攻击范围内
        public bool IsTargetInRange()
        {
            return (transform.position-target.transform.position).sqrMagnitude <= attackRange*attackRange;
        }

        //受到攻击处理
        public float SufferDamage(float amount)
        {
            hitPoints -= amount;
            //Debug.Log("Suffering damage, new health: " + hitPoints, gameObject);
            if(state != States.Dead
				&& hitPoints <= 0f)
            {
                Die();
            }

            return hitPoints;
        }

        //停止移动
		public virtual void Stop()
		{
			state = States.Idle;
		}

        //死亡
        protected virtual void Die()
        {
            state = States.Dead;
			audioSource.pitch = Random.Range(.9f, 1.1f);
			audioSource.PlayOneShot(dieAudioClip, 1f);

			if(OnDie != null)
            	OnDie(this);
        }
    }
}
