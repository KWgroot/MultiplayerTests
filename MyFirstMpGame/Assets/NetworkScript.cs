using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Net.NetworkInformation;
using System.Text;
using System;

//https://forum.unity.com/threads/simple-udp-implementation-send-read-via-mono-c.15900/

public class NetworkScript : MonoBehaviour {
    private UdpConnection connection;
    public PingReply reply;
    public bool isServer = true;
    int myID;

    private Vector3 lagDistance, newRemotePlayerPosition;
    private long? previousTimeStamp, msSinceUpdate;
    private float moveSpeed = 0.1f, pingFrequency = 60f;

    bool upKey, downKey, leftKey, rightKey, space, chainEnd = false, shooting;

    public PlayerScript[] players = new PlayerScript[2];
    Sendable sendData = new Sendable();

    private Vector2 previousXY = new Vector2(0, 0);

    void Start() 
    {
        string sendIp = "127.0.0.1";
        
        int sendPort, receivePort;
        if (isServer) {
            sendPort = 8881;
            receivePort = 11000;
            myID = 0;
        } else {
            sendPort = 11000;
            receivePort = 8881;
            myID = 1;
        }

        connection = new UdpConnection();
        connection.StartConnection(sendIp, sendPort, receivePort);

        // Start of the handshake I think
        System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
        PingOptions options = new PingOptions();

        // Use the default Ttl value which is 128,
        // but change the fragmentation behavior.
        options.DontFragment = true;

        // Create a buffer of 8 bytes of data to be transmitted.
        string data = "aaaaaaaa";
        byte[] buffer = Encoding.ASCII.GetBytes(data);
        int timeout = 500;

        StartCoroutine(PingGatherer(sendIp, timeout, buffer, pingSender, options, reply));
    }

    IEnumerator PingGatherer(string sendIp, int timeout, byte[] buffer, System.Net.NetworkInformation.Ping pingSender, PingOptions options, PingReply reply)
    {
        reply = pingSender.Send(sendIp, timeout * 2, buffer, options);
        UpdateReplyInfo(reply);

        if (reply.Status == IPStatus.Success)
        {
            Debug.Log("Player ID " + myID + " has ping of: " + reply.RoundtripTime / 2);
        }

        if (pingFrequency > 10f && (reply.RoundtripTime / 2) > 100f)
            pingFrequency -= 10f;
        else if (pingFrequency < 60 && (reply.RoundtripTime / 2) < 100f)
            pingFrequency += 10f;

        yield return new WaitForSecondsRealtime(pingFrequency);

        StartCoroutine(PingGatherer(sendIp, timeout, buffer, pingSender, options, reply));
    }

    void UpdateReplyInfo(PingReply newReply)
    {
        reply = newReply;
    }
 
    void FixedUpdate() 
    {
        //Check input...
        if (upKey) {
            players[myID].transform.Translate(0, .1f, 0);
            UpdatePositions(myID);
        }
        if (downKey)
        {
            players[myID].transform.Translate(0, -.1f, 0);
            UpdatePositions(myID);

        }
        if (leftKey && !(myID == 1 && players[myID].transform.position.x < 1f))
        {
            players[myID].transform.Translate(-.1f, 0, 0);
            UpdatePositions(myID);

        }
        if (rightKey && !(myID == 0 && players[myID].transform.position.x > -1f))
        {
            players[myID].transform.Translate(.1f, 0, 0);
            UpdatePositions(myID);
        }
        if (space)
        {
            if (!shooting)
                StartCoroutine(Shooting());
            UpdatePositions(myID);
        }

        if (!upKey && !downKey && !leftKey && !rightKey && !chainEnd)
        {
            chainEnd = true;
            UpdatePositions(myID);
        }
        else if (upKey || downKey || leftKey || rightKey)
            chainEnd = false;

        //network stuff:
        CheckIncomingMessages();
    }

