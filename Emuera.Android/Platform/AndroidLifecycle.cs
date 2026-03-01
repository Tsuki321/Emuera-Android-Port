using Android.App;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

/// <summary>
/// Android implementation of IPlatformLifecycle.
/// Delegates to the current Activity to handle app exit and restart.
/// </summary>
public class AndroidLifecycle(Activity activity) : IPlatformLifecycle
{
    public void RequestExit()
    {
        activity.RunOnUiThread(() => activity.FinishAffinity());
    }

    public void RequestRestart()
    {
        // Restart by re-launching MainActivity
        activity.RunOnUiThread(() =>
        {
            var intent = activity.PackageManager!.GetLaunchIntentForPackage(activity.PackageName!)!;
            intent.AddFlags(global::Android.Content.ActivityFlags.ClearTop | global::Android.Content.ActivityFlags.NewTask);
            activity.StartActivity(intent);
            activity.Finish();
        });
    }
}
