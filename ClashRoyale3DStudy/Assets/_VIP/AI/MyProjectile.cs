using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyProjectile : MonoBehaviour
{
    public MyAIBase caster; //投掷物释放者
    public MyAIBase target; //投掷物释放者

    public float speed = 1; //速度，按秒

    public float progress = 0;  //飞行进度
}
