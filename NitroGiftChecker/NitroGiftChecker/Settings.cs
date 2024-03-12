public class Settings
{
    /* SAVED CONFIG VALUES */
    public SharpConfig.Configuration config;
    public int Threads;
    public bool Debug;
    public bool AutoRedeem;
    public string Token;
    /* END SAVED CONFIG VALUES */

    public Settings(string file)
    {
        SharpConfig.Configuration config = SharpConfig.Configuration.LoadFromFile(file);
        Threads = config["App"]["Threads"].IntValue;
        Debug = config["App"]["Debug"].BoolValue;
        AutoRedeem = config["App"]["AutoRedeem"].BoolValue;
        Token = config["App"]["Token"].StringValue;
    }
}