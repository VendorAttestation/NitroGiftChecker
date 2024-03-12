using Spectre.Console;
using System.Net;
using System.Text;
using System.Text.Json;

internal static class Program
{
    public class NitroJson
    {
        public string message { get; set; }
        public int code { get; set; }
    }

    public static Settings? appSettings;
    private static Random random = new Random();
    private static HashSet<string> UserAgents = new HashSet<string>();
    internal static string[] Proxies = File.ReadAllLines("proxies.txt");
    internal static int ProxyIndex = 0;
    private static object ValidLock = new object();
    private static object DebugLock = new object();
    private static string date = DateTime.Now.ToString("MM-dd-yyyy");
    static WebProxy GetProxy()
    {
        try
        {
            string proxyAddress;

            lock (Proxies)
            {
                Random random = new Random();

                if (ProxyIndex >= Proxies.Length)
                {
                    ProxyIndex = 0;
                }

                proxyAddress = Proxies[ProxyIndex++];
            }

            // Create a WebProxy object from the proxy address
            if (proxyAddress.Split(':').Length == 4)
            {
                var proxyHost = proxyAddress.Split(':')[0];
                int proxyPort = Int32.Parse(proxyAddress.Split(':')[1]);
                var username = proxyAddress.Split(':')[2];
                var password = proxyAddress.Split(':')[3];
                ICredentials credentials = new NetworkCredential(username, password);
                var proxyUri = new Uri($"http://{proxyHost}:{proxyPort}");
                return new WebProxy(proxyUri, false, null, credentials);
            }
            else if (proxyAddress.Split(':').Length == 2)
            {
                var proxyHost = proxyAddress.Split(':')[0];
                int proxyPort = Int32.Parse(proxyAddress.Split(':')[1]);
                return new WebProxy(proxyHost, proxyPort);
            }
            else
            {
                throw new ArgumentException("Invalid proxy format.");
            }
        }
        catch (Exception)
        {
            throw new ArgumentException("Invalid proxy.");
        }
    }
    static string GetRandomUserAgent()
    {
        if (UserAgents.Count == 0)
        {
            throw new InvalidOperationException("UserAgents set is empty. Please add user agent strings before calling GetRandomUserAgent.");
        }

        Random random = new Random();
        int index = random.Next(0, UserAgents.Count);
        int i = 0;
        foreach (var userAgent in UserAgents)
        {
            if (i == index)
            {
                return userAgent;
            }
            i++;
        }

        // This should never happen
        throw new Exception("Failed to get random user agent.");
    }

    static async Task Main(string[] args)
    {
        appSettings = new("config.ini");

        if (appSettings.Threads > 500 || appSettings.Threads < 1)
        {
            AnsiConsole.Write(new Markup($"[red]You may only use between 1-500 threads.[/]"));
        }

        using (var httpClient = new HttpClient())
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                HttpResponseMessage response = await httpClient.GetAsync("https://cdn.jsdelivr.net/gh/microlinkhq/top-user-agents@master/src/desktop.json");
                response.EnsureSuccessStatusCode();
                var desktopUA = await response.Content.ReadAsStringAsync();
                List<string> jsonObject = JsonSerializer.Deserialize<List<string>>(desktopUA);
                foreach (var item in jsonObject)
                {
                    UserAgents.Add(item);
                }

                HttpResponseMessage response2 = await httpClient.GetAsync("https://cdn.jsdelivr.net/gh/microlinkhq/top-user-agents@master/src/mobile.json");
                response2.EnsureSuccessStatusCode();
                List<string> jsonObject2 = JsonSerializer.Deserialize<List<string>>(await response2.Content.ReadAsStringAsync());
                foreach (var item in jsonObject2)
                {
                    UserAgents.Add(item);
                }
            } 
            catch (HttpRequestException e)
            {
                if (appSettings.Debug)
                {
                    lock (DebugLock)
                    {
                        File.AppendAllText($"DebugLogs-{date}.txt", e.Message);
                    }
                }
            }
        }

                var tasks = new Task[appSettings.Threads];
        for (int i = 0; i < appSettings.Threads; i++)
        {
            tasks[i] = Task.Run(() => CheckNitro());
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        AnsiConsole.Write(new Markup($"[yellow]Checker Completed List\n[/]"));
        Console.ReadLine();
    }
    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            builder.Append(chars[random.Next(chars.Length)]);
        }

        return builder.ToString();
    }

    static async Task CheckNitro()
    {
        while (true)
        {

            Console.Title = $"Nitro Checker V1 / User_Agents Loaded: {UserAgents.Count}";

            var code = GenerateRandomString(16);
            string url = $"https://discord.com/api/v{random.Next(6, 11)}/entitlements/gift-codes/{code}";
            using (var httpClient = new HttpClient(new HttpClientHandler { Proxy = GetProxy(), UseProxy = true }))
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", GetRandomUserAgent());
                    HttpResponseMessage response = await httpClient.GetAsync(url);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    NitroJson jsonObject = JsonSerializer.Deserialize<NitroJson>(responseBody);

                    if (jsonObject.message == "Unknown Gift Code")
                    {
                        AnsiConsole.Markup($"[red]Invalid Nitro Code: {code}[/]\n");
                    }
                    else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        AnsiConsole.Markup($"[yellow]Ratelimited Use Better Proxies Or Simply Use Rotating Proxies[/]\n");
                    }
                    else
                    {
                        AnsiConsole.Markup($"[green]Valid Nitro Code: {code}[/]\n");
                        lock (ValidLock)
                        {
                            File.AppendAllText($"ValidNitros-{date}.txt", $"{code}\n");
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    if (appSettings.Debug)
                    {
                        lock (DebugLock)
                        {
                            File.AppendAllText($"DebugLogs-{date}.txt", e.Message);
                        }
                    }
                }
            }
        }
    }
}