using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace UnityRoyale
{
    public class GameManager : MonoBehaviour
    {
		[Header("Settings")]
		public bool autoStart = false;

		[Header("Public References")]
        public NavMeshSurface navMesh;
		public GameObject playersCastle, opponentCastle;
		public GameObject introTimeline;
        public PlaceableData castlePData;
		public ParticlePool appearEffectPool;

        private CardManager cardManager;    //我方出牌
        private CPUOpponent CPUOpponent;    //敌方出牌
        private InputManager inputManager;
		private AudioManager audioManager;  //音效管理
		private UIManager UIManager;
		private CinematicsManager cinematicsManager;

        private List<ThinkingPlaceable> playerUnits, opponentUnits; //自己和对手的可移动单位
        private List<ThinkingPlaceable> playerBuildings, opponentBuildings; //自己和对手的建筑单位
        private List<ThinkingPlaceable> allPlayers, allOpponents; //所有的己方单位（playerUnits+playerBuildings），对手单位（opponentUnits + opponentBuildings）
        private List<ThinkingPlaceable> allThinkingPlaceables; //所有单位（allPlayers + allOpponents）
        private List<Projectile> allProjectiles;
        private bool gameOver = false;
        private bool updateAllPlaceables = false; //used to force an update of all AIBrains in the Update loop
        private const float THINKING_DELAY = 2f;

        private void Awake()
        {
            cardManager = GetComponent<CardManager>();
            CPUOpponent = GetComponent<CPUOpponent>();
            inputManager = GetComponent<InputManager>();
			audioManager = GetComponentInChildren<AudioManager>();
			cinematicsManager = GetComponentInChildren<CinematicsManager>();
			UIManager = GetComponent<UIManager>();

			if(autoStart)
				introTimeline.SetActive(false);

			//listeners on other managers
			cardManager.OnCardUsed += UseCard;
			CPUOpponent.OnCardUsed += UseCard;

			//initialise Placeable lists, for the AIs to pick up and find a target
			playerUnits = new List<ThinkingPlaceable>();
            playerBuildings = new List<ThinkingPlaceable>();
            opponentUnits = new List<ThinkingPlaceable>();
            opponentBuildings = new List<ThinkingPlaceable>();
            allPlayers = new List<ThinkingPlaceable>();
            allOpponents = new List<ThinkingPlaceable>();
			allThinkingPlaceables = new List<ThinkingPlaceable>();
			allProjectiles = new List<Projectile>();
        }

        private void Start()
        {
			//Insert castles into lists
			SetupPlaceable(playersCastle, castlePData, Placeable.Faction.Player);
            SetupPlaceable(opponentCastle, castlePData, Placeable.Faction.Opponent);

			cardManager.LoadDeck();
            CPUOpponent.LoadDeck();

			audioManager.GoToDefaultSnapshot();

			if(autoStart)
				StartMatch();
        }

		//called by the intro cutscene
		public void StartMatch()
		{
			CPUOpponent.StartActing();
		}

        //the Update loop pings all the ThinkingPlaceables in the scene, and makes them act
        private void Update()
        {
            if(gameOver)
                return;

            ThinkingPlaceable targetToPass; //ref
			ThinkingPlaceable p; //ref

			for(int pN=0; pN<allThinkingPlaceables.Count; pN++)
            {
                p = allThinkingPlaceables[pN];

                if(updateAllPlaceables)
                    p.state = ThinkingPlaceable.States.Idle; //forces the assignment of a target in the switch below

                switch(p.state)
                {
                    case ThinkingPlaceable.States.Idle:
                        //this if is for innocuous testing Units
                        if(p.targetType == Placeable.PlaceableTarget.None)
                            break;

                        //找到最近的可攻击单位目标并将其分配给ThinkingPlaceable
                        bool targetFound = FindClosestInList(p.transform.position, GetAttackList(p.faction, p.targetType), out targetToPass);
                        if(!targetFound) Debug.LogError("No more targets!"); //this should only happen on Game Over
                        p.SetTarget(targetToPass);
						p.Seek();
                        break;


                    case ThinkingPlaceable.States.Seeking:
						if(p.IsTargetInRange())
                    	{
							p.StartAttack();
						}
                        break;
                        

					case ThinkingPlaceable.States.Attacking:
						if(p.IsTargetInRange())
						{
                            //当前攻击间隔时间超过攻击间隔时间
							if(Time.time >= p.lastBlowTime + p.attackRatio)
							{
								p.DealBlow();
								// 通过调用动画事件OnDealDamage和OnProjectileFired，动画会产生伤害，参见ThinkingPlaceable
							}
						}
						break;

					case ThinkingPlaceable.States.Dead:
						Debug.LogError("A dead ThinkingPlaceable shouldn't be in this loop");
						break;
                }
            }

			Projectile currProjectile;
			float progressToTarget;
            //遍历所有投掷物
			for(int prjN=0; prjN<allProjectiles.Count; prjN++)
            {
				currProjectile = allProjectiles[prjN];
                //投掷物移动
				progressToTarget = currProjectile.Move();
				if(progressToTarget >= 1f)
				{
					if(currProjectile.target.state != ThinkingPlaceable.States.Dead) //target might be dead already as this projectile is flying
					{
                        //受击处理
						float newHP = currProjectile.target.SufferDamage(currProjectile.damage);
						currProjectile.target.healthBar.SetHealth(newHP);
					}
					Destroy(currProjectile.gameObject);
					allProjectiles.RemoveAt(prjN);
				}
			}

            updateAllPlaceables = false; //is set to true by UseCard()
        }

        //按照游戏单位的攻击目标类型计算可攻击单位列表
        private List<ThinkingPlaceable> GetAttackList(Placeable.Faction f, Placeable.PlaceableTarget t)
        {
            switch(t)
            {
                case Placeable.PlaceableTarget.Both:
                    return (f == Placeable.Faction.Player) ? allOpponents : allPlayers;
				case Placeable.PlaceableTarget.OnlyBuildings:
                    return (f == Placeable.Faction.Player) ? opponentBuildings : playerBuildings;
				default:
					Debug.LogError("What faction is this?? Not Player nor Opponent.");
					return null;
            }
        }

        //找到最近的可攻击单位目标并将其分配给ThinkingPlaceable
        private bool FindClosestInList(Vector3 p, List<ThinkingPlaceable> list, out ThinkingPlaceable t)
        {
            t = null;
            bool targetFound = false;
            float closestDistanceSqr = Mathf.Infinity; //anything closer than here becomes the new designated target

            for(int i=0; i<list.Count; i++)
            {                
				float sqrDistance = (p - list[i].transform.position).sqrMagnitude;
                if(sqrDistance < closestDistanceSqr)
                {
                    t = list[i];
                    closestDistanceSqr = sqrDistance;
                    targetFound = true;
                }
            }

            return targetFound;
        }

        public void UseCard(CardData cardData, Vector3 position, Placeable.Faction pFaction)
        {
            for(int pNum=0; pNum<cardData.placeablesData.Length; pNum++)
            {
                PlaceableData pDataRef = cardData.placeablesData[pNum];
                Quaternion rot = (pFaction == Placeable.Faction.Player) ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                //Prefab to spawn is the associatedPrefab if it's the Player faction, otherwise it's alternatePrefab. But if alternatePrefab is null, then first one is taken
                GameObject prefabToSpawn = (pFaction == Placeable.Faction.Player) ? pDataRef.associatedPrefab : ((pDataRef.alternatePrefab == null) ? pDataRef.associatedPrefab : pDataRef.alternatePrefab);
                GameObject newPlaceableGO = Instantiate<GameObject>(prefabToSpawn, position + cardData.relativeOffsets[pNum], rot);

				if (SceneManager.GetActiveScene().buildIndex >= 8)
				{
					SetupPlaceable(newPlaceableGO, pDataRef, pFaction);
				}

				if (SceneManager.GetActiveScene().buildIndex >= 7)
				{
					appearEffectPool.UseParticles(position + cardData.relativeOffsets[pNum]);
				}
            }
			audioManager.PlayAppearSFX(position);

            updateAllPlaceables = true; //will force all AIBrains to update next time the Update loop is run
        }


        //setups all scripts and listeners on a Placeable GameObject
        private void SetupPlaceable(GameObject go, PlaceableData pDataRef, Placeable.Faction pFaction)
        {
            //Add the appropriate script
                switch(pDataRef.pType)
                {
                    case Placeable.PlaceableType.Unit:
                        Unit uScript = go.GetComponent<Unit>();
                        uScript.Activate(pFaction, pDataRef); //enables NavMeshAgent
						uScript.OnDealDamage += OnPlaceableDealtDamage;
						uScript.OnProjectileFired += OnProjectileFired;
                        AddPlaceableToList(uScript); //add the Unit to the appropriate list
                        UIManager.AddHealthUI(uScript);
                        break;

                    case Placeable.PlaceableType.Building:
                    case Placeable.PlaceableType.Castle:
                        Building bScript = go.GetComponent<Building>();
                        bScript.Activate(pFaction, pDataRef);
						bScript.OnDealDamage += OnPlaceableDealtDamage;
						bScript.OnProjectileFired += OnProjectileFired;
                        AddPlaceableToList(bScript); //add the Building to the appropriate list
                        UIManager.AddHealthUI(bScript);

                        //special case for castles
                        if(pDataRef.pType == Placeable.PlaceableType.Castle)
                        {
                            bScript.OnDie += OnCastleDead;
                        }
                        
                        navMesh.BuildNavMesh(); //rebake the Navmesh
                        break;

                    case Placeable.PlaceableType.Obstacle:
                        Obstacle oScript = go.GetComponent<Obstacle>();
                        oScript.Activate(pDataRef);
                        navMesh.BuildNavMesh(); //rebake the Navmesh
                        break;

                    case Placeable.PlaceableType.Spell:
                        //Spell sScript = newPlaceable.AddComponent<Spell>();
                        //sScript.Activate(pFaction, cardData.hitPoints);
                        //TODO: activate the spell and… ?
                        break;
                }

                go.GetComponent<Placeable>().OnDie += OnPlaceableDead;
        }

		private void OnProjectileFired(ThinkingPlaceable p)
		{
			Vector3 adjTargetPos = p.target.transform.position;
			adjTargetPos.y = 1.5f;
			Quaternion rot = Quaternion.LookRotation(adjTargetPos-p.projectileSpawnPoint.position);

			Projectile prj = Instantiate<GameObject>(p.projectilePrefab, p.projectileSpawnPoint.position, rot).GetComponent<Projectile>();
			prj.target = p.target;
			prj.damage = p.damage;
			allProjectiles.Add(prj);
		}

		private void OnPlaceableDealtDamage(ThinkingPlaceable p)
		{
			if(p.target.state != ThinkingPlaceable.States.Dead)
			{
				float newHealth = p.target.SufferDamage(p.damage);
				p.target.healthBar.SetHealth(newHealth);
			}
		}

		private void OnCastleDead(Placeable c)
		{
			cinematicsManager.PlayCollapseCutscene(c.faction);
            c.OnDie -= OnCastleDead;
            gameOver = true; //stops the thinking loop

			//stop all the ThinkingPlaceables		
			ThinkingPlaceable thkPl;
			for(int pN=0; pN<allThinkingPlaceables.Count; pN++)
            {
				thkPl = allThinkingPlaceables[pN];
				if(thkPl.state != ThinkingPlaceable.States.Dead)
				{
					thkPl.Stop();
					thkPl.transform.LookAt(c.transform.position);
					UIManager.RemoveHealthUI(thkPl);
				}
			}

			audioManager.GoToEndMatchSnapshot();
			CPUOpponent.StopActing();
		}

		public void OnEndGameCutsceneOver()
		{
			UIManager.ShowGameOverUI();
		}

        private void OnPlaceableDead(Placeable p)
        {
            p.OnDie -= OnPlaceableDead; //remove the listener
            
            switch(p.pType)
            {
                case Placeable.PlaceableType.Unit:
					Unit u = (Unit)p;
                    RemovePlaceableFromList(u);
					u.OnDealDamage -= OnPlaceableDealtDamage;
					u.OnProjectileFired -= OnProjectileFired;
					UIManager.RemoveHealthUI(u);
					StartCoroutine(Dispose(u));
                    break;

                case Placeable.PlaceableType.Building:
                case Placeable.PlaceableType.Castle:
					Building b = (Building)p;
                    RemovePlaceableFromList(b);
					UIManager.RemoveHealthUI(b);
					b.OnDealDamage -= OnPlaceableDealtDamage;
					b.OnProjectileFired -= OnProjectileFired;
                    StartCoroutine(RebuildNavmesh()); //need to fix for normal buildings
					
					//we don't dispose of the Castle
					if(p.pType != Placeable.PlaceableType.Castle)
						StartCoroutine(Dispose(b));
                    break;

                case Placeable.PlaceableType.Obstacle:
                    StartCoroutine(RebuildNavmesh());
                    break;

                case Placeable.PlaceableType.Spell:
                    //TODO: can spells die?
                    break;
            }
        }

		private IEnumerator Dispose(ThinkingPlaceable p)
		{
			yield return new WaitForSeconds(3f);

			Destroy(p.gameObject);
		}

        private IEnumerator RebuildNavmesh()
        {
            yield return new WaitForEndOfFrame();

            navMesh.BuildNavMesh();
            //FIX: dragged obstacles are included in the navmesh when it's baked
        }

        private void AddPlaceableToList(ThinkingPlaceable p)
        {
			allThinkingPlaceables.Add(p);

			if(p.faction == Placeable.Faction.Player)
            {
				allPlayers.Add(p);
            	
				if(p.pType == Placeable.PlaceableType.Unit)
                    playerUnits.Add(p);
				else
                    playerBuildings.Add(p);
            }
            else if(p.faction == Placeable.Faction.Opponent)
            {
				allOpponents.Add(p);
            	
				if(p.pType == Placeable.PlaceableType.Unit)
                    opponentUnits.Add(p);
				else
                    opponentBuildings.Add(p);
            }
            else
            {
                Debug.LogError("Error in adding a Placeable in one of the player/opponent lists");
            }
        }

        private void RemovePlaceableFromList(ThinkingPlaceable p)
        {
			allThinkingPlaceables.Remove(p);

			if(p.faction == Placeable.Faction.Player)
            {
				allPlayers.Remove(p);
            	
				if(p.pType == Placeable.PlaceableType.Unit)
                    playerUnits.Remove(p);
				else
                    playerBuildings.Remove(p);
            }
            else if(p.faction == Placeable.Faction.Opponent)
            {
				allOpponents.Remove(p);
            	
				if(p.pType == Placeable.PlaceableType.Unit)
                    opponentUnits.Remove(p);
				else
                    opponentBuildings.Remove(p);
            }
            else
            {
                Debug.LogError("Error in removing a Placeable from one of the player/opponent lists");
            }
        }
    }
}