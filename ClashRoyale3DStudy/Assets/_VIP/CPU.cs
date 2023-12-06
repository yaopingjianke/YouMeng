using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityRoyale;

public class CPU : MonoBehaviour
{
    public float interval = 5;  //出牌时间间隔

    public Transform[] range = new Transform[2];    //生成敌人的范围

    //void Start()
    async void Start()
    {
        //StartCoroutine(CardOut());
        await CardOut();
    }

    //IEnumerator CardOut()
    async Task CardOut()
    {
        while (true)
        {

            var cardList = MyCardModel.instance.list;
            var cardData = cardList[Random.Range(0, cardList.Count)];
            // var viewList = MyCardView.CreatePlacable(
            var viewList = await MyCardView.CreatePlacable(
                cardData, 
                new Vector3(Random.Range(range[0].position.x, range[1].position.x), 0, Random.Range(range[0].position.z, range[1].position.z)), 
                MyPlaceableMgr.instance.transform,
                Placeable.Faction.Opponent);
            foreach (var view in viewList)
            {
                MyPlaceableMgr.instance.his.Add(view);
            }
            // yield return new WaitForSeconds(interval);  //采用设定的时间间隔出兵
            await new WaitForSeconds(interval);  //采用设定的时间间隔出兵
        }
    }
}
