using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.Object;

/// <summary>Explicit isolated retreat/UI regression. No full battle or supporter branch is executed.</summary>
public sealed class RetreatSelectionProbe : IDisposable
{
    private IDisposable scope;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Click(SpriteObjectToggleButton button) => typeof(SpriteObjectToggleButton)
        .GetMethod("ToggleObjects", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, null);
    private static GameObject Panel(SpriteObjectToggleButton button) => ((GameObject[])typeof(SpriteObjectToggleButton)
        .GetField("objectsToEnable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(button))[0];

    public IEnumerator Run()
    {
        scope = RunSaveService.UseRepositoryForVerification(new RunSaveRepository(
            System.IO.Path.Combine(RoleFeedbackVerification.Output, "isolated-saves"), RunSaveBindings.Instance));
        yield return new WaitForSecondsRealtime(3);
        var access = BattleAccessManager.Instance;
        var actors = new[] { access.FrontOperator, access.MiddleOperator, access.BackOperator };
        CardSelectionManagerBase[] managers = { FrontCardSelectionManager.Instance, MiddleCardSelectionManager.Instance, BackCardSelectionManager.Instance };
        var buttons = FindObjectsByType<SpriteObjectToggleButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var cards = FindObjectsByType<Card>(FindObjectsSortMode.InstanceID).Where(card => card.isActiveAndEnabled).ToArray();
        Require(cards.Length==6, "Six cards were not dealt.");
        for (int i=0;i<6;i++) cards[i].SetNumber(i+1);
        var report = new List<string>();
        for (int slot=0;slot<3;slot++)
        {
            Fill();
            Require(access.CanBattle, "Full live party not approved before retreat.");
            var button=buttons.First(item => (int)item.Position==slot);
            Click(button);
            Require(Panel(button).activeSelf && CardPositionManager.Instance.CurrentSelectionManager==managers[slot], "Panel did not select its manager.");
            var released=managers[slot].FeedbackCards.Where(card => card!=null).ToArray();
            actors[slot].TakePhysicalDamage(int.MaxValue);
            Require(actors[slot].IsRetreated && !managers[slot].SelectionEnabled && !button.SlotAvailable &&
                !Panel(button).activeSelf && !button.GetComponent<Collider2D>().enabled, "Retreat did not hide/disable the slot.");
            Require(released.All(card => card.OwnerManager==null) && managers[slot].FeedbackCards.All(card => card==null), "Retreated slot kept card ownership.");
            Require(CardPositionManager.Instance.CurrentSelectionManager==null, "Card routing still points at a disabled manager.");
            Click(button); managers[slot].SelectCard(released[0]);
            Require(!Panel(button).activeSelf && released[0].OwnerManager==null, "Disabled slot accepted panel/card input.");
            Require(access.CanBattle, "Empty retreated slot still blocks battle approval.");
            int other=(slot+1)%3;
            managers[other].ResetSelection(); access.Refresh();
            Require(!access.CanBattle, "Missing cards on an active slot were ignored.");
            Require(actors[slot].Revive(actors[slot].MaxHP) && managers[slot].SelectionEnabled && button.SlotAvailable,
                "Revive did not re-enable the slot.");
            Require(!access.CanBattle, "Revived party approved without new cards.");
            Click(button); Require(Panel(button).activeSelf, "Revived panel cannot reopen.");
            report.Add("PASS: "+actors[slot].Position+" retreat closes/disables panel, frees cards, waives only its own requirements, and revive restores input");
            yield return null;
        }
        Fill();
        actors[0].TakePhysicalDamage(int.MaxValue);
        Require(actors[0].ScheduleRedeployment(1) && actors[0].ProcessRedeploymentTurn() &&
            managers[0].SelectionEnabled && buttons.First(button => button.Position==PositionType.Front).SlotAvailable,
            "Scheduled redeployment did not restore the selector.");
        foreach (var actor in actors) if (actor.IsCombatActive) actor.TakePhysicalDamage(int.MaxValue);
        access.Refresh(); Require(!access.CanBattle, "All-retreated party can start battle.");
        var library=FindFirstObjectByType<BattleFeedbackDirector>().library;
        Require(Mathf.Abs(library.maxActionGap/library.basePlaybackSpeed-.4f)<.01f, "New action gap is not 0.4 seconds at 1x.");
        report.Add("PASS: redeployment re-enables slot; all-retreated party rejected; 1x action gap is 0.4s");
        RoleFeedbackVerification.Finish(string.Join("\n",report)+"\nLethal damage/UI fixtures only; full combat and supporter conversion not run.");

        void Fill()
        {
            foreach (var actor in actors) if (actor.IsRetreated) actor.Revive(actor.MaxHP);
            foreach (var manager in managers) manager.ResetSelection();
            FrontCardSelectionManager.Instance.SetDefense(); FrontCardSelectionManager.Instance.SelectCard(cards[0]);
            MiddleCardSelectionManager.Instance.SetPhysicalMode();
            for (int i=1;i<=3;i++) MiddleCardSelectionManager.Instance.SelectCard(cards[i]);
            BackCardSelectionManager.Instance.SelectCard(cards[4]); BackCardSelectionManager.Instance.SelectCard(cards[5]);
            var targeting=EnemyTargetingManager.Instance;
            targeting.BeginTargeting(actors[1]);
            if (targeting.GetSelectedTarget(actors[1])==null)
                foreach (var target in FindObjectsByType<EnemyTargetable>(FindObjectsSortMode.InstanceID)) {
                    targeting.SelectTarget(target);
                    if (targeting.GetSelectedTarget(actors[1])!=null) break;
                }
            targeting.EndTargeting();
            access.Refresh();
        }
    }

    public void Dispose() { scope?.Dispose(); scope=null; }
}
