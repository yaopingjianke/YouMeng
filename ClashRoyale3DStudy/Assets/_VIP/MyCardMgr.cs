using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;

public class MyCardMgr : MonoBehaviour
{
    public static MyCardMgr instance;

    //活动牌
    public Transform[] cards;

    //卡牌预制体（弓箭手/战士/法师/......）
    //public GameObject[] cardPrefabs;

    //创建出的卡牌必须放在Canvas下，否则显示不出来
    public Transform canvas;

    //发牌动画的起始位置和终止位置
    public Transform startPos, endPos;

    public MeshRenderer forbiddenAreaRenderer;  //禁止区域的网格渲染器

    //预览卡牌
    private Transform previewCard;

    private void Awake()
    {
        instance = this;
    }

    //void Start()
    async void Start()
    {
        //StartCoroutine(CreateCardToPreview(0.5f));
        //StartCoroutine(PreviewToActive(0, 1f));

        //StartCoroutine(CreateCardToPreview(1.5f));
        //StartCoroutine(PreviewToActive(1, 2f));

        //StartCoroutine(CreateCardToPreview(2.5f));
        //StartCoroutine(PreviewToActive(2, 3f));

        //StartCoroutine(CreateCardToPreview(3.5f));

        // NOTE：这里的await知识异步等待，没有new一个Task对象，所以本方法的返回值可以为void
        //await CreateCardToPreview(0.5f);
        //await PreviewToActive(0, 1f);

        //await CreateCardToPreview(1.5f);
        //await PreviewToActive(1, 2f);

        //await CreateCardToPreview(2.5f);
        //await PreviewToActive(2, 3f);

        //await CreateCardToPreview(3.5f);

        await CreateCardToPreview(0.5f);
        await PreviewToActive(0, 0.5f);

        await CreateCardToPreview(0.5f);
        await PreviewToActive(1, 0.5f);

        await CreateCardToPreview(0.5f);
        await PreviewToActive(2, 0.5f);

        await CreateCardToPreview(0.5f);
    }

    /// <summary>
    /// 创建卡牌并移动到预览区
    /// </summary>
    public async Task CreateCardToPreview(float delay)
    {
        //yield return new WaitForSeconds(delay);
        await new WaitForSeconds(delay);    //  这里会创建一个Task，在await时c#会返回这个Task对象，所以返回值类型不能写void

        int iCard = Random.Range(0, MyCardModel.instance.list.Count);
        MyCard card = MyCardModel.instance.list[iCard];

        //GameObject cardPrefab = Resources.Load<GameObject>(card.cardPrefab);
        ////GameObject cardPrefab = cardPrefabs[Random.Range(0, cardPrefabs.Length)];
        //previewCard = Instantiate(cardPrefab).transform;

        //  由于是异步实例化，所以我们不能通过InstantiateAsync的返回值直接获取到创建的卡牌对象
        //  我们需要等待异步实例化完毕，同时又不能阻塞Unity程序的执行（会造成卡顿）
        //  所以我们要用c#的异步等待语法
        //  Note：在Addressable系统中，InstantiateAsync == Resources.Load + Instantiate;
        //  Note：这里报错是因为await异步等待必须写在支持异步的方法里——必须声明该方法为异步方法
        //  Note：用了异步就可以不再使用写成了，前提是我们要引入支持协程所有功能（WaitForSeconds/WaitForEndOfFrame）的一个库
        //  去github搜索Unity3dAsyncAwaitUtil这个工具，下载导入unity中
        GameObject cardPrefab = await Addressables.InstantiateAsync(card.cardPrefab).Task;
        previewCard = cardPrefab.transform;

        //false的作用是：将该物体置于父节点下的(0, 0, 0)位置
        previewCard.SetParent(canvas, false);
        previewCard.localScale = Vector3.one * 0.7f;
        previewCard.position = startPos.position;
        previewCard.DOMove(endPos.position, 0.5f);

        previewCard.GetComponent<MyCardView>().data = card;
    }

    /// <summary>
    /// 卡牌从预览区移动到活跃区
    /// </summary>
    //public IEnumerator PreviewToActive(int i, float delay)
    public async Task PreviewToActive(int i, float delay)
    {
        await new WaitForSeconds(delay);

        previewCard.localScale = Vector3.one;
        previewCard.DOMove(cards[i].position, 0.5f);

        previewCard.GetComponent<MyCardView>().index = i;
    }
}
