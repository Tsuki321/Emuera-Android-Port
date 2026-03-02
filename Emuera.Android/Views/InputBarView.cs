using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using MinorShift.Emuera.GameView;

namespace Emuera.Android.Views;

/// <summary>
/// Bottom input bar for the ERA game console.
/// Contains an EditText for typed input, an OK submit button, a right-click
/// mode toggle button, and quick number buttons (0–9) for ERA INPUT commands.
/// </summary>
public class InputBarView : LinearLayout
{
    private readonly EditText _editText;
    private readonly LinearLayout _numRow;
    private readonly Button _rightClickBtn;
    private EmueraConsole? _console;

    /// <summary>
    /// Optional delegate invoked when the right-click toggle button is pressed.
    /// Should toggle right-click mode on the <see cref="GameSurfaceView"/> and
    /// return the new active state so the button can update its visual.
    /// </summary>
    public Func<bool>? ToggleRightClickModeFunc { get; set; }

    public InputBarView(Context context) : base(context)
    {
        Orientation = global::Android.Widget.Orientation.Vertical;
        SetBackgroundColor(Color.ParseColor("#1E0F36"));
        int pad = DpToPx(context, 6);
        SetPadding(pad, pad, pad, pad);

        // ── Text input row ─────────────────────────────────────────────────
        var textRow = new LinearLayout(context)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal,
        };
        int hSpacing = DpToPx(context, 6);
        textRow.SetPadding(0, 0, 0, hSpacing);

        _editText = new EditText(context)
        {
            Hint = "Type input…",
            InputType = global::Android.Text.InputTypes.ClassText,
            ImeOptions = ImeAction.Done,
        };
        _editText.SetHintTextColor(Color.ParseColor("#998AB5"));
        _editText.SetTextColor(Color.White);
        _editText.SetBackgroundResource(Resource.Drawable.bg_input);
        int fieldPadH = DpToPx(context, 14);
        int fieldPadV = DpToPx(context, 10);
        _editText.SetPadding(fieldPadH, fieldPadV, fieldPadH, fieldPadV);

        var editParams = new LayoutParams(0, LayoutParams.WrapContent, 1f);
        editParams.MarginEnd = DpToPx(context, 8);
        textRow.AddView(_editText, editParams);

        var submitBtn = new Button(context) { Text = "OK" };
        submitBtn.SetTextColor(Color.White);
        submitBtn.SetBackgroundResource(Resource.Drawable.bg_submit_btn);
        submitBtn.SetPadding(DpToPx(context, 20), 0, DpToPx(context, 20), 0);
        submitBtn.StateListAnimator = null;
        var submitParams = new LayoutParams(
            LayoutParams.WrapContent,
            DpToPx(context, 44));
        textRow.AddView(submitBtn, submitParams);

        // ── Right-click mode toggle button ─────────────────────────────────
        _rightClickBtn = new Button(context) { Text = "R" };
        _rightClickBtn.SetTextColor(Color.ParseColor("#CCB8EC"));
        _rightClickBtn.SetBackgroundResource(Resource.Drawable.bg_num_btn);
        _rightClickBtn.StateListAnimator = null;
        _rightClickBtn.SetTypeface(null, global::Android.Graphics.TypefaceStyle.Bold);
        var rClickParams = new LayoutParams(
            DpToPx(context, 44),
            DpToPx(context, 44));
        rClickParams.MarginStart = DpToPx(context, 6);
        textRow.AddView(_rightClickBtn, rClickParams);

        AddView(textRow, new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent));

        // ── Number buttons row (0–9) ───────────────────────────────────────
        _numRow = new LinearLayout(context)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal,
        };
        _numRow.SetPadding(0, hSpacing / 2, 0, 0);

        for (int digit = 0; digit <= 9; digit++)
        {
            int d = digit;
            var btn = new Button(context) { Text = d.ToString() };
            btn.SetTextColor(Color.ParseColor("#CCB8EC"));
            btn.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 13f);
            btn.SetBackgroundResource(Resource.Drawable.bg_num_btn);
            btn.StateListAnimator = null;

            var btnParams = new LayoutParams(0, DpToPx(context, 38), 1f);
            btnParams.SetMargins(2, 0, 2, 0);
            btn.Click += (_, _) => SubmitInput(d.ToString());
            _numRow.AddView(btn, btnParams);
        }

        AddView(_numRow, new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent));

        // ── Submit on OK button or IME action ─────────────────────────────
        submitBtn.Click += (_, _) => SubmitCurrentText();

        _editText.EditorAction += (_, args) =>
        {
            if (args.ActionId == ImeAction.Done)
            {
                SubmitCurrentText();
                args.Handled = true;
            }
        };

        // ── Right-click toggle ─────────────────────────────────────────────
        _rightClickBtn.Click += (_, _) =>
        {
            bool active = ToggleRightClickModeFunc?.Invoke() ?? false;
            UpdateRightClickButtonVisual(active);
        };
    }

    /// <summary>
    /// Updates the right-click toggle button's visual to reflect the current mode.
    /// Active (right-click mode on) is highlighted in orange; inactive is default.
    /// </summary>
    public void UpdateRightClickButtonVisual(bool active)
    {
        _rightClickBtn.SetTextColor(active
            ? Color.ParseColor("#FF8C00")   // orange when right-click mode is on
            : Color.ParseColor("#CCB8EC")); // default purple-ish
    }

    /// <summary>Attach the engine console so that submit calls reach the engine.</summary>
    internal void SetConsole(EmueraConsole console)
    {
        _console = console;
    }

    /// <summary>
    /// Called when the engine enters input-wait state.
    /// Focuses the text field and raises the soft keyboard so the user can type immediately.
    /// </summary>
    internal void RequestFocusForInput()
    {
        _editText.Post(() =>
        {
            _editText.RequestFocus();
            var imm = (InputMethodManager?)Context?.GetSystemService(global::Android.Content.Context.InputMethodService);
            imm?.ShowSoftInput(_editText, ShowFlags.Implicit);
        });
    }

    /// <summary>Submit the text currently in the EditText.</summary>
    private void SubmitCurrentText()
    {
        string val = _editText.Text ?? "";
        _editText.Text = "";
        SubmitInput(val);
    }

    /// <summary>Submit an input string directly to the engine console.</summary>
    private void SubmitInput(string value)
    {
        _console?.PressEnterKey(false, value, false);
    }

    private static int DpToPx(Context ctx, float dp) =>
        (int)(dp * ctx.Resources!.DisplayMetrics!.Density + 0.5f);
}

