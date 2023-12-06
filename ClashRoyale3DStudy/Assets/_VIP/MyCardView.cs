using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityRoyale;

public class MyCardView : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    //这个卡牌的数据
    public MyCard data;

    //位于活跃区的第几位
    public int index;

    //预览卡牌的位置
    private Transform previewHolder;

    //主相机
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        previewHolder = GameObject.Find("PreviewHolder").transform;
    }

    /// <summary>
    /// 按下
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        //将该卡牌放到所在同级节点的最后一个，以保证拖拽过程显示在其他卡牌上方
        transform.SetAsLastSibling();

        //将地方区域渲染为禁放区
        MyCardMgr.instance.forbiddenAreaRenderer.enabled = true;
    }

    //是否已经卡牌变兵
    private bool isDragging = false;

    /// <summary>
    /// 拖拽
    /// </summary>
    public async void OnDrag(PointerEventData eventData)
    {
        //移动卡牌到鼠标位置
        //RectTransformUtility.ScreenPointToWorldPointInRectangle(把屏幕空间转换到哪一个用户界面下面，屏幕坐标，null, 返回值：转成世界坐标的数据)：将屏幕坐标转换为三维空间中的坐标
        //eventData包含屏幕空间坐标
        RectTransformUtility.ScreenPointToWorldPointInRectangle(transform.parent as RectTransform
            , eventData.position, null, out Vector3 posWorld);

        transform.position = posWorld;

        //从鼠标位置发射一条射线
        Ray ray = mainCam.ScreenPointToRay(eventData.position);

        //判断该射线碰到场景什么位置:射线投射：Physics.Raycast(涉嫌，返回值包含射线检测的位置，最大投射距离(float.PositiveInfinity表示无穷远)，层的编号)
        bool hitGround = Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, 1 << LayerMask.NameToLayer("PlayingField"));

        //如果碰到场景物体
        if (hitGround)
        {
            previewHolder.position = hit.point;

            //如果卡牌之前没有被拖拽出来(如果没有变成小兵)
            if (isDragging == false)
            {
                isDragging = true;  //防止重入

                //1、隐藏该卡牌
                GetComponent<CanvasGroup>().alpha = 0;

                //2.创建预览卡牌
                // 这里暂时不能用await，因为CreatePlacable还没完成之前，if这个代码段可能已经被重入了无数次
                // 就会创建多个重叠的角色，所以要么不用await，要么isDragging = true前移
                await CreatePlacable(data, hit.point, previewHolder.transform, Placeable.Faction.Player);
                // await CreatePlacable(data, hit.point, previewHolder.transform, Placeable.Faction.Player);

            }
            else
            {
                print("命中地面 & 卡牌已经变兵");
                // 1、标记卡牌为未激活（未显示出预览小兵）
                // 2、显示卡牌
                // 3、销毁预览用的小兵
            }
        }
        else    //鼠标没有命中地面（放回出牌位置）
        {
            if (isDragging)     //如果卡牌曾经激活（曾经放到场景中了）
            {
                print("鼠标没有命中地面(放回出牌位置)");
                //1、标记卡牌为未激活（未显示预览小兵）
                isDragging = false;

                //2、显示卡牌
                GetComponent<CanvasGroup>().alpha = 1f;

                //3、销毁预览用的小兵
                foreach (Transform trUnit in previewHolder)
                {
                    Destroy(trUnit.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 根据兵种数据，创建一个兵种到场地中
    /// </summary>
    //public static async TaskList<MyPlaceableView> CreatePlacable(MyCard carData, Vector3 pos, Transform parent, Placeable.Faction faction)
    public static async Task<List<MyPlaceableView>> CreatePlacable(MyCard carData, Vector3 pos, Transform parent, Placeable.Faction faction) 
    {
        List<MyPlaceableView> viewList = new List<MyPlaceableView>();

        //从卡牌数据找出该卡牌的数据
        for (int i = 0; i < carData.placeablesIndices.Length; i++)
        {
            //2.1、取出小兵数据
            int unitId = carData.placeablesIndices[i];

            MyPlaceable p = null;
            for (int j = 0; j < MyPlaceableModel.instance.list.Count; j++)
            {
                if (MyPlaceableModel.instance.list[j].id == unitId)
                {
                    p = MyPlaceableModel.instance.list[j];
                    break;
                }
            }

            //2.2、取出小兵之间的相对偏移
            Vector3 offect = carData.relativeOffsets[i];

            //2.3、生成该卡牌对应的小兵数组，并且将其设置为预览用的卡牌（将其放置到一个统一的节点下）
            // GameObject unitPrefab = Resources.Load<GameObject>(p.associatedPrefab);
            //GameObject unit = GameObject.Instantiate(unitPrefab, previewHolder, false);
            //unit.transform.localPosition = offect; 

            // parent.position = pos;
            // GameObject unit = GameObject.Instantiate(unitPrefab, parent, false);

            string prefabName = faction == Placeable.Faction.Player ? p.associatedPrefab : p.alternatePrefab;
            GameObject unit = await Addressables.InstantiateAsync(prefabName, parent, false).Task;

            unit.transform.localPosition = offect;
            unit.transform.position = pos + offect;

            MyPlaceable p2 = p.Clone();
            p2.faction = faction;
            MyPlaceableView view = unit.GetComponent<MyPlaceableView>();
            view.data = p2;
            viewList.Add(view);
        }

        return viewList;
    }

    /// <summary>
    /// 鼠标抬起
    /// </summary>
    public async void OnPointerUp(PointerEventData eventData)
    {
        //从鼠标位置发射一条射线
        Ray ray = mainCam.ScreenPointToRay(eventData.position);

        //判断该射线碰到场景什么位置
        bool hitGround = Physics.Raycast(ray, float.PositiveInfinity, 1 << LayerMask.NameToLayer("PlayingField"));

        if (hitGround)
        {
            OnCardUsed();

            // 销毁打出去的卡牌
            Destroy(this.gameObject);

            // 从预览区取出一张卡牌放到出牌区
            await MyCardMgr.instance.PreviewToActive(index, 0.5f);

            // 生成一张卡牌放到预览区
            await MyCardMgr.instance.CreateCardToPreview(1f);

        }
        else  
        {
            //卡牌放回出牌区
            transform.DOMove(MyCardMgr.instance.cards[index].position, 0.2f);
        }
        MyCardMgr.instance.forbiddenAreaRenderer.enabled = false;
    }

    /// <summary>
    /// 把预览用的兵变成实际的放在地面上的兵
    /// </summary>
    private void OnCardUsed()
    {
        // 游戏单位放到游戏单位管理器（MyPlaceableView）下
        for (int i = previewHolder.childCount - 1; i >= 0; i--)
        {
            Transform trUnit = previewHolder.GetChild(i);

            trUnit.SetParent(MyPlaceableMgr.instance.transform, true);

            MyPlaceableMgr.instance.mine.Add(trUnit.GetComponent<MyPlaceableView>());
        } 
    }
}
