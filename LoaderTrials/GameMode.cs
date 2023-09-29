using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ConVar;
using RoR2.Navigation;
using RoR2.ExpansionManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace LoaderTrials
{
  public class GameMode
  {
    public static GameObject loaderRunPrefab;
    public static GameObject extraGameModeMenu;
    private static SurvivorDef loaderDef = Addressables.LoadAssetAsync<SurvivorDef>((object)"RoR2/Base/Loader/Loader.asset").WaitForCompletion();
    public static GameEndingDef loaderRunEnding = Addressables.LoadAssetAsync<GameEndingDef>((object)"RoR2/Base/WeeklyRun/PrismaticTrialEnding.asset").WaitForCompletion();
    public static GameObject loaderRunPortal = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>((object)"RoR2/Base/moon/MoonExitArenaOrb.prefab").WaitForCompletion(), "LoaderRunPortal", false);
    public static GameObject positionIndicator = Addressables.LoadAssetAsync<GameObject>((object)"RoR2/DLC1/VoidCamp/VoidCampPositionIndicator.prefab").WaitForCompletion();

    public GameMode()
    {
      loaderRunPortal.AddComponent<NetworkIdentity>();
      PrefabAPI.RegisterNetworkPrefab(loaderRunPortal);
      UnityEngine.Object.Destroy((UnityEngine.Object)loaderRunPortal.GetComponent<MapZone>());
      loaderRunPortal.AddComponent<PortalCollision>();
      loaderRunPrefab = PrefabAPI.InstantiateClone(new GameObject("xLoaderRun"), "xLoaderRun", false);
      loaderRunPrefab.AddComponent<NetworkIdentity>();
      PrefabAPI.RegisterNetworkPrefab(loaderRunPrefab);
      Run component = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClassicRun/ClassicRun.prefab").WaitForCompletion().GetComponent<Run>();
      LoaderRun loaderRun = loaderRunPrefab.AddComponent<LoaderRun>();
      loaderRun.nameToken = "Loader Trials";
      loaderRun.userPickable = true;
      loaderRun.startingSceneGroup = component.startingSceneGroup;
      loaderRun.gameOverPrefab = component.gameOverPrefab;
      loaderRun.lobbyBackgroundPrefab = component.lobbyBackgroundPrefab;
      loaderRun.uiPrefab = component.uiPrefab;
      loaderRunPrefab.AddComponent<TeamManager>();
      loaderRunPrefab.AddComponent<RunCameraManager>();
      ContentAddition.AddGameMode(loaderRunPrefab);
      On.RoR2.UI.LanguageTextMeshController.Start += LanguageTextMeshController_Start;
      On.RoR2.GameModeCatalog.SetGameModes += GameModeCatalog_SetGameModes;
      On.RoR2.Run.OverrideRuleChoices += Run_OverrideRuleChoices;
      On.RoR2.CharacterSelectBarController.ShouldDisplaySurvivor += CharacterSelectBarController_ShouldDisplaySurvivor;
      On.RoR2.CharacterSelectBarController.GetLocalUserExistingSurvivorPreference += CharacterSelectBarController_GetLocalUserExistingSurvivorPreference;
      On.RoR2.UI.ObjectivePanelController.DestroyTimeCrystals.GenerateString += DestroyTimeCrystals_GenerateString;
      On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
      On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
    }

    private void GameModeCatalog_SetGameModes(
      On.RoR2.GameModeCatalog.orig_SetGameModes orig,
      Run[] newGameModePrefabComponents)
    {
      Array.Sort<Run>(newGameModePrefabComponents, (Comparison<Run>)((a, b) => string.CompareOrdinal(a.name, b.name)));
      orig.Invoke(newGameModePrefabComponents);
    }

    private void Run_OverrideRuleChoices(
      On.RoR2.Run.orig_OverrideRuleChoices orig,
      Run self,
      RuleChoiceMask mustInclude,
      RuleChoiceMask mustExclude,
      ulong runSeed)
    {
      orig.Invoke(self, mustInclude, mustExclude, runSeed);
      if (!(bool)(UnityEngine.Object)PreGameController.instance || PreGameController.instance.gameModeIndex != GameModeCatalog.FindGameModeIndex("xLoaderRun"))
        return;
      self.ForceChoice(mustInclude, mustExclude, "Difficulty.Hard");
      RuleChoiceDef choice1 = RuleCatalog.FindRuleDef("Misc.StageOrder")?.FindChoice("Random");
      if (choice1 != null)
        self.ForceChoice(mustInclude, mustExclude, choice1);
      ItemIndex itemIndex = ~ItemIndex.None;
      for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; ++itemIndex)
      {
        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
        RuleChoiceDef choice2 = RuleCatalog.FindRuleDef("Items." + itemDef.name)?.FindChoice((UnityEngine.Object)itemDef.requiredExpansion == (UnityEngine.Object)null ? "On" : "Off");
        if (choice2 != null)
          self.ForceChoice(mustInclude, mustExclude, choice2);
      }
      EquipmentIndex equipmentIndex = ~EquipmentIndex.None;
      for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; ++equipmentIndex)
      {
        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
        RuleChoiceDef choice3 = RuleCatalog.FindRuleDef("Equipment." + equipmentDef.name)?.FindChoice((UnityEngine.Object)equipmentDef.requiredExpansion == (UnityEngine.Object)null ? "On" : "Off");
        if (choice3 != null)
          self.ForceChoice(mustInclude, mustExclude, choice3);
      }
      foreach (UnityEngine.Object expansionDef in ExpansionCatalog.expansionDefs)
      {
        RuleChoiceDef choice4 = RuleCatalog.FindRuleDef("Expansions." + expansionDef.name)?.FindChoice("Off");
        if (choice4 != null)
          self.ForceChoice(mustInclude, mustExclude, choice4);
      }
      foreach (ArtifactDef artifactDef in ArtifactCatalog.artifactDefs)
      {
        RuleChoiceDef choice5 = RuleCatalog.FindRuleDef("Artifacts." + artifactDef.cachedName)?.FindChoice("Off");
        if (choice5 != null)
          self.ForceChoice(mustInclude, mustExclude, choice5);
      }
    }

    private bool CharacterSelectBarController_ShouldDisplaySurvivor(
      On.RoR2.CharacterSelectBarController.orig_ShouldDisplaySurvivor orig,
      CharacterSelectBarController self,
      SurvivorDef survivorDef)
    {
      return PreGameController.GameModeConVar.instance.GetString() == "xLoaderRun" ? survivorDef.cachedName == "Loader" : orig.Invoke(self, survivorDef);
    }

    private SurvivorDef CharacterSelectBarController_GetLocalUserExistingSurvivorPreference(
      On.RoR2.CharacterSelectBarController.orig_GetLocalUserExistingSurvivorPreference orig,
      CharacterSelectBarController self)
    {
      return PreGameController.GameModeConVar.instance.GetString() == "xLoaderRun" ? loaderDef : orig.Invoke(self);
    }

    private void LanguageTextMeshController_Start(
      On.RoR2.UI.LanguageTextMeshController.orig_Start orig,
      LanguageTextMeshController self)
    {
      orig.Invoke(self);
      if (!(self.token == "TITLE_ECLIPSE") || !(bool)(UnityEngine.Object)self.GetComponent<HGButton>())
        return;
      self.transform.parent.gameObject.AddComponent<LoaderRunButtonAdder>();
    }

    private string DestroyTimeCrystals_GenerateString(
      On.RoR2.UI.ObjectivePanelController.DestroyTimeCrystals.orig_GenerateString orig,
      ObjectivePanelController.ObjectiveTracker self)
    {
      if (Run.instance.gameModeIndex != GameModeCatalog.FindGameModeIndex("xLoaderRun"))
        return orig.Invoke(self);
      LoaderRun component = Run.instance.gameObject.GetComponent<LoaderRun>();
      string[] strArray = new string[3]
      {
        "Enter <color=#30e7ff>Time Portals</color> ({0}/{1})",
        "Kill <color=#30e7ff>{0}</color> Monsters",
        "Enter <color=#30e7ff>Time Portals</color> ({0}/{1}) <color=#30e7ff>WITHOUT TOUCHING THE GROUND</color>"
      };
      string str = "something went wrong :(";
      string trial = component.trial;
      if (!(trial == "portal"))
      {
        if (!(trial == "punch"))
        {
          if (trial == "lava")
            str = string.Format(strArray[2], (object)component.portalsPassed, (object)component.portalsRequiredToPass);
        }
        else
          str = string.Format(strArray[1], (object)component.enemyCount);
      }
      else
        str = string.Format(strArray[0], (object)component.portalsPassed, (object)component.portalsRequiredToPass);
      return str;
    }

    private void CharacterMaster_OnBodyStart(
      On.RoR2.CharacterMaster.orig_OnBodyStart orig,
      CharacterMaster self,
      CharacterBody body)
    {
      orig.Invoke(self, body);
      if (!NetworkServer.active || Run.instance.gameModeIndex != GameModeCatalog.FindGameModeIndex("xLoaderRun"))
        return;
      LoaderRun component = Run.instance.gameObject.GetComponent<LoaderRun>();
      ItemIndex itemIndex = ItemCatalog.itemNameToIndex["LunarDagger"];
      int itemCount = body.inventory.GetItemCount(itemIndex);
      if (itemCount > 0)
        body.inventory.RemoveItem(itemIndex, itemCount);
      if (body.isPlayerControlled && component.trial == "punch")
        body.inventory.GiveItemString("LunarDagger", 10);
      if (!body.isPlayerControlled || !(component.trial == "lava"))
        return;
      body.gameObject.AddComponent<LavaController>();
      body.inventory.GiveItemString("AlienHead", 20);
      body.inventory.GiveItemString("SecondarySkillMagazine", 5);
    }

    private void CharacterMaster_OnBodyDeath(
      On.RoR2.CharacterMaster.orig_OnBodyDeath orig,
      CharacterMaster self,
      CharacterBody body)
    {
      orig.Invoke(self, body);
      if (!NetworkServer.active || Run.instance.gameModeIndex != GameModeCatalog.FindGameModeIndex("xLoaderRun") || !(bool)(UnityEngine.Object)body.teamComponent || body.teamComponent.teamIndex != TeamIndex.Monster)
        return;
      LoaderRun component = Run.instance.gameObject.GetComponent<LoaderRun>();
      component.enemyCount = GetLiveMonsters();
      if (component.enemyCount != 0)
        return;
      if (Run.instance.stageClearCount == 2)
      {
        Run.instance.BeginGameOver(loaderRunEnding);
      }
      else
      {
        Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
        Run.instance.AdvanceStage(Run.instance.nextStageScene);
      }
    }

    public static int GetLiveMonsters()
    {
      TeamIndex teamIndex = TeamIndex.Monster;
      int liveMonsters = 0;
      foreach (CharacterMaster characterMaster in UnityEngine.Object.FindObjectsOfType<CharacterMaster>())
      {
        if (characterMaster.teamIndex == teamIndex)
        {
          CharacterBody body = characterMaster.GetBody();
          if ((bool)(UnityEngine.Object)body && (bool)(UnityEngine.Object)body.healthComponent && body.healthComponent.alive)
            ++liveMonsters;
        }
      }
      return liveMonsters;
    }
    public class LoaderRunButton : MonoBehaviour
    {
      public HGButton hgButton;

      public void Start()
      {
        this.hgButton = this.GetComponent<HGButton>();
        this.hgButton.onClick = new Button.ButtonClickedEvent();
        this.hgButton.onClick.AddListener((UnityAction)(() =>
        {
          int num = (int)Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
          RoR2.Console.instance.SubmitCmd((NetworkUser)null, "transition_command \"gamemode xLoaderRun; host 0; \"");
        }));
      }
    }

    public class LoaderRunButtonAdder : MonoBehaviour
    {
      public void Start()
      {
        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.transform.Find("GenericMenuButton (Eclipse)").gameObject, this.transform);
        gameObject.AddComponent<LoaderRunButton>();
        gameObject.GetComponent<LanguageTextMeshController>().token = "Loader Trials";
        gameObject.GetComponent<HGButton>().hoverToken = "Play a gamemode that tests your abilities as The Loader.";
      }
    }

    public class LavaController : MonoBehaviour
    {
      private CharacterBody body;
      private float delay = 5f;
      private float stopwatch;

      private void Start() => this.body = this.gameObject.GetComponent<CharacterBody>();

      private void FixedUpdate()
      {
        this.stopwatch += Time.deltaTime;
        if ((double)this.stopwatch < (double)this.delay || !this.body.characterMotor.isGrounded)
          return;
        this.body.healthComponent.TakeDamage(new DamageInfo()
        {
          damage = 5f,
          rejected = false
        });
      }
    }

    public class PortalCollision : MonoBehaviour
    {
      public GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>((object)"RoR2/Base/moon/MoonExitArenaOrbEffect.prefab").WaitForCompletion();

      private void OnTriggerEnter(Collider other)
      {
        CharacterBody component1 = other.GetComponent<CharacterBody>();
        if (!(bool)(UnityEngine.Object)component1 && component1.name != "LoaderBody(Clone)")
          return;
        EffectManager.SpawnEffect(this.effectPrefab, new EffectData()
        {
          origin = this.transform.position
        }, false);
        UnityEngine.Object.Destroy((UnityEngine.Object)this.gameObject);
        LoaderRun component2 = Run.instance.gameObject.GetComponent<LoaderRun>();
        if ((int)component2.portalsPassed != (int)component2.portalsRequiredToPass - 1)
          return;
        if (Run.instance.stageClearCount == 2)
        {
          Run.instance.BeginGameOver(loaderRunEnding);
        }
        else
        {
          Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
          Run.instance.AdvanceStage(Run.instance.nextStageScene);
        }
      }
    }

    public class LoaderRun : Run
    {
      public SpawnCard portalSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();
      public GameObject portalPrefab = loaderRunPortal;
      public string trial;
      private List<string> trialList = new List<string>()
    {
      "portal",
      "punch",
      "lava"
    };
      private bool activatedIndicators;
      public uint portalCount = 6;
      public uint portalsRequiredToPass = 6;
      private List<OnDestroyCallback> portalActiveList = new List<OnDestroyCallback>();
      public int enemyCount;

      public uint portalsPassed => (uint)((ulong)this.portalCount - (ulong)this.portalActiveList.Count);

      public override void Start()
      {
        SceneDirector.onPrePopulateSceneServer += new Action<SceneDirector>(this.onPrePopulateSceneServer);
        SceneDirector.onPostPopulateSceneServer += new Action<SceneDirector>(this.onPostPopulateSceneServer);
        Reflection.GetFieldValue<BoolConVar>(typeof(CombatDirector), "cvDirectorCombatDisable").SetBool(true);
        base.Start();
        this.CreateSpawnCard();
        ObjectivePanelController.collectObjectiveSources += new Action<CharacterMaster, List<ObjectivePanelController.ObjectiveSourceDescriptor>>(this.ReportObjective);
      }

      public override void FixedUpdate()
      {
        base.FixedUpdate();
        if (this.activatedIndicators)
          return;
        this.activatedIndicators = true;
        this.AddIndicators();
        if (!(this.trial == "portal") && !(this.trial == "lava"))
          return;
        this.AddPortalIndicators();
      }

      public override void OnDestroy()
      {
        base.OnDestroy();
        SceneDirector.onPrePopulateSceneServer -= new Action<SceneDirector>(this.onPrePopulateSceneServer);
        SceneDirector.onPostPopulateSceneServer -= new Action<SceneDirector>(this.onPostPopulateSceneServer);
        ObjectivePanelController.collectObjectiveSources -= new Action<CharacterMaster, List<ObjectivePanelController.ObjectiveSourceDescriptor>>(this.ReportObjective);
        Reflection.GetFieldValue<BoolConVar>(typeof(CombatDirector), "cvDirectorCombatDisable").SetBool(false);
      }

      private void CreateSpawnCard()
      {
        this.portalSpawnCard.prefab = this.portalPrefab;
        this.portalSpawnCard.hullSize = HullClassification.Human;
        this.portalSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Air;
        this.portalSpawnCard.requiredFlags = NodeFlags.None;
        this.portalSpawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;
      }

      private void AddPortalIndicators()
      {
        List<GameObject> gameObjectList = new List<GameObject>();
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
          if (gameObject.name == "LoaderRunPortal(Clone)")
            gameObjectList.Add(gameObject);
        }
        foreach (GameObject gameObject in gameObjectList)
          UnityEngine.Object.Instantiate<GameObject>(positionIndicator, gameObject.transform).GetComponent<PositionIndicator>().targetTransform = gameObject.transform;
      }

      private void AddIndicators()
      {
        TeamIndex teamIndex = TeamIndex.Monster;
        foreach (TeamComponent teamComponent in UnityEngine.Object.FindObjectsOfType<TeamComponent>())
        {
          if (teamComponent.teamIndex == teamIndex)
            teamComponent.RequestDefaultIndicator(positionIndicator);
        }
      }

      public void onPrePopulateSceneServer(SceneDirector director)
      {
        this.activatedIndicators = false;
        director.teleporterSpawnCard = (SpawnCard)null;
        director.interactableCredit = 0;
        this.trial = this.trialList.ElementAt<string>(Run.instance.stageClearCount);
        string trial = this.trial;
        if (!(trial == "portal"))
        {
          if (!(trial == "punch"))
          {
            if (!(trial == "lava"))
              return;
            director.monsterCredit = 0;
            DirectorPlacementRule placementRule = new DirectorPlacementRule();
            placementRule.placementMode = DirectorPlacementRule.PlacementMode.Random;
            for (int index = 0; (long)index < (long)this.portalCount; ++index)
              this.portalActiveList.Add(OnDestroyCallback.AddCallback(DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(this.portalSpawnCard, placementRule, this.stageRng)), (Action<OnDestroyCallback>)(component => this.portalActiveList.Remove(component))));
          }
          else
          {
            Reflection.GetFieldValue<BoolConVar>(typeof(CombatDirector), "cvDirectorCombatDisable").SetBool(false);
            director.monsterCredit = 100;
          }
        }
        else
        {
          director.monsterCredit = 0;
          DirectorPlacementRule placementRule = new DirectorPlacementRule();
          placementRule.placementMode = DirectorPlacementRule.PlacementMode.Random;
          for (int index = 0; (long)index < (long)this.portalCount; ++index)
            this.portalActiveList.Add(OnDestroyCallback.AddCallback(DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(this.portalSpawnCard, placementRule, this.stageRng)), (Action<OnDestroyCallback>)(component => this.portalActiveList.Remove(component))));
        }
      }

      public void onPostPopulateSceneServer(SceneDirector director)
      {
        if (!(this.trial == "punch"))
          return;
        Reflection.GetFieldValue<BoolConVar>(typeof(CombatDirector), "cvDirectorCombatDisable").SetBool(true);
        this.enemyCount = GetLiveMonsters();
      }

      public void ReportObjective(
        CharacterMaster master,
        List<ObjectivePanelController.ObjectiveSourceDescriptor> output)
      {
        string trial = this.trial;
        if (!(trial == "portal"))
        {
          if (!(trial == "punch"))
          {
            if (!(trial == "lava") || (int)this.portalsPassed == (int)this.portalCount)
              return;
            output.Add(new ObjectivePanelController.ObjectiveSourceDescriptor()
            {
              source = (UnityEngine.Object)this,
              master = master,
              objectiveType = typeof(ObjectivePanelController.DestroyTimeCrystals)
            });
          }
          else
            output.Add(new ObjectivePanelController.ObjectiveSourceDescriptor()
            {
              source = (UnityEngine.Object)this,
              master = master,
              objectiveType = typeof(ObjectivePanelController.DestroyTimeCrystals)
            });
        }
        else
        {
          if ((int)this.portalsPassed == (int)this.portalCount)
            return;
          output.Add(new ObjectivePanelController.ObjectiveSourceDescriptor()
          {
            source = (UnityEngine.Object)this,
            master = master,
            objectiveType = typeof(ObjectivePanelController.DestroyTimeCrystals)
          });
        }
      }
    }
  }
}
