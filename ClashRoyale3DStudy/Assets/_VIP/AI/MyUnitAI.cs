using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUnitAI : MyAIBase
{
    public GameObject projectilePrefab;
    public Transform firePos;

    public void OnDealDamage()
    {
        if (this.target == null)
        {
            return;
        }
        this.target.GetComponent<MyPlaceableView>().data.hitPoints -= this.GetComponent<MyPlaceableView>().data.damagePerAttack;
        if (this.target.GetComponent<MyPlaceableView>().data.hitPoints < 0)
        {
            this.target.GetComponent<MyPlaceableView>().data.hitPoints = 0;
            this.target = null;
        }
    }

    public void OnFireProjectile()
    {
        //  实例化一个火球
        GameObject go = Instantiate(
            projectilePrefab, 
            firePos.position, 
            Quaternion.identity,
            MyProjectileMgr.instance.transform
            ); //放在手部位置（世界坐标），但是不以手部为父节点（不跟手移动）
        
        //  设置投掷物的释放着（用于投掷物命中目标后伤害结算）
        go.GetComponent<MyProjectile>().caster = this;
        go.GetComponent<MyProjectile>().target = this.target;
        
        //  投掷物的飞行被MyPlaceableMgr统一管理
        MyProjectileMgr.instance.mine.Add(go.GetComponent<MyProjectile>());
    }
}
