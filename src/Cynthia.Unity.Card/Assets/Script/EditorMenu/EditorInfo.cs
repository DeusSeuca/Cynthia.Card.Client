﻿using System.Collections;
using System.Collections.Generic;
using Cynthia.Card;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using Alsein.Extensions;
using Alsein.Extensions.Extensions;
using Autofac;
using Cynthia.Card.Client;
using System;
using DG.Tweening;
using System.Threading.Tasks;

public class EditorInfo : MonoBehaviour
{
    //展示卡牌相关
    public InputField ShowSearch;
    public RectTransform ShowCardsContent;
    public GameObject UICardPrefab;
    public Scrollbar ShowCardScroll;
    public Scrollbar ShowDeckScroll;
    public Toggle[] ShowButtons;
    public RectTransform ShowDecksContext;
    public GameObject AddDeckButtonPrefab;
    public GameObject MonstersDeckPrefab;
    public GameObject NilfgaardDeckPrefab;
    public GameObject NorthernRealmsDeckPrefab;
    public GameObject ScoiaTaelDeckPrefab;
    public GameObject SkelligeDeckPrefab;
    private Faction _showFaction = Faction.All;
    private IDictionary<Faction, GameObject> _deckPrefabMap;
    private int _nowShow = -1;
    private string _showSearchMessage = "";
    //------------------------------------------------
    //DOTween动画
    public RectTransform ShowCardsTitle;
    public RectTransform EditorCardsTitle;
    public RectTransform LeftSwitchMenu;
    public RectTransform RightSwitchMenu;
    //------------------------------------------------
    //公用
    private IList<CardStatus> _cards { get => GwentMap.GetCards().ToList(); }//所有的卡牌
    private GwentClientService _clientService;
    private GlobalUIService _globalUIService;
    public EditorStatus EditorStatus { get; private set; } = EditorStatus.Close;
    //
    public GameObject EditorBodyMian;
    public GameObject EditorBodySwitch;
    public GameObject EditorBodyCore;
    public GameCard SwitchReturnButton;
    public GameObject EditorUI;
    public GameObject MainUI;
    //------------------------------------------------
    //选择卡牌相关
    public RectTransform SwitchCardsContext;
    public RectTransform SwitchCardsHeightContext;
    public SwitchUICard SwitchCardPrefab;
    public Scrollbar SwitchCardsScroll;
    private Faction _nowSwitchFaction = Faction.All;
    private string _nowSwitchLeaderId = null;
    //------------------------------------------------
    //卡组编辑相关
    private string _editorSearchMessage = "";
    public RectTransform EditorCardsContext;
    public RectTransform EditorCListContext;
    public InputField EditorSearch;
    public InputField DeckName;
    public Scrollbar EditorCardsScroll; 
    public Scrollbar EditorCListScroll;
    public Toggle[] EditorGroupButtons;
    private Group _nowEditorGroup = Group.Leader;
    private DeckModel _nowEditorDeck = null;
    //
    public GameObject EditorListCardPrefab;//列表卡牌
    public GameObject EditorMenuCardPrefab;//菜单卡牌
    public GameObject[] EditorLeadersPrefab;//领袖卡牌
    public Image EditorHeadT;
    public Image EditorHeadB;
    public Sprite[] EditorSpriteHeadT;
    public Sprite[] EditorSpriteHeadB;
    public Faction[] EditorFactionIndex;
    public Text GoldCount;  //金色数量
    public Text SilverCount;//银色数量
    public Text CopperCount;//铜色数量
    public Text AllCount;   //全部数量
    //------------------------------------------------
    private void Awake()
    {
        _clientService = DependencyResolver.Container.Resolve<GwentClientService>();
        _globalUIService = DependencyResolver.Container.Resolve<GlobalUIService>();
    }

