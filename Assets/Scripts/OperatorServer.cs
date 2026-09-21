using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using System.Threading;
using System.Text;
using System;
using System.Collections.Generic;

/// <summary>
/// WEB APP OPERATORE — controllo completo del gioco da telefono/tablet.
/// Il bambino non usa i controller: l'operatore guida ogni passo dalla pagina web,
/// guardando cosa vede il bambino tramite il casting Meta (app Meta Horizon).
///
/// Si avvia DA SOLO all'apertura del gioco e resta attivo in tutte le scene
/// (sostituisce il vecchio TabletServer, che si disattiva automaticamente).
///
/// Uso: dal telefono sulla stessa WiFi apri  http://IP-DEL-VISORE:8080
/// La pagina mostra i comandi della scena corrente (si aggiorna da sola).
/// </summary>
public class OperatorServer : MonoBehaviour
{
    public static OperatorServer Instance { get; private set; }

    const int Port = 8080;

    HttpListener m_Listener;
    Thread       m_Thread;
    volatile bool m_Running;
    volatile string m_SceneName = "";

    readonly Queue<string> m_Commands = new Queue<string>();
    readonly object m_Lock = new object();

    // Parte da solo, in qualunque scena si avvii il gioco
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("OperatorServer").AddComponent<OperatorServer>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartServer();
    }

    void Update()
    {
        m_SceneName = SceneManager.GetActiveScene().name;

        lock (m_Lock)
        {
            while (m_Commands.Count > 0)
                Execute(m_Commands.Dequeue());
        }
    }

    // ── Esecuzione comandi (main thread) ──────────────
    void Execute(string cmd)
    {
        Debug.Log("[Operatore] comando: " + cmd);
        string[] parts = cmd.Split('|');
        string action = parts[0];
        string arg    = parts.Length > 1 ? parts[1] : "";

        switch (action)
        {
            // Scena 1: scelta personaggio
            case "character":
                foreach (var p in FindObjectsByType<CharacterPickable>(FindObjectsSortMode.None))
                    if (p.Role == arg) p.SelectRemotely();
                break;
            case "startmission":
                FindAnyObjectByType<WaitingRoomManager>()?.OnStartMission();
                break;

            // Scena 2: posizionamento
            case "next":
                FindAnyObjectByType<PositioningGuide>()?.Proceed();
                break;

            // Scena 3: scelta pianeta
            case "planet":
                if (int.TryParse(arg, out int idx))
                    FindAnyObjectByType<PlanetMenu>()?.Select(idx);
                break;
            case "accept":
                FindAnyObjectByType<PlanetMenu>()?.StartMission();
                break;

            // Scena 4: missione
            case "breathing": FindAnyObjectByType<MissionController>()?.StartBreathing();     break;
            case "fuel":      FindAnyObjectByType<MissionController>()?.StartFueling();       break;
            case "fuelfull":  FindAnyObjectByType<MissionController>()?.CompleteFueling();    break;
            case "launch":    FindAnyObjectByType<MissionController>()?.StartRocketLaunch();  break;
        }
    }

    // ── Server HTTP ───────────────────────────────────
    void StartServer()
    {
        try
        {
            m_Listener = new HttpListener();
            m_Listener.Prefixes.Add($"http://*:{Port}/");
            m_Listener.Start();
            m_Running = true;
            m_Thread = new Thread(Loop) { IsBackground = true };
            m_Thread.Start();
            Debug.Log($"[Operatore] server avviato sulla porta {Port}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Operatore] server non avviato: " + e.Message);
        }
    }

    void Loop()
    {
        while (m_Running)
        {
            try { Handle(m_Listener.GetContext()); }
            catch (Exception e) { if (m_Running) Debug.LogWarning("[Operatore] " + e.Message); }
        }
    }

    void Handle(HttpListenerContext ctx)
    {
        string path  = ctx.Request.Url.AbsolutePath.TrimEnd('/');
        string query = ctx.Request.Url.Query; // es. ?v=Astronauta
        string arg   = query.StartsWith("?v=") ? Uri.UnescapeDataString(query.Substring(3)) : "";
        string body;
        string type = "text/html";

        switch (path)
        {
            case "":
            case "/":       body = Html();            break;
            case "/state":  body = m_SceneName; type = "text/plain"; break;
            default:
                // /character /startmission /next /planet /accept /breathing /fuel /fuelfull /launch
                lock (m_Lock) m_Commands.Enqueue(path.TrimStart('/') + "|" + arg);
                body = "OK"; type = "text/plain";
                break;
        }

        byte[] buf = Encoding.UTF8.GetBytes(body);
        ctx.Response.ContentType = type + "; charset=utf-8";
        ctx.Response.ContentLength64 = buf.Length;
        ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        ctx.Response.OutputStream.Close();
    }

    string Html()
    {
        return @"<!DOCTYPE html><html><head>
<meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Controllo Missione</title>
<style>
 body{margin:0;padding:16px;background:#0a0a2e;font-family:-apple-system,Arial,sans-serif;color:#fff}
 h1{font-size:20px;text-align:center;margin:6px 0 14px}
 .sec{background:#141450;border-radius:14px;padding:12px;margin-bottom:12px;opacity:.45}
 .sec.on{opacity:1;outline:2px solid #00e0d1}
 .sec h2{font-size:15px;margin:0 0 8px;color:#00e0d1}
 button{width:100%;padding:16px;font-size:17px;font-weight:bold;border:0;border-radius:10px;
        color:#fff;background:#1a6fa8;margin:4px 0;cursor:pointer}
 button:active{transform:scale(.98)}
 .row{display:flex;gap:8px}.row button{flex:1}
 .go{background:#1a8a4b}.warn{background:#b8791a}.hot{background:#a83232}
 #st{color:#8ab;text-align:center;font-size:13px}
</style></head><body>
<h1>&#128640; Controllo Missione</h1>
<p id='st'>collegamento...</p>

<div class='sec' id='s-WaitingRoom'><h2>1 &middot; Personaggio</h2>
 <div class='row'>
  <button onclick=""go('/character?v=Astronauta')"">&#128104;&#8205;&#128640; Astronauta</button>
  <button onclick=""go('/character?v=Scienziato')"">&#129514; Scienziato</button>
 </div>
 <button class='go' onclick=""go('/startmission')"">&#9654; Inizia missione</button>
</div>

<div class='sec' id='s-Posizionamento'><h2>2 &middot; Posizionamento</h2>
 <button class='go' onclick=""go('/next')"">&#9989; Bambino seduto &mdash; Avanti</button>
</div>

<div class='sec' id='s-SceltaPianeta'><h2>3 &middot; Pianeta</h2>
 <div class='row'>
  <button onclick=""go('/planet?v=0')"">&#128308; Marte</button>
  <button onclick=""go('/planet?v=1')"">&#129680; Saturno</button>
  <button onclick=""go('/planet?v=2')"">&#127761; Luna</button>
 </div>
 <button class='go' onclick=""go('/accept')"">&#9654; Accetta missione</button>
</div>

<div class='sec' id='s-LaunchMission'><h2>4 &middot; Missione (prelievo)</h2>
 <button onclick=""go('/breathing')"">&#127744; Respiro</button>
 <button class='warn' onclick=""go('/fuel')"">&#128738; Inizia rifornimento (inizio prelievo)</button>
 <button class='go' onclick=""go('/fuelfull')"">&#9989; Serbatoio pieno (fine prelievo)</button>
 <button class='hot' onclick=""go('/launch')"">&#128640; LANCIA</button>
</div>

<script>
function go(p){fetch(p).catch(()=>{});}
function poll(){fetch('/state').then(r=>r.text()).then(s=>{
  document.getElementById('st').textContent='Scena attuale: '+s;
  document.querySelectorAll('.sec').forEach(e=>e.classList.remove('on'));
  var el=document.getElementById('s-'+s); if(el)el.classList.add('on');
}).catch(()=>{document.getElementById('st').textContent='non collegato';});}
setInterval(poll,1500);poll();
</script></body></html>";
    }

    void OnDestroy()
    {
        m_Running = false;
        try { m_Listener?.Stop(); }  catch { }
        try { m_Listener?.Close(); } catch { }
    }
}
