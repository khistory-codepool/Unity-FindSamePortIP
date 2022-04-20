using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class IPFinder : MonoBehaviour
{
    [Header("Your Settings")] 
    [Range(49152, 65535)]
    public int port = 55555;
    public float findTimeInterval = 1;
    
    [Header("Result")]
    [SerializeField] private string findHostName;
    [SerializeField] private string findIp;
    
    private int _lastPort = 0;
    private int _sendCount = 0;
    private readonly int _initThreshold = 2;
    private readonly string _myReq;
    private readonly UdpClient _client = new UdpClient();
    
    private void Start()
    {
        StartCoroutineLoopsToFindRespondent();
    }
    
    private void StartCoroutineLoopsToFindRespondent()
    {
        StartCoroutine(CheckingLastPortAndMakeSame());
        StartCoroutine(BroadcastFindReqToEveryone());
        StartCoroutine(WaitResponse());
    }
    
    IEnumerator CheckingLastPortAndMakeSame()
    {
        while (true)
        {
            yield return null;

            if (_lastPort == port)
                continue;

            port = Mathf.Clamp(port, 49152, 65535);
            _lastPort = port;
            ClearResults();
        }
    }

    IEnumerator BroadcastFindReqToEveryone()
    {
        byte[] RequestData = Encoding.ASCII.GetBytes(Dns.GetHostName());
        while (true)
        {
            try
            {
                _client.EnableBroadcast = true;
                IPAddress[] BroadcastAddresses = GetDirectedBroadcastAddresses();
                foreach (var IPAddress in BroadcastAddresses)
                {
                    _client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress, port));
                    
                    _sendCount++;
                    if (_sendCount > _initThreshold)
                    {
                        _sendCount = 0;
                        ClearResults();
                    }
                    
                    Debug.Log($"(1)●○ [IP Finder] broadcast [to ip:{IPAddress}]");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"(-1)○○ [IP Finder] Exception: {e}");
            }

            yield return new WaitForSeconds(findTimeInterval);
        }
    }
    
    IEnumerator WaitResponse() {
        while (true) {
            if (_client.Available > 0) {
                try {
                    IPEndPoint ServerEp = new IPEndPoint(IPAddress.Any, port);
                    byte[] ServerResponseData = _client.Receive(ref ServerEp);
                    string ServerResponse = Encoding.ASCII.GetString(ServerResponseData);

                    findHostName = ServerResponse;
                    findIp = ServerEp.Address.ToString();
                    Debug.Log($"(2)●● [IP Finder] received [serverHostName:{ServerResponse}, findIp:{findIp}]");
                }
                catch (Exception e) {
                    Debug.Log($"(-1)○○ [IP Finder] Exception: {e}");
                }
            }
            yield return null;
        }
    }
    
    private void ClearResults()
    {
        findHostName = null;
        findIp = null;
    }
    
    //Get BroadcastAddresses
    private IPAddress[] GetDirectedBroadcastAddresses() {
        List<IPAddress> list = new List<IPAddress>();

        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces()) {
            if (item.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
                continue;
            }

            if (item.OperationalStatus != OperationalStatus.Up) {
                continue;
            }

            UnicastIPAddressInformationCollection unicasts = item.GetIPProperties().UnicastAddresses;

            foreach (UnicastIPAddressInformation unicast in unicasts) {
                IPAddress ipAddress = unicast.Address;

                if (ipAddress.AddressFamily != AddressFamily.InterNetwork) {
                    continue;
                }

                byte[] addressBytes = ipAddress.GetAddressBytes();
                byte[] subnetBytes = unicast.IPv4Mask.GetAddressBytes();

                if (addressBytes.Length != subnetBytes.Length) {
                    continue;
                }

                byte[] broadcastAddress = new byte[addressBytes.Length];
                for (int i = 0; i < broadcastAddress.Length; i++) {
                    broadcastAddress[i] = (byte)(addressBytes[i] | (subnetBytes[i] ^ 255));
                }

                list.Add(new IPAddress(broadcastAddress));
            }
        }

        return list.ToArray();
    }
}
