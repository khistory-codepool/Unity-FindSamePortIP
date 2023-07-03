using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class IPRespondent : MonoBehaviour
{
    [Header("Settings")] 
    [Range(49152, 65535)]
    public int port = 55555;

    [Header("Requestor's Infos")] 
    [SerializeField] private string finderIp;

    private int _lastPort = 0;
    private UdpClient _server = new UdpClient();
    private const string ReqKey = "FIND_KEY";

    private void Start()
    {
        StartCoroutineLoopsToResponse();
    }
    
    void StartCoroutineLoopsToResponse()
    {
        StartCoroutine(RestarterWhenServerPortChange());
        StartCoroutine(WaitIpFinder());
    }

    IEnumerator RestarterWhenServerPortChange()
    {
        while (true)
        {
            yield return null;

            if (_lastPort == port)
                continue;
                
            port = Mathf.Clamp(port, 49152, 65535);
            NewUdpClient();
            
            _lastPort = port;
        }
    }

    private void NewUdpClient()
    {
        try
        {
            _server.Close();
            _server = new UdpClient(port);
            Debug.Log($"(1)●○ [IPRespondent] new UdpClient (port:{port})");
        }
        catch (Exception e)
        {
            Debug.Log($"(-1)○○ [IPRespondent] Exception: {e}");
        }
    }
    
    IEnumerator WaitIpFinder() {
        yield return null;
        byte[] ResponseData = Encoding.ASCII.GetBytes(ReqKey);
        while (true)
        {
            if (_server.Available > 0 ) {
                try {
                    IPEndPoint UdpServerClientEp = new IPEndPoint(IPAddress.Any, port);
                    byte[] ClientRequestData = _server.Receive(ref UdpServerClientEp);
                    string recvData = Encoding.ASCII.GetString(ClientRequestData);

                    if (string.IsNullOrEmpty(finderIp) && ReqKey.Equals(recvData))
                    {
                        finderIp = UdpServerClientEp.Address.ToString();
                        _server.Send(ResponseData, ResponseData.Length, UdpServerClientEp);
                        Debug.Log($"(2)●● [IPRespondent] received from ipFinder (ip:{UdpServerClientEp.Address})");
                    }
                }
                catch (Exception e) {
                    Debug.Log($"(-1)○○ [IPRespondent] : Exception: {e}");
                }
            }
            yield return null;
        }
    }
}