    void Start()
    {
        _deckPrefabMap = new Dictionary<Faction,GameObject>
         {
             {Faction.NorthernRealms,NorthernRealmsDeckPrefab},
             {Faction.ScoiaTael,ScoiaTaelDeckPrefab},
             {Faction.Monsters,MonstersDeckPrefab},
             {Faction.Skellige,SkelligeDeckPrefab},
             {Faction.Nilfgaard,NilfgaardDeckPrefab},
         };
        ShowSearch.onValueChanged.RemoveAllListeners();
        ShowSearch.onValueChanged.AddListener(x => ShowSearchChange(x));
        EditorSearch.onValueChanged.RemoveAllListeners();
        EditorSearch.onValueChanged.AddListener(x => EditorSearchChange(x));
        DeckName.onValueChanged.RemoveAllListeners();
        DeckName.onValueChanged.AddListener(x => DeckNameChanged(x));
        //---------------------------------------------------------------------------
    }

    public void SetShowCardInfo(IList<CardStatus> cards)
    {   //设置已有卡牌
        RemoveAllChild(ShowCardsContent);
        cards.ForAll(x => 
        {
            var card = Instantiate(UICardPrefab).GetComponent<CardShowInfo>();
            card.CurrentCore = x;
            card.transform.SetParent(ShowCardsContent, false);
        });
        //------------------------------------------------------------------------//276
        var count = cards.Count;
        var height = (50f + 267f * (count % 6 > 0 ? count / 6 + 1 : count / 6));//count <= 16 ? 780f : 
        ShowCardsContent.sizeDelta = new Vector2(500.96f, height);
        //ShowCardsContent.GetComponent<GridLayoutGroup>().padding.top = 104;
        ShowCardScroll.value = 1;
    }

    public void OpenEditor(bool IsMoveLeftRight = true)
    {
        EditorStatus = EditorStatus.ShowCards;
        _nowSwitchLeaderId = null;
        _nowEditorDeck = null;
        DeckName.text = "默认名称";
        _nowEditorGroup = Group.Leader;
        EditorBodyCore.SetActive(false);
        EditorBodyMian.SetActive(true);
        if (IsMoveLeftRight)
        {
            ShowCardsTitle.anchoredPosition = new Vector2(0, 478.5f);
            EditorCardsTitle.anchoredPosition = new Vector2(0, 630f);
            LeftSwitchMenu.anchoredPosition = new Vector2(-1450, 0);
            RightSwitchMenu.anchoredPosition = new Vector2(1450, 0);
        }
        ResetEditor();
    }

    public void AutoSetShowCards()
    {   //按照筛选条件进行筛选
        SetShowCardInfo
        (
            _cards
            .Where(x => ((_showFaction == Faction.All) ? true : (x.Faction == _showFaction)))
            .Where(x => ((_showSearchMessage == "") ? true :
                (x.CardInfo().Name.Contains(_showSearchMessage) ||
                x.CardInfo().Info.Contains(_showSearchMessage) ||
                x.CardInfo().Strength.ToString().Contains(_showSearchMessage))))
            .ToList()
        );
    }

    public void ResetEditor()
    {
        EditorSearch.text = "";
        ShowSearch.text = "";
        _nowSwitchFaction = Faction.All;
        //
        SetDeckList(_clientService.User.Decks);
        ShowButtons[0].isOn = true;
    }

    public void ShowFactionClick()
    {   //展示卡牌中,切换势力显示的按钮被点击
        if (!ShowButtons.Any(x => x.isOn)) return;
        var result = ShowButtons.Select((item, index) => (item, index)).First(x => x.item.isOn).index;
        if (result == _nowShow) return;
        _showFaction = (Faction)result;
        AutoSetShowCards();
        _nowShow = result;
    }

    public void ShowSearchChange(string value)
    {   //展示卡牌中,搜索框改变
        _showSearchMessage = value;
        AutoSetShowCards();
    }

