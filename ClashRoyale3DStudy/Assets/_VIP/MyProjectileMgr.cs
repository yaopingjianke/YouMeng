using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MyProjectileMgr : MonoBehaviour
{
    public static MyProjectileMgr instance = null;

	public List<MyProjectile> mine = new List<MyProjectile>();
	public List<MyProjectile> his = new List<MyProjectile>();

    public void Awake()
    {
        instance = this;
    }

	// Unity每一帧画面自动调用Update
	void Update()
	{
		// 子弹飞行和命中运算
		UpdateProjectiles(mine);
		UpdateProjectiles(his);
	}

	private void UpdateProjectiles(List<MyProjectile> projList)
	{
		List<MyProjectile> destroyProjList = new List<MyProjectile>();
		for (int i = 0; i < projList.Count; i++)
		{
			var proj = projList[i];
			MyUnitAI casterAI = proj.caster as MyUnitAI;
			MyAIBase targetAI = proj.target;

			proj.progress += Time.deltaTime * proj.speed;

			Debug.Assert(proj.caster != null);
			
			if (proj.target == null)
			{
				Addressables.ReleaseInstance(proj.gameObject);
				destroyProjList.Add(proj);
				continue;
			}

			proj.transform.position = Vector3.Lerp(casterAI.firePos.position,
				proj.target.transform.position + Vector3.up, proj.progress);

			if (proj.progress >= 1f)
			{
				casterAI.OnDealDamage();

				if (targetAI.GetComponent<MyPlaceableView>().data.hitPoints <= 0)
				{
					MyPlaceableMgr.instance.OnEnterDie(targetAI);
				}
				//Destroy(proj.gameObject);
				Addressables.ReleaseInstance(proj.gameObject);
				destroyProjList.Add(proj);
			}
		}

		foreach (var des in destroyProjList)
		{
			projList.Remove(des);
		}
	}
}
