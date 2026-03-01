using Android.App;
using Android.Content;
using Android.OS;
using Android.Views.Animations;
using Android.Widget;
using Emuera.Android.Platform;

namespace Emuera.Android;

[Activity(Label = "Emuera", MainLauncher = true, Theme = "@style/AppTheme")]
public class MainActivity : Activity
{
    private const int RequestPickFolder = 1001;

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

    private static string ResolveUriToPath(global::Android.Net.Uri uri)
    {
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
        return uri.Path ?? uri.ToString()!;
    }

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

