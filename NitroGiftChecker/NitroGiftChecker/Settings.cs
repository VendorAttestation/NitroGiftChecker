public class Settings
{
    /* SAVED CONFIG VALUES */
    public SharpConfig.Configuration config;
    public int Threads;
    /* END SAVED CONFIG VALUES */

    public Settings(string file)
    {
        SharpConfig.Configuration config = SharpConfig.Configuration.LoadFromFile(file);
        Threads = config["App"]["Threads"].IntValue;
    }
}