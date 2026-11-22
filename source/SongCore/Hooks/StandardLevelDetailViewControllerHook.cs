using System;
using System.Reflection;
using BGLib.Polyglot;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This displays an error message to the user in the <see cref="StandardLevelDetailView"/> when the game fails to load beatmap levels.
    /// </summary>
    internal class StandardLevelDetailViewControllerHook : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;

        private ILHook _showLoadingAndDoSomethingHook = null!;

        private StandardLevelDetailViewControllerHook(StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
        }

        public void Initialize()
        {
            _showLoadingAndDoSomethingHook = new ILHook(typeof(StandardLevelDetailViewController).GetMethod(nameof(StandardLevelDetailViewController.ShowLoadingAndDoSomething), BindingFlags.Instance | BindingFlags.NonPublic)!.GetStateMachineTarget()!, ctx => {
            {
                var cursor = new ILCursor(ctx);
                cursor.GotoNext(MoveType.After, i => i.MatchCall(out var method) && method.Name == nameof(UnityEngine.Debug.LogException));
                cursor.EmitDelegate(ShowError);
            }}, true);
        }

        public void Dispose()
        {
            _showLoadingAndDoSomethingHook.Dispose();
        }

        private void ShowError()
        {
            _standardLevelDetailViewController.ShowContent(StandardLevelDetailViewController.ContentType.Error, Localization.Get("ERROR_LOADING_DATA"));
        }
    }
}
