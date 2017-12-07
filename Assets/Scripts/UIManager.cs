using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour {

    private NetworkConnectionP2P net;
    private GameObject NetPanel;
    private Text MaxPlayer;
    private Text SelectedPlayer;
    private GameObject ResetButton;
    private GameObject GameSetupPanel;
    private GameObject GamePanel;
	// Use this for initialization
	void Start () {
        Screen.SetResolution(1600, 900, false);
        net = GetComponent<NetworkConnectionP2P>();
        NetPanel = GameObject.Find("NetPanel");
        MaxPlayer = GameObject.Find("PlayerCount").GetComponent<Text>();
        SelectedPlayer = GameObject.Find("SelectedPlayer").GetComponent<Text>();
        ResetButton = GameObject.Find("Reset");
        GameSetupPanel = GameObject.Find("GameSetupPanel");
        GamePanel = GameObject.Find("GamePanel");
        GamePanel.SetActive(false);

      }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void StartGame()
    {
        int max, selected;
        Int32.TryParse(MaxPlayer.text, out max);
        Int32.TryParse(SelectedPlayer.text, out selected);

        Debug.LogFormat("using {0} of {1}", selected, max);
        List<ConnectionInfo> peers = new List<ConnectionInfo>();
        for (int i = 0; i < max; i++)
        {
            var player = NetPanel.transform.GetChild(i);
           
            var ip = i + 1 == selected ? "" : player.GetChild(0).Find("Text").GetComponent<Text>().text;
            var port = player.GetChild(1).Find("Text").GetComponent<Text>().text;
            Debug.LogFormat("Player {0} {1}:{2}", i + 1, ip, port);
            peers.Add(new ConnectionInfo(ip, port, i + 1));
        }
        if (net.BeginConnect(peers, max, selected))
        {
            GameSetupPanel.SetActive(false);
            GamePanel.SetActive(true);
        }
    }

    public void Reset(int condition = 0)
    {
        net.Reset();
        GameSetupPanel.SetActive(true);
        GamePanel.SetActive(false);
        if (condition == 1)
        {
            SceneManager.LoadScene("LoseGame");
        }
        if (condition == 2)
        {
            SceneManager.LoadScene("WinGame");
        }
 
    }


}
