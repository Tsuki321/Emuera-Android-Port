using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace Emuera.Android;

[Activity(Label = "Emuera", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int RequestPickFolder = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        FindViewById<Button>(Resource.Id.btn_pick_game)!.Click += (_, _) => PickFolder();
    }

    private void PickFolder()
    {
        var intent = new Intent(Intent.ActionOpenDocumentTree);
        StartActivityForResult(intent, RequestPickFolder);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == RequestPickFolder && resultCode == Result.Ok && data?.Data != null)
        {
            // Persist access permission across reboots
            ContentResolver!.TakePersistableUriPermission(
                data.Data,
                ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            string path = ResolveUriToPath(data.Data);
            var gameIntent = new Intent(this, typeof(GameActivity));
            gameIntent.PutExtra("GAME_ROOT", path);
            StartActivity(gameIntent);
        }
    }

    /// <summary>
    /// Converts a document-tree URI to a file-system path where possible.
    /// For primary external storage the path can be resolved; SAF URIs that
    /// do not map to a real path fall back to the URI string itself.
    /// </summary>
    private static string ResolveUriToPath(global::Android.Net.Uri uri)
    {
        // SAF document-tree URIs encode the path in the last path segment.
        // e.g. content://com.android.externalstorage.documents/tree/primary%3AMyGame
        // → /storage/emulated/0/MyGame
        string? docId = global::Android.Provider.DocumentsContract.GetTreeDocumentId(uri);
        if (!string.IsNullOrEmpty(docId))
        {
            string[] parts = docId.Split(':', 2);
            if (parts.Length == 2)
            {
                string volume = parts[0];
                string relativePath = parts[1];
                if (volume.Equals("primary", StringComparison.OrdinalIgnoreCase))
                    return $"/storage/emulated/0/{relativePath}";
            }
        }
        // Fallback: use the raw URI path
        return uri.Path ?? uri.ToString()!;
    }
}
