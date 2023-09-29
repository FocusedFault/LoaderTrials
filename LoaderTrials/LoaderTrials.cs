using BepInEx;

namespace LoaderTrials
{
  [BepInPlugin("com.Nuxlar.LoaderTrials", "LoaderTrials", "1.0.1")]

  public class LoaderTrials : BaseUnityPlugin
  {
    public void Awake()
    {
      new GameMode();
    }
  }
}