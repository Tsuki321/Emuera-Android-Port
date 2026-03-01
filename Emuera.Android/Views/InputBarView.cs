using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using MinorShift.Emuera.GameView;

namespace Emuera.Android.Views;

/// <summary>
/// Bottom input bar for the ERA game console.
/// Contains an EditText for typed input, an OK submit button,
/// and quick number buttons (0–9) for ERA INPUT commands.
/// </summary>
public class InputBarView : LinearLayout
{
    private readonly EditText _editText;
    private readonly LinearLayout _numRow;
    private EmueraConsole? _console;

    public InputBarView(Context context) : base(context)
    {
        Orientation = global::Android.Widget.Orientation.Vertical;

        // ── Text input row ─────────────────────────────────────────────────
        var textRow = new LinearLayout(context)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal,
        };

        _editText = new EditText(context)
        {
            Hint = "Input…",
            InputType = global::Android.Text.InputTypes.ClassText,
            ImeOptions = ImeAction.Done,
        };
        var editParams = new LayoutParams(0, LayoutParams.WrapContent, 1f);
        textRow.AddView(_editText, editParams);

        var submitBtn = new Button(context) { Text = "OK" };
        textRow.AddView(submitBtn);

        AddView(textRow, new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent));

        // ── Number buttons row (0–9) ───────────────────────────────────────
        _numRow = new LinearLayout(context)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal,
        };

        for (int digit = 0; digit <= 9; digit++)
        {
            int d = digit; // local copy for clarity in the lambda
            var btn = new Button(context) { Text = d.ToString() };
            var btnParams = new LayoutParams(0, LayoutParams.WrapContent, 1f);
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
    }

    /// <summary>Attach the engine console so that submit calls reach the engine.</summary>
    internal void SetConsole(EmueraConsole console)
    {
        _console = console;
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
}
