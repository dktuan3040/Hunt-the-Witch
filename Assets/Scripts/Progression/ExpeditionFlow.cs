using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExpeditionFlow
{
    public const string House="Assets/Scenes/Maps/Trong Nhà.unity";
    public const string Home="Assets/Scenes/Maps/Home.unity";
    public const string FirstMap="Assets/Scenes/Maps/M_F1.unity";
    public static bool IsExpeditionActive {get;private set;}
    public static bool IsManagedScene {get;private set;}
    public static ExpeditionScene.SceneKind CurrentKind {get;private set;}
    public static bool CanFight => !IsManagedScene||(CurrentKind==ExpeditionScene.SceneKind.Combat&&IsExpeditionActive);
    public static bool IsLoading {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){IsExpeditionActive=false;IsManagedScene=false;IsLoading=false;CurrentKind=ExpeditionScene.SceneKind.House;}
    public static ExpeditionScene.SceneKind Kind(string path)
    {
        string name=System.IO.Path.GetFileNameWithoutExtension(path);
        if(name=="Trong Nhà")return ExpeditionScene.SceneKind.House;
        if(name=="Home")return ExpeditionScene.SceneKind.Hub;
        if(name=="MainMenu")return ExpeditionScene.SceneKind.Menu;
        return ExpeditionScene.SceneKind.Combat;
    }
    public static void EnterScene(ExpeditionScene config)
    {
        IsManagedScene=true;CurrentKind=config.kind;
        if(CurrentKind==ExpeditionScene.SceneKind.Combat&&!IsExpeditionActive)BeginRun();
        else if(CurrentKind!=ExpeditionScene.SceneKind.Combat&&IsExpeditionActive)AbandonRun();
    }
    public static void BeginRun()
    {
        PlayerSessionState.StartNewGame();IsExpeditionActive=true;
    }
    public static void AbandonRun()
    {
        IsExpeditionActive=false;PlayerSessionState.StartNewGame();
    }
    public static bool BankLoot()
    {
        if(!IsExpeditionActive)return true;
        if(!GameProfile.Deposit(PlayerSessionState.Inventory))return false;
        AbandonRun();return true;
    }
    public static bool Travel(string destination,Health actor=null,bool force=false)
    {
        if(IsLoading||!Application.CanStreamedLevelBeLoaded(destination))return false;
        if(!force&&(PauseMenuUI.InteractionInputBlocked||Time.timeScale<=0))return false;
        if(actor!=null&&actor.TryGetComponent<PlayerStatePersistence>(out var state))state.Capture();
        var next=Kind(destination);
        if(next==ExpeditionScene.SceneKind.Hub&&IsExpeditionActive){if(!BankLoot())return false;}
        else if(next==ExpeditionScene.SceneKind.House||next==ExpeditionScene.SceneKind.Menu)AbandonRun();
        else if(next==ExpeditionScene.SceneKind.Combat&&!IsExpeditionActive)BeginRun();
        IsLoading=true;GameplayPause.ResetForSceneChange();
        var load=SceneManager.LoadSceneAsync(destination,LoadSceneMode.Single);
        if(load==null){IsLoading=false;return false;}
        load.completed+=_=>IsLoading=false;
        return true;
    }
    public static void PlayFromMenu(){AbandonRun();Travel(House,null,true);}
    public static void PlayerDied(){if(IsManagedScene)AbandonRun();}
}
