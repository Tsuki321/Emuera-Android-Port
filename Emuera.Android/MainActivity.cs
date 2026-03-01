using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views.Animations;
using Android.Widget;
using Emuera.Android.Platform;

namespace Emuera.Android;

[Activity(Label = "Emuera", MainLauncher = true, Theme = "@style/AppTheme")]
public class MainActivity : Activity
{
    private const int RequestPickFolder = 1001;
    private const int RequestManageStorage = 1002;

    // SharedPreferences key for recent game paths
    private const string PrefFile   = "emuera_prefs";
    private const string PrefRecent = "recent_games";
    private const int MaxRecent = 5;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        CrashLogger.Initialize(this);

        SetContentView(Resource.Layout.activity_main);

        FindViewById<Button>(Resource.Id.btn_pick_game)!.Click += (_, _) => PickFolder();

        LoadRecentGames();
        StartEntryAnimations();

        // Request all-files access so the engine can read game files via System.IO.
        // This is required on Android 11+ for external storage paths.
        EnsureManageExternalStoragePermission();
    }

    // ── Entry animations ─────────────────────────────────────────────────────

    private void StartEntryAnimations()
    {
        var fadeIn      = AnimationUtils.LoadAnimation(this, Resource.Animation.fade_in)!;
        var slideUp     = AnimationUtils.LoadAnimation(this, Resource.Animation.slide_up_fade_in)!;
        var pulse       = AnimationUtils.LoadAnimation(this, Resource.Animation.pulse)!;

        var iconContainer = FindViewById(Resource.Id.icon_container)!;
        var tvTitle       = FindViewById(Resource.Id.tv_title)!;
        var tvSubtitle    = FindViewById(Resource.Id.tv_subtitle)!;
        var btnPick       = FindViewById(Resource.Id.btn_pick_game)!;
        var cardInfo      = FindViewById(Resource.Id.card_info)!;
        var sectionRecent = FindViewById(Resource.Id.section_recent)!;

        // Icon fades in immediately with a pulse glow
        iconContainer.Alpha = 1f;
        FindViewById(Resource.Id.icon_ring)!.StartAnimation(pulse);

        // Title fades in after 150 ms
        PostDelayed(tvTitle, fadeIn, 150);

        // Subtitle fades in after 300 ms
        PostDelayed(tvSubtitle, fadeIn, 300);

        // Button slides up after 450 ms
        PostDelayed(btnPick, slideUp, 450);

        // Info card slides up after 600 ms
        PostDelayed(cardInfo, slideUp, 600);

        // Recent section slides up after 750 ms
        PostDelayed(sectionRecent, slideUp, 750);
    }

    /// Start an animation on <paramref name="view"/> after <paramref name="delayMs"/> ms,
    /// and make the view visible when it begins.
    private static void PostDelayed(global::Android.Views.View view,
                                    Animation anim, long delayMs)
    {
        anim.StartOffset = delayMs;
        anim.AnimationStart += (_, _) => view.Alpha = 1f;
        view.StartAnimation(anim);
    }

    // ── All-files permission (Android 11+) ───────────────────────────────────

#pragma warning disable CA1416 // API-level guards are checked manually via Build.VERSION.SdkInt
    private void EnsureManageExternalStoragePermission()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.R) return;
        if (global::Android.OS.Environment.IsExternalStorageManager) return;

        // Explain and redirect the user to the system settings screen.
        new AlertDialog.Builder(this)!
            .SetTitle("File Access Required")!
            .SetMessage("Emuera needs access to all files on your device so it can read game folders from external storage. " +
                        "Please grant \"All files access\" on the next screen.")!
            .SetPositiveButton("Open Settings", (_, _) =>
            {
                try
                {
                    var intent = new Intent(global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    intent.SetData(global::Android.Net.Uri.Parse($"package:{PackageName}"));
                    StartActivityForResult(intent, RequestManageStorage);
                }
                catch
                {
                    // Fallback: open the general all-files-access list
                    StartActivityForResult(
                        new Intent(global::Android.Provider.Settings.ActionManageAllFilesAccessPermission),
                        RequestManageStorage);
                }
            })!
            .SetNegativeButton("Skip", (global::Android.Content.IDialogInterfaceOnClickListener?)null)!
            .Show();
    }
