using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class ASTRONEER : SteamCMDAgent // SteamCMDAgent is used because ASTRONEER relies on SteamCMD for installation and update process
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.ASTRONEER", // WindowsGSM.XXXX
            author = "1stian",
            description = "🧩 WindowsGSM plugin for supporting Astroneer Dedicated Server",
            version = "1.0",
            url = "https://github.com/1stian/WindowsGSM.ASTRONEER", // Github repository link (Best practice)
            color = "#9eff99" // Color Hex
        };


        // - Standard Constructor and properties
        public ASTRONEER(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData; // Store server start metadata, such as start ip, port, start param, etc


        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true; // ASTRONEER requires to login steam account to install the server, so loginAnonymous = false
        public override string AppId => "728470"; // Game server appId, ASTRONEER is 728470


        // - Game server Fixed variables
        public override string StartPath => "Astro\\Binaries\\Win64\\AstroServer-Win64-Shipping.exe"; // Game server start path, for ASTRONEER, it is arma3server.exe
        public string FullName = "Astroneer Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = false;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "7777"; // Default port
        public string QueryPort = "7777"; // Default query port
        public string Defaultmap = "empty"; // Default map name
        public string Maxplayers = "4"; // Default maxplayers
        public string Additional = "-log"; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG() 
        {
            //Checking for saved directory that contains INI files.
            string workingDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);
            if (!Directory.Exists(workingDir + "\\Astro\\Saved"))
            {
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved");
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved\\Config");
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved\\Config\\WindowsServer");
            }

            string AstroServerSettings = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\AstroServerSettings.ini");
            if (await Functions.Github.DownloadGameServerConfig(AstroServerSettings, FullName))
            {
                string configText = File.ReadAllText(AstroServerSettings);
                configText = configText.Replace("{{serverip}}", _serverData.ServerIP);
                configText = configText.Replace("{{ServerName}}", _serverData.ServerName);
                configText = configText.Replace("{{console_pw}}", _serverData.GetRCONPassword());
                File.WriteAllText(AstroServerSettings, configText);
            }

            string Engine = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\Engine.ini");
            if (await Functions.Github.DownloadGameServerConfig(Engine, FullName))
            {
                string configText = File.ReadAllText(Engine);
                configText = configText.Replace("{{port}}", _serverData.ServerPort);
                File.WriteAllText(Engine, configText);
            }
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            // Prepare start parameter
            var param = new StringBuilder();

            QueryPort = Port;

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param.ToString()
                },
                EnableRaisingEvents = true
            };

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                base.Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p) => await Task.Run(() => { p.Kill(); }); // I believe ASTRONEER don't have a proper way to stop the server so just kill it
    }
}
