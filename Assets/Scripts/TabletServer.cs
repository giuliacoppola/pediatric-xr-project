using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;

public class TabletServer : MonoBehaviour
{
    [Header("Impostazioni")]
    public int port = 8080;
    public MissionController missionController;

    private HttpListener httpListener;
    private Thread listenerThread;
    private bool isRunning = false;

    // Coda comandi thread-safe
    private System.Collections.Generic.Queue<string> commandQueue 
        = new System.Collections.Generic.Queue<string>();

    void Start()
    {
        // Se c'e il nuovo OperatorServer (web app completa), questo vecchio server
        // si disattiva da solo per evitare conflitti sulla stessa porta.
        if (FindAnyObjectByType<OperatorServer>() != null)
        {
            enabled = false;
            return;
        }
        StartServer();
    }

    void Update()
    {
        // Processa comandi dal thread HTTP nel main thread Unity
        while (commandQueue.Count > 0)
        {
            string command = commandQueue.Dequeue();
            ProcessCommand(command);
        }
    }

    void StartServer()
    {
        httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://*:{port}/");
        httpListener.Start();
        isRunning = true;

        listenerThread = new Thread(ListenLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        Debug.Log($"Tablet server avviato sulla porta {port}");
    }

    void ListenLoop()
    {
        while (isRunning)
        {
            try
            {
                HttpListenerContext context = httpListener.GetContext();
                HandleRequest(context);
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError("Server error: " + e.Message);
            }
        }
    }

    void HandleRequest(HttpListenerContext context)
    {
        string path = context.Request.Url.AbsolutePath;
        string response = "";

        if (path == "/")
        {
            // Serve la pagina HTML del tablet
            response = GetTabletHTML();
            context.Response.ContentType = "text/html";
        }
        else if (path == "/breathing")
        {
            commandQueue.Enqueue("BREATHING");
            response = "OK";
        }
        else if (path == "/fuel")
        {
            commandQueue.Enqueue("FUEL");
            response = "OK";
        }
        else if (path == "/fuelfull")
        {
            commandQueue.Enqueue("FUELFULL");
            response = "OK";
        }
        else if (path == "/launch")
        {
            commandQueue.Enqueue("LAUNCH");
            response = "OK";
        }

        byte[] buffer = Encoding.UTF8.GetBytes(response);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    void ProcessCommand(string command)
    {
        Debug.Log("Comando ricevuto: " + command);

        switch (command)
        {
            case "BREATHING":
                missionController.StartBreathing();
                break;
            case "FUEL":
                missionController.StartFueling();
                break;
            case "FUELFULL":
                missionController.CompleteFueling();
                break;
            case "LAUNCH":
                missionController.StartRocketLaunch();
                break;
        }
    }

    string GetTabletHTML()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Missione Spaziale</title>
    <style>
        body {
            margin: 0;
            padding: 20px;
            background: #0a0a2e;
            font-family: Arial, sans-serif;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 20px;
        }
        h1 { color: white; font-size: 24px; text-align: center; }
        button {
            width: 100%;
            max-width: 400px;
            padding: 40px 20px;
            font-size: 24px;
            font-weight: bold;
            border: none;
            border-radius: 16px;
            cursor: pointer;
            color: white;
        }
        #btn-breathing { background: #1a6fa8; }
        #btn-fuel { background: #b8791a; }
        #btn-fuelfull { background: #1a8a4b; }
        #btn-launch { background: #a83232; }
        .step { color:#8ab; font-size:13px; margin: 4px 0 -8px; }
        .status { color: #aaa; font-size: 14px; }
    </style>
</head>
<body>
    <h1>🚀 Pannello Controllo</h1>
    <button id='btn-breathing' onclick='send(""/breathing"")'>1 · 🌀 RESPIRO</button>
    <p class='step'>Prima dell'ago: calma il bambino</p>
    <button id='btn-fuel' onclick='send(""/fuel"")'>2 · 🛢️ INIZIA RIFORNIMENTO</button>
    <p class='step'>Quando inizia il prelievo: la barra sale piano</p>
    <button id='btn-fuelfull' onclick='send(""/fuelfull"")'>3 · ⛽ SERBATOIO PIENO</button>
    <p class='step'>A prelievo finito: riempie il serbatoio</p>
    <button id='btn-launch' onclick='send(""/launch"")'>4 · 🚀 LANCIA</button>
    <p class='status' id='status'>In attesa...</p>
    <script>
        function send(path) {
            fetch(path)
                .then(() => {
                    document.getElementById('status').textContent = 
                        'Comando inviato: ' + path;
                })
                .catch(err => {
                    document.getElementById('status').textContent = 
                        'Errore: ' + err;
                });
        }
    </script>
</body>
</html>";
    }

    void OnDestroy()
    {
        // Niente Thread.Abort(): su Quest (IL2CPP) non e supportato e lancia eccezione.
        // Il thread e IsBackground: fermando il listener, GetContext esce e il loop termina.
        isRunning = false;
        try { httpListener?.Stop(); }   catch { }
        try { httpListener?.Close(); }  catch { }
    }
}