    public void Update()
    {
        //handling keyboard (in Update, because FixedUpdate isnt meant for that(!))
        if (Input.GetKeyDown("w")) upKey = true;       
        if (Input.GetKeyUp("w")) upKey = false;
        if (Input.GetKeyDown("s")) downKey = true;
        if (Input.GetKeyUp("s")) downKey = false;
        if (Input.GetKeyDown("a")) leftKey = true;
        if (Input.GetKeyUp("a")) leftKey = false;
        if (Input.GetKeyDown("d")) rightKey = true;
        if (Input.GetKeyUp("d")) rightKey = false;

        if (Input.GetKeyDown("space")) space = true;
        if (Input.GetKeyUp("space")) space = false;
    }

    void CheckIncomingMessages()
    {
        //Do the networkStuff:
        string[] o = connection.getMessages();
        if (o.Length > 0)
        {
            foreach (var json in o)
            {
                JsonUtility.FromJsonOverwrite(json, sendData);
                FixIncomingMessages(sendData);
            }
        }
    }

    IEnumerator Shooting()
    {
        shooting = true;
        Instantiate(players[sendData.id].myBullet, players[sendData.id].transform.GetChild(0).transform.position, Quaternion.identity);
        yield return new WaitForSecondsRealtime(1f);
        shooting = false;
    }

    void FixIncomingMessages(Sendable sendData)
    {
        if (sendData.shooting)
        {
            if (!shooting)
                StartCoroutine(Shooting());
        }

        players[sendData.id].previousPosition = players[sendData.id].transform.position;
        newRemotePlayerPosition = new Vector3(sendData.x, sendData.y, 0);

        lagDistance = players[sendData.id].previousPosition - newRemotePlayerPosition;
        // Lag distance can also be seen as current velocity and thus where we are headed next

        if (previousTimeStamp != null)
        {
            msSinceUpdate = sendData.timeStamp - previousTimeStamp;
            //Debug.Log("Previous time: " + previousTimeStamp + "\nNew time: " + sendData.timeStamp);
            //Debug.Log("Difference in ms was: " + msSinceUpdate);
        }

        if (msSinceUpdate > 0 && msSinceUpdate < 40 && (reply.RoundtripTime / 2) > 100 && !sendData.stoppedMoving) // Enough data to predict and noticable delay of lag -> predict movement
        {
            Debug.Log("Using extrapolation!");
            newRemotePlayerPosition.x += newRemotePlayerPosition.x - players[sendData.id].previousPosition.x;
            newRemotePlayerPosition.y += newRemotePlayerPosition.y - players[sendData.id].previousPosition.y;
        }
        else if (msSinceUpdate > 0 && msSinceUpdate < 40 && (reply.RoundtripTime / 2) > 100 && sendData.stoppedMoving) // rollback last extrapolation
        {
            Debug.Log("Roll back!");
            players[sendData.id].transform.DOMove(newRemotePlayerPosition, moveSpeed);
        }

        if (lagDistance.magnitude > 5f) // Distance too big, just put it on position
        {
            players[sendData.id].transform.DOMove(newRemotePlayerPosition, moveSpeed);
            lagDistance = Vector3.zero;
        }

        if (lagDistance.magnitude < moveSpeed) // Remote player is close to actual position, just leave him there
        {

        }
        else // Player is a bit desynced and can be fixed
        {
            players[sendData.id].transform.DOMove(newRemotePlayerPosition, moveSpeed);
        }

        previousTimeStamp = sendData.timeStamp;
    }

    public void UpdatePositions(int id)
    {
        //update sendData-object
        sendData.id = id;
        sendData.x = players[id].transform.position.x;
        sendData.y = players[id].transform.position.y;
        sendData.timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        sendData.stoppedMoving = chainEnd;
        sendData.shooting = space;

        previousXY = new Vector2(players[id].transform.position.x, players[id].transform.position.y);

        string json = JsonUtility.ToJson(sendData); //Convert to String
        //Debug.Log(json);
        
        connection.Send(json); //send the string
    }
 
    void OnDestroy() 
    {
        connection.Stop();
    }
}