using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityRoyale;

public partial class MyPlaceable
{
    public Placeable.Faction faction = Placeable.Faction.None;

    public MyPlaceable Clone()
    {
        return this.MemberwiseClone() as MyPlaceable;   //这里是浅拷贝（有空去了解一下深拷贝）
    }
}

/// <summary>
/// 游戏单位管理器
/// </summary>
public class MyPlaceableMgr : MonoBehaviour
{
    public static MyPlaceableMgr instance;

    // 己方小兵
    public List<MyPlaceableView> mine = new List<MyPlaceableView>();
    // 敌方小兵
    public List<MyPlaceableView> his = new List<MyPlaceableView>();
    

    public Transform trHisTower, trMyTower;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        his.Add(trHisTower.GetComponent<MyPlaceableView>());
        mine.Add(trMyTower.GetComponent<MyPlaceableView>());
    }

    void Update()
    {
        //游戏AI的更新调用
        UpdatePlaceable(mine);
        UpdatePlaceable(his);
    }

    /// <summary>
    /// 游戏AI的更新调用
    /// </summary>
    private void UpdatePlaceable(List<MyPlaceableView> pViews)
    {
        //游戏AI的更新调用
        for (int i = 0; i < pViews.Count; i++)
        {
            //1、区分游戏角色的状态
            MyPlaceableView view = pViews[i]; //游戏兵种上挂的跟角色数据和表现相关的脚本
            MyPlaceable data = view.data;
            MyAIBase ai = view.GetComponent<MyAIBase>(); //获取view所在对象的以MyAIBase为基类的脚本组件(MyUnit)
            NavMeshAgent nav = view.GetComponent<NavMeshAgent>();
            Animator ani = view.GetComponent<Animator>();

            //按照游戏单位当前的状态，执行状态机
            //  1、执行状态内的动作
            //  2、执行检测，判断是否要转移到其他状态
            //  3、执行状态转移
            switch (ai.state)
            {
                case AIState.Idle:
                    {
                        if (ai is MyBuildingAI)
                        {
                            //  要让国王塔具有攻击能力，直接使其跳转到AIState.Attack状态即可
                            break;
                        }
                        //  找场景内最近的敌人去攻击
                        ai.target = FindNearestEnemy(ai.transform.position, data.faction);

                        //  走到目标附近， 要停止移动
                        if (ai.target != null)
                        {

                            ai.state = AIState.Seek;

                            nav.enabled = true;

                            //  行走时要播放动画
                            ani.SetBool("IsMoving", true);
                        }
                        else
                        {
                            ani.SetBool("IsMoving", false);
                        }

                        //  检测是否敌人在范围内
                        //float a = this.GetComponent<CanvasGroup>().alpha;
                        //a = Mathf.Lerp(a, 0, (Time.time - startTime));
                        //this.GetComponent<CanvasGroup>().alpha = a;

                        //  若是，则转移到Seek状态
                    }
                    break;
                case AIState.Seek:
                    {
                        //可能出现一个目标被多个游戏单位攻击的情况，所以你没有把目标打死，不代表目标不会死，目标死亡，我们的代码会将target置为null，要检测
                        if (ai.target == null)  
                        {
                            ai.state = AIState.Idle;
                            break;
                        }

                        //  往敌人方向前进
                        nav.destination = ai.target.transform.position;

                        //  判断是否进入攻击范围
                        if (IsInAttackRange(view.transform.position, ai.target.transform.position, view.data.attackRange))
                        {
                            //  若是，则
                            //  1、停止移动
                            nav.enabled = false;

                            //  2、转移到攻击状态
                            ai.state = AIState.Attack;
                        }

                    }
                    break;
                case AIState.Attack:
                    {
                        if (ai.target == null)
                        {
                            ai.state = AIState.Idle;
                            break;
                        }

                        if (IsInAttackRange(view.transform.position, ai.target.transform.position, view.data.attackRange) == false)
                        {
                            // 切换到空闲状态
                            ai.state = AIState.Idle;
                            break;
                        }

                        // 如果在攻击间隔内，则不攻击
                        if (Time.time < ai.lastBlowTime + data.attackRatio)
                        {
                            break;
                        }

                        //面向目标
                        ai.transform.LookAt(ai.target.transform);

                        //  攻击时不要移动
                        ani.SetBool("IsMoving", false);

                        //  执行攻击动作
                        ani.SetTrigger("Attack");

                        //  攻击伤害结算
                        if (ai.target.GetComponent<MyPlaceableView>().data.hitPoints <= 0)
                        {
                            OnEnterDie(ai.target);

                            ai.state = AIState.Idle;
                        }

                        //设置上一次攻击的时间为当前时间
                        ai.lastBlowTime = Time.time;
                    }
                    break;
                case AIState.Die:
                    {
                        if (ai is MyBuildingAI)
                        {
                            //  要让国王塔具有攻击能力，直接使其跳转到AIState.Attack状态即可
                            break;
                        }
                        var rds = ai.GetComponentsInChildren<Renderer>();
                        view.dieProgress += Time.deltaTime * (1 / view.dieDuaration);
                        foreach (var rd in rds)
                        {
                            rd.material.SetFloat("_DissolveFactor", view.dieProgress);
                        }
                    }
                    break;
            }
        }

    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void OnEnterDie(MyAIBase target)
    {
        print($"{target.gameObject.name} is dead!");

        //  防止重复进入死亡状态
        if (target.state == AIState.Die)
        {
            return;
        }

        if (target.GetComponent<NavMeshAgent>() != null)
        {
            target.GetComponent<NavMeshAgent>().enabled = false;
        }


        //  1.设置死亡状态
        target.GetComponent<MyAIBase>().state = AIState.Die;
        target.GetComponent<MyPlaceableView>().data.hitPoints = 0;

        //  2.播放死亡动画
        if (target.GetComponent<Animator>() != null)
        {
            target.GetComponent<Animator>().SetTrigger("IsDead");
        }

        //  3.死亡溶解
        var rds = target.GetComponentsInChildren<Renderer>();   //不管是Mesh Render还是Skinned Mesh Render（蒙皮网格）都是Rrender类型
        var view = target.GetComponent<MyPlaceableView>();
        var color = view.data.faction == Placeable.Faction.Player ? Color.red : Color.blue;
        view.dieProgress = 0;
        foreach (var rd in rds)
        {
            Debug.Log(color);
            rd.material.SetColor("_EdgeColor", color * 8);
            rd.material.SetFloat("_EdgeWidth", 0.1f);
            rd.material.SetFloat("_DissolveFactor", view.dieProgress);
        }

        //  4.设置对象destroy延时
        Destroy(target.gameObject, view.dieDuaration);
    }

    private bool IsInAttackRange(Vector3 myPos, Vector3 targetPos, float attackRange)
    {
        return Vector3.Distance(myPos, targetPos) < attackRange;
    }

    /// <summary>
    /// 此方法为玩家、敌人通用方式，玩家找敌人，敌人找玩家，所以要传一个当前角色类型出去
    /// </summary>
    /// <param name="faction"></param>
    /// <returns></returns>
    private MyAIBase FindNearestEnemy(Vector3 myPos, Placeable.Faction faction)
    {
        List<MyPlaceableView> units = faction == Placeable.Faction.Player ? his : mine;

        float x = float.MaxValue;
        MyAIBase nearest = null;
        foreach (MyPlaceableView unit in units)
        {
            float d = Vector3.Distance(unit.transform.position, myPos);
            if (d < x && unit.data.hitPoints > 0)
            {
                x = d;
                nearest = unit.GetComponent<MyAIBase>();
            }
        }

        return nearest;
    }
}
