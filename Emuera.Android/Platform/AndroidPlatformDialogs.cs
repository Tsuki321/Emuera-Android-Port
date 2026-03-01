using Android.App;
using Android.Content;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

/// <summary>
/// Android implementation of IPlatformDialogs using AlertDialog.
/// </summary>
public class AndroidPlatformDialogs(Activity activity) : IPlatformDialogs
{
    public Task<bool> ShowYesNoAsync(string message, string title)
    {
        var tcs = new TaskCompletionSource<bool>();
        activity.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(activity)!
                .SetTitle(title)!
                .SetMessage(message)!
                .SetPositiveButton("Yes", (_, _) => tcs.TrySetResult(true))!
                .SetNegativeButton("No",  (_, _) => tcs.TrySetResult(false))!
                .SetOnCancelListener(new CancelAction(() => tcs.TrySetResult(false)))!
                .Show();
        });
        return tcs.Task;
    }

    public Task ShowInfoAsync(string message, string title)
    {
        var tcs = new TaskCompletionSource();
        activity.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(activity)!
                .SetTitle(title)!
                .SetMessage(message)!
                .SetPositiveButton("OK", (_, _) => tcs.TrySetResult())!
                .SetOnCancelListener(new CancelAction(() => tcs.TrySetResult()))!
                .Show();
        });
        return tcs.Task;
    }

    /// <summary>Helper to satisfy IDialogInterfaceOnCancelListener with a lambda.</summary>
    private sealed class CancelAction(Action onCancel) : Java.Lang.Object, IDialogInterfaceOnCancelListener
    {
        public void OnCancel(IDialogInterface? dialog) => onCancel();
    }
}