    public void RemoveAllChild(Transform father)
    {   //删除所有子物体
        for (var i = father.childCount - 1; i >= 0; i--)
        {
            Destroy(father.GetChild(i).gameObject);
        }
        father.DetachChildren();
    }

    public void SetDeckList(IList<DeckModel> decks)
    {   //设置已有卡组
        RemoveAllChild(ShowDecksContext);
        var button = Instantiate(AddDeckButtonPrefab);
        button.transform.SetParent(ShowDecksContext, false);
        //-----
        decks.ForAll(x =>
        {
            if (_deckPrefabMap == null) Start();
            var deck = Instantiate(_deckPrefabMap[GwentMap.CardMap[x.Leader].Faction]);
            deck.GetComponent<DeckShowInfo>().SetDeckInfo(x.Name,x.IsBasicDeck());
            deck.GetComponent<EditorShowDeck>().Id = x.Id;
            deck.transform.SetParent(ShowDecksContext, false);
        });
        //----
        var count = decks.Count();
        var height = (15+65+5)+(80+5)*count;//count <= 16 ? 780f : 
        ShowDecksContext.sizeDelta = new Vector2(0, height);
        ShowDeckScroll.value = 1;
    }

    public async void ShowDeckRemoveClick(string Id)
    {
        if (await _globalUIService.YNMessageBox("是否要删除卡组",$"是否删除 {_clientService.User.Decks.Single(x => x.Id == Id).Name} 卡组"))
        {
            if(!(await _clientService.RemoveDeck(Id)))
            {
                await _globalUIService.YNMessageBox("删除卡组失败", $"无法删除卡组 {_clientService.User.Decks.Single(x => x.Id == Id).Name}");
            }
            else
            {
                var i = _clientService.User.Decks.Select((item, index) => (item, index)).Single(x => x.item.Id == Id).index;
                _clientService.User.Decks.RemoveAt(i);
                SetDeckList(_clientService.User.Decks);
            }
        }
    }

    public void ShowDeckEditorClick(string Id)
    {
        Debug.Log("点击了【" + _clientService.User.Decks.Single(x => x.Id == Id).Name + "】卡组的编辑按钮");
        var deck = _clientService.User.Decks.Single(x => x.Id == Id);
        _nowEditorDeck = deck;
        _nowSwitchLeaderId = deck.Leader;
        _nowSwitchFaction = GwentMap.CardMap[deck.Leader].Faction;
        //
        ResetEditorCore();
        ShowCardsTitle.anchoredPosition = new Vector2(0, 630f);
        EditorCardsTitle.anchoredPosition = new Vector2(0, 478.5f);
        EditorBodyCore.SetActive(true);
        EditorBodyMian.SetActive(false);
        EditorStatus = EditorStatus.EditorDeck;
    }
    //=============================================================================================================================
    //以上为展示卡牌相关的内容,以下为展示框选择相关内容

    public void AddDeckClick()
    {   //点击新建按钮后
        if (_clientService.User.Decks.Count >= 40)
        {
            _globalUIService.YNMessageBox("卡组数量已经达到40", "无法添加更多卡组,请删除或者编辑现有卡组");
        }
        else
        {
            DOTween.To(() => ShowCardsTitle.anchoredPosition, x => ShowCardsTitle.anchoredPosition = x,
                        new Vector2(0, 605), 0.5f);//收回Title
            DOTween.To(() => LeftSwitchMenu.anchoredPosition, x => LeftSwitchMenu.anchoredPosition = x,
                new Vector2(-470, 0), 0.5f);//展开Left
            DOTween.To(() => RightSwitchMenu.anchoredPosition, x => RightSwitchMenu.anchoredPosition = x,
                new Vector2(468, 0), 0.5f);//展开Right
                                           /*
                                           Titile x:0 | Y:478.5 true    Y: 605 false
                                           Left y:0 | X:-470 true     X: -1450 false
                                           Right y:0 | X: 468 true     X: 1450 false*/
            EditorStatus = EditorStatus.SwitchFaction;
            SetSwitchList(((Faction[])Enum.GetValues(typeof(Faction)))
                .Where(x => x != Faction.All && x != Faction.Neutral)
                .Select(x => new CardStatus() { DeckFaction = x })
                .ToList());
            _nowSwitchLeaderId = null;
            _nowEditorDeck = null;
            DeckName.text = "默认名称";
        }
    }

