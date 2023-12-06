using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏兵种的画面表现（例如动画等）
/// </summary>
public class MyPlaceableView : MonoBehaviour
{
    public MyPlaceable data; //游戏单位数据

    public float dieDuaration = 10f;    //死亡溶解总时间，按秒
    public float dieProgress = 0f;  //当前进度

    private void OnDestroy()
    {
        //直接在两方集合中都删一遍就行，就算集合中没有这个值，Remove方法也不会出错。
        MyPlaceableMgr.instance.mine.Remove(this);
        MyPlaceableMgr.instance.his.Remove(this);
    }
}