#pragma warning restore CA1416

    // ── Folder picker ────────────────────────────────────────────────────────

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
            ContentResolver!.TakePersistableUriPermission(
                data.Data,
                ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            string path = ResolveUriToPath(data.Data);

            AddRecentGame(path);

            var gameIntent = new Intent(this, typeof(GameActivity));
            gameIntent.PutExtra("GAME_ROOT", path);
            StartActivity(gameIntent);
        }
    }

    // ── Recent games ─────────────────────────────────────────────────────────

    private void LoadRecentGames()
    {
        var prefs   = GetSharedPreferences(PrefFile, FileCreationMode.Private)!;
        string raw  = prefs.GetString(PrefRecent, "") ?? "";
        var paths   = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var listView = FindViewById<ListView>(Resource.Id.list_recent_games)!;
        var adapter  = new RecentGamesAdapter(this, paths);
        listView.Adapter = adapter;

        listView.ItemClick += (_, e) =>
        {
            string path = paths[e.Position];
            var gameIntent = new Intent(this, typeof(GameActivity));
            gameIntent.PutExtra("GAME_ROOT", path);
            StartActivity(gameIntent);
        };

        // Hide the whole recent section if there are no recent games
        var section = FindViewById(Resource.Id.section_recent)!;
        if (paths.Length == 0)
            section.Visibility = global::Android.Views.ViewStates.Gone;
    }

    private void AddRecentGame(string path)
    {
        var prefs  = GetSharedPreferences(PrefFile, FileCreationMode.Private)!;
        string raw = prefs.GetString(PrefRecent, "") ?? "";

        var list = new System.Collections.Generic.List<string>(
            raw.Split('\n', StringSplitOptions.RemoveEmptyEntries));

        list.Remove(path);        // deduplicate
        list.Insert(0, path);     // most-recent first
        if (list.Count > MaxRecent)
            list.RemoveRange(MaxRecent, list.Count - MaxRecent);

        prefs.Edit()!
             .PutString(PrefRecent, string.Join('\n', list))!
             .Apply();
    }

    // ── URI → path helper ────────────────────────────────────────────────────

    /// <summary>
    /// Converts an <c>ACTION_OPEN_DOCUMENT_TREE</c> URI to a real file-system path that
    /// <c>System.IO</c> can use.  Handles the standard document-ID formats:
    /// <list type="bullet">
    ///   <item><c>primary:{relative}</c>  → <c>/storage/emulated/0/{relative}</c></item>
    ///   <item><c>{uuid}:{relative}</c>   → <c>/storage/{uuid}/{relative}</c> (SD cards)</item>
    /// </list>
    /// Falls back to the raw URI path when the format is unrecognised.
    /// </summary>
#pragma warning disable CA1416 // API-level guards are checked manually via Build.VERSION.SdkInt
    private string ResolveUriToPath(global::Android.Net.Uri uri)
    {
        string? docId = DocumentsContract.GetTreeDocumentId(uri);
        if (!string.IsNullOrEmpty(docId))
        {
            int colon = docId.IndexOf(':');
            if (colon > 0)
            {
                string volume       = docId[..colon];
                string relativePath = docId[(colon + 1)..];

                if (volume.Equals("primary", StringComparison.OrdinalIgnoreCase))
                    return $"/storage/emulated/0/{relativePath}";

                // External SD card or other named volumes (e.g. "1234-5678", "msd", "sdcard").
                // Try to resolve via StorageManager on API 24+; fall back to /storage/{volume}.
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N
                    && GetSystemService(global::Android.Content.Context.StorageService)
                        is global::Android.OS.Storage.StorageManager sm)
                {
                    foreach (var vol in sm.StorageVolumes)
                    {
                        // Match by UUID (external SD cards) or by the description-based id.
                        bool matches = vol.Uuid?.Equals(volume, StringComparison.OrdinalIgnoreCase) == true;
                        if (!matches)
                        {
                            // Fallback: check the volume's directory name
                            var dir = vol.Directory?.Name;
                            matches = dir?.Equals(volume, StringComparison.OrdinalIgnoreCase) == true;
                        }

                        if (matches && vol.Directory != null)
                            return Path.Combine(vol.Directory.AbsolutePath, relativePath);
                    }
                }

                // Generic /storage/{volume}/{relative} — works for UUID-style SD cards.
                return $"/storage/{volume}/{relativePath}";
            }
        }
        return uri.Path ?? uri.ToString()!;
    }
#pragma warning restore CA1416

    // ── Adapter ──────────────────────────────────────────────────────────────

    private sealed class RecentGamesAdapter : BaseAdapter<string>
    {
        private readonly Context _ctx;
        private readonly string[] _paths;

        public RecentGamesAdapter(Context ctx, string[] paths)
        {
            _ctx   = ctx;
            _paths = paths;
        }

        public override int Count => _paths.Length;
        public override string this[int position] => _paths[position];
        public override long GetItemId(int position) => position;

        public override global::Android.Views.View GetView(
            int position, global::Android.Views.View? convertView,
            global::Android.Views.ViewGroup parent)
        {
            var inf  = global::Android.Views.LayoutInflater.From(_ctx)!;
            var view = inf.Inflate(Resource.Layout.list_item_game, parent, false)!;

            string path = _paths[position];
            string name = System.IO.Path.GetFileName(path.TrimEnd('/')) is { Length: > 0 } n
                          ? n : path;

            view.FindViewById<TextView>(Resource.Id.tv_game_name)!.Text = name;
            view.FindViewById<TextView>(Resource.Id.tv_game_path)!.Text = path;
            return view;
        }
    }
}