    public void SelectSwitchUICard(CardStatus card, bool isOver = true)
    {
        //悬停在卡牌上,显示卡牌信息...但是目前没有做
    }

    public void ClickSwitchUICard(CardStatus card)
    {
        if(EditorStatus == EditorStatus.SwitchFaction)
        {   //如果目前正在选择势力
            _nowSwitchFaction = card.DeckFaction;
            if (!card.IsCardBack)
                Debug.Log("发生了一个超大的问题!!!!点击查看");
            SetSwitchList(_cards.Where(x => x.Group == Group.Leader && x.Faction == _nowSwitchFaction).ToList());

            EditorStatus = EditorStatus.SwitchLeader;
        }
        else if (EditorStatus == EditorStatus.SwitchLeader)
        {   //选择了领袖
            _nowSwitchLeaderId = card.CardId;
            if (_nowEditorDeck != null) _nowEditorDeck.Leader = _nowSwitchLeaderId;
            else _nowEditorDeck = new DeckModel() { Leader = _nowSwitchLeaderId ,Deck = new List<string>()};
            //收回...不过降下编辑的
            EditorBodyCore.SetActive(true);
            EditorBodyMian.SetActive(false);
            ResetEditorCore();
            EditorStatus = EditorStatus.EditorDeck;
            DOTween.To(() => EditorCardsTitle.anchoredPosition, x => EditorCardsTitle.anchoredPosition = x,
                    new Vector2(0, 478.5f), 0.5f);//降下Title,设定标题 ********
            DOTween.To(() => LeftSwitchMenu.anchoredPosition, x => LeftSwitchMenu.anchoredPosition = x,
                new Vector2(-1450, 0), 0.5f);//收回Left
            DOTween.To(() => RightSwitchMenu.anchoredPosition, x => RightSwitchMenu.anchoredPosition = x,
                new Vector2(1450, 0), 0.5f);//收回Right
        }
    }

