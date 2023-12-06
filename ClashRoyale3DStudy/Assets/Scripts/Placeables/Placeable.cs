using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityRoyale
{
    //所有游戏对象的基类，这些对象可以被放置在游戏区域内: units, obstacles, structures, etc.
    public class Placeable : MonoBehaviour
    {
        public PlaceableType pType; //游戏单位类型
		
        [HideInInspector] public Faction faction;   //阵营
        [HideInInspector] public PlaceableTarget targetType;    //攻击目标类型
		[HideInInspector] public AudioClip dieAudioClip;    //死亡音效

        public UnityAction<Placeable> OnDie;    //死亡时要做的一些表现（音效、动作动画）

        //游戏单位类型
        public enum PlaceableType
        {
            Unit, // 游戏单位（特指那些可移动的物体）
            Obstacle, // 障碍物（陨石）
            Building, // 建筑物（生产游戏单位）
            Spell, // 法术（对目标造成瞬时或者持续性的伤害）
            Castle, // 城堡（特殊类型的游戏单位）
        }

        //游戏单位的攻击目标类型
        public enum PlaceableTarget
        {
            OnlyBuildings, // 仅建筑
            Both, // 建筑和敌人
            None, // 无法攻击
        }

        //阵营
        public enum Faction
        {
            Player, //Red
            Opponent, //Blue
            None,
        }
    }
}