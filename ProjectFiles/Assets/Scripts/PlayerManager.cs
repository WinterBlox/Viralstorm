using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayerManager : NetworkBehaviour
{
    // CARDS
    public GameObject TEMP1;
    public GameObject TEMP2;
    public GameObject DISINFECT;
    public GameObject REGEDIT;
    public GameObject ENCRYPT;
    public GameObject EXPOSE;

    // PUBLIC VARIABLES
    public GameManager GameManager;
    public GameObject PlayerArea;
    public GameObject EnemyArea;
    public GameObject PlayerYard;
    public GameObject EnemyYard;
    public bool isMyTurn;

    public GameObject PSlot1;
    public GameObject PSlot2;
    public GameObject PSlot3;
    public GameObject PSlot4;
    public GameObject PSlot5;
    public GameObject ESlot1;
    public GameObject ESlot2;
    public GameObject ESlot3;
    public GameObject ESlot4;
    public GameObject ESlot5;

    public int cardsPlayed;

    // LISTS
    List<GameObject> cards = new List<GameObject>();
    public List<GameObject> PlayerSockets = new List<GameObject>();
    public List<GameObject> EnemySockets = new List<GameObject>();



    // OnStartClient is called before Start, and is only run by the Connecting Client
    public override void OnStartClient()
    {
        base.OnStartClient();

        GameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        PlayerArea = GameObject.Find("PlayerArea");
        EnemyArea = GameObject.Find("EnemyArea");
        PlayerYard = GameObject.Find("PlayerYard");
        EnemyYard = GameObject.Find("EnemyYard");
        PSlot1 = GameObject.Find("PSlot1");
        PSlot2 = GameObject.Find("PSlot2");
        PSlot3 = GameObject.Find("PSlot3");
        PSlot4 = GameObject.Find("PSlot4");
        PSlot5 = GameObject.Find("PSlot5");
        ESlot1 = GameObject.Find("ESlot1");
        ESlot2 = GameObject.Find("ESlot2");
        ESlot3 = GameObject.Find("ESlot3");
        ESlot4 = GameObject.Find("ESlot4");
        ESlot5 = GameObject.Find("ESlot5");

        PlayerSockets.Add(PSlot1);
        PlayerSockets.Add(PSlot2);
        PlayerSockets.Add(PSlot3);
        PlayerSockets.Add(PSlot4);
        PlayerSockets.Add(PSlot5);
        EnemySockets.Add(ESlot1);
        EnemySockets.Add(ESlot2);
        EnemySockets.Add(ESlot3);
        EnemySockets.Add(ESlot4);
        EnemySockets.Add(ESlot5);

        if (isClientOnly)
        {
            isMyTurn = true;
        }
    }

    [Server] // Prevents the client from executing the functions embedded beneath

    // OnStartServer is called before Start, and is only run by the Hosting Server
    public override void OnStartServer()
    {
        cards.Add(TEMP1);
        cards.Add(TEMP2);
        cards.Add(DISINFECT);
        cards.Add(REGEDIT);
        cards.Add(ENCRYPT);
        cards.Add(EXPOSE);
    }

    [Command] // Marks the below functions as server commands
    public void CmdDealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(cards[Random.Range(0, cards.Count)], new Vector3(0, 0, 0), Quaternion.identity);
            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, "Dealt");
        }
        RpcGMChangeState("Compile {}");
    }

    public void PlayCard(GameObject card)
    {
        card.GetComponent<CardAbilities>().OnExecute();
        CmdPlayCard(card);
    }

    [Command]
    void CmdPlayCard(GameObject card)
    {
        RpcShowCard(card, "Played");
    }

    [ClientRpc]
    void RpcShowCard(GameObject card, string type)
    {
        if (type == "Dealt")
        {
            if (hasAuthority)
            {
                card.transform.SetParent(PlayerArea.transform, false);
            }
            else
            {
                card.transform.SetParent(EnemyArea.transform, false);
                card.GetComponent<CardFlipper>().Flip();
            }
        }
        else if (type == "Played")
        {
            if (cardsPlayed == 5)
            {
                cardsPlayed = 0;
            }
            if (hasAuthority)
            {
                card.transform.SetParent(PlayerSockets[cardsPlayed].transform, false);
                CmdGMCardPlayed();
            }
            if (!hasAuthority)
            {
                card.transform.SetParent(EnemySockets[cardsPlayed].transform, false);
                card.GetComponent<CardFlipper>().Flip();
            }
            cardsPlayed++;
            PlayerManager pm = NetworkClient.connection.identity.GetComponent<PlayerManager>();
            pm.isMyTurn = !pm.isMyTurn;
        }
    }

    [Command]
    public void CmdGMChangeState (string stateRequest)
    {
        RpcGMChangeState(stateRequest);
    }

    [ClientRpc]
    void RpcGMChangeState(string stateRequest)
    {
        GameManager.ChangeGameState(stateRequest);
        if (stateRequest == "Compile {}")
        {
            GameManager.ChangeReadyClicks();
        }
    }

    [Command]
    void CmdGMCardPlayed()
    {
        RpcGMCardPlayed();
    }

    [ClientRpc]
    void RpcGMCardPlayed()
    {
        GameManager.CardPlayed();
    }

    [Command]
    public void CmdExecute()
    {
        RpcExecute();
    }

    [ClientRpc]
    void RpcExecute()
    {
        for (int i = 0; i < PlayerSockets.Count; i++)
        {
            PlayerSockets[i].GetComponentInChildren<CardAbilities>().OnExecute();
            PlayerSockets[i].transform.GetChild(0).gameObject.transform.SetParent(PlayerYard.transform, false);
            EnemySockets[i].transform.GetChild(0).gameObject.transform.SetParent(EnemyYard.transform, false);
        }
    }

    [Command]
    public void CmdGMUpdateHealth (int PHP, int EHP)
    {
        RpcGMUpdateHealth(PHP, EHP);
    }

    [ClientRpc]
    public void RpcGMUpdateHealth (int PHP, int EHP)
    {
        GameManager.UpdateHealth(PHP, EHP, hasAuthority);
    }
}