    public async void SwitchReturn()
    {
        switch (EditorStatus)
        {
            case EditorStatus.SwitchLeader://选择领袖阶段,变回选择势力
                SetSwitchList(((Faction[])Enum.GetValues(typeof(Faction)))
                    .Where(x => x != Faction.All && x != Faction.Neutral)
                    .Select(x => new CardStatus() { DeckFaction = x })
                    .ToList());
                EditorStatus = EditorStatus.SwitchFaction;
                _nowSwitchLeaderId = null;
                _nowEditorDeck = null;
                DeckName.text = "默认名称";
                break;
            case EditorStatus.SwitchFaction://选择势力阶段,变为展示卡牌阶段
                //_nowSwitchFaction = Faction.All;
                //_nowSwitchLeaderId = null;
                //EditorStatus = EditorStatus.ShowCards;
                //需要补充,关闭预选模式的动画
                OpenEditor(false);
                DOTween.To(() => ShowCardsTitle.anchoredPosition, x => ShowCardsTitle.anchoredPosition = x,
                    new Vector2(0, 478.5f), 0.5f);//降下Title,设定标题 ********
                DOTween.To(() => LeftSwitchMenu.anchoredPosition, x => LeftSwitchMenu.anchoredPosition = x,
                    new Vector2(-1450, 0), 0.5f);//收回Left
                DOTween.To(() => RightSwitchMenu.anchoredPosition, x => RightSwitchMenu.anchoredPosition = x,
                    new Vector2(1450, 0), 0.5f);//收回Right
                /*
                Titile x:0 | Y:478.5 true    Y: 605 false
                Left y:0 | X:-470 true     X: -1450 false
                Right y:0 | X: 468 true     X: 1450 false*/
                break;
            case EditorStatus.ShowCards://展示卡牌阶段,关闭编辑器
                MainUI.SetActive(true);
                EditorUI.SetActive(false);
                EditorStatus = EditorStatus.Close;
                break;
            case EditorStatus.EditorDeck:
                //###################################
                //需要补充,保存并且提交
                //###################################
                //后续处理
                if(!_nowEditorDeck.IsBasicDeck())
                {
                    _=_globalUIService.YNMessageBox("卡组不符合标准","目前不支持提交不合标准的卡组,请调整后提交");
                    break;
                }
                else
                {
                    _nowEditorDeck.Name = (DeckName.text == "" ? "默认名称" : DeckName.text);
                    if (_clientService.User.Decks.Any(x => x.Id == (_nowEditorDeck.Id == null ? "" : _nowEditorDeck.Id)))
                    {
                        if(await _globalUIService.YNMessageBox("是否修改卡组?", $"是否修改卡组 {DeckName.text}"))
                        {
                            if(await _clientService.ModifyDeck(_nowEditorDeck.Id,_nowEditorDeck))
                            {
                                var i = _clientService.User.Decks.Select((item, index) => (item, index)).Single(x => x.item.Id == _nowEditorDeck.Id).index;
                                _clientService.User.Decks[i] = _nowEditorDeck;
                            }
                            else
                            {
                                if (!(await _globalUIService.YNMessageBox("发生了一个错误", "因为不明原因,无法添加卡组,是否返回初始界面", "是", "否")))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (await _globalUIService.YNMessageBox("是否新建卡组?", $"是否新建卡组 {DeckName.text}"))
                        {
                            _nowEditorDeck.Id = Guid.NewGuid().ToString();
                            if ((await _clientService.AddDeck(_nowEditorDeck)))
                            {   //如果添加卡组通过验证
                                //也在本地添加卡组
                                _clientService.User.Decks.Add(_nowEditorDeck);
                            }
                            else
                            {
                                if (!(await _globalUIService.YNMessageBox("发生了一个错误", "因为不明原因,无法添加卡组,是否返回初始界面", "是", "否")))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                OpenEditor();
                break;
        }
    }

    public void SetSwitchList(IList<CardStatus> cards)
    {//选择列表
        RemoveAllChild(SwitchCardsContext);
        cards.ForAll(x =>
        {
            var card = Instantiate(SwitchCardPrefab).GetComponent<SwitchUICard>();
            card.CardShowInfo.CurrentCore = x;
            card.transform.SetParent(SwitchCardsContext, false);
        });
        //------------------------------------------------------------------------//276
        var count = cards.Count;
        var height = (130 + (251 + 46.5f) * ((int)(count%3>0?count/3+1:count/3))+50f);//count <= 16 ? 780f : 
        SwitchCardsContext.sizeDelta = new Vector2(0, height>1000?height:1000);
        SwitchCardsHeightContext.sizeDelta = new Vector2(0, height);
        //ShowCardsContent.GetComponent<GridLayoutGroup>().padding.top = 104;
        SwitchCardsScroll.value = 1;
    }
    //=============================================================================================================================
    //以上为展示框选择,以下为编辑菜单相关
    public void DeckNameChanged(string name)
    {
        if(_nowEditorDeck!=null)
            _nowEditorDeck.Name = name;
    }

    public void ResetEditorCore()
    {//初始化
        EditorSearch.text = "";
        DeckName.text = (_nowEditorDeck.Name==null|| _nowEditorDeck.Name == "")? "默认名称":_nowEditorDeck.Name;
        EditorHeadT.sprite = EditorSpriteHeadT[GetFactionIndex(_nowSwitchFaction)];
        EditorHeadB.sprite = EditorSpriteHeadB[GetFactionIndex(_nowSwitchFaction)];
        //
        SetEditorDeck(_nowEditorDeck);
        EditorGroupButtons[0].isOn = true;
        AutoSetEditorCards();
    }

    public void ClickEditorListLeader(string id)
    {//点击了领袖   应该返回选择领袖,保留目前的卡组名称与卡牌
        EditorStatus = EditorStatus.SwitchLeader;
        SetSwitchList(_cards.Where(x => x.Group == Group.Leader && x.Faction == _nowSwitchFaction).ToList());
        DOTween.To(() => EditorCardsTitle.anchoredPosition, x => EditorCardsTitle.anchoredPosition = x,
                    new Vector2(0, 605), 0.5f);//收回Title
        DOTween.To(() => LeftSwitchMenu.anchoredPosition, x => LeftSwitchMenu.anchoredPosition = x,
            new Vector2(-470, 0), 0.5f);//展开Left
        DOTween.To(() => RightSwitchMenu.anchoredPosition, x => RightSwitchMenu.anchoredPosition = x,
            new Vector2(468, 0), 0.5f);//展开Right
        //Debug.Log("点击了领袖");
    }

    public void ClickEditorListCard(string id)
    {//点击了卡牌   应该从卡组去除对应卡牌,并且更新显示
        var subIndex = _nowEditorDeck.Deck.Select((item, index) => (item, index)).First(x => x.item == id).index;
        _nowEditorDeck.Deck.RemoveAt(subIndex);
        SetEditorDeck(_nowEditorDeck);
        //**************************************************
        var c = GetAllChilds<EditorUICoreCard>(EditorCardsContext).Where(x => x.cardShowInfo.CurrentCore.CardId == id);
        c.ForAll(x => { x.Count++; });
    }

    public void ClickEditorUICoreCard(CardStatus card)
    {//点击了显示卡牌  应该判断是否应该添加卡牌,如果可以,添加并且更新显示,否则跳出消息提醒
        var count = _nowEditorDeck.Deck.Where(x => x == card.CardId).Count();
        if (!((card.Group == Group.Copper && count >= 3) || 
            (card.Group != Group.Copper && count >= 1)|| 
            (_nowEditorDeck.Deck.Count>=40)||
            (card.Group == Group.Silver && _nowEditorDeck.Deck.Where(x => x.CardInfo().Group==Group.Silver).Count()>=6)||
            (card.Group == Group.Gold && _nowEditorDeck.Deck.Where(x => x.CardInfo().Group == Group.Gold).Count() >= 4)))
        {   //如果超过上限,禁止加入卡牌
            _nowEditorDeck.Deck.Add(card.CardId);
            SetEditorDeck(_nowEditorDeck);
            //**********************************************
            var c = GetAllChilds<EditorUICoreCard>(EditorCardsContext).Where(x => x.cardShowInfo.CurrentCore.CardId == card.CardId);
            c.ForAll(x =>{x.Count--;});
        }
        //Debug.Log("点击了菜单卡");
    }

    public void EditorGroupClick()
    {//点击了品质筛选
        if (!EditorGroupButtons.Any(x => x.isOn)) return;
        var result = EditorGroupButtons.Select((item, index) => (item, index)).First(x => x.item.isOn).index;
        var group = result == 0 ? Group.Leader : (result == 1 ? Group.Gold : (result == 2 ? Group.Silver : Group.Copper));
        if (_nowEditorGroup != group)
        {
            _nowEditorGroup = group;
            AutoSetEditorCards();
        }
    }

    public void EditorSearchChange(string value)
    {   //编辑卡牌中,搜索框改变
        _editorSearchMessage = value;
        AutoSetEditorCards();
    }

    public void AutoSetEditorCards()
    {   //按照筛选条件进行筛选
        SetEditorCardInfo
        (
            _cards
            .Where(x => ((x.Faction==Faction.Neutral)||(x.Faction == _nowSwitchFaction)))
            .Where(x => ((_editorSearchMessage == "") ? true :
                (x.CardInfo().Name.Contains(_editorSearchMessage) ||
                x.CardInfo().Info.Contains(_editorSearchMessage) ||
                x.CardInfo().Strength.ToString().Contains(_editorSearchMessage))))
            .Where(x=>_nowEditorGroup==Group.Leader?x.Group!=Group.Leader:x.Group==_nowEditorGroup)
            .ToList()
        );
    }

    public void SetEditorDeck(DeckModel deck)
    {
        RemoveAllChild(EditorCListContext);
        var factionIndex = GetFactionIndex(_nowSwitchFaction);
        var leader = Instantiate(EditorLeadersPrefab[factionIndex]).GetComponent<LeaderShow>();
        leader.SetLeader(_nowSwitchLeaderId);
        leader.GetComponent<EditorListLeader>().Id = _nowSwitchLeaderId;
        leader.transform.SetParent(EditorCListContext, false);
        deck.Deck.Select(x=> GwentMap.CardMap[x])
            .OrderByDescending(x => x.Group)
            .ThenByDescending(x => x.Strength)
            .GroupBy(x => x.CardId)
        .ForAll(x => 
        {
            var card = Instantiate(EditorListCardPrefab).GetComponent<ListCardShowInfo>();
            card.SetCardInfo(x.Key,x.Count());
            card.GetComponent<EditorListCard>().Id = x.Key;
            card.transform.SetParent(EditorCListContext,false);
        });
        AllCount.text = _nowEditorDeck.Deck.Count().ToString();
        CopperCount.text = $"{_nowEditorDeck.Deck.Where(x => GwentMap.CardMap[x].Group == Group.Copper).Count()}";
        GoldCount.text = $"{_nowEditorDeck.Deck.Where(x => GwentMap.CardMap[x].Group == Group.Gold).Count()}/4";
        SilverCount.text = $"{_nowEditorDeck.Deck.Where(x => GwentMap.CardMap[x].Group == Group.Silver).Count()}/6";
        //*****************
        //等待补充？？？
        //*****************
        var count = deck.Deck.Distinct().Count();
        var height = ((10+75+2.6f+(41.5f+2.6f)*count)+5f);
        EditorCListContext.sizeDelta = new Vector2(0, height);
        //EditorCListScroll.value = 1;
    }

    public void SetEditorCardInfo(IList<CardStatus> cards)
    {   //设置已有卡牌
        RemoveAllChild(EditorCardsContext);
        cards.ForAll(x =>
        {
            var card = Instantiate(EditorMenuCardPrefab).GetComponent<EditorUICoreCard>();
            card.cardShowInfo.CurrentCore = x;
            var canAdd = (x.Group == Group.Copper ? 3 : 1);
            card.Count = (canAdd - _nowEditorDeck.Deck.Where(c => c == x.CardId).Count());
            card.transform.SetParent(EditorCardsContext, false);
        });
        //------------------------------------------------------------------------//276
        var count = cards.Count;
        var height = (50f + 267f * (count % 6 > 0 ? count / 6 + 1 : count / 6));//count <= 16 ? 780f : 
        EditorCardsContext.sizeDelta = new Vector2(500.96f, height);
        //ShowCardsContent.GetComponent<GridLayoutGroup>().padding.top = 104;
        EditorCardsScroll.value = 1;
    }
    
    public int GetFactionIndex(Faction faction)
    {
        return EditorFactionIndex.Select((item, index) => (item, index)).Single(x => x.item == faction).index;
    }
    
    public List<T> GetAllChilds<T>(Transform context)
    {
        var count = context.childCount;
        List<T> childs = new List<T>();
        for(var i = 0; i<count;i++)
        {
            if(context.GetChild(i).GetComponent<T>()!=null)
            {
                childs.Add(context.GetChild(i).GetComponent<T>());
            }
        }
        return childs;
    }
}